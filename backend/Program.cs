using Microsoft.EntityFrameworkCore;
using SalesPerf.Backend.Infrastructure.Data;
// If deployed to a Linux server with a different locale (e.g., German/Russian), decimal parsing ("1.5") 
// might suddenly crash because the OS expects a comma ("1,5"). 
// We MUST force InvariantCulture globally on startup to guarantee deterministic backend parsing.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

// When Kubernetes routes traffic to a newly started pod, a sudden burst of concurrent DB queries 
// can starve the .NET ThreadPool (which only injects 1-2 threads per second).
// We MUST pre-warm the ThreadPool minimums to instantly handle traffic spikes without 503 timeouts.
System.Threading.ThreadPool.SetMinThreads(200, 200);


var builder = WebApplication.CreateBuilder(args);

// By default, ASP.NET only validates Scoped DI lifetimes in Development. If a developer injects a DbContext (Scoped)
// into a BackgroundService (Singleton), it works locally if untested, but CRASHES IN PRODUCTION on the first hit!
// We MUST force strict DI validation across all environments to guarantee startup safety.
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// When Kubernetes scales down (SIGTERM), the default graceful shutdown timeout is just 5 seconds.
// If AnalyticsController is running a heavy 10-second query, the connection is violently severed, causing DB rollbacks.
// We MUST increase the shutdown timeout to allow active requests to finish cleanly.
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

builder.WebHost.ConfigureKestrel(options =>
{
    // By default, the API sends a "Server: Kestrel" HTTP header with every response.
    // This leaks our exact technology stack to attackers, inviting targeted zero-day automated exploits.
    options.AddServerHeader = false;

    // Kestrel accepts an UNLIMITED number of concurrent TCP connections by default.
    // A botnet opening 100,000 idle connections will cause an immediate OutOfMemory crash.
    // We MUST enforce a physical connection limit per pod (e.g., 1000).
    options.Limits.MaxConcurrentConnections = 1000;

    // Kestrel allows HTTP request bodies up to ~30 Megabytes by default!
    // Since our API only accepts small JSON filters, a 30MB payload is an obvious memory exhaustion attack.
    // We restrict the global body size to 100 Kilobytes to reject malicious payloads directly at the socket layer.
    options.Limits.MaxRequestBodySize = 100 * 1024;
});

// If any developer or 3rd party library uses IMemoryCache, it has NO size limit by default!
// A simple loop caching objects can quickly exhaust server RAM. We MUST enforce a global size limit.
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10000;
});

// Add services to the container.
// If a developer accidentally returns an EF Core entity (Sale -> SaleItem -> Sale) from a controller,
// the default System.Text.Json serializer will get trapped in an infinite loop and crash the API (500 Error).
// We MUST configure it to ignore cycles globally to prevent production crashes.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        
        // ASP.NET serializes object properties as camelCase, but leaves Dictionary keys as-is!
        // If the backend returns a Dictionary<string, int>, frontend React code expecting camelCase keys will crash.
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// If a bot triggers a complex Analytics query that takes 5 minutes, it locks a database connection.
// Spanning 100 such requests will drain the connection pool and hang the entire API for everyone.
// We MUST enforce a global request timeout (e.g., 15s) to abort the CancellationToken and free the DB.
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
});

// We added AddEndpointsApiExplorer() but didn't actually expose the OpenAPI specification!
// Third-party BI tools (Tableau, PowerBI) need this spec to auto-generate HTTP clients.
// We MUST generate the OpenAPI endpoint.
builder.Services.AddOpenApi();

builder.Services.AddScoped<SalesPerf.Backend.Application.Interfaces.IAnalyticsService, SalesPerf.Backend.Application.Services.AnalyticsService>();
builder.Services.AddEndpointsApiExplorer();

//  Egress Bandwidth Waste (Response Compression)
// The Analytics API returns large JSON arrays. Without compression, we waste massive cloud egress bandwidth
// and slow down mobile users. Enabling Brotli/Gzip cuts JSON sizes by up to 90%.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Essential for API compression
        });

//  Ingress Asymmetry (Request Decompression)
// We compress outgoing JSON, but if a mobile client tries to send GZipped JSON, Kestrel rejects it!
// We MUST enable Request Decompression to accept compressed inbound traffic.
builder.Services.AddRequestDecompression();

// Analytics queries are heavy. We MUST inject Output Caching services so developers can decorate 
// endpoints with [OutputCache(Duration = 60)] to shield the database from dashboard refresh-spam.
builder.Services.AddOutputCache();

// By default, /api/sales and /api/SALES/ are treated as different keys by Cloudflare/CDN!
// This causes cache fragmentation. We MUST enforce strict lowercase URLs globally.
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// By default, ASP.NET logs plain text. In Kubernetes/Datadog, you cannot query or filter plain text easily.
// We MUST enable structured JSON logging so cloud log aggregators can parse properties (like TraceId and RequestPath).
builder.Logging.AddJsonConsole();

// Configure CORS
// Hardcoding localhost origins will completely block the React frontend when deployed to production domains!
// We MUST read allowed origins dynamically from appsettings.json to ensure CI/CD flexibility.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            // Without caching, every API call from the browser sends an OPTIONS preflight request first.
            // This doubles server load and adds 50-100ms latency to EVERY request.
            .SetPreflightMaxAge(TimeSpan.FromHours(1)));
});

// Configure EF Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextPool<SalesDbContext>(options => {
    options.UseNpgsql(connectionString, npgsqlOptions => 
    {
        // npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
    options.ConfigureWarnings(warnings => {
        warnings.Throw(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
        warnings.Throw(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning);
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
    });
});

// Standardizes unhandled exceptions into RFC 7807 format and hides sensitive Stack Traces in production.
builder.Services.AddProblemDetails();

// Protects the DB from being overwhelmed by scripts hitting the Analytics endpoint.
builder.Services.AddRateLimiter(options => 
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Allows Kubernetes to verify DB connectivity and stop routing traffic to dead pods.
builder.Services.AddHealthChecks()
    ;

// When deployed behind Nginx/K8s Ingress, RemoteIpAddress is the PROXY'S IP, not the user's!
// This causes our Rate Limiter to block ALL users globally after 100 requests. 
// We MUST configure Forwarded Headers to read 'X-Forwarded-For' to get the real user IP.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

// Browsers try to "sniff" content types if not explicit, opening the door to XSS attacks via uploaded JSON.
// We MUST append strict security headers globally to lock down the API's attack surface.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'");
    await next();
});

app.UseResponseCompression();
app.UseRequestDecompression();
app.UseOutputCache();
app.UseExceptionHandler();
app.UseRequestTimeouts();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    // development stuff
}
else
{
    // Without HSTS, a Man-In-The-Middle attacker can intercept the initial HTTP request before redirection.
    // UseHsts forces browsers to strictly use HTTPS for all future requests.
    app.UseHsts();
}

// Omitting HttpsRedirection allows clients to send sensitive JWTs or passwords over plain text HTTP!
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
// The app has app.UseAuthorization() but is missing app.UseAuthentication()!
// If a developer adds [Authorize] to a controller, it will mysteriously fail or allow unauthorized access
// because the identity (JWT/Cookie) is never established.
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.MapOpenApi();

// Running MigrateAsync() unconditionally on startup is catastrophic in production. 
// If deployed to Kubernetes with 5 replicas, all 5 instances will try to alter the DB schema simultaneously,
// causing deadlocks and database corruption. Migrations MUST be restricted to Development or handled by CI/CD.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<SalesDbContext>();
            
            // Apply migrations
            await context.Database.MigrateAsync();
            
            // Seed data
            await DataSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            // If MigrateAsync() fails, the exception was caught and swallowed, allowing app.Run() to execute!
            // This creates a 'Zombie Process': Kubernetes thinks the pod is healthy, but all DB queries will fail.
            // We MUST log as Critical and RETHROW the exception to trigger a hard crash (CrashLoopBackOff).
            logger.LogCritical(ex, "FATAL: An error occurred while migrating or seeding the database.");
            throw;
        }
        }
        }

// app.Run();





