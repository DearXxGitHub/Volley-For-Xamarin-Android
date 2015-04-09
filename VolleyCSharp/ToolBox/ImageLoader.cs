using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace VolleyCSharp.ToolBox
{
    public class ImageLoader
    {
        private RequestQueue mRequestQueue;
        private int mBatchResponseDelayMs = 100;
        private IImageCache mCache;
        private static Dictionary<String, BatchedImageRequest> mInFlightRequests = new Dictionary<string, BatchedImageRequest>();
        private static Dictionary<String, BatchedImageRequest> mBatchedResponses = new Dictionary<string, BatchedImageRequest>();
        private Handler mHandler = new Handler(Looper.MainLooper);
        private Action mRunnable;

        public ImageLoader(RequestQueue queue, IImageCache imageCache)
        {
            this.mRequestQueue = queue;
            this.mCache = imageCache;
        }

        public static IImageListener GetImageListener(ImageView view, int defaultImageResId, int errorImageResId)
        {
            return new DefaultImageListener(view, defaultImageResId, errorImageResId);
        }

        public bool IsCached(String requestUrl, int maxWidth, int maxHeight)
        {
            return IsCached(requestUrl, maxWidth, maxHeight, Android.Widget.ImageView.ScaleType.CenterInside);
        }

        public bool IsCached(String requestUrl, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType)
        {
            ThrowIfNotOnMainThread();
            String cacheKey = GetCacheKey(requestUrl, maxWidth, maxHeight, scaleType);
            return mCache.GetBitmap(cacheKey) != null;
        }

        public ImageContainer Get(String requestUrl, IImageListener listener)
        {
            return Get(requestUrl, listener, 0, 0);
        }

        public ImageContainer Get(String requestUrl, IImageListener listener, int maxWidth, int maxHeight)
        {
            return Get(requestUrl, listener, maxWidth, maxHeight, Android.Widget.ImageView.ScaleType.CenterInside);
        }

        public ImageContainer Get(String requestUrl, IImageListener listener, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType)
        {
            ThrowIfNotOnMainThread();

            String cacheKey = GetCacheKey(requestUrl, maxWidth, maxHeight, scaleType);

            Bitmap cachedBitmap = mCache.GetBitmap(cacheKey);
            if (cachedBitmap != null)
            {
                ImageContainer container = new ImageContainer(cachedBitmap, requestUrl, null, null);
                listener.OnResponse(container, true);
                return container;
            }

            ImageContainer imageContainer = new ImageContainer(null, requestUrl, cacheKey, listener);
            listener.OnResponse(imageContainer, true);

            BatchedImageRequest request = null;
            mInFlightRequests.TryGetValue(cacheKey, out request);
            if (request != null)
            {
                request.AddContainer(imageContainer);
                return imageContainer;
            }

            Request newRequest = MakeImageRequest(requestUrl, maxWidth, maxHeight, scaleType, cacheKey);

            mRequestQueue.Add(newRequest);
            mInFlightRequests.Add(cacheKey, new BatchedImageRequest(newRequest, imageContainer));
            return imageContainer;
        }

        protected Request MakeImageRequest(String requestUrl, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType, String cacheKey)
        {
            var listener = new DefaultImageResponseListener()
            {
                CacheKey = cacheKey,
                OnGetImageSuccess = OnGetImageSuccess
            };
            var errorListener = new DefaultErrorResponseListener()
            {
                CacheKey = cacheKey,
                OnErrorResponse = OnGetImageError
            };
            return new ImageRequest(requestUrl, listener, maxWidth, maxHeight, scaleType, Android.Graphics.Bitmap.Config.Rgb565, errorListener);
        }

        public void SetBatchedResponseDelay(int newBatchedResponseDelayMs)
        {
            mBatchResponseDelayMs = newBatchedResponseDelayMs;
        }

        protected void OnGetImageSuccess(String cacheKey, Bitmap response)
        {
            mCache.PutBitmap(cacheKey, response);

            BatchedImageRequest request = null;
            mInFlightRequests.TryGetValue(cacheKey, out request);
            mInFlightRequests.Remove(cacheKey);

            if (request != null)
            {
                request.mResponseBitmap = response;
                BatchResponse(cacheKey, request);
            }
        }

        protected void OnGetImageError(String cacheKey, VolleyError error)
        {
            BatchedImageRequest request = null;
            mInFlightRequests.TryGetValue(cacheKey, out request);
            mInFlightRequests.Remove(cacheKey);

            if (request != null)
            {
                request.Error = error;
                BatchResponse(cacheKey, request);
            }
        }

        private void BatchResponse(String cacheKey, BatchedImageRequest request)
        {
            mBatchedResponses.Add(cacheKey, request);
            if (mRunnable == null)
            {
                mRunnable = () =>
                    {
                        foreach (BatchedImageRequest bir in mBatchedResponses.Values)
                        {
                            foreach (ImageContainer container in bir.mContainers)
                            {
                                if (container.mListener == null)
                                {
                                    continue;
                                }
                                if (bir.Error == null)
                                {
                                    container.mBitmap = bir.mResponseBitmap;
                                    container.mListener.OnResponse(container, false);
                                }
                                else
                                {
                                    container.mListener.OnErrorResponse(bir.Error);
                                }
                            }
                        }
                        mBatchedResponses.Clear();
                        mRunnable = null;
                    };
            }
            mHandler.PostDelayed(mRunnable, mBatchResponseDelayMs);
        }

        private void ThrowIfNotOnMainThread()
        {
            if (Looper.MyLooper() != Looper.MainLooper)
            {
                throw new Java.Lang.IllegalStateException("ImageLoader must be invoked from the main thread.");
            }
        }

        private static String GetCacheKey(String url, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType)
        {
            return new StringBuilder(url.Length + 12).Append("#W").Append(maxWidth)
                .Append("#H").Append(maxHeight).Append("#S").Append(scaleType.Ordinal()).Append(url).ToString();
        }

        internal class DefaultImageResponseListener : IListener
        {
            public Action<String, Bitmap> OnGetImageSuccess;
            public String CacheKey{get;set;}

            public void OnResponse(object response)
            {
                if (OnGetImageSuccess != null)
                {
                    OnGetImageSuccess(CacheKey, response as Bitmap);
                }
            }
        }

        internal class DefaultErrorResponseListener : IErrorListener
        {
            public Action<String, VolleyError> OnErrorResponse;
            public String CacheKey { get; set; }

            public void IErrorListener.OnErrorResponse(VolleyError error)
            {
                if (OnErrorResponse != null)
                {
                    OnErrorResponse(CacheKey, error);
                }
            }
        }

        internal class DefaultImageListener : IImageListener
        {
            private ImageView mView;
            private int mDefaultImageResId;
            private int mErrorImageResId;

            public DefaultImageListener(ImageView view, int defaultImageResId, int errorImageResId)
            {
                this.mView = view;
                this.mDefaultImageResId = defaultImageResId;
                this.mErrorImageResId = errorImageResId;
            }

            public void OnResponse(ImageContainer response, bool isImmediate)
            {
                if (response.GetBitmap() != null)
                {
                    mView.SetImageBitmap(response.GetBitmap());
                }
                else if (mDefaultImageResId != 0)
                {
                    mView.SetImageResource(mDefaultImageResId);
                }
            }

            public void OnErrorResponse(VolleyError error)
            {
                if (mErrorImageResId != 0)
                {
                    mView.SetImageResource(mErrorImageResId);
                }
            }
        }

        internal class ImageContainer
        {
            private String mCacheKey;
            private String mRequestUrl;
            public IImageListener mListener;
            public Bitmap mBitmap;

            public ImageContainer(Bitmap bitmap, String requestUrl, String cacheKey, IImageListener listener)
            {
                this.mBitmap = bitmap;
                this.mRequestUrl = requestUrl;
                this.mCacheKey = cacheKey;
                this.mListener = listener;
            }

            public void CancelRequest()
            {
                if (mListener == null)
                {
                    return;
                }

                BatchedImageRequest request = null;
                mInFlightRequests.TryGetValue(mCacheKey, out request);
                if (request != null)
                {
                    bool canceled = request.RemoveContainerAndCancelIfNecessary(this);
                    if (canceled)
                    {
                        mInFlightRequests.Remove(mCacheKey);
                    }
                }
                else
                {
                    mBatchedResponses.TryGetValue(mCacheKey, out request);
                    if (request != null)
                    {
                        request.RemoveContainerAndCancelIfNecessary(this);
                        if (request.mContainers.Count == 0)
                        {
                            mBatchedResponses.Remove(mCacheKey);
                        }
                    }
                }
            }

            public Bitmap GetBitmap()
            {
                return mBitmap;
            }

            public String GetRequestUrl()
            {
                return mRequestUrl;
            }
        }

        internal class BatchedImageRequest
        {
            private Request mRequest;
            
            private VolleyError mError;
            public List<ImageContainer> mContainers = new List<ImageContainer>();
            public Bitmap mResponseBitmap;

            public BatchedImageRequest(Request request, ImageContainer container)
            {
                mRequest = request;
                mContainers.Add(container);
            }

            public VolleyError Error
            {
                get { return this.mError; }
                set { this.mError = value; }
            }

            public void AddContainer(ImageContainer container)
            {
                mContainers.Add(container);
            }

            public bool RemoveContainerAndCancelIfNecessary(ImageContainer container)
            {
                mContainers.Remove(container);
                if (mContainers.Count == 0)
                {
                    mRequest.Cancel();
                    return true;
                }
                return false;
            }
        }
    }
}