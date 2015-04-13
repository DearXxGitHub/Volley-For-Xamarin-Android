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
    public class RequestFuture : Java.Lang.Object, Java.Util.Concurrent.IFuture, IListener, IErrorListener
    {
        private Request mRequest;
        private bool mResultReceived = false;
        private Java.Lang.Object mResult;
        private VolleyError mException;

        static RequestFuture() { }

        public void SetRequest(Request request)
        {
            mResult = request;
        }

        public bool Cancel(bool mayInterruptIfRunning)
        {
            if (mRequest == null)
            {
                return false;
            }
            if (!IsDone)
            {
                mRequest.Cancel();
                return true;
            }
            else
            {
                return false;
            }
        }

        public Java.Lang.Object Get(long timeout, Java.Util.Concurrent.TimeUnit unit)
        {
            return DoGet((Java.Util.Concurrent.TimeUnit.Milliseconds.Convert(timeout, unit)));
        }

        public Java.Lang.Object Get()
        {
            try
            {
                return DoGet(null);
            }
            catch (Java.Util.Concurrent.TimeoutException e)
            {
                throw new Java.Lang.AssertionError(e.ToString());
            }
        }

        private Java.Lang.Object DoGet(long? timeoutMs)
        {
            if (mException != null)
            {
                throw new Java.Util.Concurrent.ExecutionException(mException);
            }

            if (mResultReceived)
            {
                return mResult;
            }

            if (timeoutMs == null)
            {
                Wait(0);
            }
            else if (timeoutMs.Value > 0)
            {
                Wait(timeoutMs.Value);
            }

            if (mException != null)
            {
                throw new Java.Util.Concurrent.ExecutionException(mException);
            }

            if (!mResultReceived)
            {
                throw new TimeoutException();
            }

            return mResult;
        }

        public bool IsCancelled
        {
            get 
            {
                if (mRequest == null)
                {
                    return false;
                }
                return mRequest.IsCanceled;
            }
        }

        public bool IsDone
        {
            get 
            {
                return mResultReceived || mException != null || IsCancelled;
            }
        }

        public void OnResponse(object response)
        {
            lock (this)
            {
                mResultReceived = true;
                mResult = response;
                NotifyAll();
            }
        }

        public void OnErrorResponse(VolleyError error)
        {
            lock (this)
            {
                mException = error;
                NotifyAll();
            }
        }
    }
}