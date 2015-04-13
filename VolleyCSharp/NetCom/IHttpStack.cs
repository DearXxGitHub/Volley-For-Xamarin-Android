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
using System.Net;

/*
 * 2015.4.13 ��д
 */

namespace VolleyCSharp.NetCom
{
    public interface IHttpStack
    {
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="request">��ʾһ������</param>
        /// <param name="additionalHeaders">��������ͷ������</param>
        /// <returns>������</returns>
        HttpWebResponse PerformRequest(Request request, Dictionary<String, String> additionalHeaders);
    }
}