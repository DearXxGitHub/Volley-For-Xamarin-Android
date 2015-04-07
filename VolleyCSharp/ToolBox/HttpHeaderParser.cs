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

namespace VolleyCSharp.ToolBox
{
    public class HttpHeaderParser
    {
        public static Entry ParseCacheHeaders(NetworkResponse response)
        {
            long now = SystemClock.CurrentThreadTimeMillis();

            Dictionary<String, String> headers = response.Headers;

            long serverDate = 0;
            long lastModified = 0;
            long serverExpires = 0;
            long softExpire = 0;
            long finalExpire = 0;
            long maxAge = 0;
            long staleWhileRevalidate = 0;
            bool hasCacheControl = false;
            bool mustRevalidate = false;

            String serverEtag = null;
            String headerValue;

            headers.TryGetValue("Date", out headerValue);
            if (headerValue != null)
            {
                serverDate = ParseDateAsEpoch(headerValue);
            }

            headers.TryGetValue("Cache-Control", out headerValue);
            if(headerValue != null)
            {
                hasCacheControl = true;
                String[] tokens = headerValue.Split(',');
            }
        }
    }
}