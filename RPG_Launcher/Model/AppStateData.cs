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

        // Refresh token is long-lived and is securely written to disk. Loads on startup, writes on shutdown.
        private static string refreshToken = string.Empty;
        public static string RefreshToken
        {
            get => refreshToken;
            set
            {
                refreshToken = value;
                DataProtection.SaveRefreshToken(refreshToken);  // Immediately write new token to file when changed.
            }
        }

        // Access token is short-lived and is never stored outside of memory. A new access token is received
        //  upon login, and one must be received each time the application is run.
        public static string AccessToken { get; set; }  = string.Empty;

        // Application instance GUID is used to identify the application sending a request to the server. This
        //  will help the server determine if login requests are valid (ex. repeated requests with different
        //  GUIDs is suspicious, or a spam request from one client but a legitimate request from another may
        //  indicate that the real user is attempting to log in legitimately while a malicious actor is
        //  spamming login attempts to their account).
        public static Guid InstanceGuid { get; private set; } = new Guid();

        // Saved username is the username that this application most recently logged in with. It is used
        //  exclusively to auto-populate the 'username' login field with the most recent account username.
        public static string SavedUsername { get; private set; } = string.Empty;



        // TODO: ACTUALLY USE GUID IN FAKE API LOGIN REQUESTS (WILL BE SENT AS JSON)



        public static void Initialize()
        {
            // Try to load refresh token from secure file immediately when initialized.
            if (!isInitialized)
            {
                RefreshToken = DataProtection.LoadRefreshToken();
                isInitialized = true;
            }

            // TODO: ACTUALLY PULL THIS VALUE FROM FILE
            SavedUsername = "savedusername";
        }

        public static void Deinitialize()
        {
            // WE DO NOT REALLY NEED TO DO THIS, AS WE UPDATE THE TOKEN ANY TIME IT IS CHANGED.

            // Save stored refresh token to file on shutdown.
            DataProtection.SaveRefreshToken(RefreshToken);
        }
    }
}
