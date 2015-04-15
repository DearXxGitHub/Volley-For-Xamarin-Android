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
using VolleyCSharp.NetCom;
using VolleyCSharp.CacheCom;
using VolleyCSharp.MainCom;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.ToolBox
{
    public class Volley
    {
        private static String DEFAULT_CACHE_DIR = "volley";

        public static RequestQueue NewRequestQueue(Context context, IHttpStack stack, int maxDiskCacheBytes)
        {
            //组织缓存根目录
            var cacheDir = Directory.CreateDirectory(context.CacheDir.Path + "/" + DEFAULT_CACHE_DIR);
            String userAgent = "volley/0";
            try
            {
                String packageName = context.PackageName;
                var info = context.PackageManager.GetPackageInfo(packageName, 0);
                userAgent = packageName + "/" + info.VersionCode;
            }
            catch (Android.Content.PM.PackageManager.NameNotFoundException) { }

            if (stack == null)
            {
                stack = new HttpClientStack();
            }

            INetwork network = new BasicNetwork(stack);

            RequestQueue queue;
            if (maxDiskCacheBytes <= -1)
            {
                queue = new RequestQueue(new DiskBasedCache(cacheDir), network);
            }
            else
            {
                queue = new RequestQueue(new DiskBasedCache(cacheDir, maxDiskCacheBytes), network);
            }
            return queue;
        }

        public static RequestQueue NewRequestQueue(Context context, int maxDiskCacheBytes)
        {
            return NewRequestQueue(context, null, maxDiskCacheBytes);
        }

        public static RequestQueue NewRequestQueue(Context context, IHttpStack stack)
        {
            return NewRequestQueue(context, stack, -1);
        }

        public static RequestQueue NewRequestQueue(Context context)
        {
            return NewRequestQueue(context, null);
        }
    }
}