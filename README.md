# AIDocumentEditor (.NET 10)

This project is a .NET 10 console port of the Python `cli_project_COMPLETE` sample. 
It starts an in-process console chat client and launches a local MCP stdio server in a child process using the same executable.

## Features

- Interactive Claude chat in the terminal
- `@documentId` inline document expansion
- MCP-backed document tools
- `/format <docId>` and `/summarize <docId>` prompt commands
- Local MCP server mode via `--mcp-server`

## Requirements

- .NET SDK 10
- Anthropic API key

## Configuration

Create a `.env` file in the project directory based on `.env.example`.

Required values:

```env
ANTHROPIC_API_KEY=your_api_key_here
CLAUDE_MODEL=claude-haiku-4-5
CLAUDE_MODEL=claude-sonnet-4-6 (more capabilities)
```

## Run

```powershell
dotnet run --project AIDocumentEditor.csproj -- --mcp-server
dotnet run --project AIDocumentEditor.csproj
```

Examples:

```text
> Tell me about @deposition.md
> /format plan.md
> /summarize report.pdf
```

To stop the chat, type `exit` or press `Ctrl+C`.

## Notes

- The local MCP server is launched automatically by the client.
- The document store is in-memory, matching the Python sample's behavior.
- If Claude edits a document through `edit_document`, the change persists for the lifetime of the chat session.