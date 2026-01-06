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
        public static string RefreshToken { get; set; } = string.Empty;

        // Access token is short-lived and is never stored outside of memory. A new access token is received
        //  upon login, and one must be received each time the application is run.
        public static string AccessToken { get; set; }  = string.Empty;



        public static void Initialize()
        {
            // Try to load refresh token from secure file immediately when initialized.
            if (!isInitialized)
            {
                RefreshToken = DataProtection.LoadRefreshToken();
                isInitialized = true;
            }
        }

        public static void Deinitialize()
        {
            // Save stored refresh token to file on shutdown.
            DataProtection.SaveRefreshToken(RefreshToken);
        }
    }
}
