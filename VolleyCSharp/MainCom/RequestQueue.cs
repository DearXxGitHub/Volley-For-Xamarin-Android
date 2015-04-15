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
using Java.Util.Concurrent.Atomic;
using VolleyCSharp.Delivery;
using VolleyCSharp.CacheCom;
using VolleyCSharp.NetCom;
using System.Collections.Concurrent;
using VolleyCSharp.Utility;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.MainCom
{
    /// <summary>
    /// 请求队列，用来启动缓存处理程序和多个网络处理程序用来处理请求
    /// </summary>
    public class RequestQueue
    {
        private AtomicInteger mSequenceGenerator = new AtomicInteger();

        private HashSet<Request> mCurrentRequests = new HashSet<Request>();

        private Dictionary<String, Queue<Request>> mWaitingRequests = new Dictionary<string, Queue<Request>>();
        private ConcurrentQueue<Request> mCacheQueue = new ConcurrentQueue<Request>();
        private ConcurrentQueue<Request> mNetworkQueue = new ConcurrentQueue<Request>();

        private const int DEFAULT_NETWORK_THREAD_POOL_SIZE = 1;
        private ICache mCache;
        private INetwork mNetwork;

        private IResponseDelivery mDelivery;
        private NetworkDispatcher[] mDispatchers;

        private CacheDispatcher mCacheDispatcher;

        private List<IRequestFinishedListener> mFinishedListeners = new List<IRequestFinishedListener>();

        public RequestQueue(ICache cache, INetwork network, int threadPoolSize, IResponseDelivery delivery)
        {
            this.mCache = cache;
            this.mNetwork = network;
            this.mDispatchers = new NetworkDispatcher[threadPoolSize];
            this.mDelivery = delivery;
        }

        public RequestQueue(ICache cache, INetwork network, int threadPoolSize)
            : this(cache, network, threadPoolSize, new ExecutorDelivery(new Handler(Looper.MainLooper))) { }

        public RequestQueue(ICache cache, INetwork network)
            : this(cache, network, DEFAULT_NETWORK_THREAD_POOL_SIZE) { }

        /// <summary>
        /// 开始处理请求池
        /// </summary>
        public void Start()
        {
            Stop();

            //新建一个缓存处理并开启
            mCacheDispatcher = new CacheDispatcher(mCacheQueue, mNetworkQueue, mCache, mDelivery);
            mCacheDispatcher.Start();

            //创建多个网络出去并开启，具体数量由DEFAULT_NETWORK_THREAD_POOL_SIZE常量决定
            for (int i = 0; i < mDispatchers.Length; i++)
            {
                NetworkDispatcher networkDsipatcher = new NetworkDispatcher(mNetworkQueue, mNetwork, mCache, mDelivery);
                mDispatchers[i] = networkDsipatcher;
                networkDsipatcher.Start();
            }
        }

        public void Stop()
        {
            if (mCacheDispatcher != null)
            {
                mCacheDispatcher.Quit();
            }
            for (int i = 0; i < mDispatchers.Length; i++)
            {
                if (mDispatchers[i] != null)
                {
                    mDispatchers[i].Quit();
                }
            }
        }

        public int GetSequenceNumber()
        {
            return mSequenceGenerator.IncrementAndGet();
        }

        public ICache GetCache()
        {
            return mCache;
        }

        /// <summary>
        /// 有条件式的取消请求
        /// </summary>
        public void CancelAll(Func<Request,bool> filter)
        {
            lock (mCurrentRequests)
            {
                foreach (Request request in mCurrentRequests)
                {
                    if (filter(request))
                    {
                        request.Cancel();
                    }
                }
            }
        }

        /// <summary>
        /// 取消指定Tag的请求
        /// </summary>
        public void CancelAll(object tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException("Cannot cancelAll with a null tag");
            }
            lock (mCurrentRequests)
            {
                foreach (Request request in mCurrentRequests)
                {
                    if (request.Tag == tag)
                    {
                        request.Cancel();
                    }
                }
            }
        }

        public Request Add(Request request)
        {
            request.SetRequestQueue(this);

            //添加到当前请求列表中
            lock (mCurrentRequests)
            {
                mCurrentRequests.Add(request);
            }

            request.Sequence = GetSequenceNumber();
            request.AddMarker("add-to-queue");

            //如果请求不需要缓存则直接加入到网络请求队列中
            if (!request.ShouldCache())
            {
                mNetworkQueue.Enqueue(request);
                return request;
            }

            /*
             * 先将请求加入到缓存请求队列中，并且将后面的重复请求
             * 添加到mWaitingRequests中，目的是当第一个请求完成后
             * 后面重复的请求直接添加到缓存请求队列中，避免重复对
             * 网络进行请求
             */
            lock (mWaitingRequests)
            {
                String cacheKey = request.GetCacheKey();
                if (mWaitingRequests.ContainsKey(cacheKey))
                {
                    Queue<Request> stagedRequests = null;
                    mWaitingRequests.TryGetValue(cacheKey, out stagedRequests);
                    if (stagedRequests == null)
                    {
                        stagedRequests = new Queue<Request>();
                    }
                    stagedRequests.Enqueue(request);
                    if (VolleyLog.DEBUG)
                    {
                        VolleyLog.V("Request for cacheKey={0} is in flight,putting on hold.", cacheKey);
                    }
                }
                else
                {
                    mWaitingRequests.Add(cacheKey, null);
                    mCacheQueue.Enqueue(request);
                }
                return request;
            }
        }

        public void Finish(Request request)
        {
            lock (mCurrentRequests)
            {
                mCurrentRequests.Remove(request);
            }

            lock (mFinishedListeners)
            {
                foreach (IRequestFinishedListener listener in mFinishedListeners)
                {
                    listener.OnRequestFinished(request);
                }
            }

            if (request.ShouldCache())
            {
                /*
                 * 在该请求完成并缓存后将后面其他重复的
                 * 请求直接加入到缓存请求队列中
                 */
                lock (mWaitingRequests)
                {
                    String cacheKey = request.GetCacheKey();
                    Queue<Request> waitingRequets = null;
                    mWaitingRequests.TryGetValue(cacheKey, out waitingRequets);
                    mWaitingRequests.Remove(cacheKey);
                    if (waitingRequets != null)
                    {
                        if (VolleyLog.DEBUG)
                        {
                            VolleyLog.V("Releasing {0} waiting requests for cacheKey={1}", waitingRequets.Count, cacheKey);
                        }
                        foreach (Request addrequest in waitingRequets)
                        {
                            mCacheQueue.Enqueue(addrequest);
                        }
                    }
                }
            }
        }

        public void AddRequestFinishedListener(IRequestFinishedListener listener)
        {
            lock (mFinishedListeners)
            {
                mFinishedListeners.Add(listener);
            }
        }

        public void RemoveRequestFinishedListener(IRequestFinishedListener listener)
        {
            lock (mFinishedListeners)
            {
                mFinishedListeners.Remove(listener);
            }
        }
    }
}