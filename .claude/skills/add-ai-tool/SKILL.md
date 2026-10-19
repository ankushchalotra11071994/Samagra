---
name: add-ai-tool
description: Add a new LLM tool (function calling) to Samagra's AI assistant and expose it over MCP. Use when the user wants the assistant/agent to be able to look up or do something new.
---

# Add an AI tool

A tool in this repo has three parts that must stay in sync:

1. **Implementation** — a method on a class in `BackEnd/Samagra.AI/Tools/` (e.g. `OrderTools`, `PolicyTools`).
2. **Function-calling registration** — an `AIFunctionFactory.Create(...)` entry in `Samagra.AI/Tools/ToolFactory.cs`, used by `AiAssistant` (`/api/ai/ask-tools`, streaming).
3. **MCP wrapper** — a `[McpServerTool]` method in `Samagra.AI/MCP/*McpTools.cs`, used by `SupportAgent` (which discovers tools through `/mcp`).

## Steps

1. Ask what the tool should do if it's unclear, and pick a `snake_case` tool name.
2. Add the method to an existing tools class or create a new one.
   - If it touches user data, get the user id from `IHttpContextAccessor` like `OrderTools.CurrentUserId` does. **Never take a user id as a tool parameter**, and filter every query by it — the model must not be able to read another customer's data.
   - Return `Task<object>`; on failure return `new { error = "..." }`, on empty results return `new { message = "..." }` instead of throwing.
   - Validate and clamp model-supplied arguments (`Guid.TryParse`, `Math.Clamp`).
   - Use `AsNoTracking()` and project to an anonymous type with only the fields the model needs.
3. Register it in `ToolFactory.CreateAll` with the same name and a description that says *when* to use it (the model reads it to choose).
4. Add the matching `[McpServerTool(Name = "...")]` + `[Description]` method in the MCP wrapper, delegating to the tools class. Use the same name and description text.
5. If you created a new class:
   - `services.AddScoped<NewTools>()` in `Samagra.AI/DependencyInjection.cs`
   - Add it to `ToolFactory.CreateAll`'s parameters and to `AiAssistant`'s constructor
   - New MCP class → `.WithTools<Samagra.AI.Mcp.NewMcpTools>()` in `Samagra.API/Program.cs`
6. `cd BackEnd && dotnet build`, and report anything that can't be verified without a running AI provider.
