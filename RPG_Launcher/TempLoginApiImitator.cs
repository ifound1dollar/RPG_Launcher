using Microsoft.IdentityModel.Tokens;
using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        private class UserDocumentData
        {
            public string Username { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;

            public UserDocumentData()
            {
                // Parameterless constructor for JSON deserialization.
            }

            public UserDocumentData(string username, string passwordHash, string email)
            {
                Username = username;
                PasswordHash = passwordHash;
                Email = email;
            }

            public UserDocumentData(string username, string passwordHash, string email, string refreshToken)
            {
                Username = username;
                PasswordHash = passwordHash;
                Email = email;
                RefreshToken = refreshToken;
            }
        }

        // this is pretending to be the user_accounts MongoDB document model; is just username and password for now
        private static readonly Dictionary<string, UserDocumentData> testUserAccounts = new()
        {
            { "testusername", new UserDocumentData("testusername", "testpassword", "testuser@email.com") },
            { "secondusername", new UserDocumentData("secondusername", "secondpassword", "seconduser@email.com") }
        };
        private static readonly Dictionary<string, AccessTokenData> testAccessTokens = [];

        private static string jwtKey = string.Empty;

        private static readonly double ACCESS_TOKEN_DURATION_MINUTES = 15;
        private static readonly double REFRESH_TOKEN_DURATION_DAYS = 30;



        public static void Initialize()
        {
            WriteUserDocumentsToFile();
            testUserAccounts.Clear();

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
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshTokenString);
            if (token == null) return null;
            string? username = ReadUsernameFromJwtToken(token);
            if (username == null) return null;

            // Try to find the passed-in refresh token (will be database query on account username in the future).
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

                // ACTUAL TOKEN VALIDITY CHECK: If stored name is valid and token is not expired, we have successfully logged in.
                if (DateTime.UtcNow < token.ValidTo)
                {
                    // Generate new valid tokens if passed-in refresh token is valid.
                    tokens = new string[2];
                    tokens[0] = GenerateAccessToken(username, clientId, ACCESS_TOKEN_DURATION_MINUTES);
                    tokens[1] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);

                    // Store both tokens, access token in memory only and refresh token in database.
                    userData.RefreshToken = tokens[1];
                    testAccessTokens.Add(tokens[0], new AccessTokenData(username, clientId, ACCESS_TOKEN_DURATION_MINUTES));
                }
                else
                {
                    // If invalid, we simply remove the stored token.
                    userData.RefreshToken = string.Empty; WriteUserDocumentsToFile();   //TEMP
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

                if (userData.PasswordHash == password)     // Will be hashed password on API side.
                {
                    tokens = new string[2];
                    tokens[0] = GenerateAccessToken(username, clientId, ACCESS_TOKEN_DURATION_MINUTES);
                    tokens[1] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);

                    // Add both tokens to containers.
                    testAccessTokens.Add(tokens[0], new AccessTokenData(username, clientId, ACCESS_TOKEN_DURATION_MINUTES));
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

            // Only allow new registration if an account with this username does not already exist.
            // IMPORTANT: Will also need to ensure email uniqueness in the future (make index on it).
            if (!testUserAccounts.ContainsKey(username))
            {
                // TODO: GENERATE PASSWORD HASH IN THE ACTUAL API

                tokens = new string[2];
                tokens[0] = GenerateAccessToken(username, clientId, ACCESS_TOKEN_DURATION_MINUTES);
                tokens[1] = GenerateRefreshToken(username, clientId, REFRESH_TOKEN_DURATION_DAYS);

                // Add both tokens to containers.
                testAccessTokens.Add(tokens[0], new AccessTokenData(username, clientId, ACCESS_TOKEN_DURATION_MINUTES));
                testUserAccounts.TryAdd(username, new UserDocumentData(username, password, email));
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteUserDocumentsToFile();

            return tokens;
        }

        public static void Logout(string accessToken)
        {
            // If we cannot find token token in our container of access tokens, then the accessToken is already completely
            //  invalidated and we can consider the user already logged out.
            // HOWEVER, we should also attempt to find the username from the access token to also invalidate any refresh token
            //  associated with the user (if applicable). Since the client machine should be the only entity other than us that
            //  has access to a refresh token, we can assume that a valid client is actually trying to log out of their account.
            //  Any existing refresh token should be immediately invalidated.

            string? username;

            // Always remove the access token directly first. Then, try to remove from refresh tokens via token's stored username.
            testAccessTokens.Remove(accessToken, out AccessTokenData? tokenData);
            if (tokenData != null)
            {
                // If we have valid AccessTokenData, use it to directly retrieve username associated with the user logging out.
                username = tokenData.Username;
            }
            else
            {
                // Else if we could not find the token, try to use the access token to retrieve the username.
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(accessToken);
                if (token == null) return;

                // Retrieve username from token, returning if invalid. IMPORTANT: ClaimType.Name MAPS TO UniqueName.
                username = ReadUsernameFromJwtToken(token);
            }

            // Remove an existing refresh token associated with the logging-out account ONLY IF we found a valid username. 
            if (username != null && testUserAccounts.TryGetValue(username, out UserDocumentData? userData))
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

        #region Private: Refresh Token Container Read/Write

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
