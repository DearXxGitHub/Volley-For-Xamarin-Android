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
                var stringRequest = new StringRequest("http://www.baidu.com", (x) =>
                {
                    Log.Debug("Test", "String Request is Finished");
                },
                (x) =>
                {
                    Log.Debug("Test", x.ToString());
                });
                requestQueue.Add(stringRequest);
            };

			FindViewById<Button> (Resource.Id.btnJson).Click += (sender, e) => 
			{
                //该测试需要开发人员搭建一个简单的web端
				var jsonRequest = new JsonRequest<Test,Test>("http://172.16.101.20:8080/MUser/PostTest",new Test{
					UName = "Test",
					UPass = "Test"
				},(x)=>
					{
						Log.Debug("Test",x.UName);
					},
					(x)=>
					{
						Log.Debug("Test",x.ToString());
					});
				requestQueue.Add(jsonRequest);
			};
        }
    }

	public class Test
	{
		public String UName{get;set;}
		public String UPass{get;set;}
	}
}