using HomeSite.Entities;
using HomeSite.Helpers;
using HomeSite.Managers;
using HomeSite.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine(Directory.GetCurrentDirectory());
    var serverInfo = ServerInfo.GetInstance();
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
    Task.Run(() => serverInfo.StartMonitoring(serverInfo.CancellationTokenSource.Token));
    //Task.Run(FileShareManager.PrepareFileShare);
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
    ConfigManager.GetConfiguration();
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
    //var com = builder.Configuration.GetConnectionString("postgresql"); options => options.UseNpgsql(builder.Configuration.GetConnectionString("postgresql")
    builder.Services.AddDbContext<UserDBContext>();
    builder.Services.AddDbContext<SharedRightsDBContext>();
    builder.Services.AddDbContext<ShareFileInfoDBContext>();

    builder.Services.AddDbContextFactory<ServerDBContext>();

    builder.Services.AddScoped<IUserHelper, UserHelper>();
	builder.Services.AddScoped<ISharedAdministrationManager, SharedAdministrationManager>();
    builder.Services.AddScoped<IFileShareManager, FileShareManager>();
    builder.Services.AddScoped<IMinecraftServerManager ,MinecraftServerManager>();

    //builder.Services.AddDbContext<UserDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("postgres")));
    builder.Services.AddSingleton<AccountVerificationManager>();
    builder.Services.AddSingleton<UserPasswordManager>();
	builder.Services.AddSingleton<LogConnectionManager>();

    builder.Services.AddMemoryCache();

    //builder.Services.AddSignalR();
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 1073741824; // if don't set default value is: 128 MB
    });

#if !DEBUG
    builder.WebHost.UseKestrel();
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Настройка HTTP (необязательно)
        options.Listen(System.Net.IPAddress.Parse(ConfigManager.LocalAddress!),80); // HTTP
        //options.Limits.MaxRequestBodySize = 209715200;
        // Настройка HTTPS
        options.Listen(System.Net.IPAddress.Parse(ConfigManager.LocalAddress!), 443, listenOptions =>
        {
            listenOptions.UseHttps(Path.Combine(Directory.GetCurrentDirectory(),"certificate.pfx"), ConfigManager.RCONPassword!);
        });
    });
    //builder.WebHost.UseUrls(["http://192.168.31.204:80", "https://192.168.31.204:443"]);
#endif
    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
#if DEBUG
    logger.Log(LogLevel.Information,"!!! Development mode !!!");
#endif
    logger.Log(LogLevel.Information, "Unskipable prepairing before launch");

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

#if !DEBUG
    app.UseHttpsRedirection();
    app.UseHsts();
#endif

    //app.MapHub<MinecraftLogHub>("/minecraftHub");

    app.UseStaticFiles();

    app.UseRouting();

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


    Console.ReadKey();
}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}
finally
{
    Console.ReadKey();
}