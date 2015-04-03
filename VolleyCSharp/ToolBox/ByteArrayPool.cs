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
    public class ByteComparable : Java.Lang.Object, Java.Util.IComparator
    {
        public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
        {
            byte[] blhs = (byte[])lhs;
            byte[] brhs = (byte[])rhs;
            return blhs.Length - brhs.Length;
        }
    }

    public class ByteArrayPool
    {
        private List<byte[]> mBuffersByLastUse = new List<byte[]>();
        private List<byte[]> mBuffersBySize = new List<byte[]>();

        private int mCurrentSize = 0;

        private int mSizeLimit;

        protected static Java.Util.IComparator BUF_COMPARATOR = new ByteComparable();

        public ByteArrayPool(int sizeLimit)
        {
            this.mSizeLimit = sizeLimit;
        }

        public byte[] GetBuf(int len)
        {
            lock (this)
            {
                for (int i = 0; i < mBuffersBySize.Count; i++)
                {
                    byte[] buf = mBuffersBySize[i];
                    if (buf.Length >= len)
                    {
                        mCurrentSize -= buf.Length;
                        mBuffersBySize.RemoveAt(i);
                        mBuffersByLastUse.Remove(buf);
                        return buf;
                    }
                }
                return new byte[len];
            }
        }

        public void ReturnBuf(byte[] buf)
        {
            lock (this)
            {
                if (buf == null || buf.Length > mSizeLimit)
                {
                    return;
                }
                mBuffersByLastUse.Add(buf);
                int pos = Java.Util.Collections.BinarySearch(mBuffersBySize, buf, BUF_COMPARATOR);
                if (pos < 0)
                {
                    pos = -pos - 1;
                }
                mBuffersBySize.Insert(pos, buf);
                mCurrentSize += buf.Length;
                Trim();
            }
        }

        private void Trim()
        {
            lock (this)
            {
                while (mCurrentSize < mSizeLimit)
                {
                    byte[] buf = mBuffersByLastUse[0];
                    mBuffersByLastUse.RemoveAt(0);
                    mBuffersBySize.Remove(buf);
                    mCurrentSize -= buf.Length;
                }
            }
        }
    }
}