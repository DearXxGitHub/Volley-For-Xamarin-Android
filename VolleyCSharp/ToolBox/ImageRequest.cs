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

namespace VolleyCSharp.ToolBox
{
    public class ImageRequest : Request
    {
        private static int IMAGE_TIMEOUT_MS = 1000;
        private static int IMAGE_MAX_RETRIES = 2;
        private static float IMAGE_BACKOFF_MULT = 2f;

        private IListener mListener;
        private Android.Graphics.Bitmap.Config mDecodeConfig;
        private int mMaxWidth;
        private int mMaxHeight;
        private Android.Widget.ImageView.ScaleType mScaleType;

        private static object sDecodeLock = new object();

        public ImageRequest(String url, IListener listener, int maxWidth, int maxHeight, Android.Widget.ImageView.ScaleType scaleType,
            Android.Graphics.Bitmap.Config decodeConfig, IErrorListener errorListener)
            : base(Method.GET, url, errorListener)
        {
            SetRetryPolicy(new DefaultRetryPolicy(IMAGE_TIMEOUT_MS, IMAGE_MAX_RETRIES, IMAGE_BACKOFF_MULT));
            mListener = listener;
            mDecodeConfig = decodeConfig;
            mMaxWidth = maxWidth;
            mMaxHeight = maxHeight;
            mScaleType = scaleType;
        }

        [Java.Lang.Deprecated]
        public ImageRequest(String url, IListener listener, int maxWidth, int maxHeight,
            Android.Graphics.Bitmap.Config decodeConfig, IErrorListener errorListener)
            : this(url, listener, maxWidth, maxHeight, Android.Widget.ImageView.ScaleType.CenterInside, decodeConfig, errorListener) { }

        public override Request.Priority GetPriority()
        {
            return Request.Priority.LOW;
        }

        private static int GetResizedDimension(int maxPrimary, int maxSecondary, int actualPrimary,
            int actualSecondary, Android.Widget.ImageView.ScaleType scaleType)
        {

        }
    }
}