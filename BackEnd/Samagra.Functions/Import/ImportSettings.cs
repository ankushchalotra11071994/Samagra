namespace Samagra.Functions.Import;

public sealed record ImportSettings(string SqlConnectionString, string ContainerName);

// File खुद ही गलत है (headers missing वगैरह) — retry करने से ठीक नहीं होगा
public sealed class InvalidImportFileException(string message) : Exception(message);
