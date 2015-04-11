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

/*
 * “—∫À µ
 */

namespace VolleyCSharp
{
    public class AuthFailureError : VolleyError
    {
        private Intent mResolutionIntent;

        public AuthFailureError() { }

        public AuthFailureError(Intent intent)
        {
            mResolutionIntent = intent;
        }

        public AuthFailureError(String message)
            : base(message) { }

        public AuthFailureError(String message, Java.Lang.Exception reason)
            : base(message, reason) { }

        public AuthFailureError(NetworkResponse response)
            : base(response) { }

        public Intent GetResolutionIntent()
        {
            return mResolutionIntent;
        }

        public override string Message
        {
            get
            {
                if (mResolutionIntent != null)
                {
                    return "User needs to (re)enter credentials.";
                }
                return base.Message;
            }
        }
    }
}