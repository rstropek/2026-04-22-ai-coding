internal static class PromptValidator
{
	public const int MaxLength = 5000;

	public static bool TryValidate(string? input, out string? errorMessage)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			errorMessage = "Input cannot be empty.";
			return false;
		}

		if (input.Length > MaxLength)
		{
			errorMessage = $"Input exceeds the {MaxLength} character limit.";
			return false;
		}

		errorMessage = null;
		return true;
	}
}