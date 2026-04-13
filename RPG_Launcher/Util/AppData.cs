using RPG_Launcher.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPG_Launcher.Util
{
    /// <summary>
    /// This static class contains various data describing the application and its state. Data includes
    ///  hard-coded application version, as well as per-application-user data like first run timestamp,
    ///  client GUID, and the last-logged-in (saved) username. This class also stores in memory the
    ///  current refresh and access tokens for the login API.
    /// Define application version here.
    /// </summary>
    public static class AppData
    {
        private static bool isInitialized = false;

        /// <summary>
        /// Data class used to represent JSON data stored in appinfo.json.
        /// </summary>
        private class AppDataJson
        {
            public string Version { get; private set; }
            public string Timestamp { get; private set; }
            public string ClientGuid { get; private set; }
            public string SavedUsername { get; private set; }
            public string PathToExecutable { get; private set; }

            public AppDataJson(string version, string timestamp, string clientGuid, string savedUsername, string pathToExecutable)
            {
                Version = version;
                Timestamp = timestamp;
                ClientGuid = clientGuid;
                SavedUsername = savedUsername;
                PathToExecutable = pathToExecutable;
            }



            public static AppDataJson CreateNew()
            {
                return new AppDataJson(version, DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), string.Empty, defaultPathToExecutable);
            }

            public static AppDataJson CreateNewWithUsername(string username)
            {
                return new AppDataJson(version, DateTime.UtcNow.ToString(), Guid.NewGuid().ToString(), username, defaultPathToExecutable);
            }

            public static AppDataJson UpdateExistingVersion(AppDataJson appData, string version)
            {
                appData.Version = version;
                return appData;
            }

            public static AppDataJson UpdateExistingUsername(AppDataJson appData, string username)
            {
                appData.SavedUsername = username;
                return appData;
            }
        }



        // Application version, hard-coded. Publicly-readable Version property is used to read this.
        private static readonly string version = "0.4.0";
        // Path to appdata.json file (should be working directory).
        private static readonly string appDataPath = "appdata.json";
        // Default path to executable.
        private static readonly string defaultPathToExecutable = "/";



        #region BASIC APP DATA (LOADS ON STARTUP)

        // Publicly-readable application version.
        public static string Version { get; private set; } = version;   // Not populated from file.

        // Timestamp (string format derived from DateTime) that is generated the first time the application is run.
        public static string Timestamp { get; private set; } = string.Empty;

        // Application-specific GUID is used to identify the application sending a request to the server. This
        //  will help the server determine if login requests are valid (ex. repeated requests with different
        //  GUIDs is suspicious, or a spam request from one client but a legitimate request from another may
        //  indicate that the real user is attempting to log in legitimately while a malicious actor is
        //  spamming login attempts to their account). This GUID is generated once on initial run, alongside
        //  other basic identifying information like initial run DateTime.
        public static Guid ClientGuid { get; set; } = Guid.Empty;

        // Saved username is the username that this application most recently logged in with. It is used
        //  exclusively to auto-populate the 'username' login field with the most recent account username.
        // Whenever SavedUsername is updated, we immediately write the change to appdata.json.
        private static string savedUsername = string.Empty;
        public static string SavedUsername
        {
            get => savedUsername;
            set
            {
                savedUsername = value;
                SaveAppData(new AppDataJson(Version, Timestamp, ClientGuid.ToString(), savedUsername, PathToExecutable));
            }
        }

        // Path to executable is the path to the folder/directory where the game executable is located.
        private static string pathToExecutable = string.Empty;
        public static string PathToExecutable
        {
            get => pathToExecutable;
            set
            {
                pathToExecutable = value;
                SaveAppData(new AppDataJson(Version, Timestamp, ClientGuid.ToString(), SavedUsername, pathToExecutable));
            }
        }

        #endregion

        #region IN-MEMORY TOKENS

        // Refresh token is long-lived and is securely written to disk. Loads on startup when Initialize() is
        //  called, and is re-written to file anytime the property is updated.
        private static string refreshToken = string.Empty;
        public static string RefreshToken
        {
            get => refreshToken;
            set
            {
                refreshToken = value;
                DataProtection.SaveRefreshToken(refreshToken);
            }
        }

        // Access token is short-lived and is never stored outside of memory. A new access token is received
        //  upon login, and one must be received each time the application is run.
        public static string AccessToken { get; set; } = string.Empty;

        // Like access token, PasswordResetToken is short-lived and only ever exists in memory. This token is
        //  populated once the client (this launcher) successfully verifies itself and requests a password
        //  reset via the login API. Reset tokens only last a very short time (5 minutes).
        public static string PasswordResetToken { get; set; } = string.Empty;

        #endregion



        /// <summary>
        /// Initializes the static AppData class, which retrieves basic application data from the
        ///  appdata.json file. This data is used by the application for both application logic and
        ///  UI purposes. This method also automatically loads the securely-stored user refresh
        ///  token into memory, which is accessible using this static class.
        /// </summary>
        public static void Initialize()
        {
            // Try to load data from appdata.json, then try to load refresh token.
            if (!isInitialized)
            {
                InitializeAppData();

                // This method requires AppData to be initialized already.
                refreshToken = DataProtection.LoadRefreshToken();

                isInitialized = true;
            }
        }

        /// <summary>
        /// Private method for actual app data retrieval on initialization. Reads appdata.json for basic
        ///  application data like version, timestamp, client GUID, and saved username. Data which
        ///  changes at runtime is immediately updated elsewhere in this class, but is initially read here.
        /// </summary>
        private static void InitializeAppData()
        {
            // PROCESS:
            //  - Try to read appdata.json file on application startup.
            //      - If does not exist, create new with empty username string. Refresh tokens file cannot be read, reset it.
            //      - If does exist, check whether stored application version matches actual version.
            //          - If mismatch, must update stored version immediately to ensure file and memory states match.
            //          - If matching, simply read values and use them to read the existing refresh token .dat file.

            try
            {
                AppDataJson? appData;

                // First, check if appdata.json file exists.
                if (!File.Exists(appDataPath))
                {
                    // If file does not exist, create a new file with initial-run data.
                    appData = AppDataJson.CreateNew();
                    SaveAppData(appData);

                    // Store newly-generated data in static fields (saved username remains empty).
                    Timestamp = appData.Timestamp;
                    ClientGuid = Guid.Parse(appData.ClientGuid);
                    PathToExecutable = appData.PathToExecutable;    // Uses default path on AppDataJson creation.

                    // Since file does not exist, we cannot read refresh token, so reset token and return.
                    DataProtection.ResetRefreshToken();
                    return;
                }

                // Else if file does exist, read JSON into AppInfoJson object and compare old data against current.
                appData = LoadAppData();
                if (appData != null)
                {
                    // If there is a version change (app updated), we should immediately update the file with the
                    //  new version before fully loading fields.
                    if (appData.Version != version)
                    {
                        // Update only the stored version using static AppDataJson method, then write immediately.
                        AppDataJson.UpdateExistingVersion(appData, version);
                        SaveAppData(appData);

                    }

                    // Store values read from the file in fields.
                    Timestamp = appData.Timestamp;
                    ClientGuid = Guid.Parse(appData.ClientGuid);
                    SavedUsername = appData.SavedUsername;
                    PathToExecutable = appData.PathToExecutable;
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
            }
        }



        /// <summary>
        /// Reads from the stored appdata.json file, returning a new AppDataJson instance with the retrieved
        ///  data, or null if failure for any reason.
        /// </summary>
        /// <returns> The populated AppDataJson object if successful, else null. </returns>
        private static AppDataJson? LoadAppData()
        {
            try
            {
                return JsonSerializer.Deserialize<AppDataJson>(File.ReadAllText(appDataPath));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// Writes the passed-in AppDataJson object to appdata.json, automatically serializing into JSON format.
        /// </summary>
        /// <param name="appInfo"> The AppDataJson object to serialize and write to appdata.json. </param>
        private static void SaveAppData(AppDataJson appInfo)
        {
            JsonSerializerOptions options = new() { WriteIndented = true };   // Pretty printing.

            try
            {
                string jsonString = JsonSerializer.Serialize(appInfo, options);
                File.WriteAllText(appDataPath, jsonString);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

    }
}
