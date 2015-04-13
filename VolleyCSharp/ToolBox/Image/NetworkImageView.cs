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
using Android.Util;

namespace VolleyCSharp.ToolBox
{
    public class NetworkImageView : ImageView
    {
        private String mUrl;
        private int mDefaultImageId;
        private int mErrorImageId;
        private ImageLoader mImageLoader;
        private ImageContainer mImageContainer;

        public NetworkImageView(Context context)
            : this(context, null) { }

        public NetworkImageView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0) { }

        public NetworkImageView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle) { }

        public void SetImageUrl(String url, ImageLoader imageLoader)
        {
            mUrl = url;
            mImageLoader = imageLoader;
            LoadImageIfNecessary(false);
        }

        public String ImageUrl
        {
            get
            {
                return mUrl;
            }
        }

        public int DefaultImageResId
        {
            get
            {
                return mDefaultImageId;
            }
            set
            {
                mDefaultImageId = value;
            }
        }

        public int ErrorImageResId
        {
            get
            {
                return mErrorImageId;
            }
            set
            {
                mErrorImageId = value;
            }
        }

        private void LoadImageIfNecessary(bool isInLayoutPass)
        {
            int width = Width;
            int height = Height;
            ScaleType scaleType = GetScaleType();

            bool wrapWidth = false, wrapHeight = false;
            if (LayoutParameters != null)
            {
                wrapWidth = LayoutParameters.Width == ViewGroup.LayoutParams.WrapContent;
                wrapHeight = LayoutParameters.Height == ViewGroup.LayoutParams.WrapContent;
            }

            bool isFullWrapContent = wrapWidth && wrapHeight;
            if (width == 0 && height == 0 && !isFullWrapContent)
            {
                return;
            }

            if (String.IsNullOrEmpty(mUrl))
            {
                if (mImageContainer != null)
                {
                    mImageContainer.CancelRequest();
                    mImageContainer = null;
                }
                SetDefaultImageOrNull();
                return;
            }

            if (mImageContainer != null && mImageContainer.GetRequestUrl() != null)
            {
                if (mImageContainer.GetRequestUrl() == mUrl)
                {
                    return;
                }
                else
                {
                    mImageContainer.CancelRequest();
                    SetDefaultImageOrNull();
                }
            }

            int maxWidth = wrapWidth ? 0 : width;
            int maxHeight = wrapHeight ? 0 : height;

            ImageContainer newContainer = mImageLoader.Get(mUrl, new DefaultImageListener(), maxWidth, maxHeight, scaleType);
            mImageContainer = newContainer;
        }

        internal class DefaultImageListener : IImageListener
        {

            public void OnResponse(ImageContainer response, bool isImmediate)
            {
                if (isImmediate && isInLayoutPass)
                {
                    Post(() =>
                        {
                            OnResponse(response, false);
                        });
                    return;
                }

                if (response.GetBitmap() != null)
                {
                    SetImageBitmap(response.GetBitmap());
                }
                else if (mDefaultImageId != 0)
                {
                    SetImageResource(mDefaultImageId);
                }
            }

            public void OnErrorResponse(VolleyError error)
            {
                if (mErrorImageId != 0)
                {
                    SetImageResource(mErrorImageId);
                }
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            LoadImageIfNecessary(true);
        }

        protected override void OnDetachedFromWindow()
        {
            if (mImageContainer != null)
            {
                mImageContainer.CancelRequest();
                SetImageBitmap(null);
                mImageContainer = null;
            }
            base.OnDetachedFromWindow();
        }

        protected override void DrawableStateChanged()
        {
            base.DrawableStateChanged();
            Invalidate();
        }
    }
}