using Grpc.Core;
using Library.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Library.UnitTests;

public class RpcProblemDetailsHandlerTests
{
    private const string Detail = "Reported by the library service.";

    private readonly CapturedProblem _written = new();

    private RpcProblemDetailsHandler Handler() =>
        new(_written, NullLogger<RpcProblemDetailsHandler>.Instance);

    [Theory]
    [InlineData(StatusCode.InvalidArgument, 400, true)]
    [InlineData(StatusCode.NotFound, 404, true)]
    [InlineData(StatusCode.AlreadyExists, 409, true)]
    [InlineData(StatusCode.FailedPrecondition, 409, true)]
    [InlineData(StatusCode.Unavailable, 503, false)]
    [InlineData(StatusCode.DeadlineExceeded, 504, false)]
    [InlineData(StatusCode.Internal, 502, false)]
    [InlineData(StatusCode.Cancelled, 502, false)]   // cancelled while the caller is still waiting
    public async Task The_api_turns_a_grpc_status_into_a_problem_response(
        StatusCode code, int status, bool detailShown)
    {
        var context = new DefaultHttpContext();

        var handled = await Handler().TryHandleAsync(
            context, new RpcException(new Status(code, Detail)), default);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(status);
        _written.Problem.ShouldNotBeNull();
        _written.Problem.Status.ShouldBe(status);
        _written.Problem.Detail.ShouldBe(detailShown ? Detail : null);
    }

    [Fact]
    public async Task The_api_answers_nobody_when_the_caller_has_hung_up()
    {
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(canceled: true) };

        var handled = await Handler().TryHandleAsync(
            context, new RpcException(new Status(StatusCode.Cancelled, "Call canceled by the client.")), default);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
        _written.Problem.ShouldBeNull();
    }

    [Fact]
    public async Task The_api_leaves_anything_but_a_grpc_failure_to_the_default_handler()
    {
        var handled = await Handler().TryHandleAsync(
            new DefaultHttpContext(), new InvalidOperationException(), default);

        handled.ShouldBeFalse();
        _written.Problem.ShouldBeNull();
    }

    private sealed class CapturedProblem : IProblemDetailsService
    {
        public ProblemDetails? Problem { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Problem = context.ProblemDetails;

            return ValueTask.CompletedTask;
        }

        // The interface's default returns false; the real service reports a successful write.
        public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            await WriteAsync(context);

            return true;
        }
    }
}
