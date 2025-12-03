using WebScraping.Domain.Enums;

namespace WebScraping.Domain.Exceptions
{
    public class ScrapingException : Exception
    {
        public ScreeningSource Source { get; }

        public ScrapingException(ScreeningSource source, string message, Exception? innerException = null)
            : base($"Error scraping {source}: {message}", innerException)
        {
            Source = source;
        }
    }
}
