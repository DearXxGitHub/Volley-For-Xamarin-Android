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
    public class CountingInputStream : Java.IO.FilterInputStream
    {
        public int BytesRead { get; set; }

        public CountingInputStream(System.IO.Stream input)
            : base(input) { }

        public override int Read()
        {
            int result = base.Read();
            if (result != -1)
            {
                return BytesRead++;
            }
            return result;
        }

        public override int Read(byte[] buffer, int offset, int length)
        {
            int result = base.Read(buffer, offset, length);
            if (result != -1)
            {
                BytesRead += result;
            }
            return result;
        }
    }
}