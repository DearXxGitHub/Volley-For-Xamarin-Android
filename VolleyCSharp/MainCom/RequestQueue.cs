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

/*
 * 15.4.13 改写
 */

namespace VolleyCSharp
{
    /// <summary>
    /// 请求队列，用来启动缓存处理程序和多个网络处理程序用来处理请求
    /// </summary>
    public class RequestQueue
    {
        private AtomicInteger mSequenceGenerator = new AtomicInteger();
        private Dictionary<String, Queue<Request>> mWaitingRequests = new Dictionary<string, Queue<Request>>();
        private HashSet<Request> mCurrentRequests = new HashSet<Request>();

        private Queue<Request> mCacheQueue = new Queue<Request>();
        private Queue<Request> mNetworkQueue = new Queue<Request>();

        private static int DEFAULT_NETWORK_THREAD_POOL_SIZE = 1;
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

            mCacheDispatcher = new CacheDispatcher(mCacheQueue, mNetworkQueue, mCache, mDelivery);
            mCacheDispatcher.Start();

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

        public void CancelAll(IRequestFilter filter)
        {
            lock (mCurrentRequests)
            {
                foreach (Request request in mCurrentRequests)
                {
                    if (filter.Apply(request))
                    {
                        request.Cancel();
                    }
                }
            }
        }

        public void CancelAll(object tag)
        {
            if (tag == null)
            {
                throw new Java.Lang.IllegalArgumentException("Cannot cancelAll with a null tag");
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
            lock (mCurrentRequests)
            {
                mCurrentRequests.Add(request);
            }

            request.Sequence = GetSequenceNumber();
            request.AddMarker("add-to-queue");

            if (!request.ShouldCache())
            {
                mNetworkQueue.Enqueue(request);
                return request;
            }

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
                    mWaitingRequests.Add(cacheKey, stagedRequests);
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