using System;

namespace Client.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether the specified string is a well-formed absolute HTTP or HTTPS URL.
        /// </summary>
        /// <remarks>This method checks that the input is a non-empty, non-whitespace string and that it
        /// represents an absolute URI with either the HTTP or HTTPS scheme. Relative URLs and URLs with other schemes
        /// will return false.</remarks>
        /// <param name="url">The string to validate as an absolute HTTP or HTTPS URL. Can be null or empty.</param>
        /// <returns>true if the string is a valid absolute HTTP or HTTPS URL; otherwise, false.</returns>
        public static bool IsValidHttpOrHttpsUrl(this string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
        /// <summary>
        /// Determines whether the specified string is a valid absolute HTTPS URL.
        /// </summary>
        /// <remarks>This method returns false for null, empty, or whitespace-only strings, as well as for
        /// URLs that are not absolute or do not use the HTTPS scheme.</remarks>
        /// <param name="url">The string to evaluate as a URL. Can be null or empty.</param>
        /// <returns>true if the string is a non-empty, absolute URL that uses the HTTPS scheme; otherwise, false.</returns>
        public static bool IsStrictlyHttpsUrl(this string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && uriResult.Scheme == Uri.UriSchemeHttps;
        }
        /// <summary>
        /// Determines whether the specified string is a strictly valid absolute HTTP URL.
        /// </summary>
        /// <remarks>This method returns false for URLs with other schemes (such as HTTPS or FTP),
        /// relative URLs, or null/whitespace input.</remarks>
        /// <param name="url">The string to validate as an absolute HTTP URL. Can be null or empty.</param>
        /// <returns>true if the string is a non-empty, absolute URL with the HTTP scheme; otherwise, false.</returns>
        public static bool IsStrictlyHttpUrl(this string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && uriResult.Scheme == Uri.UriSchemeHttp;
        }
    }
}