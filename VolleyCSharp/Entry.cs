using System;
using System.Collections.Generic;
using System.Text;

/*
 * 已核实
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
