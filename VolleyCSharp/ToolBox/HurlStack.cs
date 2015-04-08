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
    public class HurlStack : IHttpStack
    {
        private static String HEADER_CONTENT_TYPE = "Content-Type";

        private IUrlRewriter mUrlRewriter;
        private Javax.Net.Ssl.SSLSocketFactory mSslSocketFactory;

        public HurlStack()
            : this(null) { }

        public HurlStack(IUrlRewriter urlRewriter)
            : this(urlRewriter, null) { }

        public HurlStack(IUrlRewriter urlRewriter, Javax.Net.Ssl.SSLSocketFactory sslSocketFactory)
        {
            mUrlRewriter = urlRewriter;
            mSslSocketFactory = sslSocketFactory;
        }

        public Org.Apache.Http.IHttpResponse PerformRequest(Request request, Dictionary<string, string> additionalHeaders)
        {

        }

        public static Org.Apache.Http.IHttpEntity EntityFromConnection(Java.Net.HttpURLConnection connection)
        {
            var entity = new Org.Apache.Http.Entity.BasicHttpEntity();
            Java.IO.InputStream 
        }
    }
}