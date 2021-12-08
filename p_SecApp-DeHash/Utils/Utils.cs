using System.Text.RegularExpressions;

namespace p_SecApp_DeHash.Utils
{
    static public class Utils
    {
        public enum hashtype//list of all hashes available
        {
            md5,
            sha2,
            sha1,
            unknown,
        }

        public static hashtype GetType(string input)
        {
            if (Regex.IsMatch(input, "^[a-fA-F0-9]{32}$", RegexOptions.Compiled)) return hashtype.md5;
            if (Regex.IsMatch(input, "^[a-fA-F0-9]{40}$", RegexOptions.Compiled)) return hashtype.sha1;
            if (Regex.IsMatch(input, "^[a-fA-F0-9]{64}$", RegexOptions.Compiled)) return hashtype.sha2;

            return hashtype.unknown;
        }

    }
}
