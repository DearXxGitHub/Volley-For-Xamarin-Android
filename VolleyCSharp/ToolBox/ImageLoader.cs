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
        private Java.Lang.IRunnable mRunnable;

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
            private Bitmap mBitmap;
            private IImageListener mListener;
            private String mCacheKey;
            private String mRequestUrl;

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
                    
                }
            }
        }

        internal class BatchedImageRequest
        {
            private Request mRequest;
            private Bitmap mResponseBitmap;
            private VolleyError mError;
            private List<ImageContainer> mContainers = new List<ImageContainer>();

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