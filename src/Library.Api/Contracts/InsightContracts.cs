using System.ComponentModel.DataAnnotations;
using Google.Protobuf.WellKnownTypes;
using Library.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Contracts;

public sealed record TopQuery : IValidatableObject
{
    [FromQuery(Name = "limit")]
    [Range(1, 100)]
    public int Limit { get; init; } = 10;

    // Whole UTC days, so a request means the same window wherever the API happens to run.
    [FromQuery(Name = "from")]
    public DateOnly? From { get; init; }

    [FromQuery(Name = "to")]
    public DateOnly? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From > To)
        {
            yield return new ValidationResult("from must not be later than to.", ["from", "to"]);
        }
    }

    public WindowedTopRequest ToRequest()
    {
        var request = new WindowedTopRequest { Limit = Limit };

        if (From is not null)
        {
            request.From = StartOf(From.Value);
        }

        if (To is not null)
        {
            request.To = StartOf(To.Value);
        }

        return request;
    }

    private static Timestamp StartOf(DateOnly day) =>
        Timestamp.FromDateTime(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
}

public sealed record PopularBookResponse(int BookId, string Title, string Author, int TimesBorrowed);

public sealed record ActivePatronResponse(int PatronId, string Name, int BooksBorrowed);
