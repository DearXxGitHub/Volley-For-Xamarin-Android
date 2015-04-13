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

        /// <summary>
        /// 替代GetImageListener方法的重载Get
        /// </summary>
        public ImageContainer Get(String requestUrl, ImageView view, int defaultImageResId, int errorImageResId)
        {
            return Get(requestUrl, 0, 0, view, defaultImageResId, errorImageResId);
        }

        public ImageContainer Get(String requestUrl, int maxWidth, int maxHeight, ImageView view, int defaultImageResId, int errorImageResId)
        {
            return Get(requestUrl, maxWidth, maxHeight, Android.Widget.ImageView.ScaleType.CenterInside, view, defaultImageResId, errorImageResId);
        }

        public ImageContainer Get(String requestUrl, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType,
            ImageView view, int defaultImageResId, int errorImageResId)
        {
            return Get(requestUrl, maxWidth, maxHeight, scaleType,
                (x, y) =>
                {
                    if (x.Bitmap != null)
                    {
                        view.SetImageBitmap(x.Bitmap);
                    }
                    else if (defaultImageResId != 0)
                    {
                        view.SetImageResource(defaultImageResId);
                    }
                }, (x) =>
                {
                    if (errorImageResId != 0)
                    {
                        view.SetImageResource(errorImageResId);
                    }
                });
        }

        public ImageContainer Get(String requestUrl, Action<ImageContainer, bool> successResponse,
            Action<VolleyError> errorResponse)
        {
            return Get(requestUrl, 0, 0, successResponse, errorResponse);
        }

        public ImageContainer Get(String requestUrl, int maxWidth, int maxHeight, Action<ImageContainer, bool> successResponse,
            Action<VolleyError> errorResponse)
        {
            return Get(requestUrl, maxWidth, maxHeight, Android.Widget.ImageView.ScaleType.CenterInside, successResponse, errorResponse);
        }

        public ImageContainer Get(String requestUrl, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType,
            Action<ImageContainer, bool> successResponse, Action<VolleyError> errorResponse)
        {
            ThrowIfNotOnMainThread();
            if (successResponse == null)
            {
                throw new ArgumentNullException("successResponse不能为null，必须提供请求成功的委托。");
            }

            String cacheKey = GetCacheKey(requestUrl, maxWidth, maxHeight, scaleType);

            //缓存命中直接响应请求
            Bitmap cachedBitmap = mCache.GetBitmap(cacheKey);
            if (cachedBitmap != null)
            {
                ImageContainer container = new ImageContainer(cachedBitmap, requestUrl, null, mInFlightRequests,
                    mBatchedResponses);
                successResponse(container, true);
                return container;
            }

            //缓存未命中添加到请求中
            ImageContainer imageContainer = new ImageContainer(null, requestUrl, cacheKey, mInFlightRequests,
                mBatchedResponses);

            imageContainer.ErrorResponse += errorResponse;
            imageContainer.SuccessResponse += successResponse;

            successResponse(imageContainer, true);

            //是否存在相同的请求，存在则添加该请求，否则创建
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
            //将成功的请求放入缓存
            mCache.PutBitmap(cacheKey, response);

            BatchedImageRequest request = null;
            mInFlightRequests.TryGetValue(cacheKey, out request);
            mInFlightRequests.Remove(cacheKey);

            if (request != null)
            {
                request.ResponseBitmap = response;
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

        //响应请求
        private void BatchResponse(String cacheKey, BatchedImageRequest request)
        {
            mBatchedResponses.Add(cacheKey, request);
            if (mRunnable == null)
            {
                mRunnable = () =>
                    {
                        foreach (BatchedImageRequest bir in mBatchedResponses.Values)
                        {
                            foreach (ImageContainer container in bir.Containers)
                            {
                                if (container.OnSuccessResponse == null)
                                {
                                    continue;
                                }
                                if (bir.Error == null)
                                {
                                    container.Bitmap = bir.ResponseBitmap;
                                    container.OnSuccessResponse(container, false);
                                }
                                else
                                {
                                    if (container.OnErrorResponse != null)
                                    {
                                        container.OnErrorResponse(bir.Error);
                                    }
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

            //public void OnErrorResponse(VolleyError error)
            //{
            //    if (OnErrorResponse != null)
            //    {
            //        OnErrorResponse(CacheKey, error);
            //    }
            //}

            Action<VolleyError> IErrorListener.OnErrorResponse
            {
                get { throw new NotImplementedException(); }
            }

            public event Action<VolleyError> ErrorResponse;
        }
    }
}