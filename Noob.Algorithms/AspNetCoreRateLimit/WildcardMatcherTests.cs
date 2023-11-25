// ***********************************************************************
// Assembly         : Kmmp.Libraries.UnitTests
// Author           : carl.wu
// Created          : 2023-11-22
//
// Last Modified By : carl.wu
// Last Modified On : 2023-11-22
// ***********************************************************************
// <copyright file="WildcardMatcherTests.cs" company="kemai">
//     Copyright © kemai 2023
// </copyright>
// <summary></summary>
// ***********************************************************************
using Noob.Algorithms.AspNetCoreRateLimit;
using Noob.Algorithms.AspNetCoreRateLimit.Enums;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The AspNetCoreRateLimit namespace.
/// </summary>
namespace Noob.Algorithms.AspNetCoreRateLimit
{
    /// <summary>
    /// Class WildcardMatcherTests.
    /// </summary>
    public class WildcardMatcherTests
    {
        /// <summary>
        /// The API path
        /// </summary>
        private const string apiPath = "/api/clients";

        /// <summary>
        /// The API rate limit path
        /// </summary>
        private const string apiRateLimitPath = "/api/clientratelimit";

        /// <summary>
        /// The ip
        /// </summary>
        private const string ip = "::1";

        /// <summary>
        /// The rate limit options
        /// </summary>
        private readonly RateLimitOptions rateLimitOptions;


        /// <summary>
        /// The regex rate limit options
        /// </summary>
        private readonly RateLimitOptions regexRateLimitOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="WildcardMatcherTests"/> class.
        /// </summary>
        public WildcardMatcherTests()
        {
            rateLimitOptions = new RateLimitOptions()
            {
                EnableRegexRuleMatching = false,
                EndpointWhitelist = new List<string>(){
                        "delete:/api/values",
                        "*:/api/clients",
                        "*:/api/ClientRateLimit",
                        "*:/api/IpRateLimit",
                        "get:/"
                    }
            };

            regexRateLimitOptions = new RateLimitOptions()
            {
                EnableRegexRuleMatching = true,
                EndpointWhitelist = new List<string>() {
                    "((post)|(put)|(get)|(delete)):/api/values",
                    "delete:/api/clients",
                    ":/api/clients",
                    ":/api/ClientRateLimit",
                    ":/api/IpRateLimit",
                    "get:/",
                    "get:/*",
                     "((post)|(put)|(get)|(delete)):/api/clients",
                     "*:/api/clients",
                }
            };

        }


        /// <summary>
        /// Defines the test method SpecificClientRule.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="verb">The verb.</param>
        [TestCase(ClientType.Wildcard, "GET")]
        [TestCase(ClientType.Wildcard, "PUT")]
        [TestCase(ClientType.Regex, "GET")]
        [TestCase(ClientType.Regex, "PUT")]
        public void SpecificClientRule(ClientType clientType, string verb)
        {
            string urlPath = GetUrlPath(apiPath, clientType == ClientType.Regex);
            if (clientType == ClientType.Wildcard)
            {
                UrlMatch(urlPath, rateLimitOptions);
            }
            else
            {
                UrlMatch(urlPath, regexRateLimitOptions);
            }


            string verpUrlPath = $"{verb}:{apiPath}";
            if (clientType == ClientType.Wildcard)
            {
                UrlMatch(verpUrlPath, rateLimitOptions);
            }
            else
            {
                UrlMatch(verpUrlPath, regexRateLimitOptions);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urlPath"></param>
        /// <param name="rateLimitOptions"></param>
        private void UrlMatch(string urlPath, RateLimitOptions rateLimitOptions)
        {
            Assert.IsTrue(rateLimitOptions.EndpointWhitelist.Any(x => $"{urlPath}".IsUrlMatch(x, rateLimitOptions.EnableRegexRuleMatching))
                || rateLimitOptions.EndpointWhitelist.Any(x => urlPath.IsUrlMatch(x, rateLimitOptions.EnableRegexRuleMatching)));

            Assert.IsTrue(rateLimitOptions.EndpointWhitelist.Any(x => $"{urlPath}".IsUrlMatchNew(x, rateLimitOptions.EnableRegexRuleMatching)) ||
       rateLimitOptions.EndpointWhitelist.Any(x => urlPath.IsUrlMatchNew(x, rateLimitOptions.EnableRegexRuleMatching)));

        }

        /// <summary>
        /// Gets the URL path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="enableRegexRuleMatching">The enable regex rule matching.</param>
        /// <returns>string.</returns>
        private string GetUrlPath(string path, bool enableRegexRuleMatching)
        {
            return enableRegexRuleMatching ? $".+:{path}" : $"*:{path}";
        }

        /// <summary>
        /// Determines whether the specified value is match.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        [TestCaseSource(nameof(MatchSource))]
        public void IsMatch(string value, string pattern)
        {
            Assert.IsTrue(value.IsMatch(pattern));
        }


        /// <summary>
        /// Determines whether [is match new] [the specified value].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        [TestCaseSource(nameof(MatchSource))]
        public void IsMatchNew(string value, string pattern)
        {
            Assert.IsTrue(value.IsMatchNew(pattern));
        }


        /// <summary>
        /// Determines whether [is match regex] [the specified value].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        [TestCaseSource(nameof(MatchSource))]
        public void IsMatchRegex(string value, string pattern)
        {
            Assert.IsTrue(value.IsMatchRegex(pattern));
        }

        /// <summary>
        /// Matches the source.
        /// </summary>
        /// <returns>IEnumerable.</returns>
        private static IEnumerable MatchSource()
        {
            yield return new TestCaseData("Something", "S*eth??g");
            yield return new TestCaseData("Something", "*");
            yield return new TestCaseData("A very long long long stringggggggg", "A *?string*");

            yield return new TestCaseData("Reg: Performance issue when using WebSphere MQ 7.1 ,Window server 2008 R2 and java 1.6.0_21", "Reg: Performance issue when using *,Window server ???? R? and java *.*.*_*");
            yield return new TestCaseData("Reg: Performance issue when using WebSphere MQ 7.1 ,Window server 2008 R2 and java 1.6.0_21", "Reg: Performance* and java 1.6.0_21");
        }
    }
}
