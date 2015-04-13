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
using System.Net;

/*
 * 2015.4.13 改写
 */

namespace VolleyCSharp.NetCom
{
    public interface IHttpStack
    {
        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="request">表示一个请求</param>
        /// <param name="additionalHeaders">附加请求头部参数</param>
        /// <returns>请求反馈</returns>
        HttpWebResponse PerformRequest(Request request, Dictionary<String, String> additionalHeaders);
    }
}