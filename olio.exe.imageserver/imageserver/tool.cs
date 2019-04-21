using System;
using System.Collections.Generic;
using System.Text;

namespace OLIO.ImageServer
{
    static class Tool
    {
        [ThreadStatic]
        static System.Security.Cryptography.SHA256 sha256 = null;
        [ThreadStatic]
        static System.Random random = null;
        public static byte[] Sha256(byte[] src)
        {
            if(sha256==null)
            {
                sha256= System.Security.Cryptography.SHA256.Create();
            }
            return sha256.ComputeHash(src);
        }
        public static int RanInt32()
        {
            if(random==null)
            {
                random = new Random();
            }
            return random.Next();
        }
        public static byte[] RanToken(string id)
        {
            byte[] src  = System.Text.Encoding.UTF8.GetBytes( id + "_" + RanInt32());
            return Sha256(src);
        }
        public static bool BytesEqual(byte[] a,byte[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for(var i=0;i<a.Length;i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        public static byte[] HexDecode(string hexstr)
        {
            hexstr = hexstr.ToLower();
            if (hexstr.IndexOf("0x") == 0)
                hexstr = hexstr.Substring(2);
            var outb = new byte[hexstr.Length / 2];
            for(var i=0;i<outb.Length;i++)
            {
                var subs = hexstr.Substring(i * 2, 2);
                outb[i] = byte.Parse(subs);
            }
            return outb;
        }
    }
}
