using System;
using System.Collections.Generic;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static class UriExtensions
    {
        /// <summary>
        /// Parse a query string
        /// There could be multiple values per key, but it doesn't matter for configuration purposes
        /// </summary>
        /// <returns>A collection of parsed keys and values, null if there are no entries.</returns>
        public static Dictionary<string, string> ParseQuery(this Uri uri)
        {
            var queryString = uri.Query;
            if (string.IsNullOrEmpty(queryString) || queryString == "?")
                return null;

            if (queryString[0] == '?')
                queryString = queryString.Substring(1);

            var query = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var keyValues = queryString.Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyValue in keyValues)
            {
                var keyValueParts = keyValue.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                if (keyValueParts.Length == 2)
                    query.Add(Uri.UnescapeDataString(keyValueParts[0]), Uri.UnescapeDataString(keyValueParts[1]));
            }
            return query;
        }
    }
}
