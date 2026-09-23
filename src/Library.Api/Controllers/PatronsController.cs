using Library.Api.Contracts;
using Library.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/patrons")]
public sealed class PatronsController(InsightsService.InsightsServiceClient insights) : ControllerBase
{
    /// <summary>Which patrons borrowed the most within a given time frame?</summary>
    [HttpGet("most-active")]
    [ProducesResponseType<IReadOnlyList<ActivePatronResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<ActivePatronResponse>> MostActive(
        [FromQuery] TopQuery query, CancellationToken ct)
    {
        var ranked = await insights.MostActivePatronsAsync(query.ToRequest(), cancellationToken: ct);

        return [.. ranked.Patrons.Select(patron =>
            new ActivePatronResponse(patron.PatronId, patron.Name, patron.BooksBorrowed))];
    }
}
