using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

[McpServerResourceType]
internal sealed class DocumentResources
{
	private static readonly JsonSerializerOptions ResourceJsonOptions = new(JsonSerializerDefaults.Web);

	[McpServerResource(UriTemplate = "docs://documents"), Description("Returns all available document ids as JSON.")]
	public static string ListDocuments()
		=> JsonSerializer.Serialize(DocumentStore.ListIds(), ResourceJsonOptions);

	[McpServerResource(UriTemplate = "docs://documents/{docId}"), Description("Returns the full contents of a document.")]
	public static string ReadDocument(string docId)
		=> DocumentStore.Read(docId);
}