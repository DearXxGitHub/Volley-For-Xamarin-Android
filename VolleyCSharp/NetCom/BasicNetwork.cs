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
using System.Net;
using VolleyCSharp.ToolBox;

namespace VolleyCSharp.NetCom
{
    /// <summary>
    /// 封装底层网络
    /// 原版本是提供在不同系统下使用不同网络实现的方式
    /// </summary>
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

        public NetworkResponse PerformRequest(Request request)
        {
            long requestStart = SystemClock.ElapsedRealtime();
            while (true)
            {
                HttpWebResponse httpResponse = null;
                byte[] responseContents = null;
                Dictionary<String,String> responseHeaders = null;
                try
                {
                    Dictionary<String, String> headers = new Dictionary<string, string>();
                    AddCacheHeaders(headers, request.CacheEntry);
                    httpResponse = mHttpStack.PerformRequest(request, headers);
                    var statusCode = httpResponse.StatusCode;

                    responseHeaders = ConvertHeaders(httpResponse.Headers);

                    if (statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Moved)
                    {
                        String newUrl = responseHeaders["Location"];
                        request.SetRedirectUrl(newUrl);
                    }

                    Stream output = httpResponse.GetResponseStream();
                    if (output != null)
                    {
                        responseContents = EntityToBytes(output);
                    }
                    else
                    {
                        responseContents = new byte[0];
                    }

                    long requestLifetime = SystemClock.ElapsedRealtime() - requestStart;
                    LogSlowRequests(requestLifetime, request, responseContents, statusCode);

                    if (statusCode < HttpStatusCode.OK || (int)statusCode > 299)
                    {
                        throw new IOException();
                    }
                    return new NetworkResponse(statusCode, responseContents, responseHeaders, false,
                        SystemClock.ElapsedRealtime() - requestStart);
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        var result = ex.Response as HttpWebResponse;
                        if (result.StatusCode == HttpStatusCode.NotModified)
                        {
                            Entry entry = request.CacheEntry;
                            if (entry == null)
                            {
                                return new NetworkResponse(HttpStatusCode.NotModified, null,
                                    responseHeaders, true,
                                    SystemClock.ElapsedRealtime() - requestStart);
                            }

                            //entry.ResponseHeaders = entry.ResponseHeaders.Intersect(responseHeaders).ToDictionary(x => x.Key, x => x.Value);
                            return new NetworkResponse(HttpStatusCode.NotModified, entry.Data,
                                entry.ResponseHeaders, true,
                                SystemClock.ElapsedRealtime() - requestStart);
                        }
                    }
                    throw new NetworkError(ex);
                }
                catch (TimeoutException)
                {
                    AttempRetryOnException("connection", request, new TimeoutError());
                }
                catch (Java.Net.MalformedURLException e)
                {
                    throw new Java.Lang.RuntimeException("Bad URL " + request.Url, e);
                }
                catch (IOException e)
                {
                    HttpStatusCode statusCode = 0;
                    NetworkResponse networkResponse = null;
                    if (httpResponse != null)
                    {
                        statusCode = httpResponse.StatusCode;
                    }
                    else
                    {
                        throw new NoConnectionError(e);
                    }
                    if (statusCode == HttpStatusCode.MovedPermanently)
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
                        if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
                        {
                            AttempRetryOnException("auth", request, new AuthFailureError());
                        }
                        else if (statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Moved)
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

        private void LogSlowRequests(long requestLifetime, Request request, byte[] responseContents, HttpStatusCode statusCode)
        {
            if (DEBUG || requestLifetime > SLOW_REQUEST_THRESHOLD_MS)
            {
                VolleyLog.D("HTTP response for request=<{0}> [lifetime={1}],[size={2}], [rc={3}],[retryCount={4}]", requestLifetime, requestLifetime,
                    responseContents != null ? responseContents.Length.ToString() : "null",
                    statusCode, request.GetRetryPolicy().CurrentRetryCount);
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

        /// <summary>
        /// 将流转换成字节
        /// </summary>
        private byte[] EntityToBytes(Stream entity)
        {
            StreamReader sr = new StreamReader(entity);
            byte[] buffer = Encoding.UTF8.GetBytes(sr.ReadToEnd());
            entity.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// 将请求头转换成字典
        /// </summary>
        private Dictionary<String, String> ConvertHeaders(WebHeaderCollection headers)
        {
            Dictionary<String, String> dic = new Dictionary<string, string>();
            foreach (var item in headers.AllKeys)
            {
                dic.Add(item, headers[item]);
            }
            return dic;
        }
    }
}