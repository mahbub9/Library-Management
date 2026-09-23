using Grpc.Core;
using Grpc.Core.Interceptors;
using Library.Service.Domain;

namespace Library.Service.Rpc;

public sealed class CirculationRuleInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (CirculationRuleException broken)
        {
            var code = broken.Rule switch
            {
                CirculationRule.PatronNotFound => StatusCode.NotFound,
                CirculationRule.BookNotFound => StatusCode.NotFound,
                CirculationRule.LoanNotFound => StatusCode.NotFound,
                CirculationRule.AlreadyCheckedOut => StatusCode.AlreadyExists,
                CirculationRule.AlreadyReturned => StatusCode.FailedPrecondition,
                _ => StatusCode.Unknown
            };

            throw new RpcException(new Status(code, broken.Message));
        }
    }
}
