using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System.Text.Json;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Exceptions;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    public class OfacScrapingService : BaseScrapingService
    {
        private static readonly SemaphoreSlim BrowserSemaphore = new(1, 1);
        private static IBrowser? _sharedBrowser;

        public override ScreeningSource Source => ScreeningSource.OFAC;

        public OfacScrapingService(
            ILogger<OfacScrapingService> logger,
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
                Logger.LogInformation("Scraping OFAC website for entity: {EntityName}", entityName.Value);

                var hits = new List<ScreeningHit>();
                var browser = await GetBrowserAsync();

                await using var page = await browser.NewPageAsync();

                try
                {
                    await page.GoToAsync("https://sanctionssearch.ofac.treas.gov/",
                        new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });

                    Logger.LogDebug("OFAC page loaded, searching for: {EntityName}", entityName.Value);

                    await page.WaitForSelectorAsync("input[name='ctl00$MainContent$txtLastName']",
                        new WaitForSelectorOptions { Timeout = 10000 });
                    
                    await page.TypeAsync("input[name='ctl00$MainContent$txtLastName']", entityName.Value);

                    await page.ClickAsync("input[name='ctl00$MainContent$btnSearch']");

                    try
                    {
                        await page.WaitForSelectorAsync("#gvSearchResults, #MainContent_lblFoundRecords",
                            new WaitForSelectorOptions { Timeout = 15000 });
                    }
                    catch (WaitTaskTimeoutException)
                    {
                        Logger.LogWarning("No results found or timeout for: {EntityName}", entityName.Value);
                        return hits;
                    }

                    var results = await page.EvaluateFunctionAsync<List<OfacResultDto>>(@"
                    () => {
                        const results = [];
                        const rows = document.querySelectorAll('#gvSearchResults tr');
                        
                        for (let i = 1; i < rows.length; i++) {
                            const cells = rows[i].querySelectorAll('td');
                            if (cells.length >= 4) {
                                results.push({
                                    name: cells[0]?.textContent?.trim() || 'N/A',
                                    address: cells[1]?.textContent?.trim() || 'N/A',
                                    type: cells[2]?.textContent?.trim() || 'N/A',
                                    programs: cells[3]?.textContent?.trim() || 'N/A',
                                    list: cells[4]?.textContent?.trim() || 'SDN',
                                    score: 100
                                });
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

                    Logger.LogInformation("Found {Count} hits in OFAC for {EntityName}", hits.Count, entityName.Value);
                }
                finally
                {
                    await page.CloseAsync();
                }

                return hits;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error scraping OFAC for entity: {EntityName}", entityName.Value);
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
                    Logger.LogInformation("Downloading/launching Chromium browser...");

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
                    }
                    });

                    Logger.LogInformation("Browser launched successfully");
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