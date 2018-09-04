using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ElmoGameNetwork
{


    public class Encryption : MonoBehaviour
    {

        public static string GetHashString(string PlainText, string password)
        {

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(password);


            HMACSHA256 hmacsha1 = new HMACSHA256(keyByte);

            byte[] messageBytes = encoding.GetBytes(PlainText);

            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);

            return ByteToString(hashmessage);


            /*   byte[] hash = HashHMAC(HexDecode(password), StringEncode(PlainText));
               return HashEncode(hash);
             * */
        }

        public static string GetHashString(byte[] messageBytes, string password)
        {

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(password);


            HMACSHA256 hmacsha1 = new HMACSHA256(keyByte);


            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);

            return ByteToString(hashmessage);

        }

        public static string GenerateNonce(long nonce1, long nonce2)
        {
            int nonce = (int)((nonce1 * nonce2) / (2 * (nonce1 + nonce2)));

            //Debug.Log(nonce);
            string nonceString = IntToBase64(nonce);
            //Debug.Log(nonce);

            return nonceString;
        }
        public static string ByteToString(byte[] buff)
        {
            string sbinary = "";

            sbinary = Convert.ToBase64String(buff);

            return (sbinary);
        }
        public static string IntToBase64(int a)
        {
            string temp = a.ToString();
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] stringByte = encoding.GetBytes(temp);
            return Convert.ToBase64String(stringByte);

        }
        public static string StringToBase64(string a)
        {

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] stringByte = encoding.GetBytes(a);
            return Convert.ToBase64String(stringByte);

        }
    }
}
