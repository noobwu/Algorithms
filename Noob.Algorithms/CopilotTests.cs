// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-05-02
//
// Last Modified By : noob
// Last Modified On : 2023-05-02
// ***********************************************************************
// <copyright file="CopilotTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The Algorithms namespace.
/// </summary>
namespace Noob.Algorithms
{
    /// <summary>
    /// Class CopilotTests.
    /// </summary>
    [TestFixture]
    public class CopilotTests
    {
        /// <summary>
        /// 二分查询算法   
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="target">The target.</param>
        /// <returns>System.Int32.</returns>
        [TestCaseSource(nameof(BinarySearchSource))]
        public int BinarySearch(int[] array, int target)
        {
            int left = 0;
            int right = array.Length - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (array[mid] == target)
                {
                    return mid;
                }
                else if (array[mid] < target)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            return -1;
        }


        /// <summary>
        /// Binaries the search source.
        /// </summary>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable BinarySearchSource()
        {
            yield return new TestCaseData(new int[] { 1, 2, 3, 4, 5 }, 3).Returns(2);
            yield return new TestCaseData(new int[] { 1, 2, 3, 4, 5 }, 5).Returns(4);
        }

        /// <summary>
        /// Alls the file under dev ops list.
        /// 函数名 + 输入 + 输出自动填充(ctrl+alt+\)
        /// </summary>
        public void AllFileUnderDevOpsList()
        {
            var files = Directory.GetFiles("D:\\DevOps\\DevOps\\src\\DevOps\\", "*.cs", SearchOption.AllDirectories);
            var list = files.Select(x => x.Replace("D:\\DevOps\\DevOps\\src\\DevOps\\", "").Replace(".cs", "")).ToList();
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendLine($"public void {item}()");
                sb.AppendLine("{");
                sb.AppendLine("}");
                sb.AppendLine();
            }
            var result = sb.ToString();
        }

        // {
        //   "chatgpt-general": "ChatGPT 常用指令",
        //   "chatgpt-prompt-role-play": "ChatGPT 角色扮演",
        //   "chatgpt-generator-cot": "ChatGPT 思维链模式",
        //   "chatgpt-interactive-game": "ChatGPT 交互式游戏",
        //   "chatgpt-samples": "ChatGPT 示例",
        //   "chatgpt": "ChatGPT 聊天室",
        //   "stable-diffusion-examples": "StableDiffusion 示例",
        //   "stable-diffusion-generator": "AI 绘画生成器",
        //   "github-copilot-samples": "GitHub Copilot 示例",
        //   "resources": "学习资料",
        // }
        // translate to English       
        public void TranslateToEnglish()
        {

        }

        /*
          {
            "chatgpt-general": "ChatGPT 常用指令",
            "chatgpt-prompt-role-play": "ChatGPT 角色扮演",
            "chatgpt-generator-cot": "ChatGPT 思维链模式",
            "chatgpt-interactive-game": "ChatGPT 交互式游戏",
            "chatgpt-samples": "ChatGPT 示例",
            "chatgpt": "ChatGPT 聊天室",
            "stable-diffusion-examples": "StableDiffusion 示例",
            "stable-diffusion-generator": "AI 绘画生成器",
            "github-copilot-samples": "GitHub Copilot 示例",
            "resources": "学习资料",
          }
          translate to English
        */
    }
}
