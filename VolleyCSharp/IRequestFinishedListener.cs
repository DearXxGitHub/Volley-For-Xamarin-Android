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
using VolleyCSharp.MainCom;

namespace VolleyCSharp
{
    public interface IRequestFinishedListener
    {
        void OnRequestFinished(Request request);
    }
}