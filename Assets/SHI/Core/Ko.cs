namespace SHI.Core
{
    // Korean particles that depend on whether a word ends in a final consonant.
    public static class Ko
    {
        static bool HasFinal(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            char last = word[word.Length - 1];
            return last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0;
        }

        public static string I(string word) => word + (HasFinal(word) ? "이" : "가");
        public static string Eun(string word) => word + (HasFinal(word) ? "은" : "는");
        public static string Eul(string word) => word + (HasFinal(word) ? "을" : "를");
    }
}
