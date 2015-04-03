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
    public class CacheHeader
    {
        public long Size { get; set; }
        public String Key { get; set; }
        public String ETag { get; set; }
        public long ServerDate { get; set; }
        public long LastModified { get; set; }
        public long Ttl { get; set; }
        public long SoftTtl { get; set; }
        public Dictionary<String, String> ResponseHeaders;

        private CacheHeader() { }

        public CacheHeader(String key, Entry entry)
        {
            this.Key = key;
            this.Size = entry.Data.Length;
            this.ETag = entry.ETag;
            this.ServerDate = entry.ServerDate;
            this.LastModified = entry.LastModified;
            this.Ttl = entry.Ttl;
            this.SoftTtl = entry.SoftTtl;
            this.ResponseHeaders = entry.ResponseHeaders;
        }

        public static CacheHeader ReadHeader(Java.IO.InputStream input)
        {
            CacheHeader entry = new CacheHeader();
            int magic = ReadInt(input);
            if (magic != CACHE_MAGIC)
            {
                throw new Java.IO.IOException();
            }
            entry.Key = ReadString(input);
            entry.ETag = ReadString(input);
            if (entry.ETag == "")
            {
                entry.ETag = null;
            }
            entry.ServerDate = ReadLong(input);
            entry.LastModified = ReadLong(input);
            entry.Ttl = ReadLong(input);
            entry.SoftTtl = ReadLong(input);
            entry.ResponseHeaders = ReadStringStringMap(input);
            return entry;
        }

        public Entry ToCacheEntry(byte[] data)
        {
            Entry e = new Entry();
            e.Data = data;
            e.ETag = ETag;
            e.ServerDate = ServerDate;
            e.LastModified = LastModified;
            e.Ttl = Ttl;
            e.SoftTtl = SoftTtl;
            e.ResponseHeaders = ResponseHeaders;
            return e;
        }

        public bool WriteHeader(Java.IO.OutputStream output)
        {
            try
            {
                WriteInt(output, CACHE_MAGIC);
                WriteString(output, Key);
                WriteString(output, ETag == null ? "" : ETag);
                WriteLong(output, ServerDate);
                WriteLong(output, LastModified);
                WriteLong(output, Ttl);
                WriteLong(output, SoftTtl);
                WriteStringStringMap(ResponseHeaders, output);
                output.Flush();
                return true;
            }
            catch (Java.IO.IOException e)
            {
                VolleyLog.D("{0}", e.ToString());
                return false;
            }
        }
    }
}