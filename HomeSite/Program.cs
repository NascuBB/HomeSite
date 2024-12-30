using HomeSite.Helpers;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Cryptography.X509Certificates;

try
{


    var serverInfo = ServerInfo.GetInstance();
#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
    Task.Run(() => serverInfo.StartMonitoring(serverInfo.CancellationTokenSource.Token));
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
    FileShareManager.PrepareFileShare();
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddSignalR();

#if DEBUG
    Console.WriteLine("Mode=Debug");
#else
    builder.WebHost.UseKestrel();
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Настройка HTTP (необязательно)
        options.Listen(System.Net.IPAddress.Parse("192.168.31.204"),80); // HTTP

        // Настройка HTTPS
        options.Listen(System.Net.IPAddress.Parse("192.168.31.204"), 443, listenOptions =>
        {
            listenOptions.UseHttps(@"C:\Users\nonam\source\publish\certificate.pfx", "gamemode1");
        });
    });
    //builder.WebHost.UseUrls(["http://192.168.31.204:80", "https://192.168.31.204:443"]);
#endif




    var app = builder.Build();

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

    app.MapHub<MinecraftLogHub>("/minecraftHub");

    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();



    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapControllers();

    Helper.SetThisApp(app);

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