// ***********************************************************************
// Assembly         : Noob.Cryptos
// Author           : noob
// Created          : 2023-02-16
//
// Last Modified By : noob
// Last Modified On : 2023-02-16
// ***********************************************************************
// <copyright file="AESTests.cs" company="Noob.Cryptos">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

/// <summary>
/// The Cryptos namespace.
/// </summary>
namespace Noob.Cryptos
{
    /// <summary>
    /// Class AESTests.
    /// </summary>
    public class AESTests
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
        /// The charset
        /// </summary>
        public readonly string charset;
        /// <summary>
        /// Initializes a new instance of the <see cref="RSATests"/> class.
        /// </summary>
        public AESTests()
        {
            client_id = "tchzt2";
            client_secret = "aad2433934c141b58937";
            charset = "UTF-8";
        }
        /// <summary>
        /// Defines the test method CTREncrypt.
        /// </summary>
        [TestCase]
        public void CTREncrypt() {
            string security_key = "ByWelFHCgFqivFZrWs89LQ==";
            string source = "123456";
            var ciphertext = CTREncrypt(security_key, source);
            Console.WriteLine($"CTREncrypt,ciphertext:{ciphertext},source:{source},security_key:{security_key}");
            Assert.AreNotEqual(source,ciphertext);
        }

        /// <summary>
        /// CTRs the encrypt.
        /// </summary>
        /// <param name="base64Key">The base64 key.</param>
        /// <param name="src">The source.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.Exception">加密失败,{e.Message}</exception>
        public string CTREncrypt(string base64Key, string src)
        {
            try
            {
                KeyParameter key = new KeyParameter(Convert.FromBase64String(base64Key));
                // 加密
                IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
                byte[] ivData = Encoding.UTF8.GetBytes(base64Key.Substring(0, 16));
                ParametersWithIV ivParams = new ParametersWithIV(key, ivData);
                cipher.Init(true, ivParams);
                byte[] encodeResult = cipher.DoFinal(Encoding.UTF8.GetBytes(src));
                return Convert.ToBase64String(encodeResult);
            }
            catch (Exception e)
            {
                throw new Exception($"加密失败,{e.Message}");
            }
        }


        /// <summary>
        /// Defines the test method CTRDecrypt.
        /// </summary>
        [TestCase]
        public void CTRDecrypt()
        {
            string security_key = "ByWelFHCgFqivFZrWs89LQ==";
            string source = "123456";
            var ciphertext = CTREncrypt(security_key, source);
            Console.WriteLine($"CTRDecrypt#SymEncrypt,ciphertext:{ciphertext},source:{source},security_key:{security_key}");
            Assert.AreNotEqual(source, ciphertext);
            var decryptText = CTRDecrypt(security_key, ciphertext);
            Console.WriteLine($"CTRDecrypt,decryptText:{decryptText}");
            Assert.AreEqual(source, decryptText);
        }
        /// <summary>
        /// CTRs the decrypt.
        /// </summary>
        /// <param name="base64Key">The base64 key.</param>
        /// <param name="src">The source.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.Exception">解密失败,{e.Message}</exception>
        public string CTRDecrypt(string base64Key, string src)
        {
            try
            {
                byte[] keyBytes = Convert.FromBase64String(base64Key);
                byte[] ivBytes = Encoding.UTF8.GetBytes(base64Key.Substring(0, 16));
                KeyParameter keyParam = new KeyParameter(keyBytes);
                ICipherParameters param = new ParametersWithIV(keyParam, ivBytes);
                // Decrypt
                IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
                cipher.Init(false, param);
                byte[] decodeResult = cipher.DoFinal(Convert.FromBase64String(src));
                return Encoding.UTF8.GetString(decodeResult);
            }
            catch (Exception e)
            {
                throw new Exception($"解密失败,{e.Message}");
            }
        }
    }
}
