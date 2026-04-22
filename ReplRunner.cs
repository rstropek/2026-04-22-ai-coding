using OpenAI.Responses;
using System.Text;

internal sealed class ReplRunner(
	ResponsesClient client,
	string systemPrompt,
	ConversationState conversationState,
	SlashCommandDispatcher slashCommandDispatcher,
	TextReader input,
	TextWriter output,
	TextWriter error)
{

	public async Task<int> RunAsync(CancellationToken cancellationToken)
	{
		await output.WriteLineAsync("Console coding chat");
		await output.WriteLineAsync("Type /exit to quit.");

		while (!cancellationToken.IsCancellationRequested)
		{
			await output.WriteLineAsync();
			await output.WriteAsync("You: ");

			string? userInput = await input.ReadLineAsync();
			if (userInput is null)
			{
				return 0;
			}

			if (!PromptValidator.TryValidate(userInput, out string? validationError))
			{
				await output.WriteLineAsync(validationError);
				continue;
			}

			SlashCommandDispatchResult slashCommand = slashCommandDispatcher.Dispatch(userInput);
			if (slashCommand.IsCommand)
			{
				if (slashCommand.Message is not null)
				{
					await output.WriteLineAsync(slashCommand.Message);
				}

				if (slashCommand.ShouldExit)
				{
					return 0;
				}

				continue;
			}

			string conversationTranscript = conversationState.CreateTranscriptWithUserTurn(userInput);

			await output.WriteAsync("Assistant: ");
			try
			{
				string assistantResponse = await StreamAssistantResponseAsync(conversationTranscript, cancellationToken);
				if (assistantResponse.Length > 0)
				{
					conversationState.AddUserTurn(userInput);
					conversationState.AddAssistantTurn(assistantResponse);
				}

				await output.WriteLineAsync();
			}
			catch (Exception ex)
			{
				await output.WriteLineAsync();
				await error.WriteLineAsync($"Request failed: {ex.Message}");
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
			Instructions = systemPrompt,
			StoredOutputEnabled = false,
			StreamingEnabled = true,
		};
		options.InputItems.Add(ResponseItem.CreateUserMessageItem(conversationTranscript));

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

		return assistantResponse.ToString();
	}
}