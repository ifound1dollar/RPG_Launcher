using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Launcher.Util
{
    public static class DataProtection
    {
        // LATER, THE credentials.dat FILE WILL BE A .json FILE WITH MULTIPLE FIELDS

        // TODO: STORE ENTROPY BYTES IN SOME SECURE CONTAINER
        // https://stackoverflow.com/questions/1326001/windows-dpapi-what-to-do-with-entropy
        private static readonly byte[] entropy = [ 73, 161, 134, 115,   46, 185, 242, 41,   218, 14, 199, 147,   16, 131, 186, 8 ];

        private static readonly string filePath = "credentials.dat";

        public static void SaveRefreshToken(string token)
        {
            // If token is empty (can happen if there was no refresh token in the file on startup AND
            //  we have not received a new one from the server on login), simply reset token file completely.
            if (string.IsNullOrEmpty(token))
            {
                ResetRefreshToken();
                return;
            }

            // Else if token is valid (as far as we know), write it to the secure file.
            try
            {
                using FileStream fileStream = new(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                using StreamWriter sw = new(fileStream);

                byte[] originalText = Encoding.UTF8.GetBytes(token);
                byte[] encryptedText = ProtectedData.Protect(originalText, entropy, DataProtectionScope.CurrentUser);
                sw.WriteLine(Convert.ToBase64String(encryptedText));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        public static string LoadRefreshToken()
        {
            try
            {
                using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fileStream);
                string? line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) return string.Empty;

                byte[] encryptedText = Convert.FromBase64String(line);
                byte[] originalText = ProtectedData.Unprotect(encryptedText, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(originalText);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return string.Empty;
            }
        }

        public static void ResetRefreshToken()
        {
            try
            {
                // FileMode.Create will completely overwrite existing data.
                using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
                using StreamWriter sw = new(fileStream);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

    }
}
