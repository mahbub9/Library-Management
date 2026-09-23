using Grpc.Core;
using Library.Service.Domain;
using Library.Service.Rpc;
using Shouldly;

namespace Library.UnitTests;

public class CirculationRuleInterceptorTests
{
    private const string Message = "Loan 7 does not exist.";

    private readonly CirculationRuleInterceptor _interceptor = new();

    [Theory]
    [InlineData(CirculationRule.PatronNotFound, StatusCode.NotFound)]
    [InlineData(CirculationRule.BookNotFound, StatusCode.NotFound)]
    [InlineData(CirculationRule.LoanNotFound, StatusCode.NotFound)]
    [InlineData(CirculationRule.AlreadyCheckedOut, StatusCode.AlreadyExists)]
    [InlineData((CirculationRule)99, StatusCode.Unknown)]
    public async Task The_service_turns_a_broken_rule_into_its_grpc_status(
        CirculationRule rule, StatusCode expected)
    {
        var failure = await Should.ThrowAsync<RpcException>(
            () => Call(new CirculationRuleException(rule, Message)));

        failure.StatusCode.ShouldBe(expected);
        failure.Status.Detail.ShouldBe(Message);
    }

    [Fact]
    public async Task The_service_lets_any_other_failure_through_untouched()
    {
        var original = new InvalidOperationException("The database is down.");

        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Call(original));

        failure.ShouldBeSameAs(original);
    }

    // The interceptor only forwards the call context, so these calls need none.
    private Task<object> Call(Exception thrown) =>
        _interceptor.UnaryServerHandler<object, object>(new object(), null!, (_, _) => throw thrown);
}
