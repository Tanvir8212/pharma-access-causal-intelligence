using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace PharmaAccess.Api;

public sealed record ReleaseMetadata(string ApplicationVersion,string CommitHash,string BuildTimestamp)
{
    public static ReleaseMetadata Current { get; }=Create();
    private static ReleaseMetadata Create()
    {
        var assembly=typeof(ReleaseMetadata).Assembly;
        var version=assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion??"unknown";
        var metadata=assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToDictionary(x=>x.Key,x=>x.Value??"unknown",StringComparer.OrdinalIgnoreCase);
        return new(Safe(version,64),Safe(metadata.GetValueOrDefault("CommitHash")??"unknown",64),Safe(metadata.GetValueOrDefault("BuildTimestamp")??"unknown",40));
    }
    private static string Safe(string value,int length)
    {
        var safe=new string(value.Where(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '-' or ':' or 'T' or 'Z').Take(length).ToArray());
        return safe.Length>0?safe:"unknown";
    }
}

public static class SecurityPolicies
{
    public const string ResearchReader="ResearchReader", ModelGovernanceReviewer="ModelGovernanceReviewer", ModelGovernanceApprover="ModelGovernanceApprover", SystemAdministrator="SystemAdministrator";
}
public sealed class DevelopmentHeaderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder,IWebHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if(!environment.IsDevelopment())return Task.FromResult(AuthenticateResult.NoResult());
        var subject=Request.Headers["X-Development-User"].ToString();if(string.IsNullOrWhiteSpace(subject))return Task.FromResult(AuthenticateResult.NoResult());
        var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,subject),new(ClaimTypes.Name,subject)};
        foreach(var role in Request.Headers["X-Development-Roles"].ToString().Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))claims.Add(new Claim(ClaimTypes.Role,role));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));
    }
}

public sealed record RateLimitSettings(int ResearchAssistantPermitLimit=10,int GovernancePermitLimit=20,int AuthenticationPermitLimit=20,int ReportingPermitLimit=10,int WindowSeconds=60);
public interface IRequestRateLimitGate{bool TryAcquire(string partition,string bucket,out int retryAfterSeconds);}
public sealed class FixedWindowRequestRateLimitGate(RateLimitSettings settings,TimeProvider timeProvider):IRequestRateLimitGate
{
    private readonly ConcurrentDictionary<string,(long Window,int Count)> counters=[];
    public bool TryAcquire(string partition,string bucket,out int retryAfterSeconds){var now=timeProvider.GetUtcNow().ToUnixTimeSeconds();var window=now/settings.WindowSeconds;var limit=bucket switch{"rag"=>settings.ResearchAssistantPermitLimit,"governance"=>settings.GovernancePermitLimit,"auth"=>settings.AuthenticationPermitLimit,_=>settings.ReportingPermitLimit};var current=counters.AddOrUpdate(partition+":"+bucket,_=>(window,1),(_,old)=>old.Window==window?(window,old.Count+1):(window,1));retryAfterSeconds=(int)(settings.WindowSeconds-now%settings.WindowSeconds);if(current.Count<=limit)return true;var tags=new TagList{{"bucket",bucket}};OperationalTelemetry.RateLimitRejections.Add(1,tags);return false;}
}

public static class OperationalTelemetry
{
    public static readonly ActivitySource Activities=new("PharmaAccess.Api");private static readonly Meter Meter=new("PharmaAccess.Api");
    public static readonly Histogram<double> RequestDuration=Meter.CreateHistogram<double>("pharmaaccess.request.duration","ms");public static readonly Counter<long> RequestFailures=Meter.CreateCounter<long>("pharmaaccess.request.failures");public static readonly Counter<long> GeminiCalls=Meter.CreateCounter<long>("pharmaaccess.gemini.calls");public static readonly Counter<long> GeminiFallbacks=Meter.CreateCounter<long>("pharmaaccess.gemini.fallbacks");public static readonly Counter<long> ValidationFailures=Meter.CreateCounter<long>("pharmaaccess.validation.failures");public static readonly Counter<long> RateLimitRejections=Meter.CreateCounter<long>("pharmaaccess.ratelimit.rejections");public static readonly Counter<long> GovernanceApprovals=Meter.CreateCounter<long>("pharmaaccess.governance.approvals");public static readonly Counter<long> GovernanceRejections=Meter.CreateCounter<long>("pharmaaccess.governance.rejections");public static readonly Counter<long> Rollbacks=Meter.CreateCounter<long>("pharmaaccess.governance.rollbacks");public static readonly Counter<long> DriftSeverity=Meter.CreateCounter<long>("pharmaaccess.drift.severity");
}

public sealed class ApiSafetyMiddleware(RequestDelegate next,ILogger<ApiSafetyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlation=context.Request.Headers["X-Correlation-ID"].FirstOrDefault();if(string.IsNullOrWhiteSpace(correlation)||correlation.Length>128)correlation=Guid.NewGuid().ToString("N");context.TraceIdentifier=correlation;context.Response.Headers["X-Correlation-ID"]=correlation;SetHeaders(context);
        var started=Stopwatch.GetTimestamp();using var activity=OperationalTelemetry.Activities.StartActivity("http.request",ActivityKind.Server);activity?.SetTag("correlation.id",correlation);activity?.SetTag("http.route",context.Request.Path.Value);
        try{await next(context);}catch(Exception error){logger.LogError("Request failed CorrelationId={CorrelationId} Path={Path} ErrorType={ErrorType}",correlation,SafeRoute(context.Request.Path),error.GetType().Name);OperationalTelemetry.RequestFailures.Add(1);if(context.Response.HasStarted)throw;context.Response.Clear();context.Response.StatusCode=StatusCodes.Status500InternalServerError;context.Response.ContentType="application/problem+json";await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body,new ProblemDetails{Status=500,Title="The request could not be completed.",Detail="Use the correlation identifier when contacting support.",Extensions={{"correlationId",correlation}}},cancellationToken:context.RequestAborted);}
        finally{var latency=Stopwatch.GetElapsedTime(started).TotalMilliseconds;var tags=new TagList{{"status",context.Response.StatusCode}};OperationalTelemetry.RequestDuration.Record(latency,tags);logger.LogInformation("Request completed CorrelationId={CorrelationId} Route={Route} StatusCode={StatusCode} LatencyMs={LatencyMs}",correlation,SafeRoute(context.Request.Path),context.Response.StatusCode,latency);}
    }
    private static string SafeRoute(PathString path)=>path.Value is null?"/":path.Value.Length>200?path.Value[..200]:path.Value;
    private static void SetHeaders(HttpContext c){c.Response.Headers["X-Content-Type-Options"]="nosniff";c.Response.Headers["Referrer-Policy"]="no-referrer";c.Response.Headers["Content-Security-Policy"]="default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";c.Response.Headers["X-Frame-Options"]="DENY";}
}
