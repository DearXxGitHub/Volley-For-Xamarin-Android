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
    /// 日志工具
    /// </summary>
    public class MarkerLog : Java.Lang.Object
    {
        private class Marker
        {
            public String name;
            public long thread;
            public long time;

            public Marker(String name, long thread, long time)
            {
                this.name = name;
                this.thread = thread;
                this.time = time;
            }
        }

        public static bool ENABLED = VolleyLog.DEBUG;

        private static long MIN_DURATION_FOR_LOGGING_MS = 0;

        private List<Marker> mMarkers = new List<Marker>();
        private bool mFinished = false;

        private long GetTotalDuration()
        {
            if (mMarkers.Count == 0)
            {
                return 0;
            }
            long first = mMarkers.First().time;
            long last = mMarkers.Last().time;
            return last - first;
        }

        public void Add(String name, long threadId)
        {
            lock (this)
            {
                if (mFinished)
                {
                    throw new OperationCanceledException("Marker Added to finished log");
                }
                mMarkers.Add(new Marker(name, threadId, SystemClock.ElapsedRealtime()));
            }
        }

        public void Finish(String header)
        {
            mFinished = true;

            long duration = GetTotalDuration();
            if (duration <= MIN_DURATION_FOR_LOGGING_MS)
            {
                return;
            }

            long prevTime = mMarkers.First().time;
            VolleyLog.D("({0:F4} ms) {1}", duration, header);
            foreach (Marker marker in mMarkers)
            {
                long thisTime = marker.time;
                VolleyLog.D("+{0:F4} [{1:2}] {2}", (thisTime - prevTime), marker.thread, marker.name);
                prevTime = thisTime;
            }
        }

        protected override void JavaFinalize()
        {
            if (!mFinished)
            {
                Finish("Request on the loose");
                VolleyLog.E("Marker log finalized without finish() - uncaught exit point for request");
            }
        }
    }
}