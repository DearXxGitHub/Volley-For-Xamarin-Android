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

namespace VolleyCSharp.MainCom
{
    /// <summary>
    /// 如果需要自定义自己的请求方式
    /// 必须继承该类，并实现对应的抽
    /// 象方法
    /// </summary>
    public abstract class Request : IComparable<Request>
    {
        private static String DEFAULT_PARAMS_ENCODING = "UTF-8";

        public enum Method
        {
            DEPRECATED_GET_OR_POST = -1,
            GET = 0,
            POST = 1,
            PUT = 2,
            DELETE = 3,
            HEAD = 4,
            OPTIONS = 5,
            TRACE = 6,
            PATCH = 7
        }

        public enum Priority
        {
            LOW,
            NORMAL,
            HIGH,
            IMMEDIATE
        }

        private MarkerLog mEventLog = MarkerLog.ENABLED ? new MarkerLog() : null;

        private String mUrl;
        private String mRedirectUrl;

        private RequestQueue mRequestQueue;
        private bool mShouldCache = true;
        private bool mResponseDelivered = false;
        private long mRequestBirthTime = 0;
        private static long SLOW_REQUEST_THRESHOLD_MS = 3000;
        private IRetryPolicy mRetryPolicy;

        public Request(String url, Action<VolleyError> listener)
            : this(Method.DEPRECATED_GET_OR_POST, url, listener) { }

        public Request(Method method, String url, Action<VolleyError> listener)
        {
            this.mUrl = url;
            this.Methods = method;
            this.Identifier = CreateIdentifier(method, url);
            this.ErrorListener = listener;
            SetRetryPolicy(new DefaultRetryPolicy());
            this.TrafficStatsTag = FindDefaultTrafficStatsTag(url);
        }

        public Method Methods { get; private set; }
        public object Tag { get; set; }
        public Action<VolleyError> ErrorListener { get; private set; }
        public int TrafficStatsTag { get; private set; }
        public int Sequence { get; set; }
        public String Url
        {
            get
            {
                return (mRedirectUrl != null) ? mRedirectUrl : mUrl;
            }
        }
        public String OriginUrl
        {
            get
            {
                return mUrl;
            }
        }
        public String Identifier { get; private set; }
        public Entry CacheEntry { get; set; }
        public virtual bool IsCanceled { get; private set; }

        private static int FindDefaultTrafficStatsTag(String url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                var uri = Android.Net.Uri.Parse(url);
                if (uri != null)
                {
                    String host = uri.Host;
                    if (host != null)
                    {
                        return host.GetHashCode();
                    }
                }
            }
            return 0;
        }

        public Request SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            mRetryPolicy = retryPolicy;
            return this;
        }

        public void AddMarker(String tag)
        {
            if (MarkerLog.ENABLED)
            {
                mEventLog.Add(tag, Java.Lang.Thread.CurrentThread().Id);
            }
            else if (mRequestBirthTime == 0)
            {
                mRequestBirthTime = SystemClock.ElapsedRealtime();
            }
        }

        public void Finish(String tag)
        {
            if (mRequestQueue != null)
            {
                mRequestQueue.Finish(this);
            }
            if (MarkerLog.ENABLED)
            {
                long threadId = Java.Lang.Thread.CurrentThread().Id;
                if (Looper.MyLooper() != Looper.MainLooper)
                {
                    Handler mainThread = new Handler(Looper.MainLooper);
                    mainThread.Post(() =>
                    {
                        mEventLog.Add(tag, threadId);
                        mEventLog.Finish(this.ToString());
                    });
                    return;
                }
                mEventLog.Add(tag, threadId);
                mEventLog.Finish(this.ToString());
            }
            else
            {
                long requestTime = SystemClock.ElapsedRealtime() - mRequestBirthTime;
                if (requestTime >= SLOW_REQUEST_THRESHOLD_MS)
                {
                    VolleyLog.D("{0} ms:{1}", requestTime, this.ToString());
                }
            }
        }

        public Request SetRequestQueue(RequestQueue requestQueue)
        {
            this.mRequestQueue = requestQueue;
            return this;
        }

        public void SetRedirectUrl(String redirectUrl)
        {
            this.mRedirectUrl = redirectUrl;
        }

        public String GetCacheKey()
        {
            return Url;
        }

        public void Cancel()
        {
            this.IsCanceled = true;
        }

        public Dictionary<String, String> GetHeaders()
        {
            return new Dictionary<string, string>();
        }

        protected Dictionary<String, String> GetPostParams()
        {
            return GetParams();
        }

        protected String GetPostParamsEncoding()
        {
            return GetParamsEncoding();
        }

        public virtual String GetPostBodyContentType()
        {
            return GetBodyContentType();
        }

        public virtual byte[] GetPostBody()
        {
            Dictionary<String, String> postParams = GetPostParams();
            if (postParams != null && postParams.Count > 0)
            {
                return EncodeParameters(postParams, GetPostParamsEncoding());
            }
            return null;
        }

        protected Dictionary<string, string> GetParams()
        {
            return null;
        }

        protected String GetParamsEncoding()
        {
            return DEFAULT_PARAMS_ENCODING;
        }

        public virtual String GetBodyContentType()
        {
            return "application/x-www-form-urlencoded; charset=" + GetParamsEncoding();
        }

        public virtual byte[] GetBody()
        {
            Dictionary<String,String> param = GetParams();
            if (param != null && param.Count > 0)
            {
                return EncodeParameters(param, GetParamsEncoding());
            }
            return null;
        }

        private byte[] EncodeParameters(Dictionary<String, String> param, String paramsEncoding)
        {
            var encoderParams = new Java.Lang.StringBuilder();
            try
            {
                foreach (KeyValuePair<String, String> entry in param)
                {
                    encoderParams.Append(Java.Net.URLEncoder.Encode(entry.Key, paramsEncoding));
                    encoderParams.Append('=');
                    encoderParams.Append(Java.Net.URLEncoder.Encode(entry.Value, paramsEncoding));
                    encoderParams.Append('&');
                }
                return new Java.Lang.String(encoderParams).GetBytes(paramsEncoding);
            }
            catch (Java.IO.UnsupportedEncodingException uee)
            {
                throw new InvalidOperationException("Encoding not supported:" + paramsEncoding);
            }
        }

        public Request SetShouldCache(bool shouldCache)
        {
            this.mShouldCache = shouldCache;
            return this;
        }

        public bool ShouldCache()
        {
            return this.mShouldCache;
        }

        public virtual Priority GetPriority()
        {
            return Priority.NORMAL;
        }

        public int GetTimeoutMs()
        {
            return mRetryPolicy.CurrentTimeout;
        }

        public IRetryPolicy GetRetryPolicy()
        {
            return this.mRetryPolicy;
        }

        public void MarkDelivered()
        {
            mResponseDelivered = true;
        }

        public bool HasHadResponseDelivered()
        {
            return mResponseDelivered;
        }

        public abstract Response ParseNetworkResponse(NetworkResponse response);

        public VolleyError ParseNetworkError(VolleyError volleyError)
        {
            return volleyError;
        }

        public abstract void DeliverResponse(String response);

        public void DeliverError(VolleyError error)
        {
            if (ErrorListener != null)
            {
                ErrorListener(error);
            }
        }

        public int CompareTo(Request other)
        {
            Priority left = this.GetPriority();
            Priority right = other.GetPriority();

            return left == right ? this.Sequence - other.Sequence : (int)right - (int)left;
        }

        public override string ToString()
        {
            String trafficStatsTag = "0x" + Java.Lang.Integer.ToHexString(TrafficStatsTag);
            return (IsCanceled ? "[x] " : "[ ]") + Url + " " + trafficStatsTag + " " + GetPriority().ToString() + " " + Sequence;
        }

        private static long sCounter;

        /// <summary>
        /// 创建请求标识符
        /// </summary>
        private static String CreateIdentifier(Method method, String url)
        {
            return InternalUtils.SHA1Hash("Request:" + method.ToString() + ":" + url + ":"
                + Java.Lang.JavaSystem.CurrentTimeMillis() + ":" + (sCounter++));
        }
    }
}