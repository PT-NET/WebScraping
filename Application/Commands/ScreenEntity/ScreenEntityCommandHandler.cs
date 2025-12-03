using AutoMapper;
using MediatR;
using System.Diagnostics;
using WebScraping.Application.DTOs;
using WebScraping.Domain.Interfaces;
using WebScraping.Domain.ValueObjects;
namespace WebScraping.Application.Commands.ScreenEntity
{
    public class ScreenEntityCommandHandler : IRequestHandler<ScreenEntityCommand, ScreeningResponseDto>
    {
        private readonly IScreeningService _screeningService;
        private readonly IMapper _mapper;
        private readonly ILogger<ScreenEntityCommandHandler> _logger;

        public ScreenEntityCommandHandler(
            IScreeningService screeningService,
            IMapper mapper,
            ILogger<ScreenEntityCommandHandler> logger)
        {
            _screeningService = screeningService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ScreeningResponseDto> Handle(
            ScreenEntityCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing screening request for entity: {EntityName} with sources: {Sources}",
                request.EntityName,
                string.Join(", ", request.Sources));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var entityName = EntityName.Create(request.EntityName);

                var result = await _screeningService.ScreenEntityAsync(
                    entityName,
                    request.Sources,
                    cancellationToken);

                stopwatch.Stop();

                var response = new ScreeningResponseDto
                {
                    SearchedEntity = result.SearchedEntity.Value,
                    TotalHits = result.TotalHits,
                    Hits = _mapper.Map<List<HitDto>>(result.Hits),
                    SearchedAt = result.SearchedAt,
                    ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                    Errors = result.HasErrors ? result.Errors : null
                };

                _logger.LogInformation(
                    "Screening completed for {EntityName}. Found {TotalHits} hits in {ElapsedMs}ms",
                    request.EntityName,
                    result.TotalHits,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing screening request for entity: {EntityName}", request.EntityName);
                throw;
            }
        }
    }
}
