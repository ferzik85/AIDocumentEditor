using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal static class McpServerHost
{
	public static async Task RunAsync(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);

		builder.Logging.AddConsole(options =>
		{
			options.LogToStandardErrorThreshold = LogLevel.Trace;
		});

		builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithTools<DocumentTools>()
			.WithResources<DocumentResources>()
			.WithPrompts<DocumentPrompts>();

		await builder.Build().RunAsync();
	}
}