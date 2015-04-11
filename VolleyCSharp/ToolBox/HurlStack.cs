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

        #region IHttpStack

        public Org.Apache.Http.IHttpResponse PerformRequest(Request request, Dictionary<string, string> additionalHeaders)
        {
            String url = request.Url;
            Dictionary<String, String> map = request.GetHeaders();
            if (map == null)
            {
                map = new Dictionary<string, string>();
            }
            map = map.Intersect(additionalHeaders).ToDictionary(x => x.Key, x => x.Value);

            if (mUrlRewriter != null)
            {
                String rewritten = mUrlRewriter.RewriteUrl(url);
                if (rewritten == null)
                {
                    throw new IOException("URL blocked by rewriter:" + url);
                }
                url = rewritten;
            }

            Java.Net.URL parsedUrl = new Java.Net.URL(url);
            Java.Net.HttpURLConnection connection = OpenConnection(parsedUrl, request);
            foreach (KeyValuePair<String,String> val in map)
            {
                connection.AddRequestProperty(val.Key, val.Value);
            }
            SetConnectionParametersForRequest(connection, request);

            Org.Apache.Http.ProtocolVersion protocolVersion = new Org.Apache.Http.ProtocolVersion("HTTP", 1, 1);
            int responseCode = (int)connection.ResponseCode;
            if (responseCode == -1)
            {
                throw new IOException("Could not retrieve response code from HttpUrlConnection.");
            }
            Org.Apache.Http.IStatusLine responseStatus = new Org.Apache.Http.Message.BasicStatusLine(protocolVersion, responseCode,
                connection.ResponseMessage);
            var response = new Org.Apache.Http.Message.BasicHttpResponse(responseStatus);
            response.Entity = EntityFromConnection(connection);
            foreach (KeyValuePair<String,IList<String>> header in connection.HeaderFields)
            {
                if (header.Key != null)
                {
                    Org.Apache.Http.IHeader h = new Org.Apache.Http.Message.BasicHeader(header.Key, header.Value[0]);
                    response.AddHeader(h);
                }
            }
            return response;
        }

        #endregion

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