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
    public class HurlStack : IHttpStack
    {
        private static String HEADER_CONTENT_TYPE = "Content-Type";

        private IUrlRewriter mUrlRewriter;
        private Javax.Net.Ssl.SSLSocketFactory mSslSocketFactory;

        public HurlStack()
            : this(null) { }

        public HurlStack(IUrlRewriter urlRewriter)
            : this(urlRewriter, null) { }

        public HurlStack(IUrlRewriter urlRewriter, Javax.Net.Ssl.SSLSocketFactory sslSocketFactory)
        {
            mUrlRewriter = urlRewriter;
            mSslSocketFactory = sslSocketFactory;
        }

        public Org.Apache.Http.IHttpResponse PerformRequest(Request request, Dictionary<string, string> additionalHeaders)
        {
            String url = request.Url;
            Dictionary<String, String> map = request.GetHeaders();
            if (map == null)
            {
                map = new Dictionary<string, string>();
            }
            
        }

        public static Org.Apache.Http.IHttpEntity EntityFromConnection(Java.Net.HttpURLConnection connection)
        {
            var entity = new Org.Apache.Http.Entity.BasicHttpEntity();
            Stream inputStream = null;
            try
            {
                inputStream = connection.InputStream;
            }
            catch (Exception)
            {
                inputStream = connection.ErrorStream;
            }
            inputStream.CopyTo(entity.Content);
            entity.SetContentEncoding(connection.ContentEncoding);
            entity.SetContentType(connection.ContentType);
            return entity;
        }

        protected Java.Net.HttpURLConnection CreateConnection(Java.Net.URL url)
        {
            return (Java.Net.HttpURLConnection)url.OpenConnection();
        }

        private Java.Net.HttpURLConnection OpenConnection(Java.Net.URL url, Request request)
        {
            var connection = CreateConnection(url);

            int timeoutMs = request.GetTimeoutMs();
            connection.ConnectTimeout = timeoutMs;
            connection.ReadTimeout = timeoutMs;
            connection.UseCaches = false;
            connection.DoInput = true;

            if ("https" == url.Protocol && mSslSocketFactory != null)
            {
                ((Javax.Net.Ssl.HttpsURLConnection)connection).SSLSocketFactory = mSslSocketFactory;
            }

            return connection;
        }

        public static void SetConnectionParametersForRequest(Java.Net.HttpURLConnection connection, Request request)
        {
            switch (request.Methods)
            {
                case Request.Method.DEPRECATED_GET_OR_POST:
                    {
                        byte[] postBody = request.GetPostBody();
                        if (postBody != null)
                        {
                            connection.DoOutput = true;
                            connection.RequestMethod = "POST";
                            connection.AddRequestProperty(HEADER_CONTENT_TYPE, request.GetPostBodyContentType());
                            StreamWriter sw = new StreamWriter(connection.OutputStream);
                            sw.Write(postBody);
                            sw.Close();
                        }
                    }
                    break;
                case Request.Method.GET:
                    {
                        connection.RequestMethod = "GET";
                    }
                    break;
                case Request.Method.DELETE:
                    {
                        connection.RequestMethod = "DELETE";
                    }
                    break;
                case Request.Method.POST:
                    {
                        connection.RequestMethod = "POST";
                        AddBodyIfExists(connection, request);
                    }
                    break;
                case Request.Method.PUT:
                    {
                        connection.RequestMethod = "PUT";
                        AddBodyIfExists(connection, request);
                    }
                    break;
                case Request.Method.HEAD:
                    {
                        connection.RequestMethod = "HEAD";
                    }
                    break;
                case Request.Method.OPTIONS:
                    {
                        connection.RequestMethod = "OPTIONS";
                    }
                    break;
                case Request.Method.TRACE:
                    {
                        connection.RequestMethod = "TRACE";
                    }
                    break;
                case Request.Method.PATCH:
                    {
                        connection.RequestMethod = "PATCH";
                        AddBodyIfExists(connection, request);
                    }
                    break;
                default:
                    {
                        throw new Java.Lang.IllegalStateException("Unknown method type.");
                    }
            }
        }

        private static void AddBodyIfExists(Java.Net.HttpURLConnection connection, Request request)
        {
            byte[] body = request.GetBody();
            if (body != null)
            {
                connection.DoOutput = true;
                connection.AddRequestProperty(HEADER_CONTENT_TYPE, request.GetBodyContentType());
                StreamWriter sw = new StreamWriter(connection.OutputStream);
                sw.Write(body);
                sw.Close();
            }
        }
    }
}