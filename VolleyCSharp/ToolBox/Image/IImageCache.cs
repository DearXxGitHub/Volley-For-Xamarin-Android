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
    public interface IImageCache
    {
        Bitmap GetBitmap(String url);
        void PutBitmap(String url, Bitmap bitmap);
    }
}