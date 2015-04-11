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
    public class JsonObjectRequest : JsonRequest
    {
        public JsonObjectRequest(Method method, String url, String requestBody,
            IListener listener, IErrorListener errorListener)
            : base(method, url, requestBody, listener, errorListener) { }

        public JsonObjectRequest(String url, IListener listener, IErrorListener errorListener)
            : base(Method.GET, url, null, listener, errorListener) { }

        public JsonObjectRequest(Method method, String url, IListener listener, IErrorListener errorListener)
            : base(method, url, null, listener, errorListener) { }

        public JsonObjectRequest(Method method, String url, Org.Json.JSONObject jsonRequest, IListener listener,
            IErrorListener errorListener)
            : base(method, url, (jsonRequest == null) ? null : jsonRequest.ToString(), listener, errorListener) { }

        public JsonObjectRequest(String url, Org.Json.JSONObject jsonRequest, IListener listener,
            IErrorListener errorListener)
            : this(jsonRequest == null ? Method.GET : Method.POST, url, jsonRequest, listener, errorListener) { }

        public override Response ParseNetworkResponse(NetworkResponse response)
        {

        }
    }
}