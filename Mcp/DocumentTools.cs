using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
internal sealed class DocumentTools
{
	[McpServerTool(Name = "read_doc_contents", ReadOnly = true, Idempotent = true, OpenWorld = false), Description("Read the contents of a document and return it as a string.")]
	public static string ReadDocument([Description("Id of the document to read")] string docId)
		=> DocumentStore.Read(docId);

	[McpServerTool(Name = "edit_document", ReadOnly = false, Idempotent = false, OpenWorld = false), Description("Edit a document by replacing a string in the document content with a new string.")]
	public static string EditDocument(
		[Description("Id of the document that will be edited")] string docId,
		[Description("The text to replace. Must match exactly, including whitespace.")] string oldText,
		[Description("The new text to insert in place of the old text.")] string newText)
		=> DocumentStore.Replace(docId, oldText, newText);
}