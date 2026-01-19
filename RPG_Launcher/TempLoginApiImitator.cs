using Microsoft.IdentityModel.Tokens;
using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RPG_Launcher
{
    public static class TempLoginApiImitator
    {
        private class AccessTokenData
        {
            public string Username { get; private set; }
            public string Token { get; private set; }
            public Guid ClientGuid { get; private set; }
            public DateTime Expiration { get; private set; }

            public AccessTokenData(string username, string token, Guid clientGuid, double durationMinutes)
            {
                Username = username;
                Token = token;
                ClientGuid = clientGuid;
                Expiration = DateTime.UtcNow.AddMinutes(durationMinutes);
            }
        }
        private class VerificationCodeData
        {
            public string Code { get; private set; }
            public DateTime Created { get; private set; }
            public DateTime Expiration { get; private set; }
            public int AttemptCounter { get; set; }

            public VerificationCodeData(string code, double durationMinutes = 5)
            {
                Code = code;
                Created = DateTime.UtcNow;
                Expiration = DateTime.UtcNow.AddMinutes(durationMinutes);
                AttemptCounter = 0;
            }
        }
        private class UserDocumentData
        {
            // Created time will be used to destroy un-confirmed accounts after 30 days (free the username and email).
            public DateTime Created { get; set; }
            public string Username { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsEmailConfirmed { get; set; } = false;
            public bool DoesPasswordNeedReset { get; set; } = false;
            public string RefreshToken { get; set; } = string.Empty;

            public UserDocumentData()
            {
                // Parameterless constructor for JSON deserialization.
            }

            public UserDocumentData(string username, string passwordHash, string email)
            {
                Created = DateTime.UtcNow;
                Username = username;
                PasswordHash = passwordHash;
                Email = email;
            }

            public UserDocumentData(string username, string passwordHash, string email, string refreshToken)
            {
                Created = DateTime.UtcNow;
                Username = username;
                PasswordHash = passwordHash;
                Email = email;
                RefreshToken = refreshToken;
            }
        }
        private class PasswordResetTokenData
        {
            public string Username { get; private set; }
            public string Token { get; private set; }
            public DateTime Expiration { get; private set; }

            public PasswordResetTokenData(string username, string token, double durationMinutes)
            {
                Username = username;
                Token = token;
                Expiration = DateTime.UtcNow.AddMinutes(durationMinutes);
            }
        }

        private static readonly Dictionary<string, UserDocumentData> testUserAccounts = [];
        private static readonly Dictionary<string, AccessTokenData> testAccessTokens = [];
        private static readonly Dictionary<string, VerificationCodeData> testEmailConfirmationCodes = [];
        private static readonly Dictionary<string, PasswordResetTokenData> testPasswordResetTokens = [];
        private static readonly Dictionary<string, List<DateTime>> testFailedLoginAttempts = [];

        private static string jwtKey = string.Empty;

        private static readonly double ACCESS_TOKEN_DURATION_MINUTES = 15;
        private static readonly double REFRESH_TOKEN_DURATION_DAYS = 30;

        private static readonly JwtSecurityTokenHandler handler = new();



        public static void Initialize()
        {
            ReadUserDocumentsFromFile();
            jwtKey = ReadJwtKeyFromFile();

            //WriteJwtKey("q0Ix3sI9Gk5jb9a8HHjOJJ2RHvsFN1HZkT8VASLApM0");     // Only use this to completely overwrite existing JWT Key.
        }



        #region Public: API Methods

        public static string[]? LoginFromRefreshToken(string refreshToken, Guid clientId)
        {
            // We use the passed-in client GUID here to ensure that the logging-in machine is the same machine
            //  that the refresh token was originally generated for. Deny if mismatch (indicates security breach).

            string[]? tokens = null;

            // Retrieve username from the refreshToken string, returning null if unsuccessful.
            if (!TryGetDataFromTokenString(refreshToken, out string? username, out var token)) return null;

            // TODO: ADD handler.ValidateToken() CALL WITH CUSTOM TokenValidationParameters, FOR ACTUAL SECURITY CHECKS.

            if (testUserAccounts.TryGetValue(username, out UserDocumentData? userData))
            {
                // BASIC GUID VALIDATION: We simply check whether the passed-in refresh token's GUID matches the client GUID.
                // Note that we cannot check the refresh token stored in the database, because it will be stored as a hash.
                var tokenGuid = ReadGuidFromJwtToken(token);
                if (tokenGuid == null || tokenGuid != clientId)
                {
                    // If null or mismatch, then we should deny the login (retrieved refresh token from different
                    //  client than the one that the refresh token actually belongs to). Refresh tokens should be strictly
                    //  attached to the client machine that the refresh token was generated for and thus belongs to.
                    // This can easily be faked by a malicious actor, but it is still useful to check just as an added
                    //  security measure.
                    userData.RefreshToken = string.Empty; WriteUserDocumentsToFile();   // TEMP

                    // Also remove any access token for this user, just in case one remains.
                    testAccessTokens.Remove(username);
                    return tokens;
                }

                // TOKEN EXPIRATION CHECK: If stored name is valid and token is not expired, we have successfully logged in.
                if (DateTime.UtcNow > token.ValidTo)
                {
                    // If expired, we simply remove the stored token.
                    userData.RefreshToken = string.Empty; WriteUserDocumentsToFile();   //TEMP
                    return null;
                }

                // ACCOUNT STATE CHECK: Verify email confirmation state and password reset state.
                ClearFailedLoginAttempts(username);     // Successful login, so clear failed attempts.
                if (!userData.IsEmailConfirmed || userData.DoesPasswordNeedReset)
                {
                    // If unconfirmed or needs password reset, we only return a refresh token to the user.
                    tokens = new string[1];
                    tokens[0] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);
                    userData.RefreshToken = tokens[0];

                    // Send confirmation code immediately (will automatically show screen on client).
                    CreateAndSendConfirmationCode(username);
                }
                else
                {
                    // Else email is confirmed, so fully log in.
                    tokens = new string[2];
                    tokens[0] = GenerateAccessToken(username, clientId, ACCESS_TOKEN_DURATION_MINUTES);
                    tokens[1] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);

                    // Add both tokens to containers.
                    testAccessTokens.Add(username, new AccessTokenData(username, tokens[0], clientId, ACCESS_TOKEN_DURATION_MINUTES));
                    userData.RefreshToken = tokens[1];
                }
            }

            return tokens;
        }

        public static string[]? Login(string username, string password, Guid clientId)
        {
            // Client GUID is only used for token creation here (no comparison).

            string[]? tokens = null;
            if (testUserAccounts.TryGetValue(username, out var userData))
            {
                // TODO: GENERATE PASSWORD HASH IN ACTUAL API

                // Compare stored password hash with passed-in password.
                if (userData.PasswordHash != password)
                {
                    // If incorrect password, we must increment failed attempts for this account.
                    DateTime now = DateTime.UtcNow;
                    AddFailedLoginAttempt(username, now);

                    // Check failed login attempts within last 5 minutes. If at least 3, force password reset.
                    if (GetFailedLoginAttempts(username, 5, now) >= 3)
                    {
                        userData.DoesPasswordNeedReset = true;
                    }

                    // Return null because login failed.
                    return null;
                }

                // Else passwords match, so clear failed login attempts for account.
                ClearFailedLoginAttempts(username);

                // Finally, determine whether to fully log in based on account state.
                if (!userData.IsEmailConfirmed || userData.DoesPasswordNeedReset)
                {
                    // If unconfirmed or password needs reset, we only return a refresh token to the user.
                    tokens = new string[1];
                    tokens[0] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);
                    userData.RefreshToken = tokens[0];

                    // Send confirmation code immediately (will automatically show screen on client).
                    CreateAndSendConfirmationCode(username);
                }
                else
                {
                    // Else email is confirmed, so fully log in.
                    tokens = new string[2];
                    tokens[0] = GenerateAccessToken(username, clientId, ACCESS_TOKEN_DURATION_MINUTES);
                    tokens[1] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);

                    // Add both tokens to containers.
                    testAccessTokens.Add(username, new AccessTokenData(username, tokens[0], clientId, ACCESS_TOKEN_DURATION_MINUTES));
                    userData.RefreshToken = tokens[1];
                }
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteUserDocumentsToFile();

            return tokens;
        }

        public static string[]? Register(string email, string username, string password, Guid clientId)
        {
            // Client GUID is only used for token creation here (no comparison).

            string[]? tokens = null;

            // Verify validity of email, username, and password before moving on.
            string pattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";       // Email
            if (!Regex.IsMatch(email, pattern))
            {
                return null;
            }
            pattern = @"^[a-zA-Z0-9_]{5,20}$";                                          // Username, 5-20 chars, upper lower digit underscore
            if (!Regex.IsMatch(username, pattern))
            {
                return null; 
            }
            pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$";          // Password, 8+ chars, 1+ upper lower digit symbol
            if (!Regex.IsMatch(password, pattern))
            {
                return null; 
            }

            // Only allow new registration with unique emails or usernames.
            if (!testUserAccounts.ContainsKey(username) && IsEmailUnique(email))
            {
                // TODO: GENERATE PASSWORD HASH IN THE ACTUAL API

                // After registration completion but before email confirmation, only return a refresh token.
                tokens = new string[1];
                tokens[0] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);

                // Create new user account and add to database.
                testUserAccounts.TryAdd(username, new UserDocumentData(username, password, email, tokens[0]));

                // Generate a confirmation code for this user and await user confirmation.
                CreateAndSendConfirmationCode(username);
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteUserDocumentsToFile();

            return tokens;
        }

        public static void Logout(string refreshToken)
        {
            // If we cannot find token token in our container of access tokens, then the accessToken is already completely
            //  invalidated and we can consider the user already logged out.
            // HOWEVER, we should also attempt to find the username from the access token to also invalidate any refresh token
            //  associated with the user (if applicable). Since the client machine should be the only entity other than us that
            //  has access to a refresh token, we can assume that a valid client is actually trying to log out of their account.
            //  Any existing refresh token should be immediately invalidated.

            // Retrieve username from the refreshToken string, returning if unsuccessful.
            if (!TryGetDataFromTokenString(refreshToken, out string? username, out var token)) return;

            // Always remove the access token directly first.
            testAccessTokens.Remove(username);

            // Remove an existing refresh token associated with the logging-out account. 
            if (testUserAccounts.TryGetValue(username, out UserDocumentData? userData))
            {
                userData.RefreshToken = string.Empty;
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteUserDocumentsToFile();
        }





        public static bool IsAccountEmailConfirmed(string refreshToken)
        {
            // Retrieve username from the refreshToken string, returning false if unsuccessful.
            if (!TryGetDataFromTokenString(refreshToken, out string? username, out var token)) return false;

            // Ensure that the account we are searching for actually exists.
            if (testUserAccounts.TryGetValue(username, out var document))
            {
                // Verify that the passed-in refresh token is the current, valid token.
                if (document.RefreshToken != refreshToken || DateTime.UtcNow > token.ValidTo)
                {
                    return false;
                }

                // Else we simply return whether the account is validated.
                return document.IsEmailConfirmed;
            }

            // Return false if account does not exist.
            return false;
        }

        public static void SendEmailVerificationCode(string usernameOrEmail)
        {
            // THIS METHOD IS USED FOR BOTH EMAIL CONFIRMATION AND PASSWORD RESETTING.

            // Try to find an account matching the username.
            if (!testUserAccounts.TryGetValue(usernameOrEmail, out var document))
            {
                // If not found by username, try to find by email.
                document = FindDocumentByEmail(usernameOrEmail);

                // If document is still null at this point, then we found no match, so return (doing nothing).
                if (document == null)
                {
                    return;
                }
            }

            // Else we found document by either username or email, so verify that new code not within 60 seconds.
            if (testEmailConfirmationCodes.TryGetValue(document.Username, out var codeData))
            {
                // Only allow a new code to be created at most once per minute. Old code remains valid.
                if ((DateTime.UtcNow - codeData.Created) < TimeSpan.FromMinutes(1)) return;
            }

            // Either previous code does not exist or is past past expiration, so replace with new.
            CreateAndSendConfirmationCode(document.Username);
        }

        public static bool ConfirmAccountEmail(string refreshToken, string verificationCode)
        {
            // IMPORTANT: Only allow checking account confirmation with a valid refresh token.
            // We receive a refresh token on any successful login even if not confirmed, so using this
            //  refresh token ensures a logged-in account is making the confirmation.

            // Retrieve username from the refreshToken string, returning false if unsuccessful.
            if (!TryGetDataFromTokenString(refreshToken, out string? username, out var token)) return false;

            // Retrieve database entry for this email, then compare verification code.
            if (testUserAccounts.TryGetValue(username, out var document))
            {
                // Try to retrieve code data from container.
                if (!testEmailConfirmationCodes.TryGetValue(username, out var verificationCodeData))
                {
                    return false;
                }

                // If code exists, check expiration BEFORE comparing value.
                if (verificationCodeData.Expiration < DateTime.UtcNow)
                {
                    testEmailConfirmationCodes.Remove(username);
                    return false;
                }

                // Finally, compare value.
                if (verificationCodeData.Code != verificationCode)
                {
                    // If mismatch, increment counter and remove entire code if greater than threshold.
                    if ((verificationCodeData.AttemptCounter++) >= 3)       // Invalidate on 3rd failure.
                    {
                        testEmailConfirmationCodes.Remove(username);
                        return false;
                    }
                }
                else
                {
                    // If valid code, we can confirm this account and return true.
                    testEmailConfirmationCodes.Remove(username);
                    document.IsEmailConfirmed = true;

                    // TEMP: Write changes in token containers to file anytime they are changed.
                    WriteUserDocumentsToFile();

                    return true;
                }
            }

            // Return false if document not found (email does not map to an account).
            return false;
        }

        public static string? RequestPasswordResetTokenFromCode(string usernameOrEmail, string verificationCode)
        {
            // Try to find an account matching the username.
            if (!testUserAccounts.TryGetValue(usernameOrEmail, out var document))
            {
                // If not found by username, try to find by email.
                document = FindDocumentByEmail(usernameOrEmail);

                // If document is still null at this point, then we found no match, so return (doing nothing).
                if (document == null)
                {
                    return null;
                }
            }

            // Else we found a valid user document by username or email, so compare codes.
            if (testEmailConfirmationCodes.TryGetValue(document.Username, out var verificationCodeData))
            {
                if (verificationCodeData.Code != verificationCode)
                {
                    // If mismatch, increment counter and remove entire code if greater than threshold.
                    if ((verificationCodeData.AttemptCounter++) >= 3)       // Invalidate on 3rd failure.
                    {
                        testEmailConfirmationCodes.Remove(document.Username);
                    }
                }
                else
                {
                    // If codes match, consume the confirmation code.
                    testEmailConfirmationCodes.Remove(document.Username);

                    // Create reset token and add to map. Do not set 'reset required' flag, as this is optional reset.
                    string resetToken = GenerateResetToken(document.Username, minutes: 5);
                    testPasswordResetTokens[document.Username] = new PasswordResetTokenData(document.Username, resetToken, durationMinutes: 5);

                    // TEMP: Write changes in token containers to file anytime they are changed.
                    WriteUserDocumentsToFile();

                    return resetToken;
                }
            }            

            // Return null if code not found or code found but not matching.
            return null;
        }

        public static void CancelPasswordReset(string resetToken)
        {
            // Fire-and-forget API call to cancel the current reset token attempt. Simply invalidates token.

            // Retrieve username from the resetToken string, returning false if unsuccessful.
            if (!TryGetDataFromTokenString(resetToken, out string? username, out var token)) return;

            // Actually remove token.
            testPasswordResetTokens.Remove(username);
        }

        public static int ResetPasswordFromToken(string resetToken, string password)
        {
            // Retrieve username from the resetToken string, returning false if unsuccessful.
            if (!TryGetDataFromTokenString(resetToken, out string? username, out var token)) return -1;

            // Only if account exists.
            if (testUserAccounts.TryGetValue(username, out var document))
            {
                // Retrieve reset token from container.
                if (!testPasswordResetTokens.TryGetValue(username, out var resetTokenData)) return -1;
                if (resetTokenData.Expiration < DateTime.UtcNow || resetTokenData.Token != resetToken)
                {
                    // If token is expired or the tokens are mismatched, invalidate and return.
                    testPasswordResetTokens.Remove(username);
                    return -1;
                }

                // TODO: MAKE PASSWORD HASH, NOT RAW PASSWORD

                // Ensure new password does not match old password.
                if (document.PasswordHash == password)
                {
                    return 2;           // 2 denotes same password
                }

                // Else token is valid, so allow the password reset.
                document.PasswordHash = password;
                document.DoesPasswordNeedReset = false;

                // After password reset, we must invalidate refresh token for this user. Force re-login with new password.
                document.RefreshToken = string.Empty;

                return 0;               // 0 denoes success
            }

            // Return false if account does not exist.
            return -1;                  // -1 denotes generic failure
        }

        #endregion

        #region Private: Token Generation

        private static string GenerateAccessToken(string username, Guid clientGuid, double minutes)
        {
            // THIS WILL ALSO BE DONE IN THE API CALL, AS DATABASE MUST STORE ACCESS TOKEN

            string accessToken = GenerateToken(username, clientGuid, DateTime.UtcNow.AddMinutes(minutes));
            return accessToken;
        }

        private static string GenerateRefreshToken(string username, Guid clientGuid, double days)
        {
            string refreshToken = GenerateToken(username, clientGuid, DateTime.UtcNow.AddDays(days));
            return refreshToken;
        }

        private static string GenerateResetToken(string username, double minutes)
        {
            string resetToken = GenerateToken(username, new Guid(), DateTime.UtcNow.AddMinutes(minutes));
            return resetToken;
        }

        private static string GenerateToken(string username, Guid clientGuid, DateTime expiration)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                SigningCredentials = credentials,
                Expires = expiration,
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, username),   // IMPORTANT: ClaimTypes.Name maps to JwtRegisteredClaimNames.UniqueName.
                    new Claim("client_guid", clientGuid.ToString())     // Custom ClaimType for client GUID.
                ]),
            };

            // Use new token handler to create and write a new token, then return it.
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        #endregion

        #region Private: Confirmation Code Utility

        private static void CreateAndSendConfirmationCode(string username)
        {
            // TEMP HARD-CODED CONFIRMATION CODE
            string code = "000000";

            // Replace if existing.
            testEmailConfirmationCodes[username] = new VerificationCodeData(code, durationMinutes: 5);

            // TODO: SEND CODE TO TARGET EMAIL IN THE FUTURE, CAN HAVE EMAIL AS PARAMETER OR LOOK IT UP
        }

        #endregion

        #region Private: User Document Utilities

        private static void ReadUserDocumentsFromFile()
        {
            // IN THESE TEMPORARY METHODS, WE ARE NOT USING ENTROPY BECAUSE THEY AREN'T REAL.

            try
            {
                using (FileStream fileStream = new("test_server_user_documents.dat", FileMode.Open, FileAccess.Read))
                {
                    using StreamReader sr = new(fileStream);
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        byte[] encryptedText = Convert.FromBase64String(line);
                        byte[] originalText = ProtectedData.Unprotect(encryptedText, null, DataProtectionScope.CurrentUser);
                        string jsonString = Encoding.UTF8.GetString(originalText);

                        // The raw string should be a valid JSON object, so try to deserialize it.
                        UserDocumentData? userData = JsonSerializer.Deserialize<UserDocumentData>(jsonString);// USE ENCRYPTED LATER
                        if (userData != null)
                        {
                            testUserAccounts.Add(userData.Username, userData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        private static void WriteUserDocumentsToFile()
        {
            // IN THESE TEMPORARY METHODS, WE ARE NOT USING ENTROPY BECAUSE THEY AREN'T REAL.

            try
            {
                // FileMode.Create to completely replace existing data in file.
                using (FileStream fileStream = new("test_server_user_documents.dat", FileMode.Create, FileAccess.Write))
                {
                    using StreamWriter sw = new(fileStream);

                    foreach (KeyValuePair<string, UserDocumentData> pair in testUserAccounts)
                    {
                        // Serialize UserDocumentData object into JSON string.
                        string jsonString = JsonSerializer.Serialize(pair.Value, JsonSerializerOptions.Default);
                        //sw.WriteLine(jsonString);

                        // Encrypt JSON string then write to file.
                        byte[] originalText = Encoding.UTF8.GetBytes(jsonString);
                        byte[] encryptedText = ProtectedData.Protect(originalText, null, DataProtectionScope.CurrentUser);
                        sw.WriteLine(Convert.ToBase64String(encryptedText));
                    }

                }

                // If container is empty after loop, then we wrote nothing, so we must clear file entirely.
                if (testUserAccounts.Count == 0)
                {
                    // FileMode.Create will completely overwrite existing data.
                    using FileStream fileStream = new("test_server_user_documents.dat", FileMode.Create, FileAccess.Write);
                    using StreamWriter sw = new(fileStream);
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        private static bool IsEmailUnique(string email)
        {
            // Will simply use a database index for uniqueness in the future.

            foreach (KeyValuePair<string, UserDocumentData> entry in testUserAccounts)
            {
                if (email.Equals(entry.Value.Email)) return false;
            }

            return true;
        }

        private static UserDocumentData? FindDocumentByEmail(string email)
        {
            foreach (KeyValuePair<string, UserDocumentData> entry in testUserAccounts)
            {
                if (email.Equals(entry.Value.Email)) return entry.Value;
            }

            return null;
        }

        #endregion

        #region Private: JWT Token Read/Write

        private static string ReadJwtKeyFromFile()
        {
            // IN THESE TEMPORARY METHODS, WE ARE NOT USING ENTROPY BECAUSE THEY AREN'T REAL.

            try
            {
                using FileStream fileStream = new("test_server_jwt_key.dat", FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fileStream);
                string? line = sr.ReadLine();
                if (line == null) return string.Empty;

                byte[] encryptedText = Convert.FromBase64String(line);
                byte[] originalText = ProtectedData.Unprotect(encryptedText, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(originalText);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return string.Empty;
            }
        }

        private static void WriteJwtKeyToFile(string jwtKey)
        {
            // IN THESE TEMPORARY METHODS, WE ARE NOT USING ENTROPY BECAUSE THEY AREN'T REAL.

            try
            {
                // FileMode.Create to completely overwrite any existing file.
                using FileStream fileStream = new("test_server_jwt_key.dat", FileMode.Create, FileAccess.Write);
                using StreamWriter sw = new(fileStream);

                byte[] originalText = Encoding.UTF8.GetBytes(jwtKey);
                byte[] encryptedText = ProtectedData.Protect(originalText, null, DataProtectionScope.CurrentUser);
                sw.WriteLine(Convert.ToBase64String(encryptedText));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }


        #endregion

        #region Private: JWT Token Utility

        private static bool TryReadJwtToken(string jwtToken, [NotNullWhen(true)] out JwtSecurityToken? token)
        {
            token = (handler.CanReadToken(jwtToken)) ? handler.ReadJwtToken(jwtToken) : null;
            return token != null;   // Returns true if token is valid, else false if null.
        }

        private static string? ReadUsernameFromJwtToken(JwtSecurityToken token)
        {
            // Retrieve username from token. IMPORTANT: ClaimType.Name MAPS TO UniqueName.
            var username = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;

            return username;    // May be null.
        }

        private static bool TryGetDataFromTokenString(string refreshToken, [NotNullWhen(true)] out string? username, [NotNullWhen(true)] out JwtSecurityToken? token)
        {
            username = null;
            token = null;

            if (!TryReadJwtToken(refreshToken, out token)) return false;
            username = ReadUsernameFromJwtToken(token);
            
            return username != null;
        }

        private static Guid? ReadGuidFromJwtToken(JwtSecurityToken token)
        {
            // Retrieve GUID from token. Uses custom claim type string.
            var guidString = token.Claims.FirstOrDefault(claim => claim.Type == "client_guid")?.Value;

            // Return new GUID if valid claim, else return null.
            try
            {
                if (guidString != null) return new Guid(guidString);
            }
            catch
            {
                // Empty catch just to prevent crash on failed GUID initialization.
            }

            return null;
        }

        #endregion

        #region Private: Login Attempt Tracking Utility

        private static void AddFailedLoginAttempt(string username, DateTime timestamp)
        {
            // Add new entry if already existing, but NOT removing expired attempts (will remove later).
            if (testFailedLoginAttempts.TryGetValue(username, out var attempts))
            {
                attempts.Add(timestamp);    // Will automatically add in chronological order.
                return;
            }

            // If not existing, create new for this user.
            testFailedLoginAttempts.TryAdd(username, new List<DateTime> { timestamp });
        }

        private static int GetFailedLoginAttempts(string username, double minutes, DateTime nowTime)
        {
            int counter = 0;

            if (testFailedLoginAttempts.TryGetValue(username, out var attempts))
            {
                // Iterate over all attempts, counting number that occurred within time period.
                TimeSpan timeFrame = TimeSpan.FromMinutes(minutes);

                foreach (DateTime attemptTime in attempts)
                {
                    // All elements after the first element within duration will be within duration.
                    // Should just count Length - [firstIndex] and return that instead.
                    if (nowTime - attemptTime < timeFrame) counter++;
                }
            }

            // Return 0 if no entry in map.
            return counter;
        }

        private static void ClearFailedLoginAttempts(string username)
        {
            // We can just remove the entry for this user completely (frees memory).
            testFailedLoginAttempts.Remove(username);
        }

        #endregion
    }
}
