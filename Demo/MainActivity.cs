using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using VolleyCSharp.ToolBox;
using Android.Util;
using VolleyCSharp;

namespace Demo
{
    [Activity(Label = "Demo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            var requestQueue = Volley.NewRequestQueue(this);
            var stringRequest = new StringRequest("http://www.baidu.com", (x) =>
            {
                Log.Debug("Test", "String Request is Finished");
            },
            (x) =>
            {
                Log.Debug("Test", x.ToString());
            });
            requestQueue.Add(stringRequest);
            requestQueue.Start();
        }
    }
}