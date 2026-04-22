using OpenAI.Responses;
using System.Text;

internal sealed class OneShotRunner
{
	private readonly ResponsesClient _client;
	private readonly TextWriter _error;
	private readonly TextWriter _output;
	private readonly string _systemPrompt;

	public OneShotRunner(ResponsesClient client, string systemPrompt, TextWriter output, TextWriter error)
	{
		_client = client;
		_systemPrompt = systemPrompt;
		_output = output;
		_error = error;
	}

	public async Task<int> RunAsync(string? prompt, CancellationToken cancellationToken)
	{
		if (!PromptValidator.TryValidate(prompt, out string? validationError))
		{
			await _error.WriteLineAsync(validationError);
			return 1;
		}

		try
		{
			await StreamAssistantResponseAsync(prompt!, cancellationToken);
			await _output.WriteLineAsync();
			return 0;
		}
		catch (Exception ex)
		{
			await _error.WriteLineAsync($"Request failed: {ex.Message}");
			return 1;
		}
	}

	private async Task StreamAssistantResponseAsync(string prompt, CancellationToken cancellationToken)
	{
		_ = cancellationToken;

		CreateResponseOptions options = new()
		{
			Model = "gpt-5.4",
			Instructions = _systemPrompt,
			StoredOutputEnabled = false,
			StreamingEnabled = true,
		};
		options.InputItems.Add(ResponseItem.CreateUserMessageItem(prompt));

		bool wroteOutput = false;
		StringBuilder assistantResponse = new();

		await foreach (StreamingResponseUpdate update in _client.CreateResponseStreamingAsync(options))
		{
			if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
			{
				await _output.WriteAsync(textDelta.Delta);
				assistantResponse.Append(textDelta.Delta);
				wroteOutput = true;
			}
		}

		if (!wroteOutput)
		{
			await _output.WriteAsync("[no text returned]");
		}
	}
}