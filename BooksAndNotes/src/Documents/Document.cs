using System.Text.Json;

namespace BooksAndNotes;

/// <summary>The writing and signature saved as JSON in a book or note's item metadata.</summary>
public sealed class Document
{
    public const int CurrentVersion = 3;
    public const int MaxPages = 50;
    public const int MaxNotePages = 2;
    public const int MaxPageLength = 4000;
    public const int MaxTitleLength = 50;

    public int Version { get; set; } = CurrentVersion;
    public string Title { get; set; } = "";
    public List<string> Pages { get; set; } = [""];
    public bool IsSigned { get; set; }
    public string SignedBy { get; set; } = "";

    /// <summary>Creates the JSON stored on the item, using the existing save-field names.</summary>
    public string Serialize() => JsonSerializer.Serialize(this);

    /// <summary>Checks a title's length, visible content, and Unicode before signing or loading it.</summary>
    public static bool ValidTitle(string? title) => title is not null
        && title.Length is > 0 and <= MaxTitleLength && !string.IsNullOrWhiteSpace(title)
        && !title.Any(char.IsControl) && Bookmarks.WellFormedUnicode(title);

    /// <summary>Seals the writing, records its author, and removes only trailing unwritten pages.</summary>
    public bool Sign(string title, string author)
    {
        // Check the untrimmed title so whitespace cannot hide an over-length input.
        if (IsSigned || !ValidTitle(title))
            return false;
        title = title.Trim();

        while (Pages.Count > 1 && string.IsNullOrWhiteSpace(Pages[^1]))
        {
            Pages.RemoveAt(Pages.Count - 1);
        }

        Title = title;
        SignedBy = author;
        IsSigned = true;
        return true;
    }

    /// <summary>Loads supported writing and rejects invalid data instead of altering the saved text.</summary>
    public static Document Deserialize(string json, bool isBook)
    {
        using (JsonDocument parsed = JsonDocument.Parse(json))
        {
            if (parsed.RootElement.ValueKind != JsonValueKind.Object
                || new[] { nameof(Version), nameof(Title), nameof(Pages), nameof(IsSigned), nameof(SignedBy) }
                    .Any(name => !parsed.RootElement.TryGetProperty(name, out _)))
            {
                throw new InvalidDataException("Missing document fields.");
            }
        }

        Document document = JsonSerializer.Deserialize<Document>(json)
            ?? throw new InvalidDataException("The document is empty.");

        if (document.Version != CurrentVersion)
            throw new InvalidDataException($"Unsupported document version {document.Version}.");

        if (document.Title is null || document.Title.Length > MaxTitleLength)
            throw new InvalidDataException("Invalid document title.");

        if (document.Pages is null || document.Pages.Count < 1 || document.Pages.Count > (isBook ? MaxPages : MaxNotePages)
            || document.Pages.Any(page => page is null || page.Length > MaxPageLength))
            throw new InvalidDataException("Invalid document pages.");

        if (document.SignedBy is null || (document.IsSigned && !ValidTitle(document.Title)))
            throw new InvalidDataException("Invalid document signature.");

        return document;
    }
}
