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
using System.Net;
using System.IO;
using VolleyCSharp.ToolBox;
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

namespace VolleyCSharp.NetCom
{
    /// <summary>
    /// 底层网络实现，上层由BasicNetwork负责
    /// </summary>
    public class HttpClientStack : IHttpStack
    {
        internal class HttpMethod
        {
            public const String DELETE = "DELETE";
            public const String POST = "POST";
            public const String PUT = "PUT";
            public const String GET = "GET";
            public const String HEAD = "HEAD";
            public const String OPTIONS = "OPTIONS";
            public const String TRACE = "TRACE";
            public const String PATCH = "PATCH";
        }

        protected const String UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/38.0.2125.111 Safari/537.36";
        protected const String Accept = "text/html,application/xhtml+xml,text/json;q=0.9,image/webp,*/*;q=0.8";

        protected static CookieContainer Cookie { get; set; }

        protected IUrlRewriter UrlRewriter { get; set; }

        public HttpClientStack(IUrlRewriter urlRewriter = null, bool enableSession = true)
        {
            this.UrlRewriter = urlRewriter;
            if (enableSession && Cookie == null)
            {
                Cookie = new CookieContainer();
            }
        }

        [Java.Lang.SuppressWarnings]
        public static List<KeyValuePair<String, String>> GetPostParameterPairs(Dictionary<String, String> postParams)
        {
            List<KeyValuePair<String, String>> result = new List<KeyValuePair<string, string>>(postParams.Count);
            foreach (KeyValuePair<String, String> pair in postParams)
            {
                result.Add(pair);
            }
            return result;
        }

        /// <summary>
        /// 创建请求
        /// </summary>
        public HttpWebRequest CreateHttpRequest(Request request)
        {
            var webrequest = (HttpWebRequest)WebRequest.Create(request.Url);
            webrequest.Accept = Accept;
            webrequest.UserAgent = UserAgent;
            webrequest.Timeout = request.GetTimeoutMs();
            if (Cookie != null)
            {
                webrequest.CookieContainer = Cookie;
            }
            switch (request.Methods)
            {
                case Request.Method.DEPRECATED_GET_OR_POST:
                    {
                        if (request.GetPostBody() != null)
                        {
                            webrequest.Method = HttpMethod.POST;
                            webrequest.ContentType = request.GetPostBodyContentType();
                            WriteData(webrequest, request.GetPostBody());
                            return webrequest;
                        }
                        else
                        {
                            return webrequest;
                        }
                    }
                case Request.Method.GET:
                    {
                        return webrequest;
                    }
                case Request.Method.DELETE:
                    {
                        webrequest.Method = HttpMethod.DELETE;
                        return webrequest;
                    }
                case Request.Method.POST:
                    {
                        webrequest.ContentType = request.GetBodyContentType();
                        webrequest.Method = HttpMethod.POST;
                        WriteData(webrequest, request.GetPostBody());
                        return webrequest;
                    }
                case Request.Method.PUT:
                    {
                        webrequest.ContentType = request.GetBodyContentType();
                        webrequest.Method = HttpMethod.PUT;
                        WriteData(webrequest, request.GetPostBody());
                        return webrequest;
                    }
                case Request.Method.HEAD:
                    {
                        webrequest.Method = HttpMethod.HEAD;
                        return webrequest;
                    }
                case Request.Method.OPTIONS:
                    {
                        webrequest.Method = HttpMethod.OPTIONS;
                        return webrequest;
                    }
                case Request.Method.TRACE:
                    {
                        webrequest.Method = HttpMethod.TRACE;
                        return webrequest;
                    }
                case Request.Method.PATCH:
                    {
                        webrequest.Method = HttpMethod.PATCH;
                        webrequest.ContentType = request.GetPostBodyContentType();
                        WriteData(webrequest, request.GetPostBody());
                        return webrequest;
                    }
                default:
                    {
                        throw new InvalidOperationException("未知请求方式");
                    }
            }
        }

        /// <summary>
        /// 写入需要传递的数据
        /// </summary>
        public void WriteData(HttpWebRequest request, byte[] postBody)
        {
            if (postBody != null)
            {
                Stream input = request.GetRequestStream();
                input.Write(postBody, 0, postBody.Length);
                input.Flush();
            }
        }

        /// <summary>
        /// 请求前进行的操作
        /// </summary>
        protected virtual void OnPrepareRequest(HttpWebRequest request)
        {

        }

        #region IHttpStack

        public HttpWebResponse PerformRequest(Request request, Dictionary<string, string> additionalHeaders)
        {
            if (UrlRewriter != null)
            {
                String rewritten = UrlRewriter.RewriteUrl(request.Url);
                if (!String.IsNullOrEmpty(rewritten))
                {
                    request.SetRedirectUrl(rewritten);
                }
            }
            var httpRequest = CreateHttpRequest(request);
            foreach (KeyValuePair<String, String> head in additionalHeaders)
            {
                try
                {
                    httpRequest.Headers.Add(head.Key, head.Value);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            foreach (KeyValuePair<String, String> head in request.GetHeaders())
            {
                try
                {
                    httpRequest.Headers.Add(head.Key, head.Value);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return (HttpWebResponse)httpRequest.GetResponse();
        }

        #endregion
    }
}