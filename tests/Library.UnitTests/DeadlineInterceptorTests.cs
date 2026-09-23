using Grpc.Core;
using Grpc.Core.Interceptors;
using Library.Api;
using Shouldly;

namespace Library.UnitTests;

public class DeadlineInterceptorTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(10);

    private static readonly Marshaller<object> Opaque = Marshallers.Create<object>(_ => [], _ => new object());

    private static readonly Method<object, object> GetLoan =
        new(MethodType.Unary, "library.CirculationService", "GetLoan", Opaque, Opaque);

    [Fact]
    public void The_api_gives_a_call_without_a_deadline_one()
    {
        var before = DateTime.UtcNow;

        var sent = Send(new CallOptions());

        sent.Deadline.ShouldNotBeNull();
        sent.Deadline.Value.ShouldBeInRange(before + Budget, DateTime.UtcNow + Budget);
    }

    [Fact]
    public void The_api_keeps_a_deadline_the_caller_chose()
    {
        var chosen = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Send(new CallOptions(deadline: chosen)).Deadline.ShouldBe(chosen);
    }

    [Fact]
    public void The_api_still_cancels_the_call_when_its_caller_hangs_up()
    {
        using var hangUp = new CancellationTokenSource();

        Send(new CallOptions(cancellationToken: hangUp.Token)).CancellationToken.ShouldBe(hangUp.Token);
    }

    // Runs one call through the interceptor and returns the options it went out with.
    private static CallOptions Send(CallOptions options)
    {
        CallOptions sent = default;

        new DeadlineInterceptor(Budget).AsyncUnaryCall(
            new object(),
            new ClientInterceptorContext<object, object>(GetLoan, null, options),
            (_, context) =>
            {
                sent = context.Options;

                return new AsyncUnaryCall<object>(
                    Task.FromResult(new object()),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { });
            });

        return sent;
    }
}
