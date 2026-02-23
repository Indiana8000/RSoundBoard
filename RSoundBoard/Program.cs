using TestApp1.HostUI;
using TestApp1.Models;
using TestApp1.Services;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<ButtonRepository>();
        builder.Services.AddSingleton<SoundService>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        builder.WebHost.UseUrls("http://0.0.0.0:5000");

        var app = builder.Build();

        app.UseCors();
        app.UseStaticFiles();

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

        Application.Run(new MainForm(repository, soundService));

        soundService.Dispose();
        Environment.Exit(0);
    }
}

