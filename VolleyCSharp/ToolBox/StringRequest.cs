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
using VolleyCSharp.MainCom;
using VolleyCSharp.Utility;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.ToolBox
{
    /// <summary>
    /// 字符串请求
    /// </summary>
    public class StringRequest : Request
    {
        private Action<String> mListener;

        public StringRequest(Method method, String url, Action<String> listener, Action<VolleyError> errorListener)
            : base(method, url, errorListener)
        {
            mListener = listener;
        }

        public StringRequest(String url, Action<String> listener, Action<VolleyError> errorListener)
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
            return Response.Success(parsed.ToString(), HttpHeaderParser.ParseCacheHeaders(response));
        }

        public override void DeliverResponse(String response)
        {
            mListener(response);
        }
    }
}