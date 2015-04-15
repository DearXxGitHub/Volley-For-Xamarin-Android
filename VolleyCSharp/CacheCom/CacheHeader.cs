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
using VolleyCSharp.Utility;

/*
 * 15.4.13 改写
 */

namespace VolleyCSharp.CacheCom
{
    /// <summary>
    /// 表示缓存信息
    /// 用来将缓存信息写入到流中
    /// 或从流中读取
    /// </summary>
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

        /// <summary>
        /// 从流中读取缓存相关信息
        /// （当前只提供文件缓存所以是从文件中读取）
        /// </summary>
        public static CacheHeader ReadHeader(Stream input)
        {
            CacheHeader entry = new CacheHeader();
            int magic = DiskBasedCache.ReadInt(input);
            if (magic != DiskBasedCache.CACHE_MAGIC)
            {
                throw new IOException();
            }
            entry.Key = DiskBasedCache.ReadString(input);
            entry.ETag = DiskBasedCache.ReadString(input);
            if (String.IsNullOrEmpty(entry.ETag))
            {
                entry.ETag = null;
            }
            entry.ServerDate = DiskBasedCache.ReadLong(input);
            entry.LastModified = DiskBasedCache.ReadLong(input);
            entry.Ttl = DiskBasedCache.ReadLong(input);
            entry.SoftTtl = DiskBasedCache.ReadLong(input);
            entry.ResponseHeaders = DiskBasedCache.ReadStringStringMap(input);
            return entry;
        }

        /// <summary>
        /// 将原始数据转换成实体对象
        /// </summary>
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

        /// <summary>
        /// 将缓存信息写入流中
        /// </summary>
        public bool WriteHeader(Stream output)
        {
            try
            {
                DiskBasedCache.WriteInt(output, DiskBasedCache.CACHE_MAGIC);
                DiskBasedCache.WriteString(output, Key);
                DiskBasedCache.WriteString(output, ETag == null ? "" : ETag);
                DiskBasedCache.WriteLong(output, ServerDate);
                DiskBasedCache.WriteLong(output, LastModified);
                DiskBasedCache.WriteLong(output, Ttl);
                DiskBasedCache.WriteLong(output, SoftTtl);
                DiskBasedCache.WriteStringStringMap(ResponseHeaders, output);
                output.Flush();
                return true;
            }
            catch (Exception e)
            {
                VolleyLog.D("{0}", e.ToString());
                return false;
            }
        }
    }
}