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
using Org.Apache.Http;

/*
 * “—∫À µ
 */

namespace VolleyCSharp
{
    public class NetworkResponse
    {
        public int StatusCode { get; set; }
        public byte[] Data { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool NotModified { get; set; }
        public long NetworkTimeMs { get; set; }

        public NetworkResponse(int statusCode, byte[] data, Dictionary<String, String> headers, bool notModified, long networkTimeMs)
        {
            this.StatusCode = statusCode;
            this.Data = data;
            this.Headers = headers;
            this.NotModified = notModified;
            this.NetworkTimeMs = networkTimeMs;
        }

        public NetworkResponse(int statusCode, byte[] data, Dictionary<string, string> headers, bool notModified)
            : this(statusCode, data, headers, notModified, 0) { }

        public NetworkResponse(byte[] data)
            : this(HttpStatus.ScOk, data, new Dictionary<string, string>(), false, 0) { }

        public NetworkResponse(byte[] data, Dictionary<string, string> headers)
            : this(HttpStatus.ScOk, data, headers, false, 0) { }
    }
}