using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProxyR.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Indents every line by a certain amount of tab-strings.
        /// </summary>
        public static string Indent(this string source, string tabUnit = "\t", int tabCount = 1)
        {
            var tab = tabUnit.Repeat(tabCount);

            return tab + source.Replace("\n", $"\n{tab}");
        }

        /// <summary>
        ///     Returns the given string repeated multiple times.
        /// </summary>
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
        ///     Converts an empty-string or string with only whitespace to NULL, otherwise returns the same String.
        /// </summary>
        /// <param name="source">The value of the string to convert.</param>
        public static string EmptyToNull(this string source) => string.IsNullOrWhiteSpace(source) ? null : source;

         /// <summary>
        ///     Removes the end of a String.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string RemoveEnd(this string source, int count) => source.Remove(source.Length - count, count);

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
        ///     I'm so sick of typing EqualsOrdinalIgnoreCase all the time and all the space it takes up :p
        /// </summary>
        public static bool EqualsOIC(this string source, string toCompare) => source.EqualsOrdinalIgnoreCase(toCompare);

        /// <summary>
        ///     Splits a string by a single character delimiter and removes any empty entries
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
            if (source == null)
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

    }

}
