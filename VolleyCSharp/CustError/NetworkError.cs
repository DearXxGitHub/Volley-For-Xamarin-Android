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
    public class NetworkError : VolleyError
    {
        public NetworkError()
            : base() { }

        public NetworkError(Exception cause)
            : base(cause) { }

        public NetworkError(NetworkResponse networkResponse)
            : base(networkResponse) { }
    }
}