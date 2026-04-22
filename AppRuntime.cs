using OpenAI.Responses;

internal sealed class AppRuntime
{
	public AppRuntime(AppConfiguration configuration)
	{
		Client = new ResponsesClient(apiKey: configuration.ApiKey);
		SystemPrompt = configuration.SystemPrompt;
	}

	public ResponsesClient Client { get; }

	public string SystemPrompt { get; }
}