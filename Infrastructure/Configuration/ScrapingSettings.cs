namespace WebScraping.Infrastructure.Configuration
{
    public enum ScrapingMode
    {
        API,              // Uses OpenSanctions API
        DirectScraping,   // Uses Puppeteer for HTML scraping
        Hybrid            // Tries DirectScraping, if it fails then use API
    }

    public class ScrapingSettings
    {
        public ScrapingMode ScrapingMode { get; set; } = ScrapingMode.API;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public bool UseHeadlessBrowser { get; set; } = true;
    }

    public class RateLimitSettings
    {
        public int MaxCallsPerMinute { get; set; } = 20;
        public int WindowSizeSeconds { get; set; } = 60;
    }
}
