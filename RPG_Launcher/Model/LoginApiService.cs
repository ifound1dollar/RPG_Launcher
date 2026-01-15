using Microsoft.IdentityModel.Tokens;
using RPG_Launcher.Model.Data;
using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Launcher.Model
{
    /// <summary>
    /// Singleton class that directly accesses the login API. Use this class via LoginApiService.Instance.
    /// </summary>
    public class LoginApiService
    {
        #region API SERVICE SINGLETON STUFF

        private static readonly LoginApiService _instance = new();
        /// <summary>
        /// The active LoginApiService singleton instance. Use this to access the LoginApiService.
        /// </summary>
        public static LoginApiService Instance { get => _instance; }

        static LoginApiService()
        {
            // Empty constructor for static singleton pattern. See fourth pattern for why this
            //  is necessary for thread safety: https://csharpindepth.com/articles/singleton
        }

        private LoginApiService()
        {
            // This is the actual constructor that is called.
        }

        #endregion



        /// <summary>
        /// Attempts to log in to the API using an existing refresh token. Makes an HTTPS request to the API
        ///  endpoint. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, 1 for account not confirmed, -1 for generic failure. </returns>
        public int TryLoginFromRefreshToken()
        {
            // Ensure refreshToken is not empty.
            if (string.IsNullOrEmpty(AppData.RefreshToken)) return -1;

            string[]? tokens = TempLoginApiImitator.LoginFromRefreshToken(AppData.RefreshToken, AppData.ClientGuid);
            if (tokens != null && tokens.Length == 1)
            {
                // If length 1, then we only received refresh token which means the account email is not yet verified.
                AppData.RefreshToken = tokens[0];
                return 1;
            }
            else if (tokens != null && tokens.Length == 2)
            {
                // If length 2, then we are fully logged in and received an access token and a new refresh token.
                AppData.AccessToken = tokens[0];
                AppData.RefreshToken = tokens[1];
                return 0;
            }

            // Else if tokens remains null or unexpected size, return -1 for error.
            AppData.RefreshToken = string.Empty;
            return -1;
        }

        /// <summary>
        /// Attemps to log in to the API normally with username and password. Makes an HTTPS request to
        ///  the API endpoint. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for account not confirmed, -1 for generic failure. </returns>
        public int Login(NetworkCredential credential)
        {
            // Ensure credentials are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)) return -1;

            // Actual login API endpoint will take username and password strings, and returns two token strings.
            string[]? tokens = TempLoginApiImitator.Login(credential.UserName, credential.Password, AppData.ClientGuid);
            if (tokens != null && tokens.Length == 1)
            {
                // If length 1, then we only received refresh token which means the account email is not yet verified.
                AppData.SavedUsername = credential.UserName;
                AppData.RefreshToken = tokens[0];
                return 1;
            }
            else if (tokens != null && tokens.Length == 2)
            {
                // If length 2, then we are fully logged in and received an access token and a new refresh token.
                AppData.SavedUsername = credential.UserName;
                AppData.AccessToken = tokens[0];
                AppData.RefreshToken = tokens[1];
                return 0;
            }

            // Else if tokens remains null or unexpected size, return -1 for error.
            return -1;
        }

        /// <summary>
        /// Attempts to register a new user to the API with an email, username, and password. Makes an HTTPS
        ///  request to the API endpoint. Returns a status code describing attempt success or failure, and
        ///  updates AppData token fields within the method.
        /// </summary>
        /// <param name="email"> The email associated with the account attempting to be registered. </param>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for invalid input, -1 for generic failure. </returns>
        public int Register(string email, NetworkCredential credential)
        {
            // Ensure email and credentials are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)
                || string.IsNullOrEmpty(email))
            {
                return 1;      // 1 for invalid input
            }

            // Actual register API endpoint will take email, username, and string, then returns both tokens.
            var tokens = TempLoginApiImitator.Register(email, credential.UserName, credential.Password, AppData.ClientGuid);
            if (tokens != null && tokens.Length == 1)
            {
                // Store saved username after successful login (must be done before refresh token is written).
                AppData.SavedUsername = credential.UserName;
                
                // Pull refresh token from API request (will be JSON in the future). Will check for response status code.
                AppData.RefreshToken = tokens[0];
                return 0;       // 0 for success
            }

            return -1;           // -1 for generic registration failure (email or username unavailable)
        }

        /// <summary>
        /// Attempts to confirm the email of the currently-logged-in account. Sends the current refresh token
        ///  to the API along with the passed-in verification code that will have been sent to the user's
        ///  email. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="verificationCode"> The user-supplied verification code, which should have been received via email. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for invalid input, -1 for generic failure. </returns>
        public int ConfirmAccountEmail(string verificationCode)
        {
            if (string.IsNullOrEmpty(AppData.RefreshToken) || string.IsNullOrEmpty(verificationCode))
            {
                return 1;       // 1 for invalid input state
            }

            // Actually make API call and attempt confirmation.
            if (TempLoginApiImitator.ConfirmAccountEmail(AppData.RefreshToken, verificationCode))
            {
                return 0;
            }

            return -1;          // 2 for generic confirmation failure
        }

        /// <summary>
        /// Logs out of the API. Makes an HTTPS request to the API endpoint. Logout is always successful, even
        ///  if no acknowledgement is received from the server. Automatically invalidates any stored tokens.
        /// </summary>
        public void Logout()
        {
            // Call the API logout endpoint, passing it our access token so it can find us (logout requires valid access token).
            // This method will not need to return anything. If we call this logout endpoint with an access token that the
            //  server cannot find (somehow), it does not matter because the server will not allow us to use any endpoint
            //  anyway. The access token will contain our username which is used to remove our refresh token as well (logout
            //  should log the user out of everything).

            TempLoginApiImitator.Logout(AppData.RefreshToken);

            // After logout, reset access and refresh tokens to empty.
            AppData.AccessToken = string.Empty;
            AppData.RefreshToken = string.Empty;
        }
    }
}
