namespace Samagra.AI.Rag;

internal static class TextChunker
{
    public static IReadOnlyList<string> Chunk(
        string text,
        int chunkSize = 500,
        int overlap = 50)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();

        var step = chunkSize - overlap;

        for (var i = 0; i < words.Length; i += step)
        {
            var slice = words.Skip(i).Take(chunkSize);
            chunks.Add(string.Join(' ', slice));

            if (i + chunkSize >= words.Length) break;
        }

        return chunks;
    }
}