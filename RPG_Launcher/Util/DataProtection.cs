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



        // TODO: ADD ENTROPY 'SEED' DATA, WHICH SHOULD BE READ ON LAUNCH AND WHEN TOKEN IS UPDATED
        // We will store an appinfo.json file with inconspicuous data. Fields include:
        //  - application version
        //  - first run timestamp
        //  - most recent account username (used to auto-populate username field, but also for entropy generation)
        // As discussed in OneNote, this will be retrieved on each application startup and re-generated if the
        //  application version field changes (or if the file is empty or does not exist).
        // Will also be updated whenever the most recent username changes (this will only ever change when the
        //  user successfully logs in, and might just be reset to what it already was). We will be using the most
        //  recent username as part of the entropy 'seed', so the entire file must be immediately updated when a
        //  new recent username is acquired. IMPORTANT: We must ensure this file is perfectly valid BEFORE
        //  writing anything to the refresh token .dat file (otherwise decryption will fail completely if the
        //  username has changed but the new refresh token was written using the old username seed).
        // Also on launch, we must pass the most recent account username to AppStateData so it can be used
        //  to auto-populate the username field to the most recent username on login section shown). We are
        //  only concerned with username when it changes and when writing token (is not checked on startup).

        // PROCESS:
        //  - Try to read file on application startup.
        //      - If does not exist, create new with empty username string. Refresh tokens file cannot be read, reset it.
        //      - If does exist, check whether stored application version matches actual version.
        //          - If mismatch, refresh token file must be reset and user is forced to log in (also update stored version).
        //          - If matching, simply read values and use them to read the existing refresh tokens .dat file.
        //  - Any time that a successful login is made (new token received), check if stored and new usernames match.
        //      - If different, then the file must first be updated with new username BEFORE writing refresh tokens file.
        //          --- REMEMBER: Writing refresh token will open and close the file directly, not reading any in-memory data.
        //      - If the same, then the existing encryption/decryption will continue working, so just write refresh token.



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
