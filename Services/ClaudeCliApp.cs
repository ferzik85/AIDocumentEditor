using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ClaudeCliApp
{
	public static async Task RunAsync(string mcpServerFlag)
	{
		var model = RequireEnvironmentVariable("CLAUDE_MODEL");
		_ = RequireEnvironmentVariable("ANTHROPIC_API_KEY");
		using var anthropicClient = new AnthropicClient();

		using var cts = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			cts.Cancel();
		};

		var transport = CreateLocalServerTransport(mcpServerFlag);
		await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
		var tools = (await mcpClient.ListToolsAsync(cancellationToken: cts.Token)).ToList();

		var docs = await DocumentContextService.GetDocumentIdsAsync(mcpClient, cts.Token);
		var prompts = await mcpClient.ListPromptsAsync(cancellationToken: cts.Token);

		Console.WriteLine("AIDocumentEditor (.NET 10)");
		Console.WriteLine($"Model: {model}");
		Console.WriteLine($"Documents: {string.Join(", ", docs)}");
		Console.WriteLine($"Prompts: {string.Join(", ", prompts.Select(prompt => "/" + prompt.Name))}");
		Console.WriteLine("Type a message, use @docId to inline a document, or /format <docId> and /summarize <docId>. Type 'exit' to quit.");

		List<Message> messages = [];
		var parameters = new MessageParameters
		{
			Messages = messages,
			Model = model,
			MaxTokens = 4096,
			Stream = false,
			Temperature = 1.0m,
			Tools = tools.Select(ConvertTool).ToList(),
			ToolChoice = new Anthropic.SDK.Messaging.ToolChoice { Type = ToolChoiceType.Auto }
		};

		while (!cts.IsCancellationRequested)
		{
			Console.Write("> ");
			var input = Console.ReadLine();

			if (input is null)
			{
				break;
			}

			if (string.IsNullOrWhiteSpace(input))
			{
				continue;
			}

			if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
			{
				break;
			}

			if (string.Equals(input, "help", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Commands: /format <docId>, /summarize <docId>, help, exit");
				continue;
			}

			try
			{
				await DocumentContextService.AddUserTurnAsync(messages, mcpClient, input, cts.Token);
				var response = await CompleteTurnAsync(anthropicClient, mcpClient, parameters, cts.Token);

				Console.WriteLine();
				Console.WriteLine("Response:");
				Console.WriteLine(response);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error: {ex.Message}");
			}
		}
	}

	private static async Task<string> CompleteTurnAsync(
		AnthropicClient anthropicClient,
		McpClient mcpClient,
		MessageParameters parameters,
		CancellationToken cancellationToken)
	{
		while (true)
		{
			var response = await anthropicClient.Messages.GetClaudeMessageAsync(parameters, cancellationToken);
			parameters.Messages.Add(response.Message);

			if (response.ToolCalls.Count == 0)
			{
				return ExtractResponseText(response);
			}

			foreach (var toolCall in response.ToolCalls)
			{
				var resultMessage = await InvokeToolAsync(mcpClient, toolCall, cancellationToken);
				parameters.Messages.Add(resultMessage);
			}
		}
	}

	private static async Task<Message> InvokeToolAsync(McpClient mcpClient, Function toolCall, CancellationToken cancellationToken)
	{
		try
		{
			var arguments = toolCall.Arguments is null
				? null
				: JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.Arguments.ToJsonString());

			var result = await mcpClient.CallToolAsync(
				toolCall.Name,
				arguments,
				cancellationToken: cancellationToken);

			return new Message(toolCall, DocumentContextService.ExtractText(result.Content));
		}
		catch (Exception ex)
		{
			return new Message(toolCall, ex.Message, isError: true);
		}
	}

	private static Anthropic.SDK.Common.Tool ConvertTool(McpClientTool tool)
	{
		return new Anthropic.SDK.Common.Tool(
			new Function(
				tool.Name,
				tool.Description ?? string.Empty,
				JsonNode.Parse(tool.JsonSchema.GetRawText())));
	}

	private static string ExtractResponseText(MessageResponse response)
	{
		var text = string.Concat(response.Content.OfType<TextContent>().Select(content => content.Text));
		return string.IsNullOrWhiteSpace(text) ? response.Message.ToString() : text.Trim();
	}

	private static StdioClientTransport CreateLocalServerTransport(string mcpServerFlag)
	{
		var processPath = Environment.ProcessPath;
		if (string.IsNullOrWhiteSpace(processPath))
		{
			throw new InvalidOperationException("Could not determine the current executable path.");
		}

		var command = processPath;
		IList<string> arguments = [mcpServerFlag];

		if (string.Equals(Path.GetExtension(processPath), ".dll", StringComparison.OrdinalIgnoreCase))
		{
			command = "dotnet";
			arguments = [processPath, mcpServerFlag];
		}

		return new StdioClientTransport(new StdioClientTransportOptions
		{
			Name = "DocumentMcpServer",
			Command = command,
			Arguments = arguments,
			WorkingDirectory = Directory.GetCurrentDirectory()
		});
	}

	private static string RequireEnvironmentVariable(string name)
	{
		var value = Environment.GetEnvironmentVariable(name);
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new InvalidOperationException($"Environment variable '{name}' must be set. See .env.example.");
		}

		return value;
	}
}