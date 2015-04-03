using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Accounts;

namespace VolleyCSharp.ToolBox
{
    public class AndroidAuthenticator : IAuthenticator
    {
        private AccountManager mAccountManager;
        private Account mAccount;
        private String mAuthTokenType;
        private bool mNotifyAuthFailure;

        public AndroidAuthenticator(Context context, Account account, String authTokenType)
            : this(context, account, authTokenType, false) { }

        public AndroidAuthenticator(Context context, Account account, String authTokenType, bool notifyAuthFailure)
            : this(AccountManager.Get(context), account, authTokenType, notifyAuthFailure) { }

        public AndroidAuthenticator(AccountManager accountManager, Account account, String authTokenType, bool notifyAuthFailure)
        {
            this.mAccountManager = accountManager;
            this.mAccount = account;
            this.mAuthTokenType = authTokenType;
            this.mNotifyAuthFailure = notifyAuthFailure;
        }

        public Account Account
        {
            get
            {
                return mAccount;
            }
        }

        public String GetAuthToken()
        {
            IAccountManagerFuture future = mAccountManager.GetAuthToken(mAccount,
                mAuthTokenType, mNotifyAuthFailure, null, null);
            Bundle result = null;
            try
            {
                result = future.Result as Bundle;
            }
            catch (Java.Lang.Exception e)
            {
                throw new AuthFailureError("Error while retrieving auth token", e);
            }
            String authToken = null;
            if (future.IsDone && !future.IsCancelled)
            {
                if (result.ContainsKey(AccountManager.KeyIntent))
                {
                    Intent intent = result.GetParcelable(AccountManager.KeyIntent) as Intent;
                    throw new AuthFailureError(intent);
                }
                authToken = result.GetString(AccountManager.KeyAuthtoken);
            }
            if (authToken == null)
            {
                throw new AuthFailureError("Got null auth token for type: " + mAuthTokenType);
            }
            return authToken;
        }

        public void InvalidateAuthToken(String authToken)
        {
            mAccountManager.InvalidateAuthToken(mAccount.Type, authToken);
        }
    }
}