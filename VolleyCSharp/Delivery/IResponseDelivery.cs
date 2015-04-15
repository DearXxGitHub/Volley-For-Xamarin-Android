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

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.Delivery
{
    /// <summary>
    /// 如果需要在请求完成后进行拦截必须实现该接口
    /// 并在RequestQueue的构造函数中替换
    /// </summary>
    public interface IResponseDelivery
    {
        void PostResponse(Request request, Response response);
        void PostResponse(Request request, Response response, Action runnable);
        void PostError(Request request, VolleyError error);
    }
}