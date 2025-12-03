using MediatR;
using WebScraping.Application.DTOs;
using WebScraping.Domain.Enums;

namespace WebScraping.Application.Commands.ScreenEntity
{
    public record ScreenEntityCommand : IRequest<ScreeningResponseDto>
    {
        public string EntityName { get; init; } = string.Empty;
        public List<ScreeningSource> Sources { get; init; } = new();
    }
}
