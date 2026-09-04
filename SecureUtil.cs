using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace PhraseCryptApp
{
    /// <summary>
    /// Helpers for handling passwords as wipeable byte arrays instead of .NET
    /// strings.
    ///
    /// WHY THIS MATTERS
    /// A .NET string is immutable and cannot be overwritten. Once a password has
    /// been materialised as a string it stays in managed memory until the garbage
    /// collector happens to reclaim it, and it may be copied around before then.
    /// A byte array can be zeroed the moment it is no longer needed.
    ///
    /// WPF's PasswordBox exposes SecurePassword, so the password can be moved from
    /// the control into a byte array without ever becoming a string.
    ///
    /// HONEST LIMITS
    /// This narrows the window, it does not close it. SecureString itself offers
    /// only modest protection, the operating system may still page memory to disk,
    /// and nothing here defends against a keylogger reading the password as it is
    /// typed. Treat it as hygiene, not as a guarantee.
    /// </summary>
    public static class SecureUtil
    {
        /// <summary>
        /// Converts a SecureString into UTF-8 bytes without creating an
        /// intermediate managed string. The caller owns the result and must call
        /// Wipe on it when finished.
        /// </summary>
        public static byte[] ToUtf8Bytes(SecureString secure)
        {
            if (secure == null || secure.Length == 0)
            {
                return Array.Empty<byte>();
            }

            IntPtr unmanaged = IntPtr.Zero;
            char[]? chars = null;

            try
            {
                unmanaged = Marshal.SecureStringToGlobalAllocUnicode(secure);

                chars = new char[secure.Length];
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)Marshal.ReadInt16(unmanaged, i * 2);
                }

                return Encoding.UTF8.GetBytes(chars);
            }
            finally
            {
                if (chars != null)
                {
                    Array.Clear(chars, 0, chars.Length);
                }
                if (unmanaged != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanaged);
                }
            }
        }

        /// <summary>Overwrites a byte array with zeros.</summary>
        public static void Wipe(byte[]? data)
        {
            if (data != null && data.Length > 0)
            {
                Array.Clear(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Compares two byte arrays in constant time. Used for the password
        /// confirmation field so the comparison itself leaks no length or content
        /// information through timing.
        /// </summary>
        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
