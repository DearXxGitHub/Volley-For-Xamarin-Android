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
    /// 认证授权错误
    /// </summary>
    public class AuthFailureError : VolleyError
    {
        private Intent mResolutionIntent;

        public AuthFailureError() { }

        public AuthFailureError(Intent intent)
        {
            mResolutionIntent = intent;
        }

        public AuthFailureError(String message)
            : base(message) { }

        public AuthFailureError(String message, Java.Lang.Exception reason)
            : base(message, reason) { }

        public AuthFailureError(NetworkResponse response)
            : base(response) { }

        public Intent GetResolutionIntent()
        {
            return mResolutionIntent;
        }

        public override string Message
        {
            get
            {
                if (mResolutionIntent != null)
                {
                    return "User needs to (re)enter credentials.";
                }
                return base.Message;
            }
        }
    }
}