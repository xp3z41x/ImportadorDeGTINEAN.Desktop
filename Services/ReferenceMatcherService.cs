using System.Text.RegularExpressions;

namespace ImportadorDeGTINEAN.Desktop.Services
{
    public static partial class ReferenceMatcherService
    {
        private static readonly char[] Separators = ['/', '-', ' ', '\\'];

        [GeneratedRegex(@"^[^a-zA-Z0-9]+")]
        private static partial Regex LeadingSpecialChars();

        [GeneratedRegex(@"[^a-zA-Z0-9]+$")]
        private static partial Regex TrailingSpecialChars();

        public static string NormalizeReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return string.Empty;

            var trimmed = reference.Trim();
            trimmed = LeadingSpecialChars().Replace(trimmed, "");
            trimmed = TrailingSpecialChars().Replace(trimmed, "");
            return trimmed.ToLowerInvariant();
        }

        public static bool IsMatch(string spreadsheetRef, string dbRef)
        {
            var cleanSheet = NormalizeReference(spreadsheetRef);
            if (string.IsNullOrEmpty(cleanSheet))
                return false;

            var cleanDb = NormalizeReference(dbRef);
            if (string.IsNullOrEmpty(cleanDb))
                return false;

            // Exact match after normalization
            if (cleanDb.Equals(cleanSheet, StringComparison.OrdinalIgnoreCase))
                return true;

            // Token-based match: split DB reference by separators and check each token
            var tokens = dbRef.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var cleanToken = NormalizeReference(token);
                if (!string.IsNullOrEmpty(cleanToken) &&
                    cleanToken.Equals(cleanSheet, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
