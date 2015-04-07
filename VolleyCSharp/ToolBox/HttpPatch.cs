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
using Org.Apache.Http.Client.Methods;

namespace VolleyCSharp.ToolBox
{
    public class HttpPatch : HttpEntityEnclosingRequestBase
    {
        public static String METHOD_NAME = "PATCH";

        public HttpPatch()
            : base() { }

        public HttpPatch(Java.Net.URI uri)
            : base()
        {
            URI = uri;
        }

        public HttpPatch(String uri)
            : base()
        {
            URI = new Java.Net.URI(uri);
        }

        public override string Method
        {
            get { return METHOD_NAME; }
        }
    }
}