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
 * 14.4.15 ¸ÄÐ´
 */

namespace VolleyCSharp.CacheCom
{
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