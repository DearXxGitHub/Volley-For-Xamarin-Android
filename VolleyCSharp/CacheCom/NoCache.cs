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
using VolleyCSharp.CacheCom;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.CacheCom
{
    /// <summary>
    /// 如果不需要缓存则使用该类
    /// </summary>
    public class NoCache : ICache
    {
        public Entry Get(string key)
        {
            return null;
        }

        public void Put(string key, Entry entry) { }

        public void Initialize() { }

        public void Invalidate(string key, bool fullExpire) { }

        public void Remove(string key) { }

        public void Clear() { }
    }
}