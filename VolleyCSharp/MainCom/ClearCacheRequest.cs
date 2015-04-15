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
using VolleyCSharp.CacheCom;

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
    /// 清除缓存的请求
    /// </summary>
    public class ClearCacheRequest : Request
    {
        private ICache mCahce;
        private Java.Lang.IRunnable mCallback;

        public ClearCacheRequest(ICache cache, Java.Lang.IRunnable callback)
            : base(Method.GET, null, null)
        {
            mCahce = cache;
            mCallback = callback;
        }

        public override bool IsCanceled
        {
            get
            {
                mCahce.Clear();
                if (mCallback != null)
                {
                    var handler = new Handler(Looper.MainLooper);
                    handler.PostAtFrontOfQueue(mCallback);
                }
                return true;
            }
        }

        public override Request.Priority GetPriority()
        {
            return Priority.IMMEDIATE;
        }

        public override Response ParseNetworkResponse(NetworkResponse response)
        {
            return null;
        }

        public override void DeliverResponse(String response) { }
    }
}