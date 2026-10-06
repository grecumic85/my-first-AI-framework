using Microsoft.Extensions.Configuration;

namespace Automation.Core;

public sealed class AutomationSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5080/";
    public string ApiBaseUrl { get; set; } = "http://localhost:5080/";
    public string ArtifactsPath { get; set; } = "artifacts";
    public BrowserSettings Browser { get; set; } = new();
    public WaitSettings Wait { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();

    public static AutomationSettings Load(string directory)
    {
        var environment = Environment.GetEnvironmentVariable("AUTOMATION_ENVIRONMENT") ?? "local";
        if (environment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || environment.Contains(".."))
            throw new ArgumentException("Invalid AUTOMATION_ENVIRONMENT.");
        var configuration = new ConfigurationBuilder().SetBasePath(Path.GetFullPath(directory))
            .AddJsonFile("settings.json", optional: true)
            .AddJsonFile($"settings.{environment}.json", optional: true)
            .AddJsonFile("settings.local.json", optional: true)
            .AddEnvironmentVariables("AUTOMATION_").Build();
        var settings = configuration.Get<AutomationSettings>() ?? new();
        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        ValidateUri(BaseUrl, nameof(BaseUrl));
        ValidateUri(ApiBaseUrl, nameof(ApiBaseUrl));
        if (!string.IsNullOrWhiteSpace(Browser.GridUrl)) ValidateUri(Browser.GridUrl, "Browser.GridUrl");
        if (!new[] { "chrome", "firefox", "edge" }.Contains(Browser.Name.ToLowerInvariant()))
            throw new ArgumentException("Browser.Name must be chrome, firefox or edge.");
        if (Wait.TimeoutSeconds <= 0 || Wait.PollMilliseconds <= 0 || Wait.StableMilliseconds <= 0)
            throw new ArgumentException("Wait durations must be positive.");
        if (Browser.CommandTimeoutSeconds <= 0) throw new ArgumentException("Command timeout must be positive.");
        if (Auth.Mode is not ("None" or "StaticBearer" or "ClientCredentials"))
            throw new ArgumentException("Unsupported Auth.Mode.");
        if (Auth.Mode == "StaticBearer" && string.IsNullOrWhiteSpace(Auth.Token))
            throw new ArgumentException("StaticBearer requires Auth.Token.");
        if (Auth.Mode == "ClientCredentials")
        {
            ValidateUri(Auth.TokenEndpoint, "Auth.TokenEndpoint");
            if (string.IsNullOrWhiteSpace(Auth.ClientId) || string.IsNullOrWhiteSpace(Auth.ClientSecret))
                throw new ArgumentException("ClientCredentials requires client ID and secret.");
        }
    }

    private static void ValidateUri(string value, string name)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException($"{name} must be an absolute HTTP(S) URL.");
        if (!string.IsNullOrEmpty(uri.UserInfo)) throw new ArgumentException($"{name} must not contain credentials.");
        if (!uri.AbsolutePath.EndsWith('/') && name is "BaseUrl" or "ApiBaseUrl")
            throw new ArgumentException($"{name} must end in / to preserve base paths.");
    }
}

public sealed class BrowserSettings
{
    public string Name { get; set; } = "chrome";
    public bool Headless { get; set; } = true;
    public string? GridUrl { get; set; }
    public int CommandTimeoutSeconds { get; set; } = 120;
}
public sealed class WaitSettings
{
    public double TimeoutSeconds { get; set; } = 30;
    public int PollMilliseconds { get; set; } = 100;
    public int StableMilliseconds { get; set; } = 300;
}
public sealed class AuthSettings
{
    public string Mode { get; set; } = "None";
    public string Token { get; set; } = "";
    public string TokenEndpoint { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "";
}
public sealed class DatabaseSettings
{
    public string Provider { get; set; } = "PostgreSql";
    public string ConnectionString { get; set; } = "";
    public bool AllowWrites { get; set; }
}
