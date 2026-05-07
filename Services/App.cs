internal static class App
{
	internal const string McpServerFlag = "--mcp-server";

	public static async Task<int> MainAsync(string[] args)
	{
		EnvFileLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

		if (args.Contains(McpServerFlag, StringComparer.OrdinalIgnoreCase))
		{
			await McpServerHost.RunAsync(args);
			return 0;
		}

		try
		{
			await ClaudeCliApp.RunAsync(McpServerFlag);
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Fatal error: {ex.Message}");
			return 1;
		}
	}
}