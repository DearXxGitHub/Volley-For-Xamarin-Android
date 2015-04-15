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
using VolleyCSharp.MainCom;

namespace VolleyCSharp
{
    public class ParseError : VolleyError
    {
        public ParseError() { }

        public ParseError(NetworkResponse networkResponse)
            : base(networkResponse) { }

        public ParseError(Java.Lang.Throwable cause)
            : base(cause) { }
    }
}