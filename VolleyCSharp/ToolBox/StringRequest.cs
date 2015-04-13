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
    public class StringRequest : Request
    {
        private IListener mListener;

        public StringRequest(Method method, String url, IListener listener, IErrorListener errorListener)
            : base(method, url, errorListener)
        {
            mListener = listener;
        }

        public StringRequest(String url, IListener listener, IErrorListener errorListener)
            : this(Method.GET, url, listener, errorListener) { }

        public override Response ParseNetworkResponse(NetworkResponse response)
        {
            Java.Lang.String parsed;
            try
            {
                parsed = new Java.Lang.String(response.Data, HttpHeaderParser.ParseCharset(response.Headers));
            }
            catch (Java.IO.UnsupportedEncodingException)
            {
                parsed = new Java.Lang.String(response.Data);
            }
            return Response.Success(parsed, HttpHeaderParser.ParseCacheHeaders(response));
        }

        public override void DeliverResponse(object response)
        {
            mListener.OnResponse(response as string);
        }
    }
}