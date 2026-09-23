using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Library.Api;

// Without a deadline a stalled service holds every request open with it. The deadline travels with
// the call, so the service abandons the work at the moment the caller gets its 504.
public sealed class DeadlineInterceptor(TimeSpan budget) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        if (context.Options.Deadline is null)
        {
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithDeadline(DateTime.UtcNow + budget));
        }

        return continuation(request, context);
    }
}
