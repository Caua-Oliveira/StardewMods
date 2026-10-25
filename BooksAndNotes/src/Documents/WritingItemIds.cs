namespace BooksAndNotes;

/// <summary>Item IDs shared with the content pack; changing them would disconnect existing items.</summary>
internal static class WritingItemIds
{
    public const string Book = "Trapyy.BooksAndNotes_Book";
    public const string Note = "Trapyy.BooksAndNotes_Note";
    public const string SpringBook = "Trapyy.BooksAndNotes_SpringBook";
    public const string SummerBook = "Trapyy.BooksAndNotes_SummerBook";
    public const string FallBook = "Trapyy.BooksAndNotes_FallBook";
    public const string WinterBook = "Trapyy.BooksAndNotes_WinterBook";

    /// <summary>Recognizes the default book and all four seasonal covers.</summary>
    public static bool IsBook(string? qualifiedId) => qualifiedId is "(O)" + Book
        or "(O)" + SpringBook or "(O)" + SummerBook or "(O)" + FallBook or "(O)" + WinterBook;

    /// <summary>Recognizes any book or the two-sided note.</summary>
    public static bool IsWritingItem(string? qualifiedId) => IsBook(qualifiedId) || qualifiedId == "(O)" + Note;
}
