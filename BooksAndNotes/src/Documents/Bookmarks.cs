using System.Text.Json;

namespace BooksAndNotes;

/// <summary>Saves bookmark destinations, names, and colors separately from the document's writing.</summary>
internal sealed class Bookmarks
{
    public const string DataKey = "Trapyy.BooksAndNotes/PageTabs";
    public const int MaxLabelLength = 24;
    public const int MaxTabs = 10;
    private const int CurrentVersion = 6;

    // These property names are part of the saved JSON, including the name "Tabs".
    public int Version { get; set; } = CurrentVersion;
    public List<Bookmark> Tabs { get; set; } = [];

    /// <summary>Creates the bookmark JSON saved alongside the document.</summary>
    public string Serialize() => JsonSerializer.Serialize(this);

    /// <summary>Loads the supported bookmark schema and rejects invalid or duplicate destinations.</summary>
    public static Bookmarks Deserialize(string json)
    {
        using JsonDocument parsed = JsonDocument.Parse(json);
        if (parsed.RootElement.ValueKind != JsonValueKind.Object
            || !parsed.RootElement.TryGetProperty(nameof(Version), out _)
            || !parsed.RootElement.TryGetProperty(nameof(Tabs), out _))
            throw new InvalidDataException("Missing page-tab fields.");

        Bookmarks bookmarks = JsonSerializer.Deserialize<Bookmarks>(json)
            ?? throw new InvalidDataException("Empty page-tab metadata.");
        if (bookmarks.Version != CurrentVersion || bookmarks.Tabs is null || bookmarks.Tabs.Count > MaxTabs
            || bookmarks.Tabs.Any(bookmark => bookmark is null || !Valid(bookmark.Page, bookmark.Label, bookmark.Color))
            || bookmarks.Tabs.Select(bookmark => bookmark.Page).Distinct().Count() != bookmarks.Tabs.Count)
        {
            throw new InvalidDataException("Unsupported or invalid page-tab metadata.");
        }

        return bookmarks;
    }

    /// <summary>Checks a bookmark's destination, palette index, and literal display name.</summary>
    public static bool Valid(int page, string? label, int color)
        => page >= 0 && page < Document.MaxPages
            && color >= 0 && color < BookmarkColors.Choices.Length
            && label is not null && label.Length <= MaxLabelLength
            && !label.Any(char.IsControl)
            && WellFormedUnicode(label);

    /// <summary>Ensures every UTF-16 surrogate belongs to a complete character pair.</summary>
    internal static bool WellFormedUnicode(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]))
            {
                i++;
                if (i >= text.Length || !char.IsLowSurrogate(text[i]))
                    return false;
            }
            else if (char.IsLowSurrogate(text[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Adds or replaces the bookmark on a page when the values and capacity allow it.</summary>
    public bool Set(int page, string label, int color)
    {
        if (!Valid(page, label, color) || !CanSet(page))
            return false;

        Tabs.RemoveAll(bookmark => bookmark.Page == page);
        Tabs.Add(new Bookmark { Page = page, Label = label, Color = color });
        return true;
    }

    /// <summary>Allows a new bookmark below the limit, or an edit to one that already exists.</summary>
    public bool CanSet(int page) => Tabs.Count < MaxTabs || Tabs.Any(bookmark => bookmark.Page == page);

    /// <summary>Keeps bookmarked blank pages reachable after signing without adding saved writing.</summary>
    public int VisibleCount(Document document)
        => Math.Max(DocumentPages.VisibleCount(document, true), Tabs.Select(bookmark => bookmark.Page + 1).DefaultIfEmpty(1).Max());
}

/// <summary>One saved bookmark; its property names remain stable for existing item metadata.</summary>
internal sealed class Bookmark
{
    public int Page { get; set; } = -1;
    public string Label { get; set; } = null!;
    public int Color { get; set; } = -1;

    /// <summary>Formats a literal tooltip with a one-based page number and an optional name.</summary>
    public string HoverText(string pageLabel)
        => string.IsNullOrWhiteSpace(Label) ? $"{pageLabel} {Page + 1}" : $"{Label}\n{pageLabel} {Page + 1}";
}
