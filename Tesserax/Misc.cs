using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;

namespace Tesserax
{
    class Misc
    {
        #region Get Files in Natural Order
        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }

        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                return SafeNativeMethods.StrCmpLogicalW(a, b);
            }
        }

        public static string[] GetFilesInNaturalOrder(string dir)
        {
            List<string> files = Directory.GetFiles(dir).ToList();
            files.Sort(new NaturalStringComparer());
            return files.ToArray();
        }

        public static string[] GetFilesInNaturalOrder(string dir, string pattern, SearchOption options)
        {
            List<string> files = Directory.GetFiles(dir, pattern, options).ToList();
            files.Sort(new NaturalStringComparer());
            return files.ToArray();
        }
        #endregion
    }
}
