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
using Java.Net;

/*
 * “—∫À µ
 */

namespace VolleyCSharp
{
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

        private MarkerLog mEventLog = MarkerLog.ENABLED ? new MarkerLog() : null;
        private Method mMethod;
        private String mUrl;
        private String mRedirectUrl;
        private String mIdentifier;
        private int mDefaultTrafficStatsTag;
        private IErrorListener mErrorListener;
        private int mSequence;
        private RequestQueue mRequestQueue;
        private bool mShouldCache = true;
        private bool mCanceled = false;
        private bool mResponseDelivered = false;
        private long mRequestBirthTime = 0;
        private static long SLOW_REQUEST_THRESHOLD_MS = 3000;
        private IRetryPolicy mRetryPolicy;
        private Entry mCacheEntry = null;
        private object mTag;

        public Request(String url, IErrorListener listener)
            : this(Method.DEPRECATED_GET_OR_POST, url, listener) { }

        public Request(Method method, String url, IErrorListener listener)
        {
            this.mMethod = method;
            this.mUrl = url;
            this.mIdentifier = CreateIdentifier(method, url);
            this.mErrorListener = listener;
            SetRetryPolicy(new DefaultRetryPolicy());

            mDefaultTrafficStatsTag = FindDefaultTrafficStatsTag(url);
        }

        public Method Methods
        {
            get
            {
                return this.mMethod;
            }
        }

        public object Tag
        {
            get
            {
                return this.mTag;
            }
            set
            {
                this.mTag = value;
            }
        }

        public IErrorListener ErrorListener
        {
            get
            {
                return mErrorListener;
            }
        }

        public int TrafficStatsTag
        {
            get
            {
                return this.mDefaultTrafficStatsTag;
            }
        }

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

        public int Sequence
        {
            get
            {
                return mSequence;
            }
            set
            {
                this.mSequence = value;
            }
        }

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

        public String Identifier
        {
            get
            {
                return mIdentifier;
            }
        }

        public void SetRedirectUrl(String redirectUrl)
        {
            this.mRedirectUrl = redirectUrl;
        }

        public String GetCacheKey()
        {
            return Url;
        }

        public Entry CacheEntry
        {
            get
            {
                return mCacheEntry;
            }
            set
            {
                this.mCacheEntry = value;
            }
        }

        public void Cancel()
        {
            this.mCanceled = true;
        }

        public virtual bool IsCanceled
        {
            get
            {
                return mCanceled;
            }
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

        public String GetPostBodyContentType()
        {
            return GetBodyContentType();
        }

        public byte[] GetPostBody()
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

        public String GetBodyContentType()
        {
            return "application/x-www-form-urlencoded; charset=" + GetParamsEncoding();
        }

        public byte[] GetBody()
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
                    encoderParams.Append(URLEncoder.Encode(entry.Key, paramsEncoding));
                    encoderParams.Append('=');
                    encoderParams.Append(URLEncoder.Encode(entry.Value, paramsEncoding));
                    encoderParams.Append('&');
                }
                return new Java.Lang.String(encoderParams).GetBytes(paramsEncoding);
            }
            catch (Java.IO.UnsupportedEncodingException uee)
            {
                throw new Java.Lang.RuntimeException("Encoding not supported:" + paramsEncoding, uee);
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

        public enum Priority
        {
            LOW,
            NORMAL,
            HIGH,
            IMMEDIATE
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

        public abstract void DeliverResponse(object response);

        public void DeliverError(VolleyError error)
        {
            if (mErrorListener != null)
            {
                mErrorListener.OnErrorResponse(error);
            }
        }

        public int CompareTo(Request other)
        {
            Priority left = this.GetPriority();
            Priority right = other.GetPriority();

            return left == right ? this.mSequence - other.mSequence : (int)right - (int)left;
        }

        public override string ToString()
        {
            String trafficStatsTag = "0x" + Java.Lang.Integer.ToHexString(TrafficStatsTag);
            return (mCanceled ? "[x] " : "[ ]") + Url + " " + trafficStatsTag + " " + GetPriority().ToString() + " " + mSequence;
        }

        private static long sCounter;

        private static String CreateIdentifier(Method method, String url)
        {
            return InternalUtils.SHA1Hash("Request:" + method.ToString() + ":" + url + ":"
                + Java.Lang.JavaSystem.CurrentTimeMillis() + ":" + (sCounter++));
        }
    }
}