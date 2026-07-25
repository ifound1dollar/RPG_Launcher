using Microsoft.IdentityModel.Tokens;
using RPG_Launcher.Model.Responses;
using RPG_Launcher.Util;
using RPG_Launcher.ViewModel;
using RPG_Login_API.Models.UserResponses;
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
    /// Singleton class that directly accesses the login API. Use this class via LoginApiService.
    /// </summary>
    public static class LoginApiService
    {
        private static HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://login.edranagame.com/")
        };



        /// <summary>
        /// Pings the API to determine whether the server is online. Returns a status code describing the success or
        ///  failure of the ping.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, -1 for server offline or other exception. </returns>
        public static async Task<int> PingServer()
        {
            try
            {
                // The HTTP request will throw an HttpRequestException if the server is offline. Thus, we can directly return 0.
                using HttpResponseMessage response = await _httpClient.GetAsync("ping");
                return 0;
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
        /// <returns> A status code describing the request result. 0+ for success (custom code for login state), 1 for account not confirmed, -1 for generic failure,
        ///  HTTP status code otherwise. </returns>
        public static async Task<(int, string)> TryLoginFromRefreshToken()
        {
            // Ensure refreshToken is not empty.
            if (string.IsNullOrEmpty(AppData.RefreshToken)) return (-1, "Refresh login failed: no local refresh token found");

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
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
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
        /// <returns> A status code describing the request result. 1 for success but awaiting MFA code submission, 10 for email not verified,
        ///  20 for password needs reset, 30 for MFA not yet enabled, -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> Login(NetworkCredential credential)
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
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
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
        /// Submits a one-time MFA code to the API, used specifically for login operation. Accepts a code
        ///  generated by the user's authenticator app, and automatically sends the in-memory access token.
        ///  Returns a status code describing the success or failure of the final login step.
        /// </summary>
        /// <param name="mfaCode"> An MFA code generated by the user's authenticator app. </param>
        /// <returns> A status code describing whether the request was successful. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> SubmitMfaCodeForLogin(string mfaCode)
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(mfaCode))
            {
                return (-1, "Submit MFA code failed: missing MFA code input");
            }

            try
            {
                // Make request to API, requiring content and access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/submit-mfa-code")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { MfaCode = mfaCode }),
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
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Submit MFA code failed: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within (should be 0).
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, "Login completed using one-time MFA code");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Submit MFA code failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to register a new user to the API with an email, username, and password. Makes an HTTPS
        ///  request to the API endpoint. Returns a status code describing attempt success or failure, and
        ///  updates AppData token fields within the method.
        /// </summary>
        /// <param name="email"> The email associated with the account attempting to be registered. </param>
        /// <param name="credential"> A NetworkCredential object instantiated with the username and password. </param>
        /// <returns> A status code describing the request result. 0+ for success (custom code for login state), -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> Register(string email, NetworkCredential credential)
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
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
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
        public static async Task Logout()
        {
            // Call the API logout endpoint, passing it our access token so it can find us (logout requires valid access token).
            // This method will not need to return anything. If we call this logout endpoint with an access token that the
            //  server cannot find (somehow), it does not matter because the server will not allow us to use any endpoint
            //  anyway. The access token will contain our username which is used to remove our refresh token as well (logout
            //  should log the user out of everything).

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
        ///  email verification. Pulls the stored access token and sends to  the endpoint. Returns a status code
        ///  describing the success or failure of the request.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> ResendEmailVerificationCode()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Resend email verification code failed: could not refresh login session");
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
        /// Attempts to confirm the email of the currently-logged-in account. Is used for verifying primary email on new
        ///  account creation, automatically sending the stored access token. Returns a status code describing the success
        ///  or failure of the request.
        /// </summary>
        /// <param name="confirmationCode"> The user-supplied verification code, which should have been received via email. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> VerifyEmailForNewAccount(string confirmationCode)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Email verification failed: confirmation code field empty");
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
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                AppData.EmailChangeToken = string.Empty;                                // Reset on any verification regardless of context.
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
        public static async Task<int> ForgotPassword(string targetUser)
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
        public static async Task<(int, string)> InitiatePasswordReset(string usernameOrEmail, string confirmationCode)
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
        public static async Task<(int, string)> SubmitNewPasswordFromToken(NetworkCredential credential)
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
                _ = Logout();                                   // Do not await logout.
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
        public static async Task<(int, string)> ChangeUsername(string newUsername)
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(newUsername))
            {
                return (-1, "Change username failed: missing new username input");
            }

            try
            {
                // Make request to API, requiring content and access token.
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
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
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
        public static async Task<(int, string)> RequestEmailChange()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Request email change failed: could not refresh login session");
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
        public static async Task<(int, string)> InitiateEmailChange(string confirmationCode)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Initiate email change failed: missing confirmation code");
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
        public static async Task<(int, string)> SubmitChangedEmail(string newEmail)
        {
            // Ensure credentials and email change token are not empty.
            if (string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(AppData.EmailChangeToken))
            {
                return (-1, "Submit new email failed: local email change token missing or missing new email input");
            }

            try
            {
                // Make request to API, requiring content and access token (with reset Role).
                var request = new HttpRequestMessage(HttpMethod.Post, "users/submit-changed-email")
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
        /// Requests that the API resends a confirmation code to the user-provided new email on manual email change.
        ///  Retrieves the in-memory email change token and passes it to the endpoint, which the API will use to send
        ///  a confirmation code to the target email address. Returns a status code describing the success or failure
        ///  of the request.
        /// </summary>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> ResendChangedEmailVerificationCode()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Resend email verification code failed: could not refresh login session");
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
        /// Attempts to verify the user-submitted email on manual change. Automatically sends the in-memory email
        ///  change token to the endpoint, alongside the confirmation code that the user should have retrieved from
        ///  their desired new email address. Returns a status code describing the success or failure of the request.
        /// </summary>
        /// <param name="confirmationCode"> The user-supplied verification code, which should have been received via email. </param>
        /// <returns> A status code describing the request result. 0 for success, -1 for generic failure, HTTP status code otherwise. </returns>
        public static async Task<(int, string)> VerifyChangedEmail(string confirmationCode)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Changed email verification failed: confirmation code field empty");
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/verify-changed-email")
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

                // Parse raw response into LoginResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<LoginResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1. NOTE: VERIFICATION WAS SUCCESSFUL, but we have no login model.
                    return (-1, "Changed email verification successful, but API response error: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                AppData.EmailChangeToken = string.Empty;
                return (responseModel.LoginStatusCode, $"Changed email verification successful");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Changed verification failed: an unexpected error occurred during API request");
            }
        }


        /// <summary>
        /// Requests a new MFA credential setup, passing the stored access token to the API. Can only be called if
        ///  MFA has not yet been set up for the logged-in account, or when manually changing MFA info. Returns a status
        ///  code describing whether MFA was set up successfully, and a QR code in base64 if successful (error message otherwise).
        /// </summary>
        /// <returns> A status code describing the request result (success = 0), and a QR code in base64 string for if successful, error message otherwise. </returns>
        public static async Task<(int, string)> SetupMfa()
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Setup MFA failed: could not refresh login session");
            }

            try
            {
                // Make request to API, requiring content and access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/setup-mfa");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into MfaSetupResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<MfaSetupResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Setup MFA failed: could not parse API response into usable object model");
                }

                return (0, responseModel.OtpAuthLink);
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Setup MFA failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to verify the current (pending) MFA setup, passing in a one-time code from the authenticator
        ///  app previously setup using the QR code. Automatically passes the in-memory access token. Returns a
        ///  status code describing whether the request was successful, and a recovery code string if successful.
        /// </summary>
        /// <param name="mfaCode"> An MFA code generated by the user's authenticator app. </param>
        /// <returns> A status code describing whether the request was successful (success = 0), and a recovery code in hex format if successful (error message otherwise). </returns>
        public static async Task<(int, string)> VerifyMfaSetup(string mfaCode)
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(mfaCode))
            {
                return (-1, "Verify MFA setup failed: missing MFA code input");
            }

            try
            {
                // Make request to API, requiring content and access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/verify-mfa-setup")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { MfaCode = mfaCode }),
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

                // Parse raw response into MfaRecoveryCodeResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<MfaRecoveryCodeResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Verify MFA setup failed: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, responseModel.RecoveryCode);     // Return recovery code for display purposes.
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Verify MFA setup failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to recover the currently-logged-in account's MFA setup using a secure recovery code generated
        ///  on initial MFA setup. The user must submit their known recovery code, and the application automatically
        ///  submits the in-memory access token. Returns a status code describing whether recovery was successful, and
        ///  a new MFA QR code if successful (error message otherwise).
        /// </summary>
        /// <param name="recoveryCode"> The user's recovery code that was generated on initial MFA setup. </param>
        /// <returns> A status code describing whether the request was successful (success = 0), and an MFA QR code if successful (error message otherwise). </returns>
        public static async Task<(int, string)> RecoverMfa(string recoveryCode)
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(recoveryCode))
            {
                return (-1, "Recover MFA failed: missing recovery code input");
            }

            try
            {
                // Make request to API, requiring content and access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/recover-mfa")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { RecoveryCode = recoveryCode }),
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

                // Parse raw response into MfaSetupResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<MfaSetupResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Recover MFA failed: could not parse API response into usable object model");
                }

                return (0, responseModel.OtpAuthLink);
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Recover MFA failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Requests that the API regenerates the MFA recovery code for the currently-logged-in account. Only accepts
        ///  an access token with full account access, which is automatically submitted with the API request. Returns
        ///  a status code describing whether the new recovery code generation was successful, alongside the actual
        ///  recovery code string if successful (error message otherwise).
        /// </summary>
        /// <returns> A status code describing whether the request was successful (success = 0), and a recovery code in hex format if sucessful (error message otherwise). </returns>
        public static async Task<(int, string)> RegenerateMfaRecoveryCode()
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Regenerate MFA recovery code failed: could not refresh login session");
            }

            try
            {
                // Make request to API, requiring content and access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/regenerate-mfa-recovery-code");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Parse raw response into MfaRecoveryCodeResponseModel.
                var responseModel = await rawResponse.Content.ReadFromJsonAsync<MfaRecoveryCodeResponseModel>();
                if (responseModel == null)
                {
                    // If somehow we encounter a response model error, return -1.
                    return (-1, "Regenerate MFA recovery code failed: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                return (responseModel.LoginStatusCode, responseModel.RecoveryCode);     // Return recovery code for display purposes.
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Regenerate MFA recovery code failed: an unexpected error occurred during API request");
            }
        }

        #region Public: Secondary Account Email Setup

        /// <summary>
        /// Submits a new secondary email address to the API, beginning the secondary email setup process. Requires
        ///  an access token with full account access, which is automatically submitted in the request. The user must
        ///  submit their desired secondary email address, which the API will attempt to write to the pending new
        ///  secondary email address field. Returns a status code describing whether the request was successful.
        /// </summary>
        /// <param name="secondaryEmail"> The desired new secondary email for this account. </param>
        /// <returns> A status code describing whether the request was successful, error message if unsuccessful. </returns>
        public static async Task<(int, string)> SubmitSecondaryEmail(string secondaryEmail)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(secondaryEmail))
            {
                return (-1, "Submit secondary email failed: missing secondary email input");
            }

            try
            {
                // Make request to API, requiring content and full access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/submit-secondary-email")
                {
                    Content = new StringContent(
                            JsonSerializer.Serialize(new { SecondaryEmail = secondaryEmail }),
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

                // Else successful, but change is only pending so do NOT clear email changed token or log out yet.
                return (0, "Submit secondary email successful, user must now verify pending secondary email");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Submit secondary email failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Requests that the API resends the short-duration email confirmation code to the submitted new secondary
        ///  email. This will fail if attempted more than once every 60 seconds, or if the API endpoint is called
        ///  without a pending secondary email awaiting verification. Returns a status code describing whether the
        ///  resend was successful.
        /// </summary>
        /// <returns> A status code describing whether the request was successful, error message if unsuccessful. </returns>
        public static async Task<(int, string)> ResendSecondaryEmailVerificationCode()
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Resend secondary email verification code failed: could not refresh login session");
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/resend-secondary-verification-code");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Return 0 for success.
                return (0, "Resend secondary email verification code successful");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return (-1, "Resend secondary email verification code failed: an unexpected error occurred during API request");
            }
        }

        /// <summary>
        /// Attempts to verify the previously-submitted new secondary email for this account. Accepts a full
        ///  access token and the confirmation code that was sent to the email address for verification. If the
        ///  code is correct, the API will set the active secondary email field and return a new full access
        ///  login response model with the newly-verified secondary email. Returns a status code describing
        ///  whether the request was successful.
        /// </summary>
        /// <param name="confirmationCode"> The short-duration confirmation code sent to the new secondary email for verification. </param>
        /// <returns> A status code describing whether the request was successful, error message if unsuccessful. </returns>
        public static async Task<(int, string)> VerifySecondaryEmail(string confirmationCode)
        {
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken || string.IsNullOrEmpty(confirmationCode))
            {
                return (-1, "Secondary email verification failed: confirmation code field empty");
            }

            try
            {
                // Make request to API, requires access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/verify-secondary-email")
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
                    return (-1, "Secondary email verification successful, but API response error: could not parse API response into usable object model");
                }

                // Pull data from response (will be valid if we made it here), then return custom login code stored within.
                AppData.SavedUsername = responseModel.Username;
                AppData.SavedEmail = responseModel.PrimaryEmail;
                AppData.SecondaryEmail = responseModel.SecondaryEmail;
                AppData.RefreshToken = responseModel.RefreshToken;
                AppData.AccessToken = responseModel.AccessToken;
                AppData.AccessTokenExpiration = responseModel.AccessTokenExpiration;
                AppData.EmailChangeToken = string.Empty;                                // Reset on any verification regardless of context.
                return (responseModel.LoginStatusCode, $"Secondary email verification successful");
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Secondary email verification failed: an unexpected error occurred during API request");
            }
        }

        #endregion

        #region Public: Play game

        /// <summary>
        /// Makes a request to the API to play the game. This request will return a connect token on success which is
        ///  required for the client to connect to the online service. Returns a status code describing whether the
        ///  play game request was successful, alongside the actual connect token string if successful (error message
        ///  otherwise).
        /// </summary>
        /// <returns> A status code describing whether the request was successful (success = 0), and a connect token in base64 format if successful (error message otherwise). </returns>
        public static async Task<(int, string)> PlayGame()
        {
            // Ensure new username and access token are not empty.
            bool validToken = await EnsureAccessTokenIsValid();
            if (!validToken)
            {
                return (-1, "Play game failed: could not refresh login session");
            }

            try
            {
                // Make request to API, requiring content and access token.
                var request = new HttpRequestMessage(HttpMethod.Post, "users/play-game");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppData.AccessToken);
                var rawResponse = await _httpClient.SendAsync(request);
                if (!rawResponse.IsSuccessStatusCode)
                {
                    // If not success status code, then there was some error, so return HTTP status code and error message.
                    string errorMessage = await rawResponse.Content.ReadAsStringAsync();
                    return ((int)rawResponse.StatusCode, errorMessage);
                }

                // Read connect token as string, returning -1 if somehow null or empty.
                var connectTokenBase64 = await rawResponse.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(connectTokenBase64))
                {
                    return (-1, "Play game failed: could not parse API response into usable connect token string");
                }

                // Directly return connect token string.
                return (0, connectTokenBase64);
            }
            catch (Exception ex)
            {
                // Exceptions will only come from the HTTP request, meaning the action failed.
                Trace.WriteLine(ex.Message);
                return (-1, "Play game failed: an unexpected error occurred during API request");
            }
        }

        #endregion

        #region Public: Launcher state tracking

        /// <summary>
        /// Pings the API to notify it that we are still in the launcher (used to maintain account state). Does not
        ///  return any data because it is a simple GET request with our access token.
        /// </summary>
        public static async Task PingInLauncher()
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
        public static async Task NotifyLauncherExit()
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

        #endregion



        #region Private: Access Token checking

        /// <summary>
        /// Ensures there is a valid access token stored, checking whether missing, expiring within 1 minute, or already
        ///  expired. Attempts to retrieve a new access token if the current access token is missing or invalid.
        /// </summary>
        /// <returns> True if a valid access token exists or has been retrieved, false if no valid token. </returns>
        private static async Task<bool> EnsureAccessTokenIsValid()
        {
            // If no current access token OR access token is expiring within 1 minute (or already expired), try to get a new one.
            if (string.IsNullOrEmpty(AppData.AccessToken) || AppData.AccessTokenExpiration - DateTime.UtcNow < TimeSpan.FromMinutes(1))
            {
                // If refresh token is missing, display an error message and reutrn false.
                if (string.IsNullOrEmpty(AppData.RefreshToken))
                {
                    // Clear access token completely and return to login view IF no refresh token and access token is expired.
                    // If we try to refresh the access token without a valid refresh token, this means that the current
                    //  login state was not full-access (ex. bad account state like unverified email, MFA not set up, etc.).
                    //  In this case, the user will be required to re-login with their credentials, as users should only
                    //  ever be given a refresh token if they are fully-logged-in.
                    AppData.AccessToken = string.Empty;
                    MainViewModel.Instance.ShowReturnToLoginView(isError: true, "API access token expired and could not be refreshed. " +
                        "Please log into your account again.");
                    return false;
                }

                // Try to login via refresh token. If returned code is -1, then login has failed and we should return to login.
                //  Otherwise, our returned access token is valid (but might have code 0, 1, 10, 20, or 30 based on account state).
                // IMPORTANT: If refresh login fails (returns code -1), then the currently-stored refresh token has become
                //  invalid. This will typically ONLY ever happen when this same account has been logged into by another device.
                //  We should display an error message noting that the account was logged into on another device.
                var (Code, Message) = await TryLoginFromRefreshToken();
                if (Code == -1)
                {
                    _ = Logout();                                   // Do not await logout.
                    MainViewModel.Instance.ShowReturnToLoginView(true, "Could not refresh login session because your account was logged into by" +
                        " another device. If you did not perform this login, please re-login with MFA and reset your password.");
                    return false;
                }
            }

            // All codes EXCEPT -1 are good login codes, just with different success codes based on account state.
            return true;
        }

        #endregion
    }
}
