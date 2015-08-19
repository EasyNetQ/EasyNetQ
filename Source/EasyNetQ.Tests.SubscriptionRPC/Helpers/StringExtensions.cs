using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyNetQ.Tests.SubscriptionRPC.Helpers {
    public static class StringExtensions {

        public static string SafeRemove(this string subject, int from) {
            if (string.IsNullOrEmpty(subject)) return subject;
            if (subject.Length <= from) return subject;
            return subject.Remove(from);
        }

        /// <summary>
        /// The method create a Base64 encoded string from a normal string.
        /// Assumes UTF8 Encoding
        /// </summary>
        /// <param name="subject">The String containing the characters to encode.</param>
        /// <returns>The Base64 encoded string.</returns>
        public static string EncodeTo64(this string subject) {
            byte[] data = null;
            try {
                data = System.Text.Encoding.UTF8.GetBytes(subject);
                return System.Convert.ToBase64String(data);
            }
            finally {
                data = null;
            }
        }

        /// <summary>
        /// The method to Decode your Base64 strings.
        /// </summary>
        /// <param name="subject">The String containing the characters to decode.</param>
        /// <returns>A String containing the results of decoding the specified sequence of bytes.</returns>
        public static string DecodeFrom64(this string subject) {
            byte[] data = null;
            try {
                data = System.Convert.FromBase64String(subject);
                return System.Text.Encoding.UTF8.GetString(data);
            }
            finally {
                data = null;
            }
        }

        public static string ReplaceString(this string subject, string target, string replacement = "") {
            try {
                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(target))
                    return subject;
                subject = subject.Replace(target, replacement);
                return subject;
            }
            finally {
                subject = null;
                target = null;
            }
        }

        private static Regex _onlyAlphaNumeric = new Regex(@"\W", RegexOptions.Compiled);

        public static string ReplaceAllNonAlphaNumeric(this string subject, string replacement = "-") {
            try {
                if (string.IsNullOrWhiteSpace(subject)) return subject;
                return _onlyAlphaNumeric.Replace(subject, replacement);
            }
            finally {
                subject = null;
            }
        }

        public static string ReplaceAllChars(this string subject, char[] targets, string replacement = "") {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets.IsNullOrEmpty())
                    return subject;
                for (int i = 0; i < targets.Length; i++)
                    if (!string.IsNullOrWhiteSpace(subject))
                        subject = subject.ReplaceString(targets[i].ToString(), replacement);
                return subject;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static string ReplaceAllStrings(this string subject, string[] targets, string replacement = "") {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets.IsNullOrEmpty())
                    return subject;
                for (int i = 0; i < targets.Length; i++)
                    if (!string.IsNullOrWhiteSpace(subject))
                        subject = subject.ReplaceString(targets[i], replacement);
                return subject;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static string RemoveAll(this string subject, string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets.IsNullOrEmpty())
                    return subject;
                for (int i = 0; i < targets.Length; i++)
                    if (!string.IsNullOrWhiteSpace(subject))
                        subject = subject.Replace(targets[i].ToString(), "");
                return subject;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool Contains(this string subject, string target, StringComparison comp) {
            return subject.IndexOf(target, comp) >= 0;
        }

        public static bool ContainsAll(this string subject, params string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return false;

                for (int i = 0; i < targets.Length; i++)
                    if (!subject.Contains(targets[i], StringComparison.OrdinalIgnoreCase))
                        return false;
                return true;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool StartsWithAny(this string subject, params string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return false;

                for (int i = 0; i < targets.Length; i++)
                    if (subject.StartsWith(targets[i], StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool ContainsAny(this string subject, params string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return false;

                for (int i = 0; i < targets.Length; i++)
                    if (subject.Contains(targets[i], StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool IsNullOrWhiteSpace(this IList<string> subject) {
            try {
                if (subject.IsNullOrEmpty()) return true;
                return subject.Where(v => string.IsNullOrWhiteSpace(v)).NotNullOrEmpty();
            }
            finally {
                subject = null;
            }
        }

        public static int ContainsCount(this string subject, string target) {
            try {
                int counter = 0;
                int index = subject.IndexOf(target);
                while (index > 0) {
                    counter++;
                    index = subject.IndexOf(target, index + 1);
                }
                return counter;
            }
            finally {
                subject = null;
                target = null;
            }
        }

        public static int ContainsAnyCount(this string[] subject, params string[] targets) {
            try {
                int counter = 0;
                if (subject.IsNullOrEmpty() || targets.IsNullOrEmpty())
                    return counter;

                for (int i = 0; i < subject.Length; i++)
                    counter += subject[i].ContainsAnyCount(targets);
                return counter;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static int ContainsAnyCount(this string subject, params string[] targets) {
            try {
                int counter = 0;
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return counter;

                for (int i = 0; i < targets.Length; i++)
                    if (subject.Contains(targets[i], StringComparison.OrdinalIgnoreCase))
                        counter++;
                return counter;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool MatchesAll(this string[] subject, params string[] targets) {
            try {
                if (subject.IsNullOrEmpty() || targets.IsNullOrEmpty()) return false;
                for (int i = 0; i < subject.Length; i++)
                    if (!subject[i].MatchesAny(targets))
                        return false;
                return true;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static int MatchesAnyCount(this string[] subject, params string[] targets) {
            try {
                int counter = 0;
                if (subject.IsNullOrEmpty() || targets.IsNullOrEmpty()) return counter;
                for (int i = 0; i < subject.Length; i++)
                    if (subject[i].MatchesAny(targets))
                        counter++;
                return counter;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool MatchesAllBinary(this string[] subject, params string[] targets) {
            try {
                if (subject.IsNullOrEmpty() || targets.IsNullOrEmpty()) return false;
                for (int i = 0; i < subject.Length; i++)
                    if (!subject[i].MatchesAnyBinary(targets))
                        return false;
                return true;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool MatchesAnyBinary(this string subject, params string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return false;

                int hit = Array.BinarySearch<string>(targets, subject);
                return hit > 0;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool MatchesAny(this string subject, params string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return false;
                for (int i = 0; i < targets.Length; i++)
                    if (subject.Length <= targets[i].Length)
                        if (string.Compare(subject, targets[i], true) == 0)
                            return true;
                return false;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static int MatchesAnyCount(this string subject, params string[] targets) {
            try {
                int counter = 0;
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return counter;

                for (int i = 0; i < targets.Length; i++)
                    if (subject.Length <= targets[i].Length)
                        if (string.Compare(subject, targets[i], true) == 0)
                            counter++;
                return counter;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static int MatchesAnyCountBinary(this string subject, params string[] targets) {
            try {
                int counter = 0;
                if (string.IsNullOrWhiteSpace(subject) || targets.IsNullOrEmpty())
                    return counter;

                for (int i = 0; i < targets.Length; i++)
                    if (subject.MatchesAnyBinary(targets))
                        counter++;
                return counter;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static bool EndsWithAny(this string subject, params string[] targets) {
            try {
                if (string.IsNullOrWhiteSpace(subject) || targets == null || targets.Length == 0)
                    return false;
                for (int i = 0; i < targets.Length; i++)
                    if (subject.EndsWith(targets[i]))
                        return true;
                return false;
            }
            finally {
                subject = null;
                targets = null;
            }
        }

        public static string ToUpperFirstWords(this string subject) {
            if (string.IsNullOrWhiteSpace(subject)) return string.Empty;
            var names = subject.WordSplit().Select(v => v.ToUpperFirstLetter());
            return names.ToSingleString(" ").Trim();
        }

        /// <summary>
        /// Converts the First letter to Capital and sets the remainer to lowercase
        /// </summary>
        public static string ToUpperFirstLetter(this string subject) {
            if (string.IsNullOrEmpty(subject))
                return string.Empty;

            subject = subject.Trim('\'', '"');

            char[] letters = subject.ToCharArray();
            bool isQuotePrevious = false;
            letters[0] = char.ToUpper(letters[0]);
            for (int i = 1; i < letters.Length; i++) {
                if (!isQuotePrevious && Char.IsLetter(letters[i]))
                    letters[i] = char.ToLower(letters[i]);

                if (isQuotePrevious && Char.IsLetter(letters[i]))
                    letters[i] = char.ToUpper(letters[i]);

                isQuotePrevious = letters[i] == '\'';
            }
            return new string(letters);
        }

        public static bool IsAllUpper(this string subject) {
            for (int i = 0; i < subject.Length; i++)
                if (Char.IsLetter(subject[i]) && !Char.IsUpper(subject[i]))
                    return false;
            return true;
        }

        public static string OnlyAlphaNumeric(this string subject) {
            if (string.IsNullOrWhiteSpace(subject)) return subject;
            StringBuilder sb = new StringBuilder(subject.Length);
            for (int i = 0; i < subject.Length; i++)
                if (Char.IsLetter(subject[i]) || Char.IsNumber(subject[i]))
                    sb.Append(subject[i]);
            return sb.ToString();
        }

        public static bool IsNumeric(this string expression) {
            if (expression == null) return false;
            double number;
            return Double.TryParse(Convert.ToString(expression, CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Any, NumberFormatInfo.InvariantInfo, out number);
        }

        public static string ToSingleString(this IEnumerable<string> list, char separator) {
            return list.ToSingleString(separator.ToString());
        }

        public static string ToSingleString(this IEnumerable<string> list, string separator = null) {
            if (list.IsNullOrEmpty()) return string.Empty;
            StringBuilder sb = new StringBuilder(list.Count());
            foreach (var item in list)
                sb.Append(item + separator);

            return sb.ToString().Trim();
        }

        public static Dictionary<string, int> ToWordDictionary(this IEnumerable<string> text) {
            if (text.NotNullOrEmpty())
                return (from t in text
                        group t by t into grouped
                        select grouped).ToDictionary(t => t.Key, t => t.Count());
            return new Dictionary<string, int>();
        }

        public static string[] WordSplit(this string paragraphs, StringSplitOptions option = StringSplitOptions.RemoveEmptyEntries) {
            if (string.IsNullOrWhiteSpace(paragraphs)) return null;
            return paragraphs.Split(new char[] { ' ' }, option);
        }

        public static string[] LineSplit(this string paragraphs) {
            if (string.IsNullOrWhiteSpace(paragraphs)) return null;
            using (StringReader reader = new StringReader(paragraphs)) {
                var lines = new List<string>();
                string line = reader.ReadLine();
                while (line != null) {
                    lines.Add(line);
                    line = reader.ReadLine();
                }
                return lines.ToArray();
            }
        }

        public static string ShortenNumberOfWords(this string text, int length) {
            try {
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (text.Length <= length) return text;
                string[] keywords = text.WordSplit();
                int total = 0;
                var sb = new StringBuilder(length);
                bool trimmed = false;
                for (int i = 0; i < keywords.Length; i++) {
                    total++;
                    if (total <= length)
                        sb.Append(keywords[i] + " ");
                    else {
                        trimmed = true;
                        break;
                    }
                }
                string result = sb.ToString().Trim();
                keywords = null; sb = null;
                if (result.EndsWith(","))
                    return result.Substring(0, result.Length - 1);
                if (result.EndsWith(" and"))
                    return result.Substring(0, result.Length - 4);
                return result + (trimmed ? "..." : "");
            }
            finally {
                text = null;
            }
        }

        public static string ShortenNumberOfWordsWithMax(this string text, int length, int max) {
            try {
                return text.ShortenNumberOfWords(length).ShortenString(max);
            }
            finally {
                text = null;
            }
        }

        public static string ShortenWords(this string text, int length) {
            try {
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (text.Length <= length) return text;
                string[] keywords = text.WordSplit();
                int total = 0;
                var sb = new StringBuilder(length);
                for (int i = 0; i < keywords.Length; i++) {
                    total += keywords[i].Length + 1;
                    if (total <= length)
                        sb.Append(keywords[i] + " ");
                    else break;
                }
                string result = sb.ToString().Trim();
                keywords = null; sb = null;
                if (result.EndsWith(","))
                    return result.Substring(0, result.Length - 1);
                if (result.EndsWith(" and"))
                    return result.Substring(0, result.Length - 4);
                return result + "...";
            }
            finally {
                text = null;
            }
        }

        public static string ShortenString(this string text, int length, string append = "...") {
            if (!string.IsNullOrEmpty(text))
                if (text.Length > length)
                    return text.Remove(length) + append;
            return text;
        }
    }
}
