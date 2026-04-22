using OpenAI.Responses;
using System.Text;

internal sealed class OneShotRunner(ResponsesClient client, string systemPrompt, TextWriter output, TextWriter error)
{
	public async Task<int> RunAsync(string? prompt, CancellationToken cancellationToken)
	{
		if (!PromptValidator.TryValidate(prompt, out string? validationError))
		{
			await error.WriteLineAsync(validationError);
			return 1;
		}

		try
		{
			await StreamAssistantResponseAsync(prompt!, cancellationToken);
			await output.WriteLineAsync();
			return 0;
		}
		catch (Exception ex)
		{
			await error.WriteLineAsync($"Request failed: {ex.Message}");
			return 1;
		}
	}

	private async Task StreamAssistantResponseAsync(string prompt, CancellationToken cancellationToken)
	{
		_ = cancellationToken;

		CreateResponseOptions options = new()
		{
			Model = "gpt-5.4",
			Instructions = systemPrompt,
			StoredOutputEnabled = false,
			StreamingEnabled = true,
		};
		options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

		bool wroteOutput = false;
		StringBuilder assistantResponse = new();

		await foreach (StreamingResponseUpdate update in client.CreateResponseStreamingAsync(options))
		{
			if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
			{
				await output.WriteAsync(textDelta.Delta);
				assistantResponse.Append(textDelta.Delta);
				wroteOutput = true;
			}
		}

		if (!wroteOutput)
		{
			await output.WriteAsync("[no text returned]");
		}
	}
}