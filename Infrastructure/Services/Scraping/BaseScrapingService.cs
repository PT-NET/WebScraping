using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Interfaces;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    public abstract class BaseScrapingService : IScrapingService
    {
        protected readonly ILogger Logger;
        protected readonly ScrapingSettings Settings;
        protected readonly HttpClient HttpClient;
        protected readonly AsyncRetryPolicy RetryPolicy;

        public abstract ScreeningSource Source { get; }

        protected BaseScrapingService(
            ILogger logger,
            IOptions<ScrapingSettings> settings,
            HttpClient httpClient)
        {
            Logger = logger;
            Settings = settings.Value;
            HttpClient = httpClient;

            RetryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    Settings.MaxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(Settings.RetryDelayMs * retryAttempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Logger.LogWarning(
                            "Retry {RetryCount} for {Source} after {Delay}ms due to: {Error}",
                            retryCount,
                            Source,
                            timeSpan.TotalMilliseconds,
                            exception.Message);
                    });
        }

        public abstract Task<List<ScreeningHit>> ScrapeAsync(
            EntityName entityName,
            CancellationToken cancellationToken = default);

        protected async Task<HtmlDocument> LoadHtmlDocumentAsync(string url, CancellationToken cancellationToken)
        {
            var html = await RetryPolicy.ExecuteAsync(async () =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(Settings.TimeoutSeconds));

                var response = await HttpClient.GetAsync(url, cts.Token);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(cts.Token);
            });

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

        protected string GetNodeText(HtmlNode? node, string defaultValue = "N/A")
        {
            return node?.InnerText.Trim() ?? defaultValue;
        }
    }
}
