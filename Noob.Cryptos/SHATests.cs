// ***********************************************************************
// Assembly         : Noob.Cryptos
// Author           : noob
// Created          : 2023-02-17
//
// Last Modified By : noob
// Last Modified On : 2023-02-17
// ***********************************************************************
// <copyright file="SHATests.cs" company="Noob.Cryptos">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// The Cryptos namespace.
/// </summary>
namespace Noob.Cryptos
{
    /// <summary>
    /// Class SHATests.
    /// </summary>
    public class SHATests
    {
        /// <summary>
        /// 第三方应用id，对应系统中的app_id
        /// </summary>
        private readonly string client_id;
        /// <summary>
        /// 第三方应用秘钥，对请求加签使用，对应系统中的app_secret
        /// </summary>
        private readonly string client_secret;

        /// <summary>
        /// RSA 公钥
        /// </summary>
        public readonly string pubKey;
        /// <summary>
        /// RSA 私钥
        /// </summary>
        public readonly string priKey;
        /// <summary>
        /// The charset
        /// </summary>
        public readonly string charset;
        /// <summary>
        /// Initializes a new instance of the <see cref="SHATests" /> class.
        /// </summary>
        public SHATests()
        {
            client_id = "tchzt2";
            client_secret = "aad2433934c141b58937";
            priKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMrf1h4N/B6rEYYJWir2fj2PfqoqfG89nYy7fMufJs/BJxpN7d80QWUVpk/v1TvO6C1NrIvnEeAU6SaGC3goBKW6CZVsmz2TXpZwKj/NDApqO60yyTjusqMmv8tEPJyM40n5jslHOg/kZLn4LLM2U55TFDTfldptTGLVLDLhFfIBAgMBAAECgYB9KcXfOv+OKDqieEih8vuFnW8nKxkkRF5cQhvHQIRgbqliSCv2pjWmWMoHzU7AHHIP6TkIA2J63kvN0atn0UCzqSLv88BmS4HZ9LuK6Ro52Kr8/jJDpk4SiK+gq8loOIc9AA4bnlE8JCRCUmI9biySJkr3miH4PE4T1T2+VBvlMQJBAOj54YH+29egpt025PjtZYliMjUGoIyJJbcNZDubxkYnt6u+2eFX7mbty3kVFUaXk463nNPKGkMyQSoY94pPfaUCQQDe7GeCpWKDQg7PTIeEry9xarayHdUkk+QnNPrz+XvYLRddNa1vIm/DebBwDsxjRWNvBofxzTebEobMdq7LSqwtAkEAse6uqX8BXnUHHCqhw/BjvQJvQApYshzI3j5vEAuP6eLJp3TyqOVkYd45qbdNcYWwn65iK2rOlgWauVEqNcsyNQJANSX1088sepDgSQo88SR3UjoYDsVQEOV1qudVwZ9EqJivjliC3hE+xkMYDs9oaW6cs1bCSKMd08oJ+2t8ZxmJjQJAMS3Yiie/XO/I0u4ftQTLKxjRl/4FkKJ5mRNK7FwdkQiSj/RIfxPDJeUZyN8akXOeb/LHKvZTuXyYBXQxzSlSrg==";
            pubKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDK39YeDfweqxGGCVoq9n49j36qKnxvPZ2Mu3zLnybPwScaTe3fNEFlFaZP79U7zugtTayL5xHgFOkmhgt4KASlugmVbJs9k16WcCo/zQwKajutMsk47rKjJr/LRDycjONJ+Y7JRzoP5GS5+CyzNlOeUxQ035XabUxi1Swy4RXyAQIDAQAB";
            charset = "UTF-8";
        }
        /// <summary>
        /// Defines the test method SHA256Crypto.
        /// </summary>
        [TestCase]
        public void SHA256Crypto()
        {

            string sha1Seed = GetSHA1Salt(pubKey);
            Console.WriteLine($"SHA256Crypto,key:{pubKey}");
            Console.WriteLine($"SHA256Crypto,sha1Seed:{sha1Seed}");

            string sha256Text = client_id + client_secret + pubKey;
            var sha256CryptoText = SHA256Crypto(sha256Text, pubKey);

            Console.WriteLine($"SHA256Crypto,sha256Text:{sha256Text}");
            Console.WriteLine($"SHA256Crypto,sha256CryptoText:{sha256CryptoText}");
            Assert.AreNotEqual(sha256Text, sha256CryptoText);

        }
        /// <summary>
        /// Shes the a256 crypto.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public static string SHA256Crypto(string str, string key)
        {
            //加盐
            string sha1Salt = GetSHA1Salt(key);
            return SHA256Crypto(str + sha1Salt.Replace("\r\n", string.Empty));
        }

        /// <summary>
        /// Gets the sh a1 salt.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public static string GetSHA1Salt(string key)
        {
            byte[] seed = Encoding.UTF8.GetBytes(key);
            using (var seedProvider = SHA1.Create())
            {
                using (var rdProvider = SHA1.Create())
                {
                    var rd = rdProvider.ComputeHash(seedProvider.ComputeHash(seed));
                    byte[] salt = rd.Take(16).ToArray();
                    return Convert.ToBase64String(salt);
                }
            }
        }

        /// <summary>
        /// Shes the a256 crypto.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <returns>System.String.</returns>
        private static string SHA256Crypto(string src)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(src);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }
    }
}
