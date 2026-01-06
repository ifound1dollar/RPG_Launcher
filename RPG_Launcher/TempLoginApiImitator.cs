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
using System.Threading.Tasks;

namespace RPG_Launcher
{
    public static class TempLoginApiImitator
    {
        // this is pretending to be the user_accounts MongoDB document model; is just username and password for now
        private static readonly Dictionary<string, string> testUserAccounts = new()
        {
            { "testusername", "testpassword" }
        };
        private static readonly Dictionary<string, string> testRefreshTokens = [];
        private static readonly HashSet<string> testAccessTokens = [];

        private static string jwtKey = string.Empty;



        public static void Initialize()
        {
            ReadTestRefreshTokenContainerFromFile();
            jwtKey = ReadJwtKeyFromFile();

            //WriteJwtKey("q0Ix3sI9Gk5jb9a8HHjOJJ2RHvsFN1HZkT8VASLApM0");     // Only use this to completely overwrite existing JWT Key.
        }



        #region Public: API Methods

        public static string[]? LoginFromRefreshToken(string refreshToken)
        {
            // TODO: THE API WILL RETURN JSON WITH BOTH TOKENS (RE-GENERATES REFRESH TOKEN)

            string[]? tokens = null;

            // Retrieve the token from the refreshToken string, returning null if invalid.
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            if (token == null) return null;

            // Retrieve username from token, returning if invalid. IMPORTANT: ClaimType.Name MAPS TO UniqueName.
            var username = ReadUsernameFromJwtToken(token);
            if (username == null) return null;


            if (testRefreshTokens.ContainsKey(username))
            {

                // If stored name is valid and token is not expired, we have successfully logged in.
                if (DateTime.UtcNow < token.ValidTo)
                {
                    // Generate new valid tokens if passed-in refresh token is valid.
                    tokens = new string[2];
                    tokens[0] = GenerateAccessToken(username, minutes: 15);
                    tokens[1] = GenerateRefreshToken(username, days: 30);

                    // We must replace the old refresh token with the new one. Can use index operator [] to replace existing.
                    testRefreshTokens[username] = tokens[1];

                    // Add new access token to container.
                    testAccessTokens.Add(tokens[0]);
                }
                else
                {
                    // If invalid, we simply remove the stored token.
                    testRefreshTokens.Remove(refreshToken);
                }
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteTestRefreshTokenContainerToFile();

            return tokens;
        }

        public static string[]? Login(string username, string password)
        {
            // TODO: THIS IS WHERE WE WILL ACTUALLY MAKE THE API CALL (DO NOT HASH PASSWORD HERE)

            string[]? tokens = null;
            if (testUserAccounts.TryGetValue(username, out var storedPassword))
            {
                if (storedPassword == password)     // Will be hashed password on API side.
                {
                    tokens = new string[2];
                    tokens[0] = GenerateAccessToken(username, minutes: 15);
                    tokens[1] = GenerateRefreshToken(username, days: 30);

                    // Add both tokens to containers.
                    testAccessTokens.Add(tokens[0]);
                    testRefreshTokens[username] = tokens[1];    // Replace if existing.
                }
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteTestRefreshTokenContainerToFile();

            return tokens;
        }

        public static string[]? Register(string email, string username, string password)
        {
            // the actual API endpoint will also take an email argument, but we only do username/password for testing
            string[]? tokens = null;
            if (testUserAccounts.TryAdd(username, password))
            {
                tokens = new string[2];
                tokens[0] = GenerateAccessToken(username, minutes: 15);
                tokens[1] = GenerateRefreshToken(username, days: 30);

                // Add both tokens to containers.
                testAccessTokens.Add(tokens[0]);
                testRefreshTokens[username] = tokens[1];    // Replace if existing.
            }

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteTestRefreshTokenContainerToFile();

            return tokens;
        }

        public static void Logout(string accessToken)   // TODO: UPDATE TO TAKE GUID
        {
            // TODO: CREATE LOGIC TO READ USERNAME FROM ACCESS TOKEN, WHICH WILL BE USED TO INVALIDATE REFRESH AND ACCESS TOKENS
            // Both will need to be removed, and we will store a map of accessToken:tokenDataClass on the server later for easy
            //  search. The tokenDataClass will contain data like username and application instance ID to help with lookup.
            // Regardless of where the username is retrieved from, the database will have the refresh token stored within the
            //  document, and this can be removed easily via querying on the username.

            // Always remove the access token directly first. Then, try to remove from refresh tokens via token's stored username.
            testAccessTokens.Remove(accessToken);

            // Retrieve the token from the refreshToken string, returning null if invalid.
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            if (token == null) return;

            // Retrieve username from token, returning if invalid. IMPORTANT: ClaimType.Name MAPS TO UniqueName.
            var username = ReadUsernameFromJwtToken(token);
            if (username == null) return;

            testRefreshTokens.Remove(username);

            // TEMP: Write changes in token containers to file anytime they are changed.
            WriteTestRefreshTokenContainerToFile();
        }

        #endregion

        #region Private: Access Token Generation

        private static string GenerateAccessToken(string username, int minutes)
        {
            // THIS WILL ALSO BE DONE IN THE API CALL, AS DATABASE MUST STORE ACCESS TOKEN

            string accessToken = GenerateToken(username, DateTime.UtcNow.AddMinutes(minutes));
            return accessToken;
        }

        private static string GenerateRefreshToken(string username, int days)
        {
            string refreshToken = GenerateToken(username, DateTime.UtcNow.AddDays(days));
            return refreshToken;
        }

        private static string GenerateToken(string username, DateTime expiration)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ReadJwtKeyFromFile()));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                SigningCredentials = credentials,
                Expires = expiration,
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, username)    // IMPORTANT: ClaimTypes.Name maps to JwtRegisteredClaimNames.UniqueName.
                ]),
            };

            // Use new token handler to create and write a new token, then return it.
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        #endregion

        #region Private: Refresh Token Container Read/Write

        private static void ReadTestRefreshTokenContainerFromFile()
        {
            // IN THESE TEMPORARY METHODS, WE ARE NOT USING ENTROPY BECAUSE THEY AREN'T REAL.

            try
            {
                var handler = new JwtSecurityTokenHandler();

                using (FileStream fileStream = new("test_server_refresh_tokens.dat", FileMode.Open, FileAccess.Read))
                {
                    using StreamReader sr = new(fileStream);
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        byte[] encryptedText = Convert.FromBase64String(line);
                        byte[] originalText = ProtectedData.Unprotect(encryptedText, null, DataProtectionScope.CurrentUser);
                        string tokenString = Encoding.UTF8.GetString(originalText);

                        // Retrieve the token from the string, skipping if invalid.
                        var token = handler.ReadJwtToken(tokenString);
                        if (token == null) continue;

                        // Get username from string, returning if not found.
                        var username = ReadUsernameFromJwtToken(token);
                        if (username == null) continue;

                        // Skip if token is expired (will overwrite file later).
                        if (DateTime.UtcNow >= token.ValidTo)
                        {
                            continue;
                        }

                        testRefreshTokens.Add(username, tokenString);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        private static void WriteTestRefreshTokenContainerToFile()
        {
            // IN THESE TEMPORARY METHODS, WE ARE NOT USING ENTROPY BECAUSE THEY AREN'T REAL.

            try
            {
                var handler = new JwtSecurityTokenHandler();

                // Else container has values, so write all.
                using (FileStream fileStream = new("test_server_refresh_tokens.dat", FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using StreamWriter sw = new(fileStream);

                    foreach (KeyValuePair<string, string> pair in testRefreshTokens)
                    {
                        // Remove and skip any expired tokens before writing to file.
                        string tokenString = pair.Value;
                        var token = handler.ReadJwtToken(tokenString);
                        if (token != null && DateTime.UtcNow >= token.ValidTo)
                        {
                            testRefreshTokens.Remove(tokenString);
                            continue;
                        }

                        byte[] originalText = Encoding.UTF8.GetBytes(tokenString);
                        byte[] encryptedText = ProtectedData.Protect(originalText, null, DataProtectionScope.CurrentUser);
                        sw.WriteLine(Convert.ToBase64String(encryptedText));
                    }

                }

                // If container is empty after loop, then we wrote nothing, so we must clear file entirely.
                if (testRefreshTokens.Count == 0)
                {
                    // FileMode.Create will completely overwrite existing data.
                    using FileStream fileStream = new("test_server_refresh_tokens.dat", FileMode.Create, FileAccess.Write);
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

        #endregion
    }
}
