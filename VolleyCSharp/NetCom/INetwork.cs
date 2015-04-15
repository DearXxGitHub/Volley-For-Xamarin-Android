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

/*
 * 15.4.13 ��д
 */
using VolleyCSharp.MainCom;

namespace VolleyCSharp.NetCom
{
    public interface INetwork
    {
        NetworkResponse PerformRequest(Request request);
    }
}