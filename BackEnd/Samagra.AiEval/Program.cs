using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Samagra.AI;
using Samagra.AiEval;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Data;
using Samagra.Infrastructure.Repositories;

// ---- arguments ----
var chunkSize = GetArg("--chunk-size", 500);
var overlap = GetArg("--overlap", 50);
var reindex = args.Contains("--reindex") || args.Contains("--chunk-size");

// ---- host ----
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IAiUsageRecorder, AiUsageRecorder>();
builder.Services.AddAiServices(builder.Configuration);

var host = builder.Build();

// ---- reindex ----
if (reindex)
{
    Console.WriteLine($"Reindexing with chunkSize={chunkSize}, overlap={overlap}");

    using var indexScope = host.Services.CreateScope();
    var indexer = indexScope.ServiceProvider.GetRequiredService<IDocumentIndexer>();

    await indexer.ClearAsync();

    var total = 0;
    foreach (var (source, content) in EvalDocuments.All)
    {
        var count = await indexer.IndexAsync(source, content, chunkSize, overlap);
        total += count;
        Console.WriteLine($"  {source}: {count} chunks");
    }

    Console.WriteLine($"  total: {total} chunks\n");
}

// ---- test cases ----
var json = await File.ReadAllTextAsync("testset.json");
var cases = JsonSerializer.Deserialize<List<TestCase>>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? [];

Console.WriteLine($"Running {cases.Count} test cases...\n");

var startedAt = DateTime.UtcNow;
var results = new List<TestResult>();

foreach (var testCase in cases)
{
    using var scope = host.Services.CreateScope();
    var search = scope.ServiceProvider.GetRequiredService<IVectorSearch>();
    var assistant = scope.ServiceProvider.GetRequiredService<IAiAssistant>();

    var sw = Stopwatch.StartNew();

    // retrieval
    var searchResults = await search.SearchAsync(testCase.Question, topK: 3);
    var topMatch = searchResults.FirstOrDefault();

    // Recall@3 — सही source top 3 में कहीं भी?
    var rank = testCase.ExpectedSource is null
        ? 0
        : searchResults
            .Select((r, i) => (r, i))
            .Where(x => x.r.Source.Contains(testCase.ExpectedSource, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.i + 1)
            .FirstOrDefault();

    var retrievalPassed = testCase.ShouldRefuse || rank == 1;
    var inTop3 = testCase.ShouldRefuse || rank > 0;

    // answer
    var answer = await assistant.AskWithRagAsync(testCase.Question);
    sw.Stop();

    bool answerPassed;
    string? failureReason = null;

    if (testCase.ShouldRefuse)
    {
        answerPassed = answer.Contains("don't have", StringComparison.OrdinalIgnoreCase)
                    || answer.Contains("do not have", StringComparison.OrdinalIgnoreCase);

        if (!answerPassed) failureReason = "should have refused but answered";
    }
    else
    {
        var missing = testCase.ExpectedFacts
            .Where(f => !answer.Contains(f, StringComparison.OrdinalIgnoreCase))
            .ToList();

        answerPassed = missing.Count == 0;
        if (!answerPassed) failureReason = $"missing facts: {string.Join(", ", missing)}";
    }

    if (!retrievalPassed)
        failureReason = rank > 1
            ? $"expected source at rank {rank}, not first"
            : $"wrong source: expected {testCase.ExpectedSource}, got {topMatch?.Source ?? "none"}";

    results.Add(new TestResult
    {
        Case = testCase,
        Answer = answer,
        RetrievedSource = topMatch?.Source,
        Distance = topMatch?.Distance,
        Rank = rank,
        InTop3 = inTop3,
        LatencyMs = sw.ElapsedMilliseconds,
        RetrievalPassed = retrievalPassed,
        AnswerPassed = answerPassed,
        FailureReason = failureReason
    });

    Console.WriteLine($"  {testCase.Id}  {(retrievalPassed && answerPassed ? "PASS" : "FAIL")}  {testCase.Question}");
}

// ---- cost from AiUsages ----
using var costScope = host.Services.CreateScope();
var db = costScope.ServiceProvider.GetRequiredService<AppDbContext>();

var usage = await db.AiUsages
    .AsNoTracking()
    .Where(u => u.CreatedAtUtc >= startedAt)
    .GroupBy(_ => 1)
    .Select(g => new
    {
        Calls = g.Count(),
        Cost = g.Sum(x => x.CostUsd),
        Input = g.Sum(x => (long)x.InputTokens),
        Output = g.Sum(x => (long)x.OutputTokens)
    })
    .FirstOrDefaultAsync();

// ---- report ----
var n = results.Count;
var rank1 = results.Count(r => r.RetrievalPassed);
var top3 = results.Count(r => r.InTop3);
var answerOk = results.Count(r => r.AnswerPassed);

Console.WriteLine();
Console.WriteLine(new string('-', 62));
Console.WriteLine($"CONFIG      chunkSize={chunkSize}  overlap={overlap}");
Console.WriteLine();
Console.WriteLine($"RETRIEVAL   rank 1:    {rank1}/{n}  ({rank1 * 100 / n}%)");
Console.WriteLine($"            in top 3:  {top3}/{n}  ({top3 * 100 / n}%)");
Console.WriteLine($"ANSWERS     {answerOk}/{n}  ({answerOk * 100 / n}%)");
Console.WriteLine();
Console.WriteLine($"LATENCY     avg {results.Average(r => r.LatencyMs):F0} ms");

if (usage is not null)
{
    Console.WriteLine($"COST        ${usage.Cost:F6} total  ·  ${usage.Cost / n:F6} per question");
    Console.WriteLine($"TOKENS      {usage.Input} in  ·  {usage.Output} out  ·  {usage.Calls} calls");
}

var failures = results.Where(r => !r.RetrievalPassed || !r.AnswerPassed).ToList();

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("FAILURES");
    foreach (var f in failures)
    {
        Console.WriteLine($"  {f.Case.Id}  \"{f.Case.Question}\"");
        Console.WriteLine($"        {f.FailureReason}");
        Console.WriteLine($"        distance {f.Distance:F3}  ·  {f.LatencyMs} ms");
        Console.WriteLine($"        {Truncate(f.Answer, 90)}");
        Console.WriteLine();
    }
}

Console.WriteLine(new string('-', 62));

// ---- helpers ----
int GetArg(string name, int fallback)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v) ? v : fallback;
}

static string Truncate(string s, int max) =>
    s.Length <= max ? s : s[..max] + "...";