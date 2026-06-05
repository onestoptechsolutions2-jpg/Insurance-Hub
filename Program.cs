using Insurance_Hub.Data;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database provider selection ────────────────────────────────────────────
// Switch via env var:  DatabaseProvider=SqlServer
// Connection strings via env vars:
//   ConnectionStrings__PostgreSQL=Host=db;Port=5432;Database=InsuranceHub;Username=postgres;Password=secret
//   ConnectionStrings__SqlServer=Server=...
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider == "SqlServer")
    {
        var cs = builder.Configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' not found.");
        options.UseSqlServer(cs);
    }
    else
    {
        var cs = builder.Configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");
        options.UseNpgsql(cs);
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// ── Data Protection — store keys in PostgreSQL so sessions survive restarts ─
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("InsuranceHub");

// ── Email service ──────────────────────────────────────────────────────────
// Configure via env vars:
//   EmailSettings__SmtpHost=smtp.gmail.com
//   EmailSettings__SenderEmail=you@gmail.com
//   EmailSettings__SenderPassword=app-password
//   EmailSettings__AgentEmail=agent@company.com
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ── Health checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Forwarded headers (Coolify / Nginx / Traefik reverse proxy) ────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust all proxies inside Docker network
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Only redirect HTTPS when NOT running inside a container
// (Coolify's reverse proxy handles TLS termination externally)
var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!inContainer)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Health check endpoint (/health) ───────────────────────────────────────
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ── Seed database on startup ───────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.SeedAsync(db);
}

app.Run();
