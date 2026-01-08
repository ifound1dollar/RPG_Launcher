using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Launcher.Model
{
    public static class AppStateData
    {
        private static bool isInitialized = false;

        // Refresh token is long-lived and is securely written to disk. Loads on startup when Initialize() is
        //  called, and is re-written to file anytime the property is updated.
        private static string refreshToken = string.Empty;                  // Loads on startup
        public static string RefreshToken
        {
            get => refreshToken;
            set
            {
                refreshToken = value;
                DataProtection.SaveRefreshToken(refreshToken, SavedUsername);   // Immediately write new token to file when changed.
            }
        }

        // Access token is short-lived and is never stored outside of memory. A new access token is received
        //  upon login, and one must be received each time the application is run.
        public static string AccessToken { get; set; }  = string.Empty;

        // Application-specific GUID is used to identify the application sending a request to the server. This
        //  will help the server determine if login requests are valid (ex. repeated requests with different
        //  GUIDs is suspicious, or a spam request from one client but a legitimate request from another may
        //  indicate that the real user is attempting to log in legitimately while a malicious actor is
        //  spamming login attempts to their account). This GUID is generated once on initial run, alongside
        //  other basic identifying information like initial run DateTime.
        public static Guid ClientGuid { get; set; } = Guid.Empty;           // Loads on startup.

        // Saved username is the username that this application most recently logged in with. It is used
        //  exclusively to auto-populate the 'username' login field with the most recent account username.
        public static string SavedUsername { get; set; } = string.Empty;    // Loads on startup.





        /// <summary>
        /// Initializes the AppStateData tracker class, retrieving and holding persistent data like
        ///  client GUID, saved username, and refresh token string. This data remains active in-memory
        ///  until the application is shut down.
        /// </summary>
        public static void Initialize()
        {
            // Try to load refresh token from secure file immediately when initialized.
            if (!isInitialized)
            {
                refreshToken = DataProtection.InitializeAndGetToken();
                isInitialized = true;
            }
        }

        /// <summary>
        /// De-initializes the AppStateData tracker class, storing persistent data on shutdown.
        /// </summary>
        public static void Deinitialize()
        {
            // Save stored refresh token to file on shutdown.
            //DataProtection.SaveRefreshToken(RefreshToken);
        }
    }
}
