internal sealed class ConversationState
{
	private readonly List<ConversationTurn> _turns = [];

	public string CreateTranscript() => string.Join(Environment.NewLine + Environment.NewLine, _turns.Select(turn => turn.ToTranscriptLine()));

	public string CreateTranscriptWithUserTurn(string userInput)
	{
		if (_turns.Count == 0)
		{
			return ConversationTurn.User(userInput).ToTranscriptLine();
		}

		return string.Join(
			Environment.NewLine + Environment.NewLine,
			_turns.Select(turn => turn.ToTranscriptLine()).Append(ConversationTurn.User(userInput).ToTranscriptLine()));
	}

	public void AddUserTurn(string content) => _turns.Add(ConversationTurn.User(content));

	public void AddAssistantTurn(string content) => _turns.Add(ConversationTurn.Assistant(content));

	private readonly record struct ConversationTurn(string Role, string Content)
	{
		public static ConversationTurn User(string content) => new("User", content);

		public static ConversationTurn Assistant(string content) => new("Assistant", content);

		public string ToTranscriptLine() => $"{Role}: {Content}";
	}
}