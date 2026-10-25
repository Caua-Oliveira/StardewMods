namespace BooksAndNotes;

/// <summary>The bookmark palette in saved color-index order, with translation keys and sprite tints.</summary>
internal static class BookmarkColors
{
    public static readonly (string Key, string Tint)[] Choices =
    [
        ("tab.color.red", "#DC7366"),
        ("tab.color.gold", "#E3B059"),
        ("tab.color.green", "#82AF77"),
        ("tab.color.blue", "#6EA2C5"),
        ("tab.color.purple", "#A48ABF"),
        ("tab.color.pink", "#D897AF"),
        ("tab.color.white", "#FFFFFF"),
        ("tab.color.dark-grey", "#555555")
    ];
}
