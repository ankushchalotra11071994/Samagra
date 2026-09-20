using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;
using Samagra.Application.Interfaces;

namespace Samagra.AI.Rag;

internal sealed class DocumentIndexer : IDocumentIndexer
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly string _connectionString;

    public DocumentIndexer(  IEmbeddingGenerator<string, Embedding<float>> embedder,
        string connectionString)
    {
        _embedder = embedder;
        _connectionString = connectionString;
    }

 public async Task<int> IndexAsync(string source, string content, CancellationToken ct = default)
{
    var chunks = TextChunker.Chunk(content);
    var embeddings = await _embedder.GenerateAsync(chunks, cancellationToken: ct);

    var builder = new NpgsqlDataSourceBuilder(_connectionString);
    builder.UseVector();                          // ← अलग line, return value छोड़ दो
    await using var dataSource = builder.Build();
    await using var conn = await dataSource.OpenConnectionAsync(ct);

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
}