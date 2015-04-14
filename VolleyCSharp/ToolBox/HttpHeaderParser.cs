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
    public class HttpHeaderParser
    {
        public static Entry ParseCacheHeaders(NetworkResponse response)
        {
            long now = SystemClock.CurrentThreadTimeMillis();

            Dictionary<String, String> headers = response.Headers;

            long serverDate = 0;
            long lastModified = 0;
            long serverExpires = 0;
            long softExpire = 0;
            long finalExpire = 0;
            long maxAge = 0;
            long staleWhileRevalidate = 0;
            bool hasCacheControl = false;
            bool mustRevalidate = false;

            String serverEtag = null;
            String headerValue;

            headers.TryGetValue("Date", out headerValue);
            if (headerValue != null)
            {
                serverDate = ParseDateAsEpoch(headerValue);
            }

            headers.TryGetValue("Cache-Control", out headerValue);
            if(headerValue != null)
            {
                hasCacheControl = true;
                String[] tokens = headerValue.Split(',');
                for (int i = 0; i < tokens.Length; i++)
                {
                    String token = tokens[i].Trim();
                    if (token == "no-cache" || token == "no-store")
                    {
                        return null;
                    }
                    else if (token.StartsWith("max-age="))
                    {
                        try
                        {
                            maxAge = long.Parse(token.Substring(8));
                        }
                        catch (Exception) { }
                    }
                    else if (token.StartsWith("stale-while-revalidate="))
                    {
                        try
                        {
                            staleWhileRevalidate = long.Parse(token.Substring(23));
                        }
                        catch (Exception) { }
                    }
                    else if (token == "must-revalidate" || token == "proxy-revalidate")
                    {
                        mustRevalidate = true;
                    }
                }
            }

            headers.TryGetValue("Expires", out headerValue);
            if (headerValue != null)
            {
                serverExpires = ParseDateAsEpoch(headerValue);
            }

            headers.TryGetValue("Last-Modified", out headerValue);
            if (headerValue != null)
            {
                lastModified = ParseDateAsEpoch(headerValue);
            }

            headers.TryGetValue("ETag", out serverEtag);

            if (hasCacheControl)
            {
                softExpire = now + maxAge * 1000;
                finalExpire = mustRevalidate ? softExpire : softExpire + staleWhileRevalidate * 1000;
            }
            else if (serverDate > 0 && serverExpires >= serverDate)
            {
                softExpire = now + (serverExpires - serverDate);
                finalExpire = softExpire;
            }

            Entry entry = new Entry();
            entry.Data = response.Data;
            entry.ETag = serverEtag;
            entry.SoftTtl = softExpire;
            entry.Ttl = finalExpire;
            entry.ServerDate = serverDate;
            entry.LastModified = lastModified;
            entry.ResponseHeaders = headers;

            return entry;
        }

        public static long ParseDateAsEpoch(String dateStr)
        {
            try
            {
                return DateTime.Parse(dateStr).Ticks;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static String ParseCharset(Dictionary<String, String> headers, String defaultCharset)
        {
            String contentType = null;
            headers.TryGetValue(Org.Apache.Http.Protocol.HTTP.ContentType, out contentType);
            if(contentType != null)
            {
                String[] @params = contentType.Split(';');
                for (int i = 1; i < @params.Length; i++)
                {
                    String[] pair = @params[i].Trim().Split('=');
                    if (pair.Length == 2)
                    {
                        if (pair[0] == "charset")
                        {
                            return pair[1];
                        }
                    }
                }
            }
            return defaultCharset;
        }

        public static String ParseCharset(Dictionary<String, String> headers)
        {
            return ParseCharset(headers, Org.Apache.Http.Protocol.HTTP.DefaultContentType);
        }
    }
}