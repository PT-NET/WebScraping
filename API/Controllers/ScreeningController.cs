using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebScraping.Application.Commands.ScreenEntity;
using WebScraping.Application.DTOs;
using WebScraping.Domain.Interfaces;

namespace WebScraping.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ScreeningController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<ScreeningController> _logger;

        public ScreeningController(
            IMediator mediator,
            IRateLimitService rateLimitService,
            ILogger<ScreeningController> logger)
        {
            _mediator = mediator;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        /// <summary>
        /// Screens an entity against high-risk databases
        /// </summary>
        /// <param name="request">Screening request with entity name and sources</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Screening results with hits found</returns>
        [HttpPost("screen")]
        [ProducesResponseType(typeof(ScreeningResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ScreeningResponseDto>> ScreenEntity(
            [FromBody] ScreeningRequestDto request,
            CancellationToken cancellationToken)
        {
            var clientId = GetClientIdentifier();

            // Verificar rate limit
            var remaining = await _rateLimitService.GetRemainingCallsAsync(clientId, cancellationToken);

            Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());

            var command = new ScreenEntityCommand
            {
                EntityName = request.EntityName,
                Sources = request.Sources
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        private string GetClientIdentifier()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"client:{ip}";
        }
    }
}
