using backend.Services;

namespace backend.Middleware;

public class SessionMiddleware
{
    private readonly RequestDelegate _next;

    public SessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        SessionService sessionService)
    {
        var sessionId =
            context.Request.Headers["X-Session-Id"]
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session =
                await sessionService
                    .GetValidSessionAsync(sessionId);

            if (session != null)
            {
                context.Items["CurrentSession"] = session;
                context.Items["CurrentUser"] = session.User;

                await sessionService
                    .UpdateActivityAsync(session);
            }
        }

        await _next(context);
    }
}