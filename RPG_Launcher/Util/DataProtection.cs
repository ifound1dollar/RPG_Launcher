using RPG_Launcher.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPG_Launcher.Util
{
    public static class DataProtection
    {
        /// ----- DATA PROTECTION DESCRIPTION -----
        /// This DataProtection class exists exclusively to securely store data in the project's working
        ///  directory. It utilizes Windows' Data Protection API (DPAPI) to securely encrypt and decrypt
        ///  data that is strictly clamped to the current windows user.
        /// The hard-coded entropyBase byte[] along with the appinfo.json file work together to generate
        ///  what is effectively a symmetric encryption/decryption key that this class will use for all
        ///  Protect() and Unprotect() operations. Upon application initialization, the AppStateData
        ///  class calls the InitializeAndGetToken() method to validate appinfo.json and retrieve the
        ///  stored refresh token, if applicable.
        /// Data stored in appinfo.json is not stored in any field, but is instead read directly from
        ///  the file whenever it is needed. The AppInfoJson nested class exists exclusively to hold the
        ///  JSON data retrieved from the file and is naturally used for writing JSON data as well.
        /// When the refresh token is read or written from/to disk, the appinfo.json file is retrieved
        ///  to generate the DPAPI 'entropy' key. This follows a pretty simple algorithm in which each
        ///  byte in entropyBase is multiplied against bytes in the strings stored in the JSON file.
        ///  The refresh token is only read once on startup, but is re-written whenever a new refresh
        ///  token is received from the server (upon explicit login or access token renewal).
        ///  
        /// FOR APPLICATION SECURITY, the entropyBase byte[] must be updated EVERY SINGLE TIME that
        ///  the application code is modified and thus the version changes. This helps reduce the
        ///  likelihood that a user will have cracked the scheme because the 'entropy' key will be
        ///  entirely different each version of the application. Upon application load, this class
        ///  automatically checks for a mismatch between the stored version in appinfo.json and
        ///  the actual version (defined somewhere in the code). If there is a mismatch, the entire
        ///  appinfo.json file is regenerated before the DPAPI can be used.
        /// Whenever the application version changes, the entropyBase encryption key 'seed' will
        ///  also have been changed, so any stored refresh token will be entirely inaccessible
        ///  anyway (different key entirely). Completely regenerating the file is advantageous
        ///  for security.
        /// ----- END DESCRIPTION -----

        // This is the hard-coded base entropy that is attached directly to the application. This is not
        //  the final value (consider it a 'seed').
        // https://stackoverflow.com/questions/1326001/windows-dpapi-what-to-do-with-entropy
        // https://stackoverflow.com/questions/2585746/securely-storing-optional-entropy-while-using-dpapi
        private static readonly byte[] seed =
            [ 73, 161, 134, 115,   46, 185, 242, 41,   218, 14, 199, 147,   16, 131, 186, 8 ];

        private static readonly string credentialsPath = "credentials.dat";





        /// <summary>
        /// Writes the passed-in refresh token to a secure credentials file.
        /// </summary>
        /// <param name="token"> The JWT token in string form. </param>
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
                // Importantly, AppData.SavedUsername should have been updated before calling this method. Any
                //  mismatch will cause reading the refresh token file in the future to fail (invalid key).

                using FileStream fileStream = new(credentialsPath, FileMode.OpenOrCreate, FileAccess.Write);
                using StreamWriter sw = new(fileStream);

                byte[] originalText = Encoding.UTF8.GetBytes(token);
                byte[] encryptedText = ProtectedData.Protect(originalText, GetEntropy(), DataProtectionScope.CurrentUser);
                sw.WriteLine(Convert.ToBase64String(encryptedText));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        /// <summary>
        /// Reads the stored refresh token from the secure credentials file.
        /// </summary>
        /// <returns> The retrieved JWT token string. </returns>
        public static string LoadRefreshToken()
        {
            try
            {
                using FileStream fileStream = new(credentialsPath, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fileStream);
                string? line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) return string.Empty;

                byte[] encryptedText = Convert.FromBase64String(line);
                byte[] originalText = ProtectedData.Unprotect(encryptedText, GetEntropy(), DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(originalText);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Resets the secure credentials file to empty. Should be called whenever the user logs out.
        /// </summary>
        public static void ResetRefreshToken()
        {
            try
            {
                // FileMode.Create will completely overwrite existing data.
                using FileStream fileStream = new(credentialsPath, FileMode.Create, FileAccess.Write);
                using StreamWriter sw = new(fileStream);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }



        /// <summary>
        /// Calculates and retrieves an 'entropy' byte[] which is used by the DPAPI to securely encode and
        ///  decode (Protect/Unprotect) files. Can be thought of as a symmetric ecryption/decryption key.
        /// </summary>
        /// <returns> The calculated DPAPI 'entropy' byte[]. </returns>
        private static byte[] GetEntropy()
        {
            // Here is our logic for using data retrieved from appdata.json (stored in AppData class) and using
            //  it to securely store the user's refresh token.

            byte[] entropy = new byte[seed.Length];
            Array.Copy(seed, entropy, seed.Length);

            // TIMESTAMP SCRAMBLING
            byte[] bytes = Encoding.UTF8.GetBytes(AppData.Timestamp);
            for (int i = 0; i < entropy.Length; i++)
            {
                // Multiply the current byte directly to the new entropy byte[] byte, using % to avoid out-of-bounds.
                entropy[i] *= bytes[i % bytes.Length];
            }

            // SAVEDUSERNAME SCRAMBLING
            bytes = Encoding.UTF8.GetBytes(AppData.SavedUsername);
            for (int i = 0; i < entropy.Length; i++)
            {
                entropy[i] *= bytes[i % bytes.Length];
            }

            return entropy;
        }
    }
}
