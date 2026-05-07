using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static partial class DocumentContextService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = false
	};

	public static async Task AddUserTurnAsync(
		List<Anthropic.SDK.Messaging.Message> messages,
		McpClient mcpClient,
		string input,
		CancellationToken cancellationToken)
	{
		if (input.StartsWith("/", StringComparison.Ordinal))
		{
			await AddPromptMessagesAsync(messages, mcpClient, input, cancellationToken);
			return;
		}

		var context = await BuildDocumentContextAsync(mcpClient, input, cancellationToken);
		var prompt = $"""
			The user has a question:
			<query>
			{input}
			</query>

			The following context may be useful in answering their question:
			<context>
			{context}
			</context>

			Note the user's query might contain references to documents like "@report.pdf". The "@" is only
			included as a way of mentioning the doc. The actual name of the document would be "report.pdf".
			If the document content is included in this prompt, you don't need to use an additional tool to read it.
			Answer the user's question directly and concisely. Start with the exact information they need.
			Don't refer to or mention the provided context in any way; just use it to inform your answer.
			""";

		messages.Add(new Anthropic.SDK.Messaging.Message(Anthropic.SDK.Messaging.RoleType.User, prompt));
	}

	public static async Task<List<string>> GetDocumentIdsAsync(McpClient mcpClient, CancellationToken cancellationToken)
	{
		var resource = await mcpClient.ReadResourceAsync("docs://documents", cancellationToken: cancellationToken);
		var json = ExtractText(resource.Contents);

		return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
	}

	private static async Task AddPromptMessagesAsync(
		List<Anthropic.SDK.Messaging.Message> messages,
		McpClient mcpClient,
		string input,
		CancellationToken cancellationToken)
	{
		var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length < 2)
		{
			throw new InvalidOperationException("Commands require a document id. Example: /format deposition.md");
		}

		var promptName = parts[0][1..];
		var docId = parts[1];
		_ = await mcpClient.GetPromptAsync(
			promptName,
			new Dictionary<string, object?> { ["docId"] = docId },
			cancellationToken: cancellationToken);

		var promptText = promptName switch
		{
			"format" => $"Please format the document '{docId}'. Read it first, then return an improved formatted version and explain the changes briefly.",
			"summarize" => $"Please summarize the document '{docId}'. Read it first, then provide a concise, useful summary.",
			_ => throw new InvalidOperationException($"Unknown prompt '/{promptName}'.")
		};

		messages.Add(new Anthropic.SDK.Messaging.Message(Anthropic.SDK.Messaging.RoleType.User, promptText));
	}

	private static async Task<string> BuildDocumentContextAsync(
		McpClient mcpClient,
		string input,
		CancellationToken cancellationToken)
	{
		var mentions = MentionPattern()
			.Matches(input)
			.Select(match => match.Groups[1].Value)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (mentions.Count == 0)
		{
			return string.Empty;
		}

		var availableDocs = await GetDocumentIdsAsync(mcpClient, cancellationToken);
		var builder = new StringBuilder();

		foreach (var docId in availableDocs)
		{
			if (!mentions.Contains(docId, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			var content = await GetDocumentContentAsync(mcpClient, docId, cancellationToken);
			builder
				.AppendLine($"<document id=\"{docId}\">")
				.AppendLine(content)
				.AppendLine("</document>");
		}

		return builder.ToString();
	}

	private static async Task<string> GetDocumentContentAsync(McpClient mcpClient, string docId, CancellationToken cancellationToken)
	{
		var resource = await mcpClient.ReadResourceAsync($"docs://documents/{docId}", cancellationToken: cancellationToken);
		return ExtractText(resource.Contents);
	}

	public static string ExtractText(IEnumerable<object?> contents)
	{
		var text = string.Concat(contents.Select(ExtractSingleText));
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("The MCP resource did not return text content.");
		}

		return text;
	}

	private static string ExtractSingleText(object? content)
	{
		return content switch
		{
			TextResourceContents textResource => textResource.Text ?? string.Empty,
			BlobResourceContents blobResource => Encoding.UTF8.GetString(blobResource.Blob.Span),
			CallToolResult toolResult => string.Concat(toolResult.Content.Select(ExtractSingleText)),
			TextContentBlock textBlock => textBlock.Text ?? string.Empty,
			ImageContentBlock _ => string.Empty,
			null => string.Empty,
			_ => content.ToString() ?? string.Empty
		};
	}

	[GeneratedRegex("@([A-Za-z0-9._-]+)", RegexOptions.CultureInvariant)]
	private static partial Regex MentionPattern();
}