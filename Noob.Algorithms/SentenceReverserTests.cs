// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-05-20
//
// Last Modified By : noob
// Last Modified On : 2025-05-20
// ***********************************************************************
// <copyright file="SentenceReverserTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{
    /// <summary>
    /// 句子反转工具类。支持O(1)空间原地倒装英文句子，输出新字符串。
    /// </summary>
    public static class SentenceReverser
    {
        /// <summary>
        /// 将英文句子单词顺序反转（原地，O(1)空间）。
        /// 例如: "London bridge is falling down" -> "down falling is bridge London"
        /// </summary>
        /// <param name="sentence">输入英文句子</param>
        /// <returns>倒装后的句子字符串</returns>
        public static string ReverseSentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return sentence;

            // 移除首尾多余空格，便于处理极端空白场景
            var trimmed = sentence.Trim();

            // 若无单词分隔，直接返回
            if (!trimmed.Contains(' '))
                return trimmed;

            // 转换为字符数组
            char[] chars = trimmed.ToCharArray();

            // 1. 整体反转
            Reverse(chars, 0, chars.Length - 1);

            // 2. 逐单词反转
            int n = chars.Length;
            int wordStart = 0;

            for (int i = 0; i <= n; i++)
            {
                if (i == n || chars[i] == ' ')
                {
                    Reverse(chars, wordStart, i - 1);
                    wordStart = i + 1;
                }
            }

            // 合并多余空格
            return RemoveExtraSpaces(new string(chars));
        }

        /// <summary>
        /// 反转字符数组指定区间（左右闭区间）。
        /// </summary>
        /// <param name="array">字符数组</param>
        /// <param name="left">左指针</param>
        /// <param name="right">右指针</param>
        private static void Reverse(char[] array, int left, int right)
        {
            while (left < right)
            {
                (array[left], array[right]) = (array[right], array[left]);
                left++;
                right--;
            }
        }

        /// <summary>
        /// 去除句子中多余空格（多空格归一、两端无空格）。
        /// </summary>
        /// <param name="s">输入字符串</param>
        /// <returns>格式化字符串</returns>
        private static string RemoveExtraSpaces(string s)
        {
            // 保证所有单词间最多一个空格，两端无空格
            var result = new System.Text.StringBuilder();
            int n = s.Length;
            bool inWord = false;
            for (int i = 0; i < n; i++)
            {
                if (s[i] != ' ')
                {
                    result.Append(s[i]);
                    inWord = true;
                }
                else if (inWord)
                {
                    result.Append(' ');
                    inWord = false;
                }
            }
            // 去除最后一个空格
            if (result.Length > 0 && result[^1] == ' ')
                result.Length--;
            return result.ToString();
        }
    }

    /// <summary>
    /// Defines test class SentenceReverserTests.
    /// </summary>
    [TestFixture]
    public class SentenceReverserTests
    {
        /// <summary>
        /// Defines the test method ReverseSentence_ReturnsExpected.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="expected">The expected.</param>
        [TestCase("London bridge is falling down", "down falling is bridge London")]
        [TestCase("the quick brown fox", "fox brown quick the")]
        [TestCase("hello", "hello")]
        [TestCase("",  "")]
        [TestCase(" ", " ")]
        [TestCase("  hello   world   ","world hello")]
        [TestCase("  leading spaces", "spaces leading")] // 可选，测试空格边界
        public void ReverseSentence_ReturnsExpected(string input, string expected)
        {
            Assert.AreEqual(expected, SentenceReverser.ReverseSentence(input));
        }
    }
}
