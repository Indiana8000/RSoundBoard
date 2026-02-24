# Copilot Instructions

## Project Guidelines
- All code, comments, documentation, and project files in the RSoundBoard project must be written in English. Assistant responses to the user should remain in the language the user is speaking (e.g., German if user speaks German).

## Project Type
- **Framework:** .NET 8 (net8.0-windows)
- **Language:** C# 12
- **Application Type:** Windows Forms with ASP.NET Core Minimal API backend
- **Deployment:** Single-file self-contained executable (win-x64)

## Architecture & Patterns
- **Pattern:** Service-based architecture with dependency injection
- **Services:** Use constructor injection via `builder.Services.AddSingleton<T>()`
- **Repositories:** Use repository pattern for data access (e.g., `ButtonRepository`)
- **API:** Minimal API approach with `app.MapGet/MapPost`
- **Frontend:** Static files served from embedded resources

## Code Style
- **Nullable Reference Types:** Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings:** Enabled - avoid redundant using statements
- **Type Declaration:** Use `var` for local variables when type is obvious
- **String Initialization:** Use `string.Empty` instead of `""`
- **Guid:** Use `Guid.NewGuid()` for ID generation
- **Async/Await:** Use async methods with `Task` or `Task<T>` return types
- **Naming Conventions:**
  - Classes, Methods, Properties: `PascalCase`
  - Private fields: `_camelCase` with underscore prefix
  - Parameters, local variables: `camelCase`

## Specific Practices
- **Thread Safety:** Use `SemaphoreSlim` for async locking
- **Disposal:** Implement `IDisposable` for resources (audio players, streams)
- **Null Handling:** Use nullable types (`int?`, `IWavePlayer?`) and null-conditional operators
- **Configuration:** Use `SettingsService` for application settings
- **Audio:** NAudio library for sound playback and microphone input

## Dependencies
- **NAudio:** Audio playback and recording
- **Microsoft.Extensions.FileProviders.Embedded:** Embedded resource file provider
- Avoid adding new dependencies unless absolutely necessary

## Error Handling
- Return proper HTTP status codes in API endpoints (e.g., `Results.Ok()`, `Results.NotFound()`)
- Use try-catch blocks for resource-intensive operations (audio, file I/O)

## Comments & Documentation
- Minimize inline comments - code should be self-explanatory
- Add comments only for complex logic or non-obvious behavior
- Use XML documentation comments (`///`) for public APIs if needed