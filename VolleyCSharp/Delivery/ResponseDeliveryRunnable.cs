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

namespace VolleyCSharp.Delivery
{
    /// <summary>
    /// 用来负责具体在主线程上执行回调
    /// </summary>
    public class ResponseDeliveryRunnable : Java.Lang.Object, Java.Lang.IRunnable
    {
        private Request mRequest;
        private Response mResponse;
        private Action mRunnable;

        public ResponseDeliveryRunnable(Request request, Response response, Action runnable)
        {
            this.mRequest = request;
            this.mResponse = response;
            this.mRunnable = runnable;
        }

        public void Run()
        {
            if (mResponse.IsSuccess)
            {
                mRequest.DeliverResponse(mResponse.Result);
            }
            else
            {
                mRequest.DeliverError(mResponse.MError);
            }

            if (mResponse.Intermediate)
            {
                mRequest.AddMarker("intermediate-response");
            }
            else
            {
                mRequest.Finish("done");
            }

            if (mRunnable != null)
            {
                mRunnable();
            }
        }
    }
}