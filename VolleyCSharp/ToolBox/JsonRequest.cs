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
    public abstract class JsonRequest : Request
    {
        protected static String PROTOCOL_CHARSET = "utf-8";
        private static String PROTOCOL_CONTENT_TYPE = "application/json;charset=utf-8";

        private IListener mListener;
        private String mRequestBody;

        public JsonRequest(String url, String requestBody, IListener listener, IErrorListener errorListener)
            : this(Method.DEPRECATED_GET_OR_POST, url, requestBody, listener, errorListener) { }

        public JsonRequest(Method method, String url, String requestBody, IListener listener, IErrorListener errorListener)
            : base(method, url, errorListener)
        {
            mListener = listener;
            mRequestBody = requestBody;
        }

        public override void DeliverResponse(object response)
        {
            mListener.OnResponse(response);
        }

        public abstract override Response ParseNetworkResponse(NetworkResponse response);

        public override string GetPostBodyContentType()
        {
            return GetBodyContentType();
        }

        public override byte[] GetPostBody()
        {
            return GetBody();
        }

        public override string GetBodyContentType()
        {
            return PROTOCOL_CONTENT_TYPE;
        }

        public override byte[] GetBody()
        {
            try
            {
                return mRequestBody == null ? null : Encoding.UTF8.GetBytes(mRequestBody);
            }
            catch (Exception)
            {
                VolleyLog.WTF("Unsupported Encoding while trying to get the bytes of {0} using {1}",
                    mRequestBody, PROTOCOL_CHARSET);
                return null;
            }
        }
    }
}