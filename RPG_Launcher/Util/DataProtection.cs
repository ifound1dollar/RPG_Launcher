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

        /// <summary>
        /// Data class used to represent JSON data stored in appinfo.json.
        /// </summary>
        private class AppInfoJson
        {
            public string Version { get; private set; }
            public string Timestamp { get; private set; }
            public string ClientGuid { get; private set; }
            public string SavedUsername { get; private set; }

            public AppInfoJson(string version, string timestamp, string clientGuid, string savedUsername)
            {
                Version = version;
                Timestamp = timestamp;
                ClientGuid = clientGuid;
                SavedUsername = savedUsername;
            }



            public static AppInfoJson CreateNew()
            {
                return new AppInfoJson(version, DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), string.Empty);
            }

            public static AppInfoJson CreateNewWithUsername(string username)
            {
                return new AppInfoJson(version, DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), username);
            }

            public static AppInfoJson UpdateExistingUsername(AppInfoJson appInfo, string username)
            {
                appInfo.SavedUsername = username;
                return appInfo;
            }
        }

        // This is the hard-coded base entropy that is attached directly to the application. This is not
        //  the final value (consider it a 'seed').
        // https://stackoverflow.com/questions/1326001/windows-dpapi-what-to-do-with-entropy
        // https://stackoverflow.com/questions/2585746/securely-storing-optional-entropy-while-using-dpapi
        private static readonly byte[] entropyBase =
            [ 73, 161, 134, 115,   46, 185, 242, 41,   218, 14, 199, 147,   16, 131, 186, 8 ];
        private static readonly string version = "0.0.1";

        private static readonly string credentialsPath = "credentials.dat";
        private static readonly string appInfoPath = "appinfo.json";
        private static readonly JsonSerializerOptions options = new() { WriteIndented = true };     // Indented for easy reading.





        /// <summary>
        /// Initializes the DataProtection class, which reads appinfo.json for basic application state
        ///  and creates/updates it as necessary. Populates fields in AppStateData that exist for the 
        ///  lifetime of the application.
        /// </summary>
        /// <returns></returns>
        public static string InitializeAndGetToken()
        {
            // PROCESS:
            //  - Try to read appinfo.json file on application startup.
            //      - If does not exist, create new with empty username string. Refresh tokens file cannot be read, reset it.
            //      - If does exist, check whether stored application version matches actual version.
            //          - If mismatch, refresh token file must be reset and user is forced to log in (also update stored version).
            //          - If matching, simply read values and use them to read the existing refresh tokens .dat file.

            try
            {
                AppInfoJson? appInfo;

                // First, check if appinfo.json file exists.
                if (!File.Exists(appInfoPath))
                {
                    // If file does not exist, create a new file with initial-run data.
                    appInfo = AppInfoJson.CreateNew();
                    SaveAppInfo(appInfo);

                    // Store newly-generated GUID in AppStateData.
                    AppStateData.ClientGuid = Guid.Parse(appInfo.ClientGuid);

                    // Since file does not exist, we cannot read refresh token, so reset it and return.
                    ResetRefreshToken();
                    return string.Empty;
                }

                // Else if file does exist, read JSON into AppInfoJson object and compare old data against current.
                appInfo = LoadAppInfo();
                if (appInfo != null)
                {
                    // If there is a version change, we can assume that a new patch has been downloaded and our old
                    //  data protection scheme will no longer work. We need to fully re-generate the file.
                    if (appInfo.Version != version)
                    {
                        // Use existing saved username (might be empty but that is fine).
                        var newAppInfo = AppInfoJson.CreateNewWithUsername(appInfo.SavedUsername);
                        SaveAppInfo(newAppInfo);

                        // Pass app info to AppStateData before resetting and returning empty string.
                        AppStateData.ClientGuid = Guid.Parse(newAppInfo.ClientGuid);
                        AppStateData.SavedUsername = newAppInfo.SavedUsername;

                        // Because appinfo.json was invalid, refresh token is unreadable, so reset it and return.
                        ResetRefreshToken();
                        return string.Empty;
                    }

                    // Else if version matches, then the file is good-to-go. Store app info in AppStateData and return token.
                    AppStateData.ClientGuid = Guid.Parse(appInfo.ClientGuid);
                    AppStateData.SavedUsername = appInfo.SavedUsername;
                    return LoadRefreshToken();
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
            }

            // Default return null (if exception is thrown or appInfo is null).
            return string.Empty;
        }



        /// <summary>
        /// Writes the passed-in refresh token to a secure credentials file. Takes an additional 'username'
        ///  argument to ensure that the username associated with the token is stored properly.
        /// </summary>
        /// <param name="token"> The JWT token in string form. </param>
        /// <param name="username"> The username associated with the token. </param>
        public static void SaveRefreshToken(string token, string username)
        {
            // PROCESS:
            //  - Any time that a successful login is made (new token received), check if stored and new usernames match.
            //      - If different, then appinfo.json must first be updated with new username BEFORE writing refresh tokens file.
            //      - If the same, then the existing encryption/decryption will continue working, so just write refresh token.
            // Note that a new login will result in an entirely new refresh token, so the existing stored refresh token will
            //  have already been invalidated. The saved username is required for properly encrypting/decrypting the key, and
            //  checking for a mismatch ensures that the scheme will work.



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
                // First, read existing appinfo.json to see if there is a mismatch.
                var appInfo = LoadAppInfo();
                if (appInfo != null && appInfo.SavedUsername != username)
                {
                    // If mismatch, we must replace the saved username in the file (everything else remains).
                    SaveAppInfo(AppInfoJson.UpdateExistingUsername(appInfo, username));
                }

                // Now that we have handled any potential mismatch and appinfo.json is correctly updated, we can write token.
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
        private static string LoadRefreshToken()
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
        private static void ResetRefreshToken()
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
            // Here is our logic for reading appinfo.json and using it to generate entropy.

            byte[] entropy = new byte[entropyBase.Length];
            Array.Copy(entropyBase, entropy, entropyBase.Length);

            var appInfo = LoadAppInfo();
            if (appInfo != null)
            {
                // TIMESTAMP SCRAMBLING
                byte[] bytes = Encoding.UTF8.GetBytes(appInfo.Timestamp);
                for (int i = 0; i < entropy.Length; i++)
                {
                    // Multiply the current byte directly to the new entropy byte[] byte, using % to avoid out-of-bounds.
                    entropy[i] *= bytes[i % bytes.Length];
                }

                // SAVEDUSERNAME SCRAMBLING
                bytes = Encoding.UTF8.GetBytes(appInfo.SavedUsername);
                for (int i = 0; i < entropy.Length; i++)
                {
                    entropy[i] *= bytes[i % bytes.Length];
                }
            }

            // If somehow appinfo.json could not be read, we wind up returning entropyBase directly (is copied above).
            return entropy;
        }





        /// <summary>
        /// Reads from the stored appinfo.json file, returning a new AppInfoJson instance with the retrieved
        ///  data, or null if failure for any reason.
        /// </summary>
        /// <returns> The populated AppInfoJson object if successful, else null. </returns>
        private static AppInfoJson? LoadAppInfo()
        {
            try
            {
                return JsonSerializer.Deserialize<AppInfoJson>(File.ReadAllText(appInfoPath));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex); 
            }

            return null;
        }

        /// <summary>
        /// Writes the passed-in AppInfoJson object to appinfo.json, automatically serializing into JSON format.
        /// </summary>
        /// <param name="appInfo"> The AppInfoJson object to serialize and write to appinfo.json. </param>
        private static void SaveAppInfo(AppInfoJson appInfo)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(appInfo, options);
                File.WriteAllText(appInfoPath, jsonString);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
}
