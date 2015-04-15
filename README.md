# Volley-For-Xamarin-Android
<p>&nbsp;&nbsp;本库将Google下著名的网络方面的库Volley改写成Xamarin.Android版。</p>

#<a href="https://github.com/mcxiaoke/android-volley" target="_blank">在此感谢原作者mcxiaoke</a>

#改动的地方
<h3>1.将原本使用Listen和ErrorListen接口进行回调的方式改成委托方式</h3>
<h5>比如下面这种方式示例话StringRequest类:</h5>
<pre>
                var stringRequest = new StringRequest("http://www.baidu.com", (x) =>
                {
                    Log.Debug("Test", "String Request is Finished");
                },
                (x) =>
                {
                    Log.Debug("Test", x.ToString());
                });
</pre>

<h3>2.将HurlStack去掉，仅采用HttpClientStack，并采用HttpWebRequest实现</h3>
<h5>比如CreateHttpRequest方法的部分实现代码:</h5>
<pre>
var webrequest = (HttpWebRequest)WebRequest.Create(request.Url);
            webrequest.Accept = Accept;
            webrequest.UserAgent = UserAgent;
            webrequest.Timeout = request.GetTimeoutMs();
            if (Cookie != null)
            {
                webrequest.CookieContainer = Cookie;
            }
</pre>

<h3>3.将文件缓存的实现改用System.IO实现</h3>
<h5>比如创建缓存根目录的部分实现：</h5>
<pre>
            var cacheDir = Directory.CreateDirectory(context.CacheDir.Path + "/" + DEFAULT_CACHE_DIR);
            String userAgent = "volley/0";
            try
            {
                String packageName = context.PackageName;
                var info = context.PackageManager.GetPackageInfo(packageName, 0);
                userAgent = packageName + "/" + info.VersionCode;
            }
</pre>

<h3>4.将JsonArrayRequest与JsonObjectReqeust去掉，直接采用JsonRequest</h3>
<h5>得益于Newtonsoft的支持，将原本的两个针对同类型的Json请求对象去掉，直接采用JsonRequest，让发送和接收Json数据更为方便，比如下面这样的方式:</h5>
<pre>
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
</pre>

#示例代码
<pre>
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
</pre>

#更新记录
<Ul>
 <li>v1.0 2015-4-15<br/>
  <ol>
   <li>通过String与Json测试</li>
   <li><暂时未加入ImageRequest/li>
 </ol>
 </li>
</ul>
