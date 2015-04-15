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
    /// 公共错误基类
    /// </summary>
    public class VolleyError : Exception
    {
        public NetworkResponse networkResponse;
        private long networkTimeMs;

        public VolleyError()
        {
            networkResponse = null;
        }

        public VolleyError(NetworkResponse response)
        {
            networkResponse = response;
        }

        public VolleyError(String exceptionMessage)
            : base(exceptionMessage)
        {
            networkResponse = null;
        }

        public VolleyError(String exceptionMessage, Exception reason)
            : base(exceptionMessage, reason)
        {
            networkResponse = null;
        }

        public VolleyError(Exception reason)
            : base("", reason)
        {
            networkResponse = null;
        }

        public long NetworkTimeMs
        {
            get
            {
                return networkTimeMs;
            }
            set
            {
                networkTimeMs = value;
            }
        }
    }
}