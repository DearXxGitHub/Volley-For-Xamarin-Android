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
            requestQueue.Start();
            FindViewById<Button>(Resource.Id.btnString).Click += (e, s) =>
            {
                var stringRequest = new StringRequest("http://item.taobao.com/item.htm?spm=a230r.1.14.9.waPbtr&id=41701482681&ns=1&abbucket=9#detail", (x) =>
                {
                    Log.Debug("Test", "String Request is Finished");
                },
                (x) =>
                {
                    Log.Debug("Test", x.ToString());
                });
                requestQueue.Add(stringRequest);
            };
        }
    }
}