using HomeSite.Managers;

namespace HomeSite.Middleware
{
    public class EmailVerificationMiddleware
    {
        private readonly RequestDelegate _next;

        public EmailVerificationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, AccountVerificationManager verificationManager)
        {
            var username = context.User.Identity.Name;


            // Разрешённые публичные пути
            var publicPaths = new[]
            {
                "/", "/privacy", "/changes", "/account/login", "/account/register", "/account", "/sse", "/account/verification", "/fileshare", "/shared/downloadfile"
            };

            var path = context.Request.Path.ToString().ToLower();
            bool isPublic = publicPaths.Any(publicPath =>
                path == publicPath || path.StartsWith(publicPath + "/"));
            if (isPublic)
            {
                await _next(context);
                return;
            }
            if (username == null)
            {
                context.Response.Redirect("/account/login");
                return;
            }
            if (!isPublic && verificationManager.RequiresVerification(username))
            {
                context.Response.Redirect("/account/verification");
                return;
            }

            await _next(context);
        }
    }
}
