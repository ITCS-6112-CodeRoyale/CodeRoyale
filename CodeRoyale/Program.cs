using CodeRoyale.Components;
using CodeRoyale.Components.Account;
using CodeRoyale.Data;
using CodeRoyale.Data.Questions;
using CodeRoyale.Hubs;
using CodeRoyale.Identity;
using CodeRoyale.Live;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

try
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();
}
catch (Exception ex)
{
    Console.Error.WriteLine("Failed to configure logging.");
    Console.Error.WriteLine(ex);
    return 1;
}

builder.Services.AddSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// Live game state is in-memory and shared, so the service is a singleton and the
// loop that advances it is a single background worker.
builder.Services.AddSingleton<GameService>();
builder.Services.AddHostedService<GameLoopService>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<ApplicationUserProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// to set:
// cd CodeRoyale & dotnet user-secrets set ConnectionStrings:Database "-connection string-"

string connectionString = builder.Configuration.GetConnectionString(ApplicationDbContextFactory.ConnectionStringName)
                          ?? throw new FormatException($"Connection string not found. Add to user secrets at \"ConnectionStrings:{ApplicationDbContextFactory.ConnectionStringName}\".");

ServerVersion version = await ServerVersion.AutoDetectAsync(connectionString);
builder.Services.AddDbContext<ApplicationDbContext>(
    opts => opts.UseMySql(connectionString, version)
                .UseAsyncSeeding(async (db, _, token) =>
                {
                    List<CodeLanguage> languages = await db.Set<CodeLanguage>().ToListAsync();
                    if (!languages.Exists(x => x.LanguageCode == string.Empty))
                    {
                        AddLanguage(string.Empty, "Miscellaneous");
                    }
                    if (!languages.Exists(x => x.LanguageCode == "js"))
                    {
                        AddLanguage("js", "JavaScript");
                    }
                    if (!languages.Exists(x => x.LanguageCode == "csharp"))
                    {
                        AddLanguage("csharp", "C#");
                    }

                    if (!await db.Set<QuestionSet>().AnyAsync(s => s.User == null, token))
                    {
                        BuiltInDecks.AddAllToSet(db.Set<QuestionSet>(), languages);
                    }

                    await db.SaveChangesAsync(token);

                    void AddLanguage(string id, string displayName)
                    {
                        CodeLanguage l = new CodeLanguage { DisplayName = displayName, LanguageCode = id };
                        languages.Add(l);
                        db.Set<CodeLanguage>().Add(l);
                    }
                })
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<CircuitOptions>(options =>
{
#if DEBUG
    options.DetailedErrors = true;
#endif
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(5);
    options.DisconnectedCircuitMaxRetained = 16;
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapHub<GameHub>("/gamehub");

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

app.Run();
return 0;