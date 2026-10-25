using System.Text.Json;

namespace BooksAndNotes;

/// <summary>Avoids parsing the same item JSON repeatedly while its tooltip is displayed.</summary>
internal sealed class DocumentSignatureCache
{
    private string? _cachedJson;
    private string? _cachedAuthor;

    /// <summary>Reads the signed author, reusing the last result while the saved JSON is unchanged.</summary>
    public string? GetAuthor(string json)
    {
        if (string.Equals(_cachedJson, json, StringComparison.Ordinal))
            return _cachedAuthor;

        _cachedJson = json;
        _cachedAuthor = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement data = document.RootElement;
            if (data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("IsSigned", out JsonElement signed) && signed.ValueKind == JsonValueKind.True
                && data.TryGetProperty("SignedBy", out JsonElement author) && author.ValueKind == JsonValueKind.String)
                _cachedAuthor = author.GetString();
        }
        catch (JsonException)
        {
            // Cache failures too: malformed writing should keep the item's ordinary tooltip.
        }

        return _cachedAuthor;
    }
}
