using OpenAI.Responses;

internal sealed class AppRuntime(AppConfiguration configuration)
{
	public ResponsesClient Client { get; } = new(apiKey: configuration.ApiKey);

	public string SystemPrompt { get; } = configuration.SystemPrompt;
}