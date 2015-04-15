using System;
using System.Collections.Generic;
using System.Text;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp
{
    public class Entry
    {
        public byte[] Data { get; set; }
        public String ETag { get; set; }
        public long ServerDate { get; set; }
        public long LastModified { get; set; }
        public long Ttl { get; set; }
        public long SoftTtl { get; set; }
        public Dictionary<String, String> ResponseHeaders = new Dictionary<string, string>();

        public bool IsExpired
        {
            get { return this.Ttl < Java.Lang.JavaSystem.CurrentTimeMillis(); }
        }

        public bool RefreshNeeded()
        {
            return this.SoftTtl < Java.Lang.JavaSystem.CurrentTimeMillis();
        }
    }
}
