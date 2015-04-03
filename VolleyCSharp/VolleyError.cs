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
    public class VolleyError : Java.Lang.Exception
    {
        public NetworkResponse networkResponse;
        private long networkTimeMs;

        public VolleyError()
        {
            networkResponse = null;
        }

        public VolleyError(NetworkResponse response)
        {
            networkResponse = response;
        }

        public VolleyError(String exceptionMessage)
            : base(exceptionMessage)
        {
            networkResponse = null;
        }

        public VolleyError(String exceptionMessage, Java.Lang.Throwable reason)
            : base(exceptionMessage, reason)
        {
            networkResponse = null;
        }

        public VolleyError(Java.Lang.Throwable cause)
            : base(cause)
        {
            networkResponse = null;
        }

        public long NetworkTimeMs
        {
            get
            {
                return networkTimeMs;
            }
            set
            {
                networkTimeMs = value;
            }
        }
    }
}