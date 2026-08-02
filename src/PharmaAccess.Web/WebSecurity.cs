using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PharmaAccess.Web;
public static class WebPolicies{public const string ModelGovernanceReviewer="ModelGovernanceReviewer";}
public sealed class WebDevelopmentAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder,IWebHostEnvironment environment):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync(){if(!environment.IsDevelopment())return Task.FromResult(AuthenticateResult.NoResult());var subject=Request.Headers["X-Development-User"].ToString();if(string.IsNullOrWhiteSpace(subject))return Task.FromResult(AuthenticateResult.NoResult());var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,subject),new(ClaimTypes.Name,subject)};foreach(var role in Request.Headers["X-Development-Roles"].ToString().Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))claims.Add(new(ClaimTypes.Role,role));return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));}
}
public sealed class WebSafetyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context){var id=context.Request.Headers["X-Correlation-ID"].FirstOrDefault();if(string.IsNullOrWhiteSpace(id)||id.Length>128)id=Guid.NewGuid().ToString("N");context.TraceIdentifier=id;context.Response.Headers["X-Correlation-ID"]=id;context.Response.Headers["X-Content-Type-Options"]="nosniff";context.Response.Headers["Referrer-Policy"]="no-referrer";context.Response.Headers["X-Frame-Options"]="DENY";context.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; connect-src 'self' ws: wss:; frame-ancestors 'none'; object-src 'none'";await next(context);}
}
