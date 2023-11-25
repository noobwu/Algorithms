// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-11-25
// SourceLink       : https://github.com/stefanprodan/AspNetCoreRateLimit/blob/master/src/AspNetCoreRateLimit/Core/WildcardMatcher.cs
//
// Last Modified By : noob
// Last Modified On : 2023-11-25
// ***********************************************************************
// <copyright file="WildcardMatcher.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// The AspNetCoreRateLimit namespace.
/// </summary>
namespace Noob.Algorithms.AspNetCoreRateLimit
{
    /// <summary>
    /// Class WildcardMatcher.
    /// </summary>
    public static class WildcardMatcher
    {

        /// <summary>
        /// Determines whether [is URL match] [the specified value].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <param name="useRegex">if set to <c>true</c> [use regex].</param>
        /// <returns><c>true</c> if [is URL match] [the specified value]; otherwise, <c>false</c>.</returns>
        public static bool IsUrlMatchNew(this string source, string value, bool useRegex)
        {
            if (useRegex)
            {
                return IsRegexMatch(source, value);
            }
            return source.IsWildCardMatchNew(value);
        }

        /// <summary>
        /// Determines whether [is URL match] [the specified value].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <param name="useRegex">if set to <c>true</c> [use regex].</param>
        /// <returns><c>true</c> if [is URL match] [the specified value]; otherwise, <c>false</c>.</returns>
        public static bool IsUrlMatch(this string source, string value, bool useRegex)
        {
            if (useRegex)
            {
                return IsRegexMatch(source, value);
            }
            return source.IsWildCardMatch(value);
        }

        /// <summary>
        /// Determines whether [is wild card match] [the specified value].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if [is wild card match] [the specified value]; otherwise, <c>false</c>.</returns>
        public static bool IsWildCardMatchNew(this string source, string value)
        {
            return source != null && value != null && source.ToLowerInvariant().IsMatchNew(value.ToLowerInvariant());
        }

        /// <summary>
        /// Determines whether [is wild card match] [the specified value].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if [is wild card match] [the specified value]; otherwise, <c>false</c>.</returns>
        public static bool IsWildCardMatch(this string source, string value)
        {
            return source != null && value != null && source.ToLowerInvariant().IsMatch(value.ToLowerInvariant());
        }

        /// <summary>
        /// Determines whether [is regex match] [the specified value].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if [is regex match] [the specified value]; otherwise, <c>false</c>.</returns>
        public static bool IsRegexMatch(this string source, string value)
        {
            if (source == null || string.IsNullOrEmpty(value))
            {
                return false;
            }
            // if the regex is e.g. /api/values/ the path should be an exact match
            // if all paths below this should be included the regex should be /api/values/*
            if (value[value.Length - 1] != '$')
            {
                value += '$';
            }
            if (value[0] != '^')
            {
                value = '^' + value;
            }
            return Regex.IsMatch(source, value, RegexOptions.IgnoreCase);
        }

        // Thanks to Zoran Horvat
        //http://www.c-sharpcorner.com/uploadfile/b81385/efficient-string-matching-algorithm-with-use-of-wildcard-characters/
        //https://www.codinghelmet.com/?path=net/sysexpand/text/download
        /// <summary>
        /// Determines whether the specified pattern is match.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="singleWildcard">The single wildcard.</param>
        /// <param name="multipleWildcard">The multiple wildcard.</param>
        /// <returns><c>true</c> if the specified pattern is match; otherwise, <c>false</c>.</returns>
        public static bool IsMatch(this string value, string pattern, char singleWildcard = '?', char multipleWildcard = '*')
        {

            var inputPosStack = new int[(value.Length + 1) * (pattern.Length + 1)];   // Stack containing input positions that should be tested for further matching
            var patternPosStack = new int[inputPosStack.Length];                      // Stack containing pattern positions that should be tested for further matching
            var stackPos = -1;                                                          // Points to last occupied entry in stack; -1 indicates that stack is empty
            var pointTested = new bool[value.Length + 1, pattern.Length + 1];       // Each true value indicates that input position vs. pattern position has been tested

            var inputPos = 0;   // Position in input matched up to the first multiple wildcard in pattern
            var patternPos = 0; // Position in pattern matched up to the first multiple wildcard in pattern

            //if (pattern == null)
            //    pattern = string.Empty;

            // Match beginning of the string until first multiple wildcard in pattern
            while (inputPos < value.Length && patternPos < pattern.Length && pattern[patternPos] != multipleWildcard && (value[inputPos] == pattern[patternPos] || pattern[patternPos] == singleWildcard))
            {
                inputPos++;
                patternPos++;
            }

            // Push this position to stack if it points to end of pattern or to a general wildcard character
            if (patternPos == pattern.Length || pattern[patternPos] == multipleWildcard)
            {
                pointTested[inputPos, patternPos] = true;
                inputPosStack[++stackPos] = inputPos;
                patternPosStack[stackPos] = patternPos;
            }

            var matched = false;

            // Repeat matching until either string is matched against the pattern or no more parts remain on stack to test
            while (stackPos >= 0 && !matched)
            {
                inputPos = inputPosStack[stackPos];         // Pop input and pattern positions from stack
                patternPos = patternPosStack[stackPos--];   // Matching will succeed if rest of the input string matches rest of the pattern

                if (inputPos == value.Length && patternPos == pattern.Length)
                    matched = true;     // Reached end of both pattern and input string, hence matching is successful
                else if (patternPos == pattern.Length - 1)
                    matched = true;     // Current pattern character is multiple wildcard and it will match all the remaining characters in the input string
                else
                {
                    // First character in next pattern block is guaranteed to be multiple wildcard
                    // So skip it and search for all matches in value string until next multiple wildcard character is reached in pattern

                    for (var curInputStart = inputPos; curInputStart < value.Length; curInputStart++)
                    {

                        var curInputPos = curInputStart;
                        var curPatternPos = patternPos + 1;

                        while (curInputPos < value.Length && curPatternPos < pattern.Length && pattern[curPatternPos] != multipleWildcard &&
                               (value[curInputPos] == pattern[curPatternPos] || pattern[curPatternPos] == singleWildcard))
                        {
                            curInputPos++;
                            curPatternPos++;
                        }

                        // If we have reached next multiple wildcard character in pattern without breaking the matching sequence, then we have another candidate for full match
                        // This candidate should be pushed to stack for further processing
                        // At the same time, pair (input position, pattern position) will be marked as tested, so that it will not be pushed to stack later again
                        if (((curPatternPos == pattern.Length && curInputPos == value.Length) || (curPatternPos < pattern.Length && pattern[curPatternPos] == multipleWildcard))
                            && !pointTested[curInputPos, curPatternPos])
                        {
                            pointTested[curInputPos, curPatternPos] = true;
                            inputPosStack[++stackPos] = curInputPos;
                            patternPosStack[stackPos] = curPatternPos;
                        }
                    }
                }
            }

            return matched;
        }

        /// <summary>
        /// Determines whether [is match new] [the specified value].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="singleWildcard">The single wildcard.</param>
        /// <param name="multipleWildcard">The multiple wildcard.</param>
        /// <returns><c>true</c> if [is match new] [the specified value]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentException">Value and pattern must not be null or empty.</exception>
        public static bool IsMatchNew(this string value, string pattern, char singleWildcard = '?', char multipleWildcard = '*')
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Value and pattern must not be null or empty.");
            }

            return MatchInternal(value, pattern, singleWildcard, multipleWildcard);
        }

        /// <summary>
        /// Matches the internal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="singleWildcard">The single wildcard.</param>
        /// <param name="multipleWildcard">The multiple wildcard.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool MatchInternal(string value, string pattern, char singleWildcard = '?', char multipleWildcard = '*')
        {
            int valueLength = value.Length;
            int patternLength = pattern.Length;
            int[,] dp = new int[valueLength + 1, patternLength + 1];

            // 初始化动态规划矩阵
            dp[0, 0] = 1;
            for (int i = 1; i <= valueLength; i++)
            {
                dp[i, 0] = 0;
            }
            for (int j = 1; j <= patternLength; j++)
            {
                if (pattern[j - 1] == multipleWildcard)
                {
                    dp[0, j] = dp[0, j - 1];
                }
            }

            // 动态规划填充矩阵
            for (int i = 1; i <= valueLength; i++)
            {
                for (int j = 1; j <= patternLength; j++)
                {
                    if (pattern[j - 1] == singleWildcard || value[i - 1] == pattern[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1];
                    }
                    else if (pattern[j - 1] == multipleWildcard)
                    {
                        dp[i, j] = dp[i - 1, j] | dp[i, j - 1];
                    }
                    else
                    {
                        dp[i, j] = 0;
                    }
                }
            }

            return dp[valueLength, patternLength] == 1;
        }

        /// <summary>
        /// Determines whether [is match regex] [the specified pattern].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="singleWildcard">The single wildcard.</param>
        /// <param name="multipleWildcard">The multiple wildcard.</param>
        /// <returns><c>true</c> if [is match regex] [the specified pattern]; otherwise, <c>false</c>.</returns>
        public static bool IsMatchRegex(this string value, string pattern, char singleWildcard = '?', char multipleWildcard = '*')
        {

            string escapedSingle = Regex.Escape(new string(singleWildcard, 1));
            string escapedMultiple = Regex.Escape(new string(multipleWildcard, 1));

            pattern = Regex.Escape(pattern);
            pattern = pattern.Replace(escapedSingle, ".");
            pattern = $"^{pattern.Replace(escapedMultiple, ".*")}$";

            Regex reg = new Regex(pattern);

            return reg.IsMatch(value);

        }
    }
}
