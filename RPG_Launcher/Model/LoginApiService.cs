using Microsoft.IdentityModel.Tokens;
using RPG_Launcher.Model.Data;
using RPG_Launcher.Model.Responses;
using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://localhost:7127/api/")
        };



        /// <summary>
        /// Pings the API to determine whether the server is online. Returns a status code describing the success or
        ///  failure of the ping.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, 1 for server offline, -1 for other exception. </returns>
        public async Task<int> PingServer()
        {
            try
            {
                // The HTTP request will throw an HttpRequestException if the server is offline. Thus, we can directly return 0.
                using HttpResponseMessage response = await _httpClient.GetAsync("ping");
                return 0;
            }
            catch (HttpRequestException ex)
            {
                Trace.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Attempts to log in to the API using an existing refresh token. Makes an HTTPS request to the API
        ///  endpoint. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, 1 for email not confirmed, 2 for password needs reset, -1 for generic failure. </returns>
        public async Task<int> TryLoginFromRefreshToken()
        {
            // Ensure refreshToken is not empty.
            if (string.IsNullOrEmpty(AppData.RefreshToken)) return -1;

            try
            {
                // Make request to API and check response code.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/login-refresh")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { RefreshToken = AppData.RefreshToken }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    Trace.WriteLine($"bad status code (code: {rawResponse.StatusCode})");

                    // If not success status code, then there was some error, so remove refresh token and return -1.
                    AppData.RefreshToken = string.Empty;
                    return -1;
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    Trace.WriteLine("cannot parse into LoginResponseModel");

                    // If somehow we encounter a response model error, clear refresh token and return -1.
                    AppData.RefreshToken = string.Empty;
                    return -1;
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.AccessToken = responseModel.AccessToken;
                AppData.RefreshToken = responseModel.RefreshToken;
                return responseModel.LoginStatusCode;                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Attemps to log in to the API normally with username and password. Makes an HTTPS request to
        ///  the API endpoint. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for account not confirmed, -1 for generic failure. </returns>
        public async Task<int> Login(NetworkCredential credential)
        {
            // Ensure credentials are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)) return -1;

            try
            {
                // Make request to API and check response code.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/login")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { Username = credential.UserName, Password = credential.Password }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    Trace.WriteLine($"bad status code (code: {rawResponse.StatusCode})");

                    // If not success status code, then there was some error, so return -1.
                    return -1;
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    Trace.WriteLine("cannot parse into LoginResponseModel");

                    // If somehow we encounter a response model error, return -1.
                    return -1;
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = credential.UserName;        // Save currently-logged-in username on success.
                AppData.AccessToken = responseModel.AccessToken;
                AppData.RefreshToken = responseModel.RefreshToken;
                return responseModel.LoginStatusCode;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Attempts to register a new user to the API with an email, username, and password. Makes an HTTPS
        ///  request to the API endpoint. Returns a status code describing attempt success or failure, and
        ///  updates AppData token fields within the method.
        /// </summary>
        /// <param name="email"> The email associated with the account attempting to be registered. </param>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for invalid input, -1 for generic failure. </returns>
        public async Task<int> Register(string email, NetworkCredential credential)
        {
            // Ensure email and credentials are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)
                || string.IsNullOrEmpty(email))
            {
                return -1;
            }

            try
            {
                // Make request to API and check response code.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/register")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { Username = credential.UserName, Email = email, Password = credential.Password }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return -1.
                    return -1;
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return -1;
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = credential.UserName;        // Save currently-logged-in username on success.
                AppData.AccessToken = responseModel.AccessToken;
                AppData.RefreshToken = responseModel.RefreshToken;
                return responseModel.LoginStatusCode;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Logs out of the API. Makes an HTTPS request to the API endpoint. Logout is always successful, even
        ///  if no acknowledgement is received from the server. Automatically invalidates any stored tokens.
        /// </summary>
        public async Task Logout()
        {
            // Call the API logout endpoint, passing it our access token so it can find us (logout requires valid access token).
            // This method will not need to return anything. If we call this logout endpoint with an access token that the
            //  server cannot find (somehow), it does not matter because the server will not allow us to use any endpoint
            //  anyway. The access token will contain our username which is used to remove our refresh token as well (logout
            //  should log the user out of everything).

            if (string.IsNullOrEmpty(AppData.AccessToken) && string.IsNullOrEmpty(AppData.RefreshToken)) return;

            try
            {
                // Make request to API, no response or content but requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/logout");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

            // Clear all tokens after logout, regardless of whether error was thrown. Fully log out client-side.
            AppData.AccessToken = string.Empty;
            AppData.RefreshToken = string.Empty;
        }





        /// <summary>
        /// Requests a new confirmation code from the login API. Does not return any usable status code
        ///  for security reasons, but checks here for valid input state.
        /// </summary>
        /// <param name="targetUser"> The account username or email to send the confirmation/verification code to. </param>
        public async Task SendConfirmationCode(string targetUser)
        {
            if (string.IsNullOrEmpty(targetUser)) return;

            try
            {
                // Make request to API, no response or content AND no access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/confirmation-code")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { UsernameOrEmail = targetUser }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);

                // We do not know whether the request was successful; sharing information like username/password not
                //  found is a security vulnerability, so we make API request and return success.
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Attempts to confirm the email of the currently-logged-in account. Sends the current refresh token
        ///  to the API along with the passed-in verification code that will have been sent to the user's
        ///  email. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="verificationCode"> The user-supplied verification code, which should have been received via email. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for invalid input, -1 for generic failure. </returns>
        public async Task<int> VerifyAccountEmail(string verificationCode)
        {
            if (string.IsNullOrEmpty(AppData.RefreshToken) || string.IsNullOrEmpty(verificationCode))
            {
                return -1;
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/verify-email")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { Code = verificationCode }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return -1.
                    return -1;
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return -1;
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.AccessToken = responseModel.AccessToken;
                AppData.RefreshToken = responseModel.RefreshToken;
                return responseModel.LoginStatusCode;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Requests a password reset token from the login API, passing in the associated username or email with
        ///  the verification code received via email. Does not require a refresh token because the forgot
        ///  password functionality will not have a valid refresh token. Returns a status code describing whether
        ///  the reset token request was successful.
        /// </summary>
        /// <param name="usernameOrEmail"> The account username or email that the password reset is being requested for. </param>
        /// <param name="verificationCode"> The one-time verification code received by the user via email. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for invalid input, -1 for request denied. </returns>
        public async Task<int> RequestPasswordReset(string usernameOrEmail, string verificationCode)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(verificationCode))
            {
                return -1;
            }

            try
            {
                // Make request to API with content and parse response.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/request-password-reset")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { UsernameOrEmail = usernameOrEmail, Code = verificationCode }),
                            Encoding.UTF8,
                            "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return -1.
                    return -1;
                }

                // Parse raw response into response model.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<PasswordResetTokenResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return -1;
                }

                // Store reset token, then return 0 for success.
                AppData.PasswordResetToken = responseModel.ResetToken;
                return 0;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Attempts to reset the current account's password via a login API call. Passes the username and password
        ///  form a NetworkCredential and requires a valid in-memory password reset token to be successful. Returns a
        ///  status code describing whether the password reset was successful.
        /// </summary>
        /// <param name="credential"> A NetworkCredential containing the existing username and the new password. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for invalid input, 2 for same password as old, -1 for generic failure. </returns>
        public async Task<int> ResetPasswordFromToken(NetworkCredential credential)
        {
            // Ensure credentials and reset token are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)
                || string.IsNullOrEmpty(AppData.PasswordResetToken))
            {
                return -1;
            }

            try
            {
                // Make request to API, requiring content and access token (with reset Role).
                var request = new HttpRequestMessage(HttpMethod.Post, "users/reset-password")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { NewPassword = credential.Password }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.PasswordResetToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return -1.
                    return -1;
                }

                // Parse raw response into response model.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<ResetPasswordCompleteResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return -1;
                }

                // If unsuccessful, then old password matched new password (return 1 for same password).
                if (!responseModel.Success)
                {
                    return 1;
                }

                // Else successful, so immediately log out and return 0 for success.
                await Logout();
                AppData.PasswordResetToken = string.Empty;
                return 0;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }
    }
}
