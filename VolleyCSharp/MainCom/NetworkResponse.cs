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
using Org.Apache.Http;
using System.Net;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.MainCom
{
    /// <summary>
    /// 用于封装回应，通过Request的ParseNetworkResponse处理后
    /// 封装为更抽象的Response对象
    /// </summary>
    public class NetworkResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public byte[] Data { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool NotModified { get; set; }
        public long NetworkTimeMs { get; set; }

        public NetworkResponse(HttpStatusCode statusCode, byte[] data, Dictionary<String, String> headers, bool notModified, long networkTimeMs)
        {
            this.StatusCode = statusCode;
            this.Data = data;
            this.Headers = headers;
            this.NotModified = notModified;
            this.NetworkTimeMs = networkTimeMs;
        }

        public NetworkResponse(HttpStatusCode statusCode, byte[] data, Dictionary<string, string> headers, bool notModified)
            : this(statusCode, data, headers, notModified, 0) { }

        public NetworkResponse(byte[] data)
            : this(HttpStatusCode.OK, data, new Dictionary<string, string>(), false, 0) { }

        public NetworkResponse(byte[] data, Dictionary<string, string> headers)
            : this(HttpStatusCode.OK, data, headers, false, 0) { }
    }
}