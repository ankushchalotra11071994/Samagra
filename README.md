# Samagra

A full-stack e-commerce platform built to explore production patterns in .NET, 
with an AI layer integrated into the existing architecture rather than bolted on.

## Tech Stack
- **Backend:** .NET 10, ASP.NET Core Web API, Clean Architecture (Domain / Application / Infrastructure / API)
- **Frontend:** React
- **Data:** SQL Server (EF Core), PostgreSQL + pgvector
- **Caching & Resilience:** Redis, rate limiting
- **Auth:** JWT with ASP.NET Identity
- **Observability:** Application Insights
- **AI:** Azure OpenAI (gpt-4.1-mini, text-embedding-3-small), Microsoft.Extensions.AI
- **CI:** GitHub Actions

## AI Assistant (Samagra.AI)
A separate project that plugs into the clean architecture through interfaces 
defined in the Application layer — the API never references Azure SDKs directly.

- **Chat** via `IChatClient`, provider-agnostic
- **RAG pipeline:** documents are chunked with overlap, embedded, and stored in pgvector
- **Vector search** using cosine distance, with a relevance threshold so unrelated 
  chunks never reach the model
- **Grounded answers:** the model answers only from retrieved context, cites its 
  source, and says "I don't have that information" when context is missing

## Local Setup
1. `docker compose up -d` (SQL Server, PostgreSQL + pgvector)
2. Copy `appsettings.Development.json.example` → `appsettings.Development.json` and fill in your Azure OpenAI endpoint and key
3. `dotnet run --project BackEnd/Samagra.API`
