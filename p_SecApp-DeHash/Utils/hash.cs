using System;
using System.Security.Cryptography;
using System.Text;

namespace p_SecApp_DeHash.Utils
{
    public class hash
    {
        readonly SHA256 sha256 = new SHA256Managed();
        readonly MD5 md5 = new MD5CryptoServiceProvider();
        readonly SHA1Managed sha1 = new SHA1Managed();

        Byte[] originalBytes;
        Byte[] encodedBytes;

        /// <summary>
        /// Convert to MD5
        /// </summary>
        /// <param name="originalString"></param>
        /// <returns></returns>
        public string EncodeMD5(string originalString)
        {
            originalBytes = ASCIIEncoding.Default.GetBytes(originalString);
            encodedBytes = md5.ComputeHash(originalBytes);

            return BitConverter.ToString(encodedBytes);
        }

        /// <summary>
        /// Convert to SHA1
        /// </summary>
        /// <param name="originalString"></param>
        /// <returns></returns>
        public string EncodeSHA1(string originalString)
        {
            originalBytes = ASCIIEncoding.Default.GetBytes(originalString);
            encodedBytes = sha1.ComputeHash(originalBytes);

            return BitConverter.ToString(encodedBytes);
        }

        /// <summary>
        /// Convert to SHA256
        /// </summary>
        /// <param name="originalString"></param>
        /// <returns></returns>
        public string EncodeSHA256(string originalString)
        {
            originalBytes = ASCIIEncoding.Default.GetBytes(originalString);
            encodedBytes = sha256.ComputeHash(originalBytes);

            return BitConverter.ToString(encodedBytes);
        }
    }
}
