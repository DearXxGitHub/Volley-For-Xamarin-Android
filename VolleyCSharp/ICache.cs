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
 * “—∫À µ
 */

namespace VolleyCSharp
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