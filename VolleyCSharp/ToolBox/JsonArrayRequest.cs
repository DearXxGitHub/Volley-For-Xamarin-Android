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
    public class JsonArrayRequest : JsonRequest
    {
        public JsonArrayRequest(Method method, String url, String requestBody,
            IListener listener, IErrorListener errorListener)
            : base(method, url, requestBody, listener, errorListener) { }

        public JsonArrayRequest(String url, IListener listener, IErrorListener errorListener)
            : base(Method.GET, url, null, listener, errorListener) { }

        public JsonArrayRequest(Method method, String url, IListener listener, IErrorListener errorListener)
            : base(method, url, null, listener, errorListener) { }

        public JsonArrayRequest(Method method, String url, Org.Json.JSONArray jsonRequest, IListener listener, IErrorListener errorListener)
            : base(method, url, (jsonRequest == null) ? null : jsonRequest.ToString(), listener, errorListener) { }

        public JsonArrayRequest(Method method, String url, Org.Json.JSONObject jsonRequest, IListener listener, IErrorListener errorListener)
            : base(method, url, (jsonRequest == null) ? null : jsonRequest.ToString(), listener, errorListener) { }

        public JsonArrayRequest(String url, Org.Json.JSONArray jsonRequest, IListener listener, IErrorListener errorListener)
            : this(jsonRequest == null ? Method.GET : Method.POST, url, jsonRequest, listener, errorListener) { }

        public JsonArrayRequest(String url, Org.Json.JSONObject jsonRequest, IListener listener,
            IErrorListener errorListener)
            : this(jsonRequest == null ? Method.GET : Method.POST, url, jsonRequest, listener, errorListener) { }

        public override Response ParseNetworkResponse(NetworkResponse response)
        {
            String charset = HttpHeaderParser.ParseCharset(response.Headers, PROTOCOL_CHARSET);
            String jsonString = null;
            if (charset == "utf-8")
            {
                jsonString = Encoding.UTF8.GetString(response.Data);

            }
            return Response.Success(new Org.Json.JSONArray(jsonString), HttpHeaderParser.ParseCacheHeaders(response));
        }
    }
}