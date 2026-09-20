using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;
using Samagra.Application.Interfaces;

namespace Samagra.AI.Rag;

internal sealed class VectorSearch : IVectorSearch
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly string _connectionString;

    public VectorSearch(
        IEmbeddingGenerator<string, Embedding<float>> embedder,
        string connectionString)
    {
        _embedder = embedder;
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query, int topK = 3, CancellationToken ct = default)
    {
        // 1. सवाल का embedding बनाओ
        var queryEmbedding = await _embedder.GenerateAsync(query, cancellationToken: ct);

        var builder = new NpgsqlDataSourceBuilder(_connectionString);
        builder.UseVector();
        await using var dataSource = builder.Build();
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        // 2. सबसे पास वाले ढूँढो
        await using var cmd = new NpgsqlCommand(
            """
            SELECT source, content, embedding <=> $1 AS distance
            FROM document_chunks
            ORDER BY distance
            LIMIT $2
            """, conn);

        cmd.Parameters.AddWithValue(new Vector(queryEmbedding.Vector));
        cmd.Parameters.AddWithValue(topK);

        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new SearchResult(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDouble(2)));
        }

        return results;
    }
}