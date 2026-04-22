Create a simple chat app in the console:

* Use the OpenAI API with the .NET OpenAI SDK
* Use the `gpt-5.4` model (not a mistake, this is the latest model)
* Take the API key from the setting OPENAI_API_KEY (dotnet user secret)
* Suggest a system prompt for a simple coding agent
  * Store the system prompt in `system-prompt.md`
  * Make sure that the system prompt file is copied to the output directory on build
* Loop:
  * Ask user for input
    * Quit if the user inputs `/exit`
    * Limit the length of the user input to 5000 characters
  * Send the input to the OpenAI API, use the **Responses API**
  * Set the parameter for storing to `false` to avoid storing the conversation
  * Print the response to the console, use streaming results
