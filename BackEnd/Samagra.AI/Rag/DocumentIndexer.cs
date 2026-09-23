using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;
using Samagra.AI.Middleware;
using Samagra.Application.Interfaces;

namespace Samagra.AI.Rag;

internal sealed class DocumentIndexer : IDocumentIndexer
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly string _connectionString;

    public DocumentIndexer(
        IEmbeddingGenerator<string, Embedding<float>> embedder,
        string connectionString)
    {
        _embedder = embedder;
        _connectionString = connectionString;
    }

    public async Task<int> IndexAsync(
        string source,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        CancellationToken ct = default)
    {
        var chunks = TextChunker.Chunk(content, chunkSize, overlap);

        var options = new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { [CostTrackingChatClient.FeatureKey] = "indexing" }
        };

        var embeddings = await _embedder.GenerateAsync(chunks, options, ct);

        await using var conn = await OpenAsync(ct);

        for (var i = 0; i < chunks.Count; i++)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO document_chunks (source, content, embedding) VALUES ($1, $2, $3)",
                conn);

            cmd.Parameters.AddWithValue(source);
            cmd.Parameters.AddWithValue(chunks[i]);
            cmd.Parameters.AddWithValue(new Vector(embeddings[i].Vector));

            await cmd.ExecuteNonQueryAsync(ct);
        }

        return chunks.Count;
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("DELETE FROM document_chunks", conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var builder = new NpgsqlDataSourceBuilder(_connectionString);
        builder.UseVector();
        var dataSource = builder.Build();
        return await dataSource.OpenConnectionAsync(ct);
    }
}