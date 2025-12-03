using Microsoft.Extensions.Options;
using PuppeteerSharp;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Exceptions;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    public class OfacDirectScrapingService : BaseScrapingService
    {
        private static readonly SemaphoreSlim BrowserSemaphore = new(1, 1);
        private static IBrowser? _sharedBrowser;

        public override ScreeningSource Source => ScreeningSource.OFAC;

        public OfacDirectScrapingService(
            ILogger<OfacDirectScrapingService> logger,
            IOptions<ScrapingSettings> settings,
            HttpClient httpClient)
            : base(logger, settings, httpClient)
        {
        }

        public override async Task<List<ScreeningHit>> ScrapeAsync(
            EntityName entityName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Scraping OFAC website directly (Puppeteer) for entity: {EntityName}", entityName.Value);

                var hits = new List<ScreeningHit>();
                var browser = await GetBrowserAsync();

                await using var page = await browser.NewPageAsync();

                try
                {
                    Logger.LogDebug("Navigating to OFAC sanctions search page...");

                    await page.GoToAsync("https://sanctionssearch.ofac.treas.gov/",
                        new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                            Timeout = Settings.TimeoutSeconds * 1000
                        });

                    Logger.LogDebug("Page loaded, looking for search input...");

                    await page.WaitForSelectorAsync("input[id$='txtLastName']",
                        new WaitForSelectorOptions { Timeout = 10000 });

                    await page.TypeAsync("input[id$='txtLastName']", entityName.Value);

                    Logger.LogDebug("Search term entered, submitting form...");

                    await Task.WhenAll(
                        page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }, Timeout = 20000 }),
                        page.ClickAsync("input[id$='btnSearch']")
                    );

                    Logger.LogDebug("Form submitted, waiting for results...");

                    await Task.Delay(2000);

                    var results = await page.EvaluateFunctionAsync<List<OfacResultDto>>(@"
                    () => {
                        const results = [];
                        
                        const resultTable = document.querySelector('table[id*=""gvSearchResults""]');
                        
                        if (!resultTable) {
                            console.log('No result table found');
                            return results;
                        }
                        
                        const rows = resultTable.querySelectorAll('tr');
                        console.log('Found rows:', rows.length);
                        
                        for (let i = 1; i < rows.length && results.length < 20; i++) {
                            const cells = rows[i].querySelectorAll('td');
                            
                            if (cells.length >= 3) {
                                const name = cells[0]?.textContent?.trim() || 'N/A';
                                const address = cells[1]?.textContent?.trim() || 'N/A';
                                const type = cells[2]?.textContent?.trim() || 'N/A';
                                const programs = cells.length > 3 ? cells[3]?.textContent?.trim() || 'N/A' : 'N/A';
                                const list = cells.length > 4 ? cells[4]?.textContent?.trim() || 'SDN' : 'SDN';
                                
                                if (name && name !== 'N/A' && !name.includes('Download') && !name.includes('Search')) {
                                    results.push({
                                        name: name,
                                        address: address,
                                        type: type,
                                        programs: programs,
                                        list: list,
                                        score: 100
                                    });
                                }
                            }
                        }
                        
                        return results;
                    }
                ");

                    foreach (var result in results)
                    {
                        hits.Add(ScreeningHit.CreateOfacHit(
                            name: result.Name,
                            address: result.Address,
                            type: result.Type,
                            programs: result.Programs,
                            list: result.List,
                            score: result.Score
                        ));
                    }

                    Logger.LogInformation("Found {Count} hits in OFAC (Direct Scraping) for {EntityName}", hits.Count, entityName.Value);
                }
                finally
                {
                    await page.CloseAsync();
                }

                return hits;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in direct scraping of OFAC for entity: {EntityName}", entityName.Value);
                throw new ScrapingException(Source, ex.Message, ex);
            }
        }

        private async Task<IBrowser> GetBrowserAsync()
        {
            await BrowserSemaphore.WaitAsync();
            try
            {
                if (_sharedBrowser == null || !_sharedBrowser.IsConnected)
                {
                    Logger.LogInformation("Downloading/launching Chromium browser for scraping...");

                    var browserFetcher = new BrowserFetcher();
                    await browserFetcher.DownloadAsync();

                    _sharedBrowser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = Settings.UseHeadlessBrowser,
                        Args = new[]
                        {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    },
                        DefaultViewport = new ViewPortOptions
                        {
                            Width = 1920,
                            Height = 1080
                        }
                    });

                    Logger.LogInformation("Chromium browser launched successfully");
                }

                return _sharedBrowser;
            }
            finally
            {
                BrowserSemaphore.Release();
            }
        }

        private class OfacResultDto
        {
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Programs { get; set; } = string.Empty;
            public string List { get; set; } = string.Empty;
            public double Score { get; set; }
        }
    }
}
