using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerPromptType]
internal sealed class DocumentPrompts
{
	[McpServerPrompt(Name = "format"), Description("Rewrites the contents of the document in Markdown format.")]
	public static IEnumerable<ChatMessage> FormatDocument([Description("Id of the document to format")] string docId)
	{
		var prompt = $"""
			Your goal is to reformat a document to be written with markdown syntax.

			The id of the document you need to reformat is:
			<document_id>
			{docId}
			</document_id>

			Add headers, bullet points, tables, and structure as needed. Feel free to add a little connective text, but do not change the meaning of the document.
			Use the 'edit_document' tool to edit the document. After the document has been edited, respond with the final version of the doc. Don't explain your changes.
			""";

		return [new ChatMessage(ChatRole.User, prompt)];
	}

	[McpServerPrompt(Name = "summarize"), Description("Summarizes the contents of a document.")]
	public static IEnumerable<ChatMessage> SummarizeDocument([Description("Id of the document to summarize")] string docId)
	{
		var prompt = $"""
			Summarize the document with id '{docId}'.
			If needed, use the 'read_doc_contents' tool first.
			Provide a concise summary with the main point first.
			""";

		return [new ChatMessage(ChatRole.User, prompt)];
	}
}