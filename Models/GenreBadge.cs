namespace Lib_Mgmt.Models
{
    /// <summary>
    /// Maps a genre name to one of a fixed set of badge colour classes so the
    /// same genre always gets the same colour across the app.
    /// </summary>
    public static class GenreBadge
    {
        private static readonly string[] Palette =
            { "g-indigo", "g-teal", "g-violet", "g-amber", "g-rose", "g-slate" };

        public static string CssClass(string genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) return "g-slate";
            int hash = 0;
            foreach (char c in genre.Trim().ToLowerInvariant())
            {
                hash = (hash * 31 + c) & 0x7fffffff;
            }
            return Palette[hash % Palette.Length];
        }
    }
}
