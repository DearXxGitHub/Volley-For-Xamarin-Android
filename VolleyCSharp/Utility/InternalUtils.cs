using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.Utility
{
    public static class InternalUtils
    {
        private static char[] HEX_CHARS = "0123456789ABCDEF".ToCharArray();

        private static String ConvertToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 0xff;
                hexChars[j * 2] = HEX_CHARS[v >> 4];
                hexChars[j * 2 + 1] = HEX_CHARS[v & 0x0f];
            }
            return new String(hexChars);
        }

        public static String SHA1Hash(String text)
        {
            String hash = null;
            try
            {
                var digest = Java.Security.MessageDigest.GetInstance("SHA-1");
                byte[] bytes = new Java.Lang.String(text).GetBytes("UTF-8");
                digest.Update(bytes, 0, bytes.Length);
                hash = ConvertToHex(digest.Digest());
            }
            catch (Java.Security.NoSuchAlgorithmException e)
            {
                e.PrintStackTrace();
            }
            catch (Java.IO.UnsupportedEncodingException e)
            {
                e.PrintStackTrace();
            }
            return hash;
        }
    }
}