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
            public Guid ClientGuid { get; private set; }
            public DateTime Expiration { get; private set; }

            public AccessTokenData(string username, Guid clientGuid, double durationMinutes)
            {
                Username = username;
                ClientGuid = clientGuid;
                Expiration = DateTime.Now.AddMinutes(durationMinutes);
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

        private static readonly Dictionary<string, UserDocumentData> testUserAccounts = [];
        private static readonly Dictionary<string, AccessTokenData> testAccessTokens = [];
        private static readonly Dictionary<string, VerificationCodeData> testEmailConfirmationCodes = [];

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

        public static string[]? LoginFromRefreshToken(string refreshTokenString, Guid clientId)
        {
            // We use the passed-in client GUID here to ensure that the logging-in machine is the same machine
            //  that the refresh token was originally generated for. Deny if mismatch (indicates security breach).

            string[]? tokens = null;

            // Retrieve the token and username from the refreshToken string, returning null if either is invalid.
            if (!TryReadJwtToken(refreshTokenString, out JwtSecurityToken? token)) return null;
            string? username = ReadUsernameFromJwtToken(token);
            if (username == null) return null;

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

                // EMAIL CONFIRMATION CHECK: Verify email confirmation state, which determines whether we return an access token.
                if (!userData.IsEmailConfirmed)
                {
                    // If unconfirmed, we only return a refresh token to the user.
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
                    testAccessTokens.Add(tokens[0], new AccessTokenData(username, clientId, ACCESS_TOKEN_DURATION_MINUTES));
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
                if (userData.PasswordHash == password)
                {
                    // If valid password, determine which tokens to return based on email verification state.
                    if (!userData.IsEmailConfirmed)
                    {
                        // If unconfirmed, we only return a refresh token to the user.
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
                        testAccessTokens.Add(tokens[0], new AccessTokenData(username, clientId, ACCESS_TOKEN_DURATION_MINUTES));
                        userData.RefreshToken = tokens[1];
                    }

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
                testUserAccounts.TryAdd(username, new UserDocumentData(username, password, email));

                // Generate a confirmation code for this user and await user confirmation.
                CreateAndSendConfirmationCode(username);
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteUserDocumentsToFile();

            return tokens;
        }

        public static bool ResendEmailConfirmationCode(string refreshToken)
        {
            // Retrieve the token and username from the refreshToken string, returning null if either is invalid.
            if (!TryReadJwtToken(refreshToken, out JwtSecurityToken? token)) return false;
            string? username = ReadUsernameFromJwtToken(token);
            if (username == null) return false;

            // Ensure that the account we are searching for actually exists.
            if (testUserAccounts.TryGetValue(username, out var document))
            {
                // Verify that the refresh token is the current, valid refresh token AND that the account is not already confirmed.
                if (document.RefreshToken != refreshToken || document.IsEmailConfirmed)
                {
                    return false; 
                }
                
                // If token and account state are valid, check if we have an existing code for this account.
                if (testEmailConfirmationCodes.TryGetValue(username, out var codeData))
                {
                    // Only allow a new code to be created at most once per minute. Old code remains valid.
                    if ((DateTime.UtcNow - codeData.Created) < TimeSpan.FromMinutes(1)) return false;
                }

                // Else no existing code or existing code is from more than one minute ago, so generate new.
                CreateAndSendConfirmationCode(username);
                return true;
            }

            // Return false if account does not exist.
            return false;
        }

        public static bool ConfirmAccountEmail(string refreshToken, string verificationCode)
        {
            // IMPORTANT: Only allow checking account confirmation with a valid refresh token.
            // We receive a refresh token on any successful login even if not confirmed, so using this
            //  refresh token ensures a logged-in account is making the confirmation.

            // Retrieve the token and username from the refreshToken string, returning null if either is invalid.
            if (!TryReadJwtToken(refreshToken, out JwtSecurityToken? token)) return false;
            string? username = ReadUsernameFromJwtToken(token);
            if (username == null) return false;

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

        public static void Logout(string refreshToken)
        {
            // If we cannot find token token in our container of access tokens, then the accessToken is already completely
            //  invalidated and we can consider the user already logged out.
            // HOWEVER, we should also attempt to find the username from the access token to also invalidate any refresh token
            //  associated with the user (if applicable). Since the client machine should be the only entity other than us that
            //  has access to a refresh token, we can assume that a valid client is actually trying to log out of their account.
            //  Any existing refresh token should be immediately invalidated.

            // Retrieve the token and username from the refreshToken string, returning null if either is invalid.
            if (!TryReadJwtToken(refreshToken, out JwtSecurityToken? token)) return;
            string? username = ReadUsernameFromJwtToken(token);
            if (username == null) return;

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
    }
}
