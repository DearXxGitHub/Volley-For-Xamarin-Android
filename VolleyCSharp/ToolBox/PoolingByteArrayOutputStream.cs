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

namespace VolleyCSharp.ToolBox
{
    public class PoolingByteArrayOutputStream : Java.IO.ByteArrayOutputStream
    {
        private static int DEFAULT_SIZE = 256;
        private ByteArrayPool mPool;

        public PoolingByteArrayOutputStream(ByteArrayPool pool)
            : this(pool, DEFAULT_SIZE) { }

        public PoolingByteArrayOutputStream(ByteArrayPool pool, int size)
        {
            mPool = pool;
            Buf = mPool.GetBuf(Math.Max(size, DEFAULT_SIZE));
        }

        public override void Close()
        {
            mPool.ReturnBuf(Buf.ToArray());
            Buf = null;
            base.Close();
        }

        public override void Flush()
        {
            mPool.ReturnBuf(Buf.ToArray());
        }

        private void Expand(int i)
        {
            if (Count + i <= Buf.Count)
            {
                return;
            }
            byte[] newbuf = mPool.GetBuf((Count + i) * 2);
            Buf.CopyTo(newbuf, 0);
            mPool.ReturnBuf(Buf.ToArray());
            Buf = newbuf;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (this)
            {
                Expand(count);
                base.Write(buffer, offset, count);
            }
        }

        public override void Write(int oneByte)
        {
            lock (this)
            {
                Expand(1);
                base.Write(oneByte);
            }
        }
    }
}