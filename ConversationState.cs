using OpenAI.Responses;

internal sealed class ConversationState
{
	private readonly List<ConversationTurn> _turns = [];

	public IEnumerable<ResponseItem> CreateInputItems()
	{
		foreach (ConversationTurn turn in _turns)
		{
			yield return turn.ToResponseItem();
		}
	}

	public IEnumerable<ResponseItem> CreateInputItemsWithUserTurn(string userInput)
	{
		foreach (ResponseItem item in CreateInputItems())
		{
			yield return item;
		}

		yield return ResponseItem.CreateUserMessageItem(userInput);
	}

	public void AddUserTurn(string content) => _turns.Add(ConversationTurn.User(content));

	public void AddAssistantTurn(string content) => _turns.Add(ConversationTurn.Assistant(content));

	private readonly record struct ConversationTurn(ConversationRole Role, string Content)
	{
		public static ConversationTurn User(string content) => new(ConversationRole.User, content);

		public static ConversationTurn Assistant(string content) => new(ConversationRole.Assistant, content);

		public ResponseItem ToResponseItem() => Role switch
		{
			ConversationRole.User => ResponseItem.CreateUserMessageItem(Content),
			ConversationRole.Assistant => ResponseItem.CreateAssistantMessageItem(Content),
			_ => throw new InvalidOperationException($"Unsupported conversation role '{Role}'.")
		};
	}

	private enum ConversationRole
	{
		User,
		Assistant,
	}
}