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

namespace VolleyCSharp.CacheCom
{
    /// <summary>
    /// 如果需要自定义缓存必须实现该类
    /// 并且在CacheDispatcher的构造函数中传入
    /// </summary>
    public interface ICache
    {
        Entry Get(String key);
        void Put(String key, Entry entry);
        void Initialize();
        void Invalidate(String key, bool fullExpire);
        void Remove(String key);
        void Clear();
    }
}