---
name: rag-eval
description: Run Samagra's RAG evaluation harness (Samagra.AiEval), optionally with different chunk size/overlap, and summarize retrieval, answer accuracy, latency and cost. Use when tuning chunking, changing retrieval/prompt code, or adding eval cases.
---

# RAG evaluation

`BackEnd/Samagra.AiEval` is a console app that runs every case in `testset.json` through `IVectorSearch` and `IAiAssistant.AskWithRagAsync` and prints a report.

## Prerequisites
- SQL Server, PostgreSQL+pgvector and the AI provider configured (it loads `appsettings.Development.json` / user secrets, same `Ai` and `ConnectionStrings` sections as the API).
- It makes real LLM calls and costs money with Azure OpenAI — say so before running, especially reindexes.

## Run

```bash
cd BackEnd/Samagra.AiEval
dotnet run                                    # evaluate the current index
dotnet run -- --chunk-size 300 --overlap 50   # clear the vector table, reindex EvalDocuments, then evaluate
dotnet run -- --reindex                       # reindex with defaults (500 / 50)
```

Run it from inside the project directory — it reads `testset.json` with a relative path.

Reindexing writes to the provider's active vector table (`document_chunks` for OpenAI, `document_chunks_768` for Ollama) and **clears** it first.

## Report back
- Config, retrieval rank-1 % and top-3 %, answer %, average latency, cost/tokens.
- For each failure: id, question, reason, distance.
- When comparing configs, show them side by side in a table and recommend one.

## Adding eval cases
Add to `testset.json`: `id`, `question`, `expectedSource` (substring of the document source, or `null`), `expectedFacts` (strings that must appear in the answer), and `shouldRefuse: true` for out-of-scope questions (the answer must contain "don't have" / "do not have"). Source documents are in `EvalDocuments.cs`.
