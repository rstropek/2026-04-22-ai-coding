using Microsoft.Extensions.Configuration;

internal static class AppBootstrapper
{
	public static async Task<AppBootstrapResult> TryCreateAsync(CancellationToken cancellationToken)
	{
		IConfiguration configuration = new ConfigurationBuilder()
			.AddUserSecrets<Program>(optional: true)
			.AddEnvironmentVariables()
			.Build();

		string? apiKey = configuration["OPENAI_API_KEY"];
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return AppBootstrapResult.Failure(BootstrapErrorMessages.MissingApiKey);
		}

		string systemPromptPath = Path.Combine(AppContext.BaseDirectory, "system-prompt.md");
		if (!File.Exists(systemPromptPath))
		{
			return AppBootstrapResult.Failure(BootstrapErrorMessages.MissingSystemPrompt(systemPromptPath));
		}

		string systemPrompt = await File.ReadAllTextAsync(systemPromptPath, cancellationToken);
		return AppBootstrapResult.Succeeded(new AppConfiguration(apiKey, systemPrompt));
	}
}

internal sealed record AppConfiguration(string ApiKey, string SystemPrompt);

internal sealed record AppBootstrapResult(bool Success, AppConfiguration? Configuration, string? ErrorMessage)
{
	public static AppBootstrapResult Succeeded(AppConfiguration configuration) => new(true, configuration, null);

	public static AppBootstrapResult Failure(string errorMessage) => new(false, null, errorMessage);
}

internal static class BootstrapErrorMessages
{
	public const string MissingApiKey = "Missing OPENAI_API_KEY. Set it with 'dotnet user-secrets set OPENAI_API_KEY \"<your-key>\"'.";

	public static string MissingSystemPrompt(string path) => $"Missing system prompt file at '{path}'.";
}