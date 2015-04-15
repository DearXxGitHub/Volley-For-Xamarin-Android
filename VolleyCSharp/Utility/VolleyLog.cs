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
using Android.Util;
using System.Threading;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.Utility
{
    /// <summary>
    /// 日志输出工具类
    /// </summary>
    public class VolleyLog : Java.Lang.Object
    {
        public static String TAG = "Volley";

        public static bool DEBUG = Log.IsLoggable(TAG, LogPriority.Verbose);

        public static void SetTag(String tag)
        {
            D("Changing log tag to {0}", tag);
            TAG = tag;

            DEBUG = Log.IsLoggable(TAG, LogPriority.Verbose);
        }

        private static String BuildMessage(String format, params object[] args)
        {
            String msg = (args == null) ? format : String.Format(format, args);
            var trace = new Java.Lang.Throwable().FillInStackTrace().GetStackTrace();

            String caller = "<unknown>";
            Java.Lang.Class volleyLogClass = new VolleyLog().Class;

            for (int i = 2; i < trace.Length; i++)
            {
                var clazz = trace[i].Class;
                if (!clazz.Equals(volleyLogClass))
                {
                    String callingClass = trace[i].ClassName;
                    callingClass = callingClass.Substring(callingClass.LastIndexOf('.') + 1);
                    callingClass = callingClass.Substring(callingClass.LastIndexOf('$') + 1);

                    caller = callingClass + "." + trace[i].MethodName;
                    break;
                }
            }
            return String.Format("[{0}] {1}:{2}", Thread.CurrentThread.ManagedThreadId, caller, msg);
        }

        public static void V(String format, params object[] args)
        {
            if (DEBUG)
            {
                Log.Verbose(TAG, BuildMessage(format, args));
            }
        }

        public static void D(String format, params object[] args)
        {
            Log.Debug(TAG, BuildMessage(format, args));
        }

        public static void E(String format, params object[] args)
        {
            Log.Error(TAG, BuildMessage(format, args));
        }

        public static void E(Exception tr,String format,params object[] args)
        {
            Log.Error(TAG,BuildMessage(format,args));
        }

        public static void WTF(String format, params object[] args)
        {
            Log.Wtf(TAG, BuildMessage(format, args));
        }

        public static void WTF(Exception tr, String format, params object[] args)
        {
            Log.Wtf(TAG, BuildMessage(format, args));
        }
    }
}