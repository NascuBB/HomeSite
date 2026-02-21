using Docker.DotNet;
using HomeSite.Entities;
using HomeSite.Helpers;
using HomeSite.Managers;
using HomeSite.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine(Directory.GetCurrentDirectory());
    ConfigManager.GetConfiguration();
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllersWithViews();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    });
    builder.Services.AddDbContext<UserDBContext>();
    builder.Services.AddDbContext<SharedRightsDBContext>();
    builder.Services.AddDbContext<ShareFileInfoDBContext>();

    builder.Services.AddDbContextFactory<ServerDBContext>();

    builder.Services.AddScoped<IUserHelper, UserHelper>();
	builder.Services.AddScoped<ISharedAdministrationManager, SharedAdministrationManager>();
    builder.Services.AddScoped<IFileShareManager, FileShareManager>();
    builder.Services.AddScoped<IMinecraftServerManager ,MinecraftServerManager>();

    builder.Services.AddSingleton<AccountVerificationManager>();
    builder.Services.AddSingleton<UserPasswordManager>();
	builder.Services.AddSingleton<LogConnectionManager>();

    builder.Services.AddSingleton<IDockerClient>(new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient());
    builder.Services.AddMemoryCache();

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 1073741824; // if don't set default value is: 128 MB
    });

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"/app/Config/Keys"));

    var cultureInfo = new CultureInfo("ru-RU");
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
#if DEBUG
    logger.Log(LogLevel.Information,"!!! Development mode !!!");
#endif
    logger.Log(LogLevel.Information, "Unskipable prepairing before launch");

    using (var scope = app.Services.CreateScope())
    {
        var dockerClient = scope.ServiceProvider.GetRequiredService<IDockerClient>();
        try
        {
            await Helper.EnsureMinecraftImageExists((DockerClient)dockerClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Docker Init Error]: {ex.Message}");
        }
        var services = scope.ServiceProvider;
        var contextTypes = new[]
        {
            typeof(UserDBContext),
            typeof(ServerDBContext),
            typeof(SharedRightsDBContext),
            typeof(ShareFileInfoDBContext)
        };

        foreach (var type in contextTypes)
        {
            try
            {
                var context = (DbContext)services.GetRequiredService(type);
                context.Database.Migrate();
                logger.LogInformation($"Migration for {type.Name} successful.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while migrating {type.Name}.");
                throw;
            }
        }
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseHsts();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });


    app.UseAuthentication();
    app.UseAuthorization();
    app.UseWebSockets();

    app.UseMiddleware<EmailVerificationMiddleware>();


    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapControllers();

    Helper.SetThisApp(app);
    //EnumGenerator.GenerateEnums("versions", "Generated/VersionEnums.cs");

    Thread thread = new Thread(() =>
    {
        app.Run();
    });
    thread.Start();
}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}