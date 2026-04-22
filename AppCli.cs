using System.CommandLine;

internal static class AppCli
{
	public static Task<int> InvokeAsync(string[] args, AppRuntime runtime)
	{
		ConversationState conversationState = new();
		SlashCommandDispatcher slashCommandDispatcher = new();
		ReplRunner replRunner = new(runtime.Client, runtime.SystemPrompt, conversationState, slashCommandDispatcher, Console.In, Console.Out, Console.Error);
		OneShotRunner oneShotRunner = new(runtime.Client, runtime.SystemPrompt, Console.Out, Console.Error);

		Argument<string> promptArgument = new("prompt")
		{
			Description = "The prompt to send once before exiting.",
		};

		Command runCommand = new("run", "Send a single prompt and exit.");
		runCommand.Arguments.Add(promptArgument);
		runCommand.SetAction((parseResult, token) => oneShotRunner.RunAsync(parseResult.GetValue(promptArgument), token));

		RootCommand rootCommand = new("Console coding chat")
		{
			runCommand,
		};
		rootCommand.SetAction((_, token) => replRunner.RunAsync(token));

		return rootCommand.Parse(args).InvokeAsync();
	}
}