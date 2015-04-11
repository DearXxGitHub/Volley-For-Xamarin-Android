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
    public class HttpClientStack : IHttpStack
    {
        protected Org.Apache.Http.Client.IHttpClient mClient;
        private static String HEADER_CONTENT_TYPE = "Content-Type";

        public HttpClientStack(Org.Apache.Http.Client.IHttpClient client)
        {
            mClient = client;
        }

        public static void AddHeaders(Org.Apache.Http.Client.Methods.IHttpUriRequest httpRequest, Dictionary<String, String> headers)
        {
            foreach (String key in headers.Keys)
            {
                httpRequest.SetHeader(key, headers[key]);
            }
        }

        public static List<KeyValuePair<String, String>> GetPostParameterPairs(Dictionary<String, String> postParams)
        {
            List<KeyValuePair<String, String>> result = new List<KeyValuePair<string, string>>(postParams.Count);
            foreach (KeyValuePair<String, String> pair in postParams)
            {
                result.Add(pair);
            }
            return result;
        }

        public Org.Apache.Http.IHttpResponse PerformRequest(Request request, Dictionary<string, string> additionalHeaders)
        {
            
        }

        public static Org.Apache.Http.Client.Methods.IHttpUriRequest CreateHttpRequest(Request request, Dictionary<String, String> additionalHeaders)
        {
            switch (request.Methods)
            {
                case Request.Method.DEPRECATED_GET_OR_POST:
                    {
                        byte[] postBody = request.GetPostBody();
                        if (postBody != null)
                        {
                            var postRequest = new Org.Apache.Http.Client.Methods.HttpPost(request.Url);
                            postRequest.AddHeader(HEADER_CONTENT_TYPE, request.GetPostBodyContentType());
                            postRequest.Entity = new Org.Apache.Http.Entity.ByteArrayEntity(postBody);
                            return postRequest;
                        }
                        else
                        {
                            return new Org.Apache.Http.Client.Methods.HttpGet(request.Url);
                        }
                    }
                case Request.Method.GET:
                    {
                        return new Org.Apache.Http.Client.Methods.HttpGet(request.Url);
                    }
                case Request.Method.DELETE:
                    {
                        return new Org.Apache.Http.Client.Methods.HttpDelete(request.Url);
                    }
                case Request.Method.POST:
                    {
                        var postRequest = new Org.Apache.Http.Client.Methods.HttpPost(request.Url);
                        postRequest.AddHeader(HEADER_CONTENT_TYPE, request.GetBodyContentType());
                        SetEntityIfNonEmptyBody(postRequest, request);
                        return postRequest;
                    }
                case Request.Method.PUT:
                    {
                        var putRequest = new Org.Apache.Http.Client.Methods.HttpPut(request.Url);
                        putRequest.AddHeader(HEADER_CONTENT_TYPE, request.GetBodyContentType());
                        SetEntityIfNonEmptyBody(putRequest, request);
                        return putRequest;
                    }
                case Request.Method.HEAD:
                    {
                        return new Org.Apache.Http.Client.Methods.HttpHead(request.Url);
                    }
                case Request.Method.OPTIONS:
                    {
                        return new Org.Apache.Http.Client.Methods.HttpOptions(request.Url);
                    }
                case Request.Method.TRACE:
                    {
                        return new Org.Apache.Http.Client.Methods.HttpTrace(request.Url);
                    }
                case Request.Method.PATCH:
                    {
                        var patchRequest = new HttpPatch(request.Url);
                        patchRequest.AddHeader(HEADER_CONTENT_TYPE, request.GetPostBodyContentType());
                        SetEntityIfNonEmptyBody(patchRequest, request);
                        return patchRequest;
                    }
                default:
                    {
                        throw new Java.Lang.IllegalStateException("Unknown request method.");
                    }
            }
        }

        public static void SetEntityIfNonEmptyBody(Org.Apache.Http.Client.Methods.HttpEntityEnclosingRequestBase httpRequest, Request request)
        {
            byte[] body = request.GetBody();
            if (body != null)
            {
                httpRequest.Entity = new Org.Apache.Http.Entity.ByteArrayEntity(body); 
            }
        }

        protected void OnPrepareRequest(Org.Apache.Http.Client.Methods.IHttpUriRequest request)
        {

        }
    }
}