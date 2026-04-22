using OpenAI.Responses;
using System.Text;

internal sealed class ReplRunner
{
	private readonly ResponsesClient _client;
	private readonly ConversationState _conversationState;
	private readonly TextReader _input;
	private readonly TextWriter _output;
	private readonly TextWriter _error;
	private readonly SlashCommandDispatcher _slashCommandDispatcher;
	private readonly string _systemPrompt;

	public ReplRunner(
		ResponsesClient client,
		string systemPrompt,
		ConversationState conversationState,
		SlashCommandDispatcher slashCommandDispatcher,
		TextReader input,
		TextWriter output,
		TextWriter error)
	{
		_client = client;
		_systemPrompt = systemPrompt;
		_conversationState = conversationState;
		_slashCommandDispatcher = slashCommandDispatcher;
		_input = input;
		_output = output;
		_error = error;
	}

	public async Task<int> RunAsync(CancellationToken cancellationToken)
	{
		await _output.WriteLineAsync("Console coding chat");
		await _output.WriteLineAsync("Type /exit to quit.");

		while (!cancellationToken.IsCancellationRequested)
		{
			await _output.WriteLineAsync();
			await _output.WriteAsync("You: ");

			string? userInput = await _input.ReadLineAsync();
			if (userInput is null)
			{
				return 0;
			}

			if (!PromptValidator.TryValidate(userInput, out string? validationError))
			{
				await _output.WriteLineAsync(validationError);
				continue;
			}

			SlashCommandDispatchResult slashCommand = _slashCommandDispatcher.Dispatch(userInput);
			if (slashCommand.IsCommand)
			{
				if (slashCommand.Message is not null)
				{
					await _output.WriteLineAsync(slashCommand.Message);
				}

				if (slashCommand.ShouldExit)
				{
					return 0;
				}

				continue;
			}

			string conversationTranscript = _conversationState.CreateTranscriptWithUserTurn(userInput);

			await _output.WriteAsync("Assistant: ");
			try
			{
				string assistantResponse = await StreamAssistantResponseAsync(conversationTranscript, cancellationToken);
				if (assistantResponse.Length > 0)
				{
					_conversationState.AddUserTurn(userInput);
					_conversationState.AddAssistantTurn(assistantResponse);
				}

				await _output.WriteLineAsync();
			}
			catch (Exception ex)
			{
				await _output.WriteLineAsync();
				await _error.WriteLineAsync($"Request failed: {ex.Message}");
			}
		}

		return 0;
	}

	private async Task<string> StreamAssistantResponseAsync(string conversationTranscript, CancellationToken cancellationToken)
	{
		_ = cancellationToken;

		CreateResponseOptions options = new()
		{
			Model = "gpt-5.4",
			Instructions = _systemPrompt,
			StoredOutputEnabled = false,
			StreamingEnabled = true,
		};
		options.InputItems.Add(ResponseItem.CreateUserMessageItem(conversationTranscript));

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

		return assistantResponse.ToString();
	}
}