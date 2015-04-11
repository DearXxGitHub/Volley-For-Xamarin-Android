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

namespace VolleyCSharp
{
    public interface IErrorListener
    {
        Action<VolleyError> OnErrorResponse { get; private set; }
        event Action<VolleyError> ErrorResponse;
    }
}