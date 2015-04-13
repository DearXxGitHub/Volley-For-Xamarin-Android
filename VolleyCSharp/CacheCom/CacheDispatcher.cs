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

/*
 * 15.4.13 改写
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
        private Queue<Request> mCacheQueue;
        private Queue<Request> mNetworkQueue;
        private ICache mCache;
        private IResponseDelivery mDelivery;
        private volatile bool mQuit = false;

        public CacheDispatcher(Queue<Request> cacheQueue, Queue<Request> networkQueue, ICache cache, IResponseDelivery delivery)
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
                    Request request = mCacheQueue.Dequeue();
                    request.AddMarker("cache-queue-take");

                    if (request.IsCanceled)
                    {
                        request.Finish("cache-discard-canceled");
                        continue;
                    }

                    Entry entry = mCache.Get(request.GetCacheKey());
                    if (entry == null)
                    {
                        request.AddMarker("cache-miss");
                        mNetworkQueue.Enqueue(request);
                        continue;
                    }

                    if (entry.IsExpired)
                    {
                        request.AddMarker("cache-hit-expired");
                        request.CacheEntry = entry;
                        mNetworkQueue.Enqueue(request);
                        continue;
                    }

                    request.AddMarker("cache-hit");
                    Response response = request.ParseNetworkResponse(new NetworkResponse(entry.Data, entry.ResponseHeaders));
                    request.AddMarker("cache-hit-parsed");

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
                            try
                            {
                                mNetworkQueue.Enqueue(request);
                            }
                            catch (Java.Lang.InterruptedException) { }
                        });
                    }
                }
                catch (Java.Lang.InterruptedException)
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