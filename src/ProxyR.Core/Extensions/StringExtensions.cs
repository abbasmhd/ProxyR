using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProxyR.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for string operations.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Indents a string with the specified tab unit and tab count.
        /// </summary>
        /// <param name="source">The string to indent.</param>
        /// <param name="tabUnit">The tab unit to use for indentation.</param>
        /// <param name="tabCount">The number of tab units to use for indentation.</param>
        /// <returns>The indented string.</returns>
        public static string Indent(this string source, string tabUnit = "\t", int tabCount = 1)
        {
            var tab = tabUnit.Repeat(tabCount);

            return tab + source.Replace("\n", $"\n{tab}");
        }

        /// <summary>
        /// Repeats a given string a specified number of times.
        /// </summary>
        /// <param name="source">The string to be repeated.</param>
        /// <param name="count">The number of times to repeat the string.</param>
        /// <returns>The repeated string.</returns>
        public static string Repeat(this string source, int count)
        {
            var builder = new StringBuilder();

            for (var index = 0; index < count; index++)
            {
                builder.Append(source);
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Determines whether the string has non-whitespace characters present in it.
        ///     Uses String.IsNullOrWhiteSpace();
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool HasContent(this string source) => !string.IsNullOrWhiteSpace(source);

        /// <summary>
        ///     Extension method on string references to determine whether the string is null or WhiteSpace
        /// </summary>
        /// <param name="source">The string reference to test</param>
        /// <returns>
        ///     True if the string is null or containing only whitespace characters, false
        ///     if there are any non-whitespace characters
        /// </returns>
        public static bool IsNullOrWhiteSpace(this string source) => string.IsNullOrWhiteSpace(source);

        /// <summary>
        ///     Extension method on string references to determine whether the string is null or empty
        /// </summary>
        /// <param name="source">The string reference to test</param>
        /// <returns>True if the string is null or empty</returns>
        public static bool IsNullOrEmpty(this string source) => string.IsNullOrEmpty(source);

        /// <summary>
        ///     Returns whether the string is a whole number. i.e. string representation of an int.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsNumber(this string source) => source.All(char.IsNumber);

        /// <summary>
        ///     Does a string comparison ignoring case using ordinal comparison.
        /// </summary>
        /// <param name="source">String extension executed on</param>
        /// <param name="toCompare">String to compare with source string</param>
        /// <returns>True if string are equal, false if not.</returns>
        public static bool EqualsOrdinalIgnoreCase(this string source, string toCompare)
        {
            // Extension methods can be called on null references, ensure we don't crash when calling this method.
            if (source == null)
            {
                source = string.Empty;
            }

            return source.Equals(toCompare, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares two strings using ordinal comparison ignoring case.
        /// </summary>
        public static bool EqualsOIC(this string source, string toCompare) => source.EqualsOrdinalIgnoreCase(toCompare);

        /// <summary>
        /// Splits a string into an array of strings based on the specified delimiters, while removing any empty entries.
        /// </summary>
        public static IEnumerable<string> SplitRemoveEmpty(this string source, params char[] delimiter)
            => source.IsNullOrWhiteSpace()
                   ? Array.Empty<string>()
                   : source.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        ///     Same as Split Remove Empty but trims each entry (this seems to be needed after
        ///     using SplitRemoveEmpty constantly... So, a shortcut.
        /// </summary>
        /// <param name="source">String we are splitting</param>
        /// <param name="delimiter">List of delimiters</param>
        /// <returns>Split, trimmed, empty items removed array of the parts.</returns>
        public static string[] SplitTrimRemoveEmpty(this string source, params char[] delimiter)
        {
            if (source is null)
            {
                return Array.Empty<string>();
            }

            var items = source.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < items.Length; i++)
            {
                items[i] = items[i].Trim();
            }

            return items;
        }

        /// <summary>
        ///     Checks whether a string is in a list of strings in a Case Insensitive manner
        /// </summary>
        /// <param name="source">The string to check for the existence of in the list</param>
        /// <param name="list">The list to check.</param>
        /// <returns>True if source is found in the list, false if not</returns>
        public static bool IsIn(this string source, params string[] list) => list?.Any(l => l.EqualsOrdinalIgnoreCase(source)) == true;

        /// <summary>
        ///     Checks whether a string is NOT IN a list of strings in a Case Insensitive manner
        /// </summary>
        /// <param name="source">The string to check for the existence of in the list</param>
        /// <param name="list">The list to check.</param>
        /// <returns>True if source is found in the list, false if not</returns>
        public static bool IsNotIn(this string source, params string[] list) => !source.IsIn(list);

        /// <summary>
        /// Uppercases the first character of the string.
        /// </summary>
        public static string ToUpperFirst(this string source)
        {
            if (!string.IsNullOrWhiteSpace(source))
                return source;

            if (!char.IsLetter(source[0]))
                return source;

            var result = $"{source[0].ToString().ToUpperInvariant()}{new string(source.Skip(1).ToArray())}";
            return result;
        }

        /// <summary>
        /// Converts a Camel, Pascal, Kebab or Snake case into seperate capitalized words
        /// </summary>
        public static string ToCapitalWordCase(this string source)
        {
            var words = new List<string>();
            var currentWord = new List<char>();
            char? lastChar = null;

            foreach (var currentChar in source)
            {
                if (currentChar == '_' || currentChar == '-' || currentChar == ' ')
                {
                    commitWord();
                }
                else if (char.IsUpper(currentChar) && lastChar is not null && (!char.IsLetter(lastChar.Value) || !char.IsUpper(lastChar.Value)))
                {
                    commitWord();
                    currentWord.Add(currentChar);
                }
                else
                {
                    currentWord.Add(currentChar);
                }

                lastChar = currentChar;
            }

            commitWord();

            var result = string.Join(" ", words.Select(x => x.Trim())).Trim();
            return result;

            void commitWord()
            {
                if (currentWord.Any())
                {
                    var word = new string(currentWord.ToArray());
                    words.Add(word.ToUpperFirst());
                    currentWord.Clear();
                }
            }
        }

        /// <summary>
        /// Converts a Camel, Kebab, Snake or Word case into Pascal-case
        /// </summary>
        public static string ToPascalCase(this string source)
        {
            var words = new List<string>();
            var currentWord = new List<char>();
            char? lastChar = null;

            foreach (var currentChar in source)
            {
                if (currentChar == '_' || currentChar == '-' || currentChar == ' ')
                {
                    commitWord();
                }
                else if (char.IsUpper(currentChar) && lastChar != null && (!char.IsLetter(lastChar.Value) || !char.IsUpper(lastChar.Value)))
                {
                    commitWord();
                    currentWord.Add(currentChar);
                }
                else
                {
                    currentWord.Add(currentChar);
                }

                lastChar = currentChar;
            }

            commitWord();

            var result = string.Join(string.Empty, words.Select(x => x.Trim())).Trim();
            return result;

            void commitWord()
            {
                if (currentWord.Any())
                {
                    var word = new string(currentWord.ToArray());
                    words.Add(word.ToUpperFirst());
                    currentWord.Clear();
                }
            }
        }

        /// <summary>
        /// Converts a Pascal-Case string to a Camel-Case one.
        /// </summary>
        public static string ToCamelCase(this string value)
        {
            // Split into words based on the casing of the characters.
            var words = Regex.Split(value, "([A-Z]{0,}[a-z]*)")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            // Make the first word lower-case.
            words[0] = words[0].ToLower();

            // Join to a new result.
            var result = String.Join(string.Empty, words);

            return result;
        }
    }

}
