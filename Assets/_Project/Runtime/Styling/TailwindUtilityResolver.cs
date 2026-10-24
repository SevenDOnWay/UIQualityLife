using System;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Resolves standard Tailwind CSS and atomic utility classes into computed styling properties.
    /// Handles classes such as round-pill, rounded-full, rounded-sm, rounded-md, rounded-lg, rounded-xl, rounded-2xl, and numeric dimensions.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public static class TailwindUtilityResolver
    {
        /// <summary>
        /// Attempts to resolve a Tailwind utility class token into the computed style model.
        /// </summary>
        /// <param name="token">Class name token (e.g. "round-pill", "rounded-lg", "rounded-[20px]").</param>
        /// <param name="style">Target computed style model.</param>
        /// <returns>True if the token was recognized and applied as a utility class.</returns>
        public static bool TryResolve(string token, ComputedUIStyle style)
        {
            if (string.IsNullOrEmpty(token) || style == null) return false;
            string c = token.Trim().ToLowerInvariant();

            if (c == "round-pill" || c == "rounded-pill" || c == "rounded-full" || c == "pill")
            {
                style.hasRadius = true;
                style.radiusToken = "999";
                return true;
            }

            if (c == "rounded-none")
            {
                style.hasRadius = true;
                style.radiusToken = "0";
                return true;
            }

            if (c == "rounded-sm")
            {
                style.hasRadius = true;
                style.radiusToken = "4";
                return true;
            }

            if (c == "rounded" || c == "rounded-md")
            {
                style.hasRadius = true;
                style.radiusToken = "8";
                return true;
            }

            if (c == "rounded-lg")
            {
                style.hasRadius = true;
                style.radiusToken = "16";
                return true;
            }

            if (c == "rounded-xl")
            {
                style.hasRadius = true;
                style.radiusToken = "24";
                return true;
            }

            if (c == "rounded-2xl")
            {
                style.hasRadius = true;
                style.radiusToken = "32";
                return true;
            }

            // Direct numeric radius e.g. rounded-15, round-16, rounded-[20px]
            if (c.StartsWith("rounded-") || c.StartsWith("round-"))
            {
                string sub = c.StartsWith("rounded-") ? c.Substring(8) : c.Substring(6);
                sub = sub.Trim('[', ']', 'p', 'x');
                if (float.TryParse(sub, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r))
                {
                    style.hasRadius = true;
                    style.radiusToken = r.ToString();
                    return true;
                }
            }

            return false;
        }
    }
}
