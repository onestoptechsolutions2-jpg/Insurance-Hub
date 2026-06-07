using Insurance_Hub.Data;
using Insurance_Hub.Models;
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

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
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
builder.Services.AddHostedService<PolicyReminderService>();

// ── Health checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Forwarded headers (Coolify / Traefik reverse proxy) ───────────────────
// XForwardedHost  → lets Url.Action / RedirectToAction use the public domain
// XForwardedProto → lets the app know the public scheme is https
// XForwardedFor   → preserves the real client IP
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor   |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;   // <-- critical for correct URL generation
    // Trust any proxy on the Docker internal network (172.x.x.x, 10.x.x.x)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = null; // allow unlimited proxy hops inside the cluster
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
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbInitializer.SeedAsync(db);
    await DbInitializer.SeedRolesAsync(roleManager);
}

app.Run();
