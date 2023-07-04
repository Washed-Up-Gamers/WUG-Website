namespace WUG.Extensions;

public static class HttpContextExtensions
{
    public static User GetUser(this HttpContext context)
    {
        if (!context.Items.ContainsKey("user"))
            return null;
        return (User)context.Items["user"]!;
    }
}
