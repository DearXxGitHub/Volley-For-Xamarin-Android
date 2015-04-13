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
using Android.Graphics;

namespace VolleyCSharp.ToolBox
{
    /// <summary>
    /// 成批图片请求
    /// 主要是将同属性的请求封装成组
    /// </summary>
    public class BatchedImageRequest
    {
        private Request mRequest;

        private VolleyError mError;
        private List<ImageContainer> mContainers = new List<ImageContainer>();
        public Bitmap ResponseBitmap { get; set; }

        public BatchedImageRequest(Request request, ImageContainer container)
        {
            mRequest = request;
            mContainers.Add(container);
        }

        public VolleyError Error
        {
            get { return this.mError; }
            set { this.mError = value; }
        }

        /// <summary>
        /// 添加一个新的图片请求
        /// </summary>
        /// <param name="container"></param>
        public void AddContainer(ImageContainer container)
        {
            mContainers.Add(container);
        }

        public bool RemoveContainerAndCancelIfNecessary(ImageContainer container)
        {
            mContainers.Remove(container);
            if (mContainers.Count == 0)
            {
                mRequest.Cancel();
                return true;
            }
            return false;
        }

        public List<ImageContainer> Containers
        {
            get
            {
                return mContainers;
            }
        }
    }
}