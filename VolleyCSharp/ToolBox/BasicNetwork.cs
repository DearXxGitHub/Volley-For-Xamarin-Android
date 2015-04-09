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
using System.IO;

namespace VolleyCSharp.ToolBox
{
    public class BasicNetwork : INetwork
    {
        protected static bool DEBUG = VolleyLog.DEBUG;
        private static int SLOW_REQUEST_THRESHOLD_MS = 3000;
        private static int DEFAULT_POOL_SIZE = 4096;
        protected IHttpStack mHttpStack;
        protected ByteArrayPool mPool;

        public BasicNetwork(IHttpStack httpStack)
            : this(httpStack, new ByteArrayPool(DEFAULT_POOL_SIZE)) { }

        public BasicNetwork(IHttpStack httpStack, ByteArrayPool pool)
        {
            this.mHttpStack = httpStack;
            this.mPool = pool;
        }

        public NetworkResponse PerformRequest(Request request)
        {

        }

        private void LogSlowRequests(long requestLifetime, Request request, byte[] responseContents, Org.Apache.Http.IStatusLine statusLine)
        {
            if (DEBUG || requestLifetime > SLOW_REQUEST_THRESHOLD_MS)
            {
                VolleyLog.D("HTTP response for request=<{0}> [lifetime={1}],[size={2}], [rc={3}],[retryCount={4}]", requestLifetime, requestLifetime, 
                    responseContents != null ? responseContents.Length.ToString() : "null",
                    statusLine.StatusCode, request.GetRetryPolicy().CurrentRetryCount);
            }
        }

        private static void AttempRetryOnException(String logPrefix, Request request, VolleyError exception)
        {
            IRetryPolicy retryPolicy = request.GetRetryPolicy();
            int oldTimeout = request.GetTimeoutMs();

            try
            {
                retryPolicy.Retry(exception);
            }
            catch (VolleyError e)
            {
                request.AddMarker(String.Format("{0}-timeout-giveup[timeout={1}]", logPrefix, oldTimeout));
                throw e;
            }
            request.AddMarker(String.Format("{0}-retry [timeout-{1}]", logPrefix, oldTimeout));
        }

        private void AddCacheHeaders(Dictionary<String, String> headers, Entry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.ETag != null)
            {
                headers.Add("If-None-Match", entry.ETag);
            }

            if (entry.LastModified > 0)
            {
                var refTime = new DateTime(entry.LastModified);
                headers.Add("If-Modified-Since", refTime.ToString());
            }
        }

        protected void LogError(String what, String url, long start)
        {
            long now = SystemClock.ElapsedRealtime();
            VolleyLog.V("HTTP ERROR({0}) {1} ms to fetch {2}", what, (now - start), url);
        }

        private byte[] EntityToBytes(Org.Apache.Http.IHttpEntity entity)
        {
            StreamWriter bytes = new StreamWriter(mPool
        }
    }
}