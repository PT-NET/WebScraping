using WebScraping.Domain.Interfaces;
using WebScraping.Infrastructure.Configuration;
using WebScraping.Infrastructure.Services;
using WebScraping.Infrastructure.Services.Scraping;

namespace WebScraping.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            
            services.Configure<ScrapingSettings>(
                configuration.GetSection("ScrapingSettings"));
            services.Configure<RateLimitSettings>(
                configuration.GetSection("RateLimitSettings"));

            
            services.AddHttpClient<OfacApiScrapingService>()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "RiskScreeningAPI/1.0 (Educational Purpose)");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

            services.AddHttpClient<OfacDirectScrapingService>()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.Timeout = TimeSpan.FromSeconds(60);
                });

            services.AddHttpClient<OffshoreLeaksScrapingService>()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "RiskScreeningAPI/1.0 (Educational Purpose)");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

            services.AddHttpClient<WorldBankScrapingService>()
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

            
            services.AddTransient<OfacApiScrapingService>();
            services.AddTransient<OfacDirectScrapingService>();

            
            services.AddTransient<IScrapingService, HybridOfacScrapingService>();

            
            services.AddTransient<IScrapingService, OffshoreLeaksScrapingService>();
            services.AddTransient<IScrapingService, WorldBankScrapingService>();

            
            services.AddScoped<IScreeningService, ScreeningService>();

            // Rate limiting
            services.AddMemoryCache();
            services.AddSingleton<IRateLimitService, RateLimitService>();

            return services;
        }
    }
}