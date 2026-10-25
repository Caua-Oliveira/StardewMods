namespace BooksAndNotes;

/// <summary>Maps displayed pages to saved writing without saving every blank page in a draft.</summary>
public static class DocumentPages
{
    /// <summary>Returns a saved page's text, or an empty string for an unwritten display page.</summary>
    public static string GetText(Document document, int index)
        => index >= 0 && index < document.Pages.Count ? document.Pages[index] ?? "" : "";

    /// <summary>Shows all draft pages, only saved signed-book pages, and both sides of every note.</summary>
    public static int VisibleCount(Document document, bool isBook)
    {
        if (!isBook)
            return Document.MaxNotePages;

        if (document.IsSigned)
            return document.Pages.Count;

        return Document.MaxPages;
    }

    /// <summary>Updates an editable page, creating preceding blank pages only when they are needed.</summary>
    public static bool SetPage(Document document, bool isBook, int index, string text)
    {
        if (document.IsSigned || index < 0 || index >= (isBook ? Document.MaxPages : Document.MaxNotePages)
            || text.Length > Document.MaxPageLength)
            return false;

        if (index >= document.Pages.Count && text.Length == 0)
            return false;

        while (document.Pages.Count <= index)
        {
            document.Pages.Add("");
        }

        document.Pages[index] = text;
        return true;
    }
}
