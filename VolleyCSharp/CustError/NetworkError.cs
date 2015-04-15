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

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp
{
    /// <summary>
    /// 网络错误
    /// </summary>
    public class NetworkError : VolleyError
    {
        public NetworkError()
            : base() { }

        public NetworkError(Exception cause)
            : base(cause) { }

        public NetworkError(NetworkResponse networkResponse)
            : base(networkResponse) { }
    }
}