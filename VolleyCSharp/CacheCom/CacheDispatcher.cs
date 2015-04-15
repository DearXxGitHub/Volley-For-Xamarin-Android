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
using VolleyCSharp.Delivery;
using System.Collections.Concurrent;
using VolleyCSharp.Utility;
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

namespace VolleyCSharp.CacheCom
{
    /// <summary>
    /// 缓存处理程序
    /// 用来处理存在于缓存中的请求，如果该请求不存在则转发到网络请求队列
    /// </summary>
    public class CacheDispatcher : Java.Lang.Thread
    {
        private static bool DEBUG = VolleyLog.DEBUG;

        /*
         * 原java版采用闭包引用RequestQueue中的以下变量，
         * 而C#版本采用构造函数注入的形式，并且将原本的
         * 普通队列改成支持多线程的队列
         */
        private ConcurrentQueue<Request> mCacheQueue;
        private ConcurrentQueue<Request> mNetworkQueue;

        private ICache mCache;
        private IResponseDelivery mDelivery;
        private volatile bool mQuit = false;

        public CacheDispatcher(ConcurrentQueue<Request> cacheQueue, ConcurrentQueue<Request> networkQueue, ICache cache, IResponseDelivery delivery)
        {
            this.mCacheQueue = cacheQueue;
            this.mNetworkQueue = networkQueue;
            this.mCache = cache;
            this.mDelivery = delivery;
        }

        public void Quit()
        {
            mQuit = true;
            Interrupt();
        }

        public override void Run()
        {
            if (DEBUG)
            {
                VolleyLog.V("start new dispatcher");
            }
            Process.SetThreadPriority(ThreadPriority.Background);
            mCache.Initialize();

            while (true)
            {
                try
                {
                    Request request = null;
                    if (!mCacheQueue.TryDequeue(out request))
                    {
                        if (mQuit)
                        {
                            return;
                        }
                        continue;
                    }
                    request.AddMarker("cache-queue-take");

                    //请求是否已取消
                    if (request.IsCanceled)
                    {
                        request.Finish("cache-discard-canceled");
                        continue;
                    }

                    //不存在该缓存
                    Entry entry = mCache.Get(request.GetCacheKey());
                    if (entry == null)
                    {
                        request.AddMarker("cache-miss");
                        mNetworkQueue.Enqueue(request);
                        continue;
                    }

                    //缓存过期
                    if (entry.IsExpired)
                    {
                        request.AddMarker("cache-hit-expired");
                        request.CacheEntry = entry;
                        mNetworkQueue.Enqueue(request);
                        continue;
                    }

                    //缓存命中
                    request.AddMarker("cache-hit");
                    Response response = request.ParseNetworkResponse(new NetworkResponse(entry.Data, entry.ResponseHeaders));
                    request.AddMarker("cache-hit-parsed");

                    //判断缓存是否需要更新
                    if (!entry.RefreshNeeded())
                    {
                        mDelivery.PostResponse(request, response);
                    }
                    else
                    {
                        request.AddMarker("cache-hit-refresh-needed");
                        request.CacheEntry = entry;
                        response.Intermediate = true;
                        mDelivery.PostResponse(request, response, () =>
                        {
                            mNetworkQueue.Enqueue(request);
                        });
                    }
                }
                catch (Exception)
                {
                    if (mQuit)
                    {
                        return;
                    }
                    continue;
                }
            }
        }
    }
}