using RPG_Launcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPG_Launcher.Model.Requests
{
    public static class RequestModels
    {

        #region Login/Auth

        public static StringContent RefreshLogin()
        {
            return CreateStringContentFromAnonymous(new
            {
                RefreshToken = AppData.RefreshToken,
                ClientGuid = AppData.ClientGuid
            });
        }

        public static StringContent Login(NetworkCredential credential)
        {
            return CreateStringContentFromAnonymous(new
            {
                UsernameOrEmail = credential.UserName,
                Password = credential.Password
            });
        }

        public static StringContent SubmitMfaCode(string mfaCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                MfaCode = mfaCode
            });
        }

        #endregion

        #region New Account

        public static StringContent Register(string email, NetworkCredential credential)
        {
            return CreateStringContentFromAnonymous(new
            {
                Email = email,
                Username = credential.UserName,
                Password = credential.Password
            });
        }

        public static StringContent VerifyEmail(string confirmationCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                Code = confirmationCode
            });
        }

        #endregion

        #region Recovery / Reset Password

        public static StringContent ForgotPassword(string usernameOrEmail)
        {
            return CreateStringContentFromAnonymous(new
            {
                UsernameOrEmail = usernameOrEmail
            });
        }

        public static StringContent InitiateResetPassword(string usernameOrEmail, string confirmationCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                UsernameOrEmail = usernameOrEmail,
                Code = confirmationCode
            });
        }

        public static StringContent SubmitResetPassword(NetworkCredential credential)
        {
            return CreateStringContentFromAnonymous(new
            {
                NewPassword = credential.Password
            });
        }

        #endregion

        #region Account Management

        public static StringContent ChangeUsername(string newUsername, NetworkCredential credential)
        {
            return CreateStringContentFromAnonymous(new
            {
                NewUsername = newUsername,
                CurrentPassword = credential.Password
            });
        }

        public static StringContent ChangePassword(NetworkCredential newCredential, NetworkCredential oldCredential)
        {
            return CreateStringContentFromAnonymous(new
            {
                NewPassword = newCredential.Password,
                CurrentPassword = oldCredential.Password
            });
        }

        public static StringContent SubmitChangedEmail(string newEmail, NetworkCredential credential)
        {
            return CreateStringContentFromAnonymous(new
            {
                NewEmail = newEmail,
                CurrentPassword = credential.Password
            });
        }

        public static StringContent VerifyChangedEmail(string confirmationCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                Code = confirmationCode
            });
        }

        public static StringContent SubmitSecondaryEmail(string secondaryEmail, NetworkCredential credential)
        {
            return CreateStringContentFromAnonymous(new
            {
                SecondaryEmail = secondaryEmail,
                CurrentPassword = credential.Password
            });
        }

        public static StringContent VerifySecondaryEmail(string confirmationCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                Code = confirmationCode
            });
        }

        #endregion

        #region MFA Configuration

        public static StringContent VerifyMfaSetup(string mfaCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                MfaCode = mfaCode
            });
        }

        public static StringContent RecoverMfa(string recoveryCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                RecoveryCode = recoveryCode
            });
        }

        public static StringContent InitiateMfaHardReset(string confirmationCode)
        {
            return CreateStringContentFromAnonymous(new
            {
                Code = confirmationCode
            });
        }

        #endregion





        #region Private Utility: Create String Content

        private static StringContent CreateStringContentFromAnonymous<T>(T data)
        {
            return new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json");
        }

        #endregion

    }

}
