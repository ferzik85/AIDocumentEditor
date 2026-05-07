internal static class DocumentStore
{
	private static readonly Dictionary<string, string> Documents = new(StringComparer.OrdinalIgnoreCase)
	{
		["deposition.md"] = "This deposition covers the testimony of Angela Smith, P.E.",
		["report.pdf"] = "The report details the state of a 20m condenser tower.",
		["financials.docx"] = "These financials outline the project's budget and expenditures.",
		["outlook.pdf"] = "This document presents the projected future performance of the system.",
		["plan.md"] = "The plan outlines the steps for the project's implementation.",
		["spec.txt"] = "These specifications define the technical requirements for the equipment."
	};

	public static IReadOnlyList<string> ListIds() => Documents.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();

	public static string Read(string docId)
	{
		if (!Documents.TryGetValue(docId, out var content))
		{
			throw new InvalidOperationException($"Document '{docId}' was not found.");
		}

		return content;
	}

	public static string Replace(string docId, string oldText, string newText)
	{
		var current = Read(docId);
		if (!current.Contains(oldText, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("The exact text to replace was not found in the document.");
		}

		Documents[docId] = current.Replace(oldText, newText, StringComparison.Ordinal);
		return Documents[docId];
	}
}