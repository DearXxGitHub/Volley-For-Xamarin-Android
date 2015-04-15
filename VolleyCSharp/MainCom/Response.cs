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
    /// <summary>
    /// 用来封装请求完成的对象
    /// </summary>
    public class Response
    {
        public static Response Success(String result, Entry cacheEntry)
        {
            return new Response(result, cacheEntry);
        }

        public static Response Error(VolleyError error)
        {
            return new Response(error);
        }

        public String Result { get; private set; }
        public Entry CacheEntry { get; private set; }
        public VolleyError MError { get; private set; }
        public bool Intermediate { get; set; }

        public bool IsSuccess
        {
            get
            {
                return this.MError == null;
            }
        }

        private Response(String result, Entry cacheEntry)
        {
            this.Result = result;
            this.CacheEntry = cacheEntry;
            this.MError = null;
        }

        private Response(VolleyError error)
        {
            this.Result = null;
            this.CacheEntry = null;
            this.MError = error;
        }
    }
}