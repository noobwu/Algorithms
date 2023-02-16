// ***********************************************************************
// Assembly         : Noob.Cryptos
// Author           : noob
// Created          : 2023-02-16
//
// Last Modified By : noob
// Last Modified On : 2023-02-16
// ***********************************************************************
// <copyright file="RSATests.cs" company="Noob.Cryptos">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using System.Text;
/// <summary>
/// The Cryptos namespace.
/// </summary>
namespace Noob.Cryptos
{
    /// <summary>
    /// Class RSATests.
    /// </summary>
    [TestFixture]
    public class RSATests
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
        /// 
        /// </summary>
        public readonly string charset;
        /// <summary>
        /// Initializes a new instance of the <see cref="RSATests"/> class.
        /// </summary>
        public RSATests()
        {
            client_id = "tchzt2";
            client_secret = "aad2433934c141b58937";
            priKey = "MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMrf1h4N/B6rEYYJWir2fj2PfqoqfG89nYy7fMufJs/BJxpN7d80QWUVpk/v1TvO6C1NrIvnEeAU6SaGC3goBKW6CZVsmz2TXpZwKj/NDApqO60yyTjusqMmv8tEPJyM40n5jslHOg/kZLn4LLM2U55TFDTfldptTGLVLDLhFfIBAgMBAAECgYB9KcXfOv+OKDqieEih8vuFnW8nKxkkRF5cQhvHQIRgbqliSCv2pjWmWMoHzU7AHHIP6TkIA2J63kvN0atn0UCzqSLv88BmS4HZ9LuK6Ro52Kr8/jJDpk4SiK+gq8loOIc9AA4bnlE8JCRCUmI9biySJkr3miH4PE4T1T2+VBvlMQJBAOj54YH+29egpt025PjtZYliMjUGoIyJJbcNZDubxkYnt6u+2eFX7mbty3kVFUaXk463nNPKGkMyQSoY94pPfaUCQQDe7GeCpWKDQg7PTIeEry9xarayHdUkk+QnNPrz+XvYLRddNa1vIm/DebBwDsxjRWNvBofxzTebEobMdq7LSqwtAkEAse6uqX8BXnUHHCqhw/BjvQJvQApYshzI3j5vEAuP6eLJp3TyqOVkYd45qbdNcYWwn65iK2rOlgWauVEqNcsyNQJANSX1088sepDgSQo88SR3UjoYDsVQEOV1qudVwZ9EqJivjliC3hE+xkMYDs9oaW6cs1bCSKMd08oJ+2t8ZxmJjQJAMS3Yiie/XO/I0u4ftQTLKxjRl/4FkKJ5mRNK7FwdkQiSj/RIfxPDJeUZyN8akXOeb/LHKvZTuXyYBXQxzSlSrg==";
            pubKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDK39YeDfweqxGGCVoq9n49j36qKnxvPZ2Mu3zLnybPwScaTe3fNEFlFaZP79U7zugtTayL5xHgFOkmhgt4KASlugmVbJs9k16WcCo/zQwKajutMsk47rKjJr/LRDycjONJ+Y7JRzoP5GS5+CyzNlOeUxQ035XabUxi1Swy4RXyAQIDAQAB";
            charset = "UTF-8";

        }
        /// <summary>
        /// The maximum encrypt block
        /// </summary>
        public const int MAX_ENCRYPT_BLOCK = 117;

        /// <summary>
        /// Defines the test method PubEncrypt.
        /// </summary>
        [TestCase]
        public void PubEncrypt()
        {
            var ciphertext = PubEncrypt(pubKey, client_secret);
            Console.WriteLine($"PubEncrypt,client_secret:{client_secret}");
            Console.WriteLine($"PubEncrypt,pubKey:{pubKey}");
            Console.WriteLine($"PubEncrypt,ciphertext:{ciphertext}");
            Assert.AreNotEqual(client_secret, ciphertext);
        }


        /// <summary>
        /// RSAs the encrypt.
        /// </summary>
        /// <param name="pubKey">The pub key.</param>
        /// <param name="src">The source.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.Exception">加密失败" + e.Message</exception>
        public string PubEncrypt(string pubKey, string src)
        {
            string target = null;
            MemoryStream outStream = null;
            try
            {
                AsymmetricKeyParameter key = PublicKeyFactory.CreateKey(Convert.FromBase64String(pubKey));

                OaepEncoding cipher = new OaepEncoding(new RsaEngine(), new Sha256Digest(), new Sha256Digest(), null);
                cipher.Init(true, key);

                byte[] data = Encoding.UTF8.GetBytes(src);
                int inputLen = data.Length;
                outStream = new MemoryStream();
                int offSet = 0;
                byte[] cache;
                int i = 0;

                // 对数据分段加密
                while (inputLen - offSet > 0)
                {
                    if (inputLen - offSet > MAX_ENCRYPT_BLOCK)
                    {
                        cache = cipher.ProcessBlock(data, offSet, MAX_ENCRYPT_BLOCK);
                    }
                    else
                    {
                        cache = cipher.ProcessBlock(data, offSet, inputLen - offSet);
                    }
                    outStream.Write(cache, 0, cache.Length);
                    i++;
                    offSet = i * MAX_ENCRYPT_BLOCK;
                }

                target = Convert.ToBase64String(outStream.ToArray());
            }
            catch (Exception e)
            {
                throw new Exception("加密失败" + e.Message);
            }
            finally
            {
                outStream?.Close();
            }
            return target;
        }

        /// <summary>
        /// Defines the test method PubEncrypt.
        /// </summary>
        [TestCase]
        public void PriDecrypt()
        {
            var ciphertext = PubEncrypt(pubKey, client_secret);
            Console.WriteLine($"PriDecrypt#PubEncrypt,client_secret:{client_secret}");
            Console.WriteLine($"PriDecrypt#PubEncrypt,pubKey:{pubKey}");
            Console.WriteLine($"PriDecrypt#PubEncrypt,ciphertext:{ciphertext}");
            Assert.AreNotEqual(client_secret, ciphertext);

            var decryptText = PriDecrypt(priKey, ciphertext);
            Console.WriteLine($"PriDecrypt,decryptText:{decryptText}");

            Assert.AreEqual(client_secret, decryptText);
        }
        // RSA最大解密密文大小
        private const  int MAX_DECRYPT_BLOCK = 256;

        /// <summary>
        /// Pris the decrypt.
        /// </summary>
        /// <param name="priKey">The pri key.</param>
        /// <param name="src">The source.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.Exception">解密失败" + e.Message</exception>
        public string PriDecrypt(string priKey, string src)
        {
            string target = null;
            MemoryStream outStream = null;
            try
            {
                AsymmetricKeyParameter key = PrivateKeyFactory.CreateKey(Convert.FromBase64String(priKey));

                IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/ECB/OAEPWithSHA-256AndMGF1Padding");
                cipher.Init(false, key);
                byte[] data = Convert.FromBase64String(src);
                int inputLen = data.Length;
                outStream = new MemoryStream();
                int offSet = 0;
                byte[] cache;
                int i = 0;
                // 对数据分段解密
                while (inputLen - offSet > 0)
                {
                    if (inputLen - offSet > MAX_DECRYPT_BLOCK)
                    {
                        cache = cipher.DoFinal(data, offSet, MAX_DECRYPT_BLOCK);
                    }
                    else
                    {
                        cache = cipher.DoFinal(data, offSet, inputLen - offSet);
                    }
                    outStream.Write(cache, 0, cache.Length);
                    i++;
                    offSet = i * MAX_DECRYPT_BLOCK;
                }
                target = Encoding.UTF8.GetString(outStream.ToArray());
            }
            catch (Exception e)
            {
                throw new Exception("解密失败" + e.Message);
            }
            finally
            {
                outStream?.Close();
            }
            return target;
        }
    }
}