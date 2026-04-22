internal sealed class SlashCommandDispatcher
{
	private readonly Dictionary<string, Func<SlashCommandDispatchResult>> _commands = new(StringComparer.OrdinalIgnoreCase)
	{
		["/exit"] = static () => SlashCommandDispatchResult.Exit(),
	};

	public SlashCommandDispatchResult Dispatch(string input)
	{
		if (!input.StartsWith("/", StringComparison.Ordinal))
		{
			return SlashCommandDispatchResult.NotACommand();
		}

		string commandName = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
		if (_commands.TryGetValue(commandName, out Func<SlashCommandDispatchResult>? handler))
		{
			return handler();
		}

		return SlashCommandDispatchResult.Unknown(commandName);
	}
}

internal readonly record struct SlashCommandDispatchResult(bool IsCommand, bool ShouldExit, string? Message)
{
	public static SlashCommandDispatchResult NotACommand() => new(false, false, null);

	public static SlashCommandDispatchResult Exit() => new(true, true, null);

	public static SlashCommandDispatchResult Unknown(string commandName) => new(true, false, $"Unknown slash command '{commandName}'.");
}