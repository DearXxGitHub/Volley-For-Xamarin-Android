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
 * 15.3.13 改写
 */

namespace VolleyCSharp.Delivery
{
    /// <summary>
    /// 用来负责具体在主线程上执行的回调
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