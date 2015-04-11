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

        #region INetwork

        public override NetworkResponse PerformRequest(Request request)
        {
            long requestStart = SystemClock.ElapsedRealtime();
            while (true)
            {
                Org.Apache.Http.IHttpResponse httpResponse = null;
                byte[] responseContents = null;
                Dictionary<String,String> responseHeaders = null;
                try
                {
                    Dictionary<String, String> headers = new Dictionary<string, string>();
                    AddCacheHeaders(headers, request.CacheEntry);
                    httpResponse = mHttpStack.PerformRequest(request, headers);
                    Org.Apache.Http.IStatusLine statusLine = httpResponse.StatusLine;
                    int statusCode = statusLine.StatusCode;

                    responseHeaders = ConvertHeaders(httpResponse.GetAllHeaders());

                    if (statusCode == Org.Apache.Http.HttpStatus.ScNotModified)
                    {
                        Entry entry = request.CacheEntry;
                        if (entry == null)
                        {
                            return new NetworkResponse(Org.Apache.Http.HttpStatus.ScNotModified, null,
                                responseHeaders, true,
                                SystemClock.ElapsedRealtime() - requestStart);
                        }

                        entry.ResponseHeaders = entry.ResponseHeaders.Intersect(responseHeaders).ToDictionary(x => x.Key, x => x.Value);
                        return new NetworkResponse(Org.Apache.Http.HttpStatus.ScNotModified, entry.Data,
                            entry.ResponseHeaders, true,
                            SystemClock.ElapsedRealtime() - requestStart);
                    }

                    if (statusCode == Org.Apache.Http.HttpStatus.ScMovedPermanently || statusCode == Org.Apache.Http.HttpStatus.ScMovedTemporarily)
                    {
                        String newUrl = responseHeaders["Location"];
                        request.SetRedirectUrl(newUrl);
                    }

                    if (httpResponse.Entity != null)
                    {
                        responseContents = EntryToBytes(httpResponse.Entity);
                    }
                    else
                    {
                        responseContents = new byte[0];
                    }

                    long requestLifetime = SystemClock.ElapsedRealtime() - requestStart;
                    LogSlowRequests(requestLifetime, requestLifetime, responseContents, statusLine);

                    if (statusCode < 200 || statusCode > 299)
                    {
                        throw new IOException();
                    }
                    return new NetworkResponse(statusCode, responseContents, responseHeaders, false,
                        SystemClock.ElapsedRealtime() - requestStart);
                }
                catch (Java.Net.SocketTimeoutException)
                {
                    AttempRetryOnException("socket", request, new TimeoutError());
                }
                catch (Org.Apache.Http.Conn.ConnectTimeoutException)
                {
                    AttempRetryOnException("connection", request, new TimeoutError());
                }
                catch (Java.Net.MalformedURLException e)
                {
                    throw new Java.Lang.RuntimeException("Bad URL " + request.Url, e);
                }
                catch (Java.IO.IOException e)
                {
                    int statusCode = 0;
                    NetworkResponse networkResponse = null;
                    if (httpResponse != null)
                    {
                        statusCode = httpResponse.StatusLine.StatusCode;
                    }
                    else
                    {
                        throw new NoConnectionError(e);
                    }
                    if (statusCode == Org.Apache.Http.HttpStatus.ScMovedPermanently)
                    {
                        VolleyLog.E("Request at {0} has been redirected to {1}", request.OriginUrl, request.Url);
                    }
                    else
                    {
                        VolleyLog.E("Unexpected response code {0} for {1}", statusCode, request.Url);
                    }
                    if (responseContents != null)
                    {
                        networkResponse = new NetworkResponse(statusCode, responseContents,
                            responseHeaders, false, SystemClock.ElapsedRealtime() - requestStart);
                        if (statusCode == Org.Apache.Http.HttpStatus.ScUnauthorized || statusCode == Org.Apache.Http.HttpStatus.ScForbidden)
                        {
                            AttempRetryOnException("auth", request, new AuthFailureError());
                        }
                        else if (statusCode == Org.Apache.Http.HttpStatus.ScMovedPermanently || statusCode == Org.Apache.Http.HttpStatus.ScMovedTemporarily)
                        {
                            AttempRetryOnException("redirect", request, new AuthFailureError(networkResponse));
                        }
                        else
                        {
                            throw new ServerError(networkResponse);
                        }
                    }
                    else
                    {
                        throw new NetworkError(networkResponse);
                    }
                }
            }
        }

        #endregion

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