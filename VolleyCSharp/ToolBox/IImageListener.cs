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
    public interface IImageListener : IErrorListener
    {
        void OnResponse(ImageContainer response, bool isImmediate);
    }
}