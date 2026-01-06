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

        // This is the hard-coded base entropy that is attached directly to the application. This is not
        //  the final value.
        // https://stackoverflow.com/questions/1326001/windows-dpapi-what-to-do-with-entropy
        // https://stackoverflow.com/questions/2585746/securely-storing-optional-entropy-while-using-dpapi
        private static readonly byte[] entropyBase =
            [ 73, 161, 134, 115,   46, 185, 242, 41,   218, 14, 199, 147,   16, 131, 186, 8 ];

        private static readonly string filePath = "credentials.dat";



        // TODO: ADD ENTROPY 'SEED' DATA, WHICH SHOULD BE MANAGED BY THIS CLASS (DOES NOT NEED TO BE STORED IN AppStateData)
        // We will store an appinfo.json file with inconspicuous data. Fields include:
        //  - application version
        //  - first run timestamp
        //  - windows username
        // As discussed in OneNote, this will be retrieved on each application startup and re-generated if the
        //  application version field changes (or if the file is empty or does not exist). May also be regenerated
        //  if the windows username changes (PROBABLY SHOULD BE).



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
                byte[] encryptedText = ProtectedData.Protect(originalText, entropyBase, DataProtectionScope.CurrentUser);
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
                byte[] originalText = ProtectedData.Unprotect(encryptedText, entropyBase, DataProtectionScope.CurrentUser);
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
