using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
using System.Text;

#pragma warning disable OPENAI001

var configuration = new ConfigurationBuilder()
	.AddUserSecrets<Program>(optional: true)
	.AddEnvironmentVariables()
	.Build();

string? apiKey = configuration["OPENAI_API_KEY"];
if (string.IsNullOrWhiteSpace(apiKey))
{
	Console.Error.WriteLine("Missing OPENAI_API_KEY. Set it with 'dotnet user-secrets set OPENAI_API_KEY " + '"' + "<your-key>" + '"' + "'.");
	return;
}

string systemPromptPath = Path.Combine(AppContext.BaseDirectory, "system-prompt.md");
if (!File.Exists(systemPromptPath))
{
	Console.Error.WriteLine($"Missing system prompt file at '{systemPromptPath}'.");
	return;
}

string systemPrompt = await File.ReadAllTextAsync(systemPromptPath);
ResponsesClient client = new(apiKey: apiKey);
List<string> conversationTurns = [];

Console.WriteLine("Console coding chat");
Console.WriteLine("Type /exit to quit.");

while (true)
{
	Console.WriteLine();
	Console.Write("You: ");
	string? userInput = Console.ReadLine();

	if (userInput is null)
	{
		break;
	}

	if (string.Equals(userInput, "/exit", StringComparison.OrdinalIgnoreCase))
	{
		break;
	}

	if (string.IsNullOrWhiteSpace(userInput))
	{
		Console.WriteLine("Input cannot be empty.");
		continue;
	}

	if (userInput.Length > 5000)
	{
		Console.WriteLine("Input exceeds the 5000 character limit.");
		continue;
	}

	conversationTurns.Add($"User: {userInput}");
	string conversationTranscript = string.Join(Environment.NewLine + Environment.NewLine, conversationTurns);

	CreateResponseOptions options = new()
	{
		Model = "gpt-5.4",
		Instructions = systemPrompt,
		StoredOutputEnabled = false,
		StreamingEnabled = true,
	};
	options.InputItems.Add(ResponseItem.CreateUserMessageItem(conversationTranscript));

	Console.Write("Assistant: ");
	bool wroteOutput = false;
	StringBuilder assistantResponse = new();

	try
	{
		await foreach (StreamingResponseUpdate update in client.CreateResponseStreamingAsync(options))
		{
			if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
			{
				Console.Write(textDelta.Delta);
				assistantResponse.Append(textDelta.Delta);
				wroteOutput = true;
			}
		}

		if (!wroteOutput)
		{
			Console.Write("[no text returned]");
		}

		if (assistantResponse.Length > 0)
		{
			conversationTurns.Add($"Assistant: {assistantResponse}");
		}

		Console.WriteLine();
	}
	catch (Exception ex)
	{
		conversationTurns.RemoveAt(conversationTurns.Count - 1);
		Console.WriteLine();
		Console.Error.WriteLine($"Request failed: {ex.Message}");
	}
}
