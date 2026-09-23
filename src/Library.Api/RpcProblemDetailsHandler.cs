using Grpc.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api;

public sealed class RpcProblemDetailsHandler(
    IProblemDetailsService problemDetails, ILogger<RpcProblemDetailsHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not RpcException rpc)
        {
            return false;
        }

        // The caller hung up and the call was cancelled with it: nobody to answer, nothing failed.
        if (rpc.StatusCode == StatusCode.Cancelled && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;

            return true;
        }

        var (status, title) = rpc.StatusCode switch
        {
            StatusCode.InvalidArgument => (StatusCodes.Status400BadRequest, "Invalid request"),
            StatusCode.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            StatusCode.AlreadyExists => (StatusCodes.Status409Conflict, "Already checked out"),
            StatusCode.FailedPrecondition => (StatusCodes.Status409Conflict, "Not allowed in this state"),
            StatusCode.Unavailable => (StatusCodes.Status503ServiceUnavailable, "The library service is unavailable"),
            StatusCode.DeadlineExceeded => (StatusCodes.Status504GatewayTimeout, "The library service timed out"),
            _ => (StatusCodes.Status502BadGateway, "The library service failed")
        };

        var upstreamFault = status >= StatusCodes.Status500InternalServerError;

        if (upstreamFault)
        {
            log.LogError(rpc, "Library service call failed with {StatusCode}", rpc.StatusCode);
        }

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = upstreamFault ? null : rpc.Status.Detail
            }
        });
    }
}
