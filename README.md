# Library

A REST API in front of a gRPC service in front of SQL Server. It answers the four questions in the
brief:

| Question | Endpoint |
|---|---|
| What are the most borrowed books? | `GET /api/books/most-borrowed` |
| Which patrons borrowed the most in a given time frame? | `GET /api/patrons/most-active` |
| What is a reading pace, in pages per day? | `GET /api/loans/{loanId}/reading-pace` |
| What else was borrowed by people who borrowed a title? | `GET /api/books/{bookId}/borrowed-together` |

The circulation desk behind them: `POST /api/loans`, `POST /api/loans/{loanId}/return`,
`GET /api/loans/{loanId}`. Checking out is the only request with a body:

```json
{ "patronId": 6, "bookId": 5 }
```

## Running

Prerequisites: the .NET 10 SDK, Docker Desktop, and a trusted local certificate
(`dotnet dev-certs https --trust`). The API reaches the service over HTTPS, so an untrusted
certificate appears as a gRPC connection failure rather than a browser warning.

```
docker compose up -d
```

The first run pulls the SQL Server image, about 600 MB. Then start both projects on their `https`
profiles. The API is configured to find the service at `https://localhost:7285`:

```
dotnet run --project src/Library.Service --launch-profile https
dotnet run --project src/Library.Api --launch-profile https
```

In Visual Studio, pick the **Library** launch profile and press F5 to start both. Shared launch
profiles are a preview feature, so enable **Tools → Options → Environment → Preview Features →
Multi-Project Launch Profiles** first, or set both projects to start from the solution properties.

The service applies its migration on startup and, in Development, seeds ten books, six patrons and
thirty loans.

Interactive reference: `https://localhost:7138/scalar`, which Visual Studio opens with the API.
Ready-made requests: [src/Library.Api/Library.Api.http](src/Library.Api/Library.Api.http).

To point at an existing SQL Server instead, override the connection string:

```
$env:ConnectionStrings__library = "Server=.;Database=library;Trusted_Connection=True;TrustServerCertificate=True"
```

The password in `docker-compose.yml` is plain text deliberately: it is a local database of ten
invented books, and a reviewer should not have to hunt for a secret to start it. Anywhere but a
local container the connection string comes from the environment or a secret store, which is why
the base `appsettings.json` carries none and the service refuses to start without one.

### First run

The seed is a fixed table, so these are reproducible:

```
GET /api/books/most-borrowed?limit=3

[ { "bookId": 1, "title": "The Pragmatic Programmer",              "timesBorrowed": 5 },
  { "bookId": 2, "title": "Designing Data-Intensive Applications", "timesBorrowed": 4 },
  { "bookId": 3, "title": "Domain-Driven Design",                  "timesBorrowed": 4 } ]

GET /api/loans/18/reading-pace   ->  daysHeld 1,   pagesPerDay 258
GET /api/loans/19/reading-pace   ->  daysHeld 1.5, pagesPerDay 116.7
GET /api/loans/29/reading-pace   ->  409, that book is still out
```

## Tests

```
dotnet test
```

| Category | Project | What it covers | Tests |
|---|---|---|---|
| Unit | `Library.UnitTests` | reading-pace arithmetic, window semantics, query validation, status translation at both boundaries, the call deadline | 39 |
| Integration | `Library.Tests/Integration` | circulation rules, both simultaneous-request races, and the four queries against real SQL Server | 14 |
| Functional | `Library.Tests/Functional` | each HTTP behaviour through the whole pipeline | 21 |
| System | `Library.Tests/System` | complete journeys across several endpoints | 2 |

Counts are test cases as the runner reports them, so each row of a theory counts once.

`Library.Tests` starts a disposable SQL Server container, so Docker must be running. Without it,
run the unit tests alone: `dotnet test --project tests/Library.UnitTests`. The functional and system
tests host both applications in memory; the API keeps the gRPC clients it registers in production
and only their transport is swapped, so a request crosses controller, protobuf, service, EF Core and
SQL Server.

The circulation rules are tested against SQL Server rather than a fake because one of them, that a
patron may hold only one copy of a title at a time, is enforced by a filtered unique index. An
in-memory provider would check half the guarantee.

## Design notes

**The API holds no database connection.** It has no `DbContext` and no reference to the service
project; both compile the same `proto/library.proto` through `Library.Contracts`, so the two sides
cannot drift.

**One open loan per patron per title is enforced by the database**, with a unique index on
`(PatronId, BookId)` filtered to `WHERE ReturnedOn IS NULL`. The desk checks first so the ordinary
case gets a clear 409; if two requests both pass that check, the index rejects the second. Returns
are guarded the same way: `ReturnedOn` is a concurrency token, so of two simultaneous returns the
second is a 409 rather than an overwrite.

**Date windows are half-open**, `from` inclusive and `to` exclusive, so two adjacent months never
both count the same loan. They are whole UTC days written `yyyy-MM-dd`: a date without an offset
would otherwise be read in the server's time zone, and `01/09/2025` is 9 January to some callers and
1 September to others.

**Borrowed-together counts distinct readers, not loans.** Someone who borrowed the same title three
times is one shared reader; counting loan rows would let a single enthusiast outrank a genuinely
popular pairing.

**Reading pace is measured per loan**, the borrow-and-return event the brief describes. It uses
elapsed time with a floor of one day, so a same-afternoon return does not report an absurd figure. A
book that has not come back has no pace, so that request is a 409.

**Failures cross the boundary as status codes.** Circulation rules become gRPC statuses in the
service and ProblemDetails in the API. Above 500 the response carries no upstream detail and the API
logs it; a caller that hangs up gets no answer rather than an error. Every call carries a
ten-second deadline, so a stalled service cannot hold requests open indefinitely.

**Rankings order by count, then by id**, so a "top 5" is reproducible. Times are passed into the
domain as parameters rather than read from a clock, which keeps the rules deterministic without a
fake.

**There are no application-defined interfaces.** `CirculationDesk`, `BookInsights` and
`PatronInsights` have one implementation each and nothing substitutes them: the generated gRPC
client and `DbContext` are already the seams, and the tests use the real ones.

## Assumptions

- "Within a given time frame" filters on the checkout date, not the return date.
- "Borrowed the most books" counts loans, so a title borrowed twice counts twice. Borrowed-together
  counts people instead, because that question is about readers.
- A patron may hold many titles at once, but only one copy of a given title. There is no copy-level
  inventory, so there is no "all copies are out" rule.
- "Assuming continuous reading" means the whole book was read across the time it was held.
- The dataset is small enough that reporting runs against the write model.

## Warm-up exercises

The four starter tasks are in [warmup/](warmup/), outside the solution. See
[warmup/README.md](warmup/README.md).

## AI assistance

I used AI on this one. It set up the solution and the project files, generated the EF migration,
wrote the in-memory test host and the table-driven edge case tests, filled in the `.http` requests,
and drafted this README. I also had it review the finished code a few times.

Those reviews found four things worth fixing. 5xx responses were echoing transport error text back
to callers. Date windows were being read in the server's time zone, so the same request meant
different things on different machines. Two simultaneous returns of one loan could both succeed.
And calls to the service had no deadline, so a stalled service would have held every request open
with it. Each fix came with a test.

The domain model, the circulation rules, the four queries and the error handling are mine.
