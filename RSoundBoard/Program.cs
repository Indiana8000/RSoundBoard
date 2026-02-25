using TestApp1.HostUI;
using TestApp1.Models;
using TestApp1.Services;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

class Program
{
    private const string MutexName = "RSoundBoard_SingleInstance_Mutex";
    private const string EventName = "RSoundBoard_ShowWindow_Event";

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(true, MutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            // Another instance is already running
            SignalFirstInstance();
            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<ButtonRepository>();
        builder.Services.AddSingleton<SoundService>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        builder.WebHost.UseUrls("http://0.0.0.0:5000");

        var app = builder.Build();

        app.UseCors();

        // Use embedded files for single-file deployment
        var embeddedProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot");
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedProvider
        });

        var settingsService = app.Services.GetRequiredService<SettingsService>();
        var repository = app.Services.GetRequiredService<ButtonRepository>();
        var soundService = app.Services.GetRequiredService<SoundService>();

        app.MapGet("/api/buttons", async () =>
        {
            var buttons = await repository.GetAllAsync();
            return Results.Ok(buttons);
        });

        app.MapPost("/api/play/{id:guid}", async (Guid id) =>
        {
            var button = await repository.GetByIdAsync(id);
            if (button == null) return Results.NotFound();

            await soundService.PlayAsync(button.FilePath);
            return Results.Ok();
        });

        app.MapPost("/api/stop", () =>
        {
            soundService.Stop();
            return Results.Ok();
        });

        app.MapPost("/api/buttons", async (SoundButton button) =>
        {
            var created = await repository.AddAsync(button);
            return Results.Ok(created);
        });

        app.MapPut("/api/buttons/{id:guid}", async (Guid id, SoundButton button) =>
        {
            var updated = await repository.UpdateAsync(id, button);
            return updated ? Results.Ok() : Results.NotFound();
        });

        app.MapDelete("/api/buttons/{id:guid}", async (Guid id) =>
        {
            var deleted = await repository.DeleteAsync(id);
            return deleted ? Results.Ok() : Results.NotFound();
        });

        app.MapFallback(() => Results.Redirect("/index.html"));

        var webServerTask = Task.Run(() => app.Run());

        var mainForm = new MainForm(repository, soundService, settingsService);

        // Start background thread to listen for show window events
        var showWindowThread = new Thread(() => ListenForShowWindowEvent(mainForm))
        {
            IsBackground = true
        };
        showWindowThread.Start();

        Application.Run(mainForm);

        soundService.Dispose();
        Environment.Exit(0);
    }

    private static void SignalFirstInstance()
    {
        try
        {
            using var eventWaitHandle = EventWaitHandle.OpenExisting(EventName);
            eventWaitHandle.Set();
        }
        catch
        {
            // Event does not exist, first instance might not be ready yet
        }
    }

    private static void ListenForShowWindowEvent(MainForm mainForm)
    {
        try
        {
            using var eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);

            while (true)
            {
                eventWaitHandle.WaitOne();

                // Show the main form on the UI thread
                mainForm.Invoke(() => mainForm.BringToFront());
            }
        }
        catch
        {
            // Thread terminated
        }
    }
}

