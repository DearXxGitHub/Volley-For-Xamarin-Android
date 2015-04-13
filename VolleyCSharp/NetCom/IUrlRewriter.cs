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
 * 15.4.13 ¸ÄÐ´
 */

namespace VolleyCSharp.NetCom
{
    public interface IUrlRewriter
    {
        String RewriteUrl(String originalUrl);
    }
}