using backend.Models;

namespace backend.Extensions;

public static class HttpContextExtensions
{
    public static User? GetCurrentUser(
        this HttpContext context)
    {
        return context.Items["CurrentUser"] as User;
    }

    public static UserSession? GetCurrentSession(
        this HttpContext context)
    {
        return context.Items["CurrentSession"]
            as UserSession;
    }
}