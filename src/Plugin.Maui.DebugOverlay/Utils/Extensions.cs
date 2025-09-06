using System.Text.RegularExpressions;

namespace Plugin.Maui.DebugOverlay.Utils
{
    internal static class Extensions
    {
        /// <summary>
        /// Removes the HEX color at the end of the string and returns the trimmed string and Color.
        /// Assumes the last 9 characters are always in the form #RRGGBB.
        /// </summary>
        internal static (string Text, Color Color) StripHexColor(string formattedText)
        {
            if (string.IsNullOrWhiteSpace(formattedText) || formattedText.Length < 9)
                return (formattedText, Colors.Transparent);

            // Extract the HEX color
            string hexColor = formattedText.Substring(formattedText.Length - 9, 9);
            Color color;
            try
            {
                color = Color.FromArgb(hexColor);
            }
            catch
            {
                color = Colors.Transparent;
            }

            // Remove the HEX color and trim leftover spaces
            string textWithoutColor = formattedText.Substring(0, formattedText.Length - 9).Trim();

            return (textWithoutColor, color);
        }



        private static readonly Regex UidRegex = new Regex(@"#Uid:([0-9a-fA-F\-]+)#", RegexOptions.Compiled);

        /// <summary>
        /// Extracts the Guid from a string if present.
        /// Returns (guid, cleanedDetails).
        /// If no Guid found, guid will be Guid.Empty.
        /// </summary>
        internal static (Guid guid, string cleaned) ExtractGuidFromString(string details)
        {
            if (string.IsNullOrEmpty(details))
                return (Guid.Empty, details);

            var match = UidRegex.Match(details);
            if (match.Success && Guid.TryParse(match.Groups[1].Value, out var guid))
            {
                // Remove the #Uid:...# part
                var cleaned = UidRegex.Replace(details, "").Trim();
                return (guid, cleaned);
            }

            return (Guid.Empty, details);
        }
    }
}
