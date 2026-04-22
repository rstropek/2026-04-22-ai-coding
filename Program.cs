#pragma warning disable OPENAI001

AppBootstrapResult bootstrapResult = await AppBootstrapper.TryCreateAsync(CancellationToken.None);
if (!bootstrapResult.Success)
{
	Console.Error.WriteLine(bootstrapResult.ErrorMessage);
	return 1;
}

AppRuntime runtime = new(bootstrapResult.Configuration!);
return await AppCli.InvokeAsync(args, runtime);
