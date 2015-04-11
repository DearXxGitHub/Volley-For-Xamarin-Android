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
        private VolleyCSharp.ToolBox.ImageLoader.ImageContainer mImageContainer;

        public NetworkImageView(Context context)
            : this(context, null) { }

        public NetworkImageView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0) { }

        public NetworkImageView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle) { }


    }
}