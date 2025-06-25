namespace WikiApp.Data.Model
{
    public class NoteSearchResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public int MatchStartIndex { get; set; }


        public string HighlightedKeyword { get; set; } = string.Empty;
    }
}
