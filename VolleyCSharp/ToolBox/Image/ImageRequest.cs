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
using Android.Graphics;

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
            if ((maxPrimary == 0) && (maxSecondary == 0))
            {
                return actualPrimary;
            }

            if (scaleType == Android.Widget.ImageView.ScaleType.FitXy)
            {
                if (maxPrimary == 0)
                {
                    return actualPrimary;
                }
                return maxPrimary;
            }

            double ratio = 0;

            if (maxPrimary == 0)
            {
                ratio = (double)maxSecondary / (double)actualSecondary;
                return (int)(actualPrimary * ratio);
            }

            if (maxSecondary == 0)
            {
                return maxPrimary;
            }

            ratio = (double)actualSecondary / (double)actualPrimary;
            int resized = maxPrimary;

            if (scaleType == Android.Widget.ImageView.ScaleType.CenterCrop)
            {
                if ((resized * ratio) < maxSecondary)
                {
                    resized = (int)(maxSecondary / ratio);
                }
                return resized;
            }

            if ((resized * ratio) > maxSecondary)
            {
                resized = (int)(maxSecondary / ratio);
            }
            return resized;
        }

        public override Response ParseNetworkResponse(NetworkResponse response)
        {
            lock (sDecodeLock)
            {
                try
                {
                    return DoParse(response);
                }
                catch (Java.Lang.OutOfMemoryError e)
                {
                    VolleyLog.E("Caught OOM for {0} byte image,url={1}", response.Data.Length, Url);
                    return Response.Error(new ParseError(e));
                }
            }
        }

        private Response DoParse(NetworkResponse response)
        {
            byte[] data = response.Data;
            BitmapFactory.Options decodeOption = new BitmapFactory.Options();
            Bitmap bitmap = null;
            if (mMaxWidth == 0 && mMaxHeight == 0)
            {
                decodeOption.InPreferredConfig = mDecodeConfig;
                bitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length, decodeOption);
            }
            else
            {
                decodeOption.InJustDecodeBounds = true;
                BitmapFactory.DecodeByteArray(data, 0, data.Length, decodeOption);
                int actualWidth = decodeOption.OutWidth;
                int actualHeight = decodeOption.OutHeight;

                int desiredWidth = GetResizedDimension(mMaxWidth, mMaxHeight, actualWidth, actualHeight, mScaleType);
                int desiredHeight = GetResizedDimension(mMaxHeight, mMaxWidth, actualHeight, actualWidth, mScaleType);

                decodeOption.InJustDecodeBounds = false;
                decodeOption.InSampleSize = FindBestSampleSize(actualWidth, actualHeight, desiredWidth, desiredHeight);
                Bitmap tempBitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length, decodeOption);

                if (tempBitmap != null && (tempBitmap.Width > desiredWidth || tempBitmap.Height > desiredHeight))
                {
                    bitmap = Bitmap.CreateScaledBitmap(tempBitmap, desiredWidth, desiredHeight, true);
                    tempBitmap.Recycle();
                }
                else
                {
                    bitmap = tempBitmap;
                }
            }

            if (bitmap == null)
            {
                return Response.Error(new ParseError(response));
            }
            else
            {
                return Response.Success(bitmap, HttpHeaderParser.ParseCacheHeaders(response));
            }
        }

        public override void DeliverResponse(object response)
        {
            mListener.OnResponse(response);
        }
    }
}