using Microsoft.IdentityModel.Tokens;
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
        /// <returns> A status code describing the request result. 0 for success, 1 for account not confirmed, -1 for generic failure,
        ///  HTTP status code otherwise. </returns>
        public async Task<(int, string)> TryLoginFromRefreshToken()
        {
            // Ensure refreshToken is not empty.
            if (string.IsNullOrEmpty(AppData.RefreshToken)) return (-2, "Refresh login failed: no local refresh token found");

            try
            {
                // Make request to API and check response code.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/login-refresh")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { RefreshToken = AppData.RefreshToken, ClientGuid = AppData.ClientGuid }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so remove refresh token and return status code.
                    AppData.RefreshToken = string.Empty;
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, clear refresh token and return -1.
                    AppData.RefreshToken = string.Empty;
                    return (-1, "Refresh login failed: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.Email;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, $"Refresh login successful with login status code {responseModel.LoginStatusCode}");                
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Refresh login failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attemps to log in to the API normally with username and password. Makes an HTTPS request to
        ///  the API endpoint. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0 for success, 1 for account not confirmed, -1 for generic failure,
        ///  HTTP status code otherwise. </returns>
        public async Task<(int, string)> Login(NetworkCredential credential)
        {
            // Ensure credentials are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password))
            {
                return (-1, "Login failed: all input fields must be set");
            }

            try
            {
                // Make request to API and check response code.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/login")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { UsernameOrEmail = credential.UserName, Password = credential.Password }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Login failed: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.Email;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, $"Login successful with login status code {responseModel.LoginStatusCode}");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Login failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to register a new user to the API with an email, username, and password. Makes an HTTPS
        ///  request to the API endpoint. Returns a status code describing attempt success or failure, and
        ///  updates AppData token fields within the method.
        /// </summary>
        /// <param name="email"> The email associated with the account attempting to be registered. </param>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> Register(string email, NetworkCredential credential)
        {
            // Ensure email and credentials are not empty.
            if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)
                || string.IsNullOrEmpty(email))
            {
                return (-1, "Registration failed: all input fields must be set");
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
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1. NOTE: REGISTRATION WAS SUCCESSFUL, but we have no login model.
                    return (-1, "Registration successful, but API response error: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.Email;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, $"Registration successful for new user");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Registration failed: an unexpected error occurred during API request");
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

            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken) return;

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
        /// Requests the API resend a confirmation code to the currently-logged-in email, used for new account
        ///  email verification AND for manual email change logic (automatically determines which email to send
        ///  the code to depending on data hidden in access token). Pulls the stored access token and sends to 
        ///  the endpoint. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> ResendEmailVerificationCode()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Resend email verification code failed: local access token missing");
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/resend-email-verification-code");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Return 0 for success.
                return (0, "Resend email verification code successful");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return (-1, "Resend email verification code failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to confirm the email of the currently-logged-in account. Sends the current refresh token
        ///  to the API along with the passed-in verification code that will have been sent to the user's
        ///  email. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="confirmationCode"> The user-supplied verification code, which should have been received via email. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> VerifyAccountEmail(string confirmationCode)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Email verification failed: local access token missing or confirmation code field empty");
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/verify-email")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { Code = confirmationCode }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1. NOTE: VERIFICATION WAS SUCCESSFUL, but we have no login model.
                    return (-1, "Email verification successful, but API response error: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.Email;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, $"Email verification successful");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Email verification failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Requests a new 'forgot password' confirmation code to be sent to the account with the provided username or
        ///  email. Does not return any usable status code for security reasons, but checks here for valid input state.
        /// </summary>
        /// <param name="targetUser"> The account username or email to send the confirmation/verification code to. </param>
        /// <returns> A non-HTTP status code (custom) describing success or failure. Returns 0 if successful, -1 if HTTP request error.
        ///  NOTE: Does not return an HTTP status code for security reasons (would be vulnerable to username/email lookup attack). </returns>
        public async Task<int> ForgotPassword(string targetUser)
        {
            if (string.IsNullOrEmpty(targetUser)) return -1;

            try
            {
                // Make request to API, no response or content AND no access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/forgot-password")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { UsernameOrEmail = targetUser }),
                        Encoding.UTF8,
                        "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);

                // We do not know whether the request was successful; sharing information like username/password not
                //  found is a security vulnerability, so we make API request and return success.
                return 0;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Initiates the password reset process via the login API, passing in the associated username or email with
        ///  the verification code received via email. Does not require a refresh token because the forgot
        ///  password functionality will not have a valid refresh token. Returns a status code describing whether
        ///  the password reset process was successfully initiated.
        /// </summary>
        /// <param name="usernameOrEmail"> The account username or email that the password reset is being initiated for. </param>
        /// <param name="confirmationCode"> The one-time confirmation code received sent to the user's email. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> InitiatePasswordReset(string usernameOrEmail, string confirmationCode)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Initiate password reset failed: both input fields must be set");
            }

            try
            {
                // Make request to API with content and parse response.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/initiate-password-reset")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { UsernameOrEmail = usernameOrEmail, Code = confirmationCode }),
                            Encoding.UTF8,
                            "application/json")
                };
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into response model.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<PasswordResetTokenResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Initiate password reset failed: could not parse API response into usable object model");
                }

                // Store reset token, then return 0 for success.
                AppData.PasswordResetToken = responseModel.PasswordResetToken;
                return (0, "Initiate password reset successful");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Initiate password reset failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to reset the current account's password via a login API call. Passes the username and password
        ///  from a NetworkCredential and requires a valid in-memory password reset token to be successful. Returns a
        ///  status code describing whether the password reset was successful.
        /// </summary>
        /// <param name="credential"> A NetworkCredential containing the existing username and the new password. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> SubmitNewPasswordFromToken(NetworkCredential credential)
        {
            // Ensure credentials and reset token are not empty.
            if (string.IsNullOrEmpty(credential.Password) || string.IsNullOrEmpty(AppData.PasswordResetToken))
            {
                return (-1, "New password submission failed: local password reset token missing or missing new password input");
            }

            try
            {
                // Make request to API, requiring content and access token (with reset Role).
                var request = new HttpRequestMessage(HttpMethod.Post, "users/submit-new-password")
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
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Else successful, so immediately log out and return 0 for success.
                AppData.PasswordResetToken = string.Empty;
                await Logout();
                return (0, "New password submission successful, user must re-login");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "New password submission failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to change the current account's username via a login API call. Passes the desired new username
        ///  to the API, automatically sending the access token currently stored in memory for validation. Returns a
        ///  status code describing whether the username change was successful.
        /// </summary>
        /// <param name="newUsername"> The desired new username for this account. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> ChangeUsername(string newUsername)
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(newUsername))
            {
                return (-1, "Change username failed: local access token missing or missing new username input");
            }

            try
            {
                // Make request to API, requiring content and access token (with reset Role).
                var request = new HttpRequestMessage(HttpMethod.Post, "users/change-username")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { NewUsername = newUsername }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1. NOTE: VERIFICATION WAS SUCCESSFUL, but we have no login model.
                    return (-1, "Change username successful, but API response error: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.Email;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, $"Change username successful");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Change username failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Requests a new 'change email' confirmation code from the API for the currently-logged-in user. Automatically
        ///  passes our current access token to the endpoint, which is where the API retrieves the email to send the code
        ///  to. Returns a status code describing whether the email change request was successful.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> RequestEmailChange()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Request email change failed: local access token missing");
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/request-email-change");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Return 0 for success.
                return (0, "Request email change successful");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return (-1, "Request email change failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Initiates the manual email change process via the API, passing in a confirmation code and automatically sending
        ///  the locally-stored access token (requires full access). Returns an 'email change' access token which is stored
        ///  and will be used for submitting and verifying a new email. Returns a status code describing whether the email
        ///  change initiation was successful.
        /// </summary>
        /// <param name="confirmationCode"></param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> InitiateEmailChange(string confirmationCode)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Initiate email change failed: local access token missing or missing confirmation code");
            }

            try
            {
                // Make request to API with content and parse response.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/initiate-email-change")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { Code = confirmationCode }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into response model.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<EmailChangeTokenResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Initiate email change failed: could not parse API response into usable object model");
                }

                // Store reset token, then return 0 for success.
                AppData.EmailChangeToken = responseModel.EmailChangeToken;
                return (0, "Initiate email change successful");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Initiate email change failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Submits the user's desired new email to the API, used for email change logic. Accepts just a newEmail string,
        ///  but requires passing the currently-stored EmailChangeToken to validate access. Returns a status code
        ///  describing whether the new email submission was successful.
        /// </summary>
        /// <param name="newEmail"> The desired new email for this account. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> SubmitNewEmailFromToken(string newEmail)
        {
            // Ensure credentials and email change token are not empty.
            if (string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(AppData.EmailChangeToken))
            {
                return (-1, "Submit new email failed: local email change token missing or missing new email input");
            }

            try
            {
                // Make request to API, requiring content and access token (with reset Role).
                var request = new HttpRequestMessage(HttpMethod.Post, "users/submit-new-email")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { NewEmail = newEmail }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.EmailChangeToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Else successful, but change is only pending so do NOT clear email changed token or log out yet.
                return (0, "Submit new email successful, user must now verify new email");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Submit new email failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to verify the new email that was just submitted to the API during email change process. Accepts
        ///  a confirmation code from the email and passes the in-memory EmailChangeToken to verify endpoint access.
        ///  Returns a status code describing whether the new email verification was successful.
        /// </summary>
        /// <param name="confirmationCode"> The one-time confirmation code received sent to the desired new email. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public async Task<(int, string)> VerifyNewEmailFromToken(string confirmationCode)
        {
            // Ensure credentials and reset token are not empty.
            if (string.IsNullOrEmpty(confirmationCode) || string.IsNullOrEmpty(AppData.EmailChangeToken))
            {
                return (-1, "New email verification successful: local password reset token missing or missing new password input");
            }

            try
            {
                // Make request to API, requiring content and access token (with reset Role).
                var request = new HttpRequestMessage(HttpMethod.Post, "users/verify-new-email")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { Code = confirmationCode }),
                            Encoding.UTF8,
                            "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.EmailChangeToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Else successful, so immediately log out and return 0 for success.
                AppData.EmailChangeToken = string.Empty;
                await Logout();
                return (0, "New email verification successful, user must re-login");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "New email verification successful: an unexpected error occurred during API request");
            }
        }



        /// <summary>
        /// Pings the API to notify it that we are still in the launcher (used to maintain account state). Does not
        ///  return any data because it is a simple GET request with our access token.
        /// </summary>
        public async Task PingInLauncher()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken) return;

            try
            {
                // Make request to API, no response or content but requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/ping-in-launcher");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Notifies the API that the launcher has been closed. This should be automatically invoked on application exit
        ///  so that the API is aware of launcher exit status. Does not return any data.
        /// </summary>
        public async Task NotifyLauncherExit()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken) return;

            try
            {
                // Make request to API, no response or content but requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/notify-launcher-exit");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }



        #region Private: Access Token checking

        /// <summary>
        /// Ensures there is a valid access token stored, checking whether missing, expiring within 1 minute, or already
        ///  expired. Attempts to retrieve a new access token if the current access token is missing or invalid.
        /// </summary>
        /// <returns> True if a valid access token exists or has been retrieved, false if no valid token. </returns>
        private async Task<bool> EnsureAccessTokenIsValid()
        {
            // If no current access token OR access token is expiring within 1 minute (or already expired), try to get a new one.
            if (string.IsNullOrEmpty(AppData.AccessToken) || AppData.AccessTokenExpiration - DateTime.UtcNow < TimeSpan.FromMinutes(1))
            {
                // Try to login via refresh token, returning false if return code is != 0 (anything other than 0 does not allow access).
                var (Code, Message) = await TryLoginFromRefreshToken();
                if (Code != 0) return false;
            }

            // Else access token is good OR we got new valid token, so return true.
            return true;
        }

        #endregion
    }
}
