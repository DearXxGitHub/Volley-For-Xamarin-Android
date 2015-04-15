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
 * 15.4.15 ¸ÄÐ´
 */

namespace VolleyCSharp.ToolBox
{
    public interface IAuthenticator
    {
        String GetAuthToken();
        void InvalidateAuthToken(String authToken);
    }
}