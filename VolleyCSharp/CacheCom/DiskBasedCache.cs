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
using System.IO;
using VolleyCSharp.Utility;

/*
 * 原作者Github（java）：https://github.com/mcxiaoke/android-volley
 * 
 * C#作者：Y-Z-F
 * 博客地址：http://www.cnblogs.com/yaozhenfa/
 * Github地址：https://github.com/yaozhenfa/
 * 
 * 15.4.15 审核通过
 */

namespace VolleyCSharp.CacheCom
{
    /// <summary>
    /// 提供文件缓存
    /// </summary>
    public class DiskBasedCache : ICache
    {
        public static int DEFAULT_DISK_USAGE_BYTES = 5 * 1024 * 1024;
        public static float HYSTERESIS_FACTOR = 0.9F;
        public static int CACHE_MAGIC = 0x20150306;

        private Dictionary<String, CacheHeader> mEntries = new Dictionary<string, CacheHeader>(16);
        private long mTotalSize = 0;
        /// <summary>
        /// 缓存根文件夹
        /// </summary>
        private DirectoryInfo mRootDirectory;
        private int mMaxCacheSizeInBytes;

        public DiskBasedCache(DirectoryInfo rootDirectory, int maxCacheSizeInBytes)
        {
            this.mRootDirectory = rootDirectory;
            this.mMaxCacheSizeInBytes = maxCacheSizeInBytes;
        }

        public DiskBasedCache(DirectoryInfo rootDirectory)
            : this(rootDirectory, DEFAULT_DISK_USAGE_BYTES) { }

        /// <summary>
        /// 获取缓存数据
        /// </summary>
        public Entry Get(string key)
        {
            lock (this)
            {
                CacheHeader entry = null;
                mEntries.TryGetValue(key, out entry);
                if (entry == null)
                {
                    return null;
                }

                FileInfo file = GetFileForKey(key);
                FileStream fs = null;
                try
                {
                    fs = file.Open(FileMode.OpenOrCreate);
                    CacheHeader.ReadHeader(fs);
                    byte[] data = StreamToBytes(fs, (int)(fs.Length - fs.Position));
                    return entry.ToCacheEntry(data);
                }
                catch (IOException e)
                {
                    VolleyLog.D("{0}:{1}", file.FullName, e.ToString());
                }
                finally
                {
                    if (fs != null)
                    {
                        try
                        {
                            fs.Close();
                        }
                        catch (IOException) { }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 新增缓存数据
        /// </summary>
        public void Put(string key, Entry entry)
        {
            lock (this)
            {
                PruneIfNeeded(entry.Data.Length);
                var file = GetFileForKey(key);
                try
                {
                    var fos = file.Open(FileMode.OpenOrCreate);
                    CacheHeader e = new CacheHeader(key, entry);
                    bool success = e.WriteHeader(fos);
                    if (!success)
                    {
                        fos.Close();
                        VolleyLog.D("Failed to write header for {0}", file.FullName);
                        throw new IOException();
                    }
                    fos.Write(entry.Data, 0, entry.Data.Length);
                    fos.Close();
                    PutEntry(key, e);
                    return;
                }
                catch (IOException) { }
                file.Delete();
                if (File.Exists(file.FullName))
                {
                    VolleyLog.D("Could not clean up file {0}", file.FullName);
                }
            }
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        public void Initialize()
        {
            lock (this)
            {
                if (!mRootDirectory.Exists)
                {
                    mRootDirectory.Create();
                    if (!mRootDirectory.Exists)
                    {
                        VolleyLog.E("Unable to create cache dir {0}", mRootDirectory.FullName);
                    }
                    return;
                }

                //获取已缓存文件并添加到缓存表中
                FileInfo[] files = mRootDirectory.GetFiles();
                if (files == null)
                {
                    return;
                }
                foreach (FileInfo file in files)
                {
                    FileStream fs = null;
                    try
                    {
                        fs = file.Open(FileMode.OpenOrCreate);
                        CacheHeader entry = CacheHeader.ReadHeader(fs);
                        entry.Size = fs.Length;
                        PutEntry(entry.Key, entry);
                    }
                    catch (IOException)
                    {
                        if (file != null)
                        {
                            file.Delete();
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (fs != null)
                            {
                                fs.Close();
                            }
                        }
                        catch (IOException) { }
                    }
                }
            }
        }

        /// <summary>
        /// 强制数据失效
        /// </summary>
        public void Invalidate(string key, bool fullExpire)
        {
            lock (this)
            {
                Entry entry = Get(key);
                if (entry != null)
                {
                    entry.SoftTtl = 0;
                    if (fullExpire)
                    {
                        entry.Ttl = 0;
                    }
                    Put(key, entry);
                }
            }
        }

        /// <summary>
        /// 删除缓存数据
        /// </summary>
        public void Remove(string key)
        {
            lock (this)
            {
                var fi = GetFileForKey(key);
                fi.Delete();
                RemoveEntry(key);
                if (File.Exists(fi.FullName))
                {
                    VolleyLog.D("Could not delete cache entry for key={0},filename={1}", key, fi.FullName);
                }
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                FileInfo[] files = mRootDirectory.GetFiles();
                if (files != null)
                {
                    foreach (FileInfo file in files)
                    {
                        file.Delete();
                    }
                }
                mEntries.Clear();
                mTotalSize = 0;
                VolleyLog.D("Cache cleared.");
            }
        }

        /// <summary>
        /// 根据Key获取文件名
        /// </summary>
        private String GetFilenameForKey(String key)
        {
            int firstHalfLength = key.Length / 2;
            String locakFilename = Java.Lang.String.ValueOf(key.Substring(0, firstHalfLength).GetHashCode());
            locakFilename += Java.Lang.String.ValueOf(key.Substring(firstHalfLength).GetHashCode());
            return locakFilename;
        }

        /// <summary>
        /// 根据Key获取文件对象
        /// </summary>
        public FileInfo GetFileForKey(String key)
        {
            String filePath = mRootDirectory.FullName + "/" + GetFilenameForKey(key);
            return new FileInfo(filePath);
        }

        /// <summary>
        /// 当需要的空间大于指定空间后清除部分缓存
        /// </summary>
        private void PruneIfNeeded(int neededSpace)
        {
            if (mTotalSize + neededSpace < mMaxCacheSizeInBytes)
            {
                return;
            }

            if (VolleyLog.DEBUG)
            {
                VolleyLog.V("Pruning old cache entries.");
            }

            long before = mTotalSize;
            int prunedFiles = 0;
            long startTime = SystemClock.ElapsedRealtime();
            Dictionary<string, CacheHeader> delDic = new Dictionary<string, CacheHeader>();

            foreach (KeyValuePair<String, CacheHeader> pair in mEntries)
            {
                CacheHeader e = pair.Value;
                var fi = GetFileForKey(e.Key);
                fi.Delete();
                if (!File.Exists(fi.FullName))
                {
                    mTotalSize -= e.Size;
                }
                else
                {
                    VolleyLog.D("Could not delete cache entry for key={0},filename={1}", e.Key, GetFilenameForKey(e.Key));
                }
                prunedFiles++;
                delDic.Add(pair.Key,pair.Value);
                if (mTotalSize + neededSpace < mMaxCacheSizeInBytes * HYSTERESIS_FACTOR)
                {
                    break;
                }
            }
            foreach (KeyValuePair<string, CacheHeader> del in delDic)
            {
                mEntries.Remove(del.Key);
            }
            if (VolleyLog.DEBUG)
            {
                VolleyLog.V("Pruned {0} files,{1} bytes,{2} ms", prunedFiles, (mTotalSize - before), SystemClock.ElapsedRealtime() - startTime);
            }
        }

        private void PutEntry(String key, CacheHeader entry)
        {
            if (!mEntries.ContainsKey(key))
            {
                mTotalSize += entry.Size;
            }
            else
            {
                CacheHeader oldEntry = mEntries[key];
                mTotalSize += (entry.Size - oldEntry.Size);
            }
            if (mEntries.ContainsKey(key))
            {
                mEntries[key] = entry;
            }
            else
            {
                mEntries.Add(key, entry);
            }
        }

        private void RemoveEntry(String key)
        {
            CacheHeader entry = null;
            mEntries.TryGetValue(key, out entry);
            if (entry != null)
            {
                mTotalSize -= entry.Size;
                mEntries.Remove(key);
            }
        }

        #region 静态公共方法

        public static byte[] StreamToBytes(Stream s, int length)
        {
            byte[] bytes = new byte[length];
            int count, pos = 0;
            while (pos < length && ((count = s.Read(bytes, pos, length - pos)) != -1))
            {
                pos += count;
            }
            if (pos != length)
            {
                throw new IOException("Expected " + length + " bytes,read " + pos + " bytes");
            }
            return bytes;
        }

        public static int Read(Stream s)
        {
            int b = s.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }
            return b;
        }

        public static void WriteInt(Stream s, int n)
        {
            s.WriteByte(Convert.ToByte((n >> 0) & 0xff));
            s.WriteByte(Convert.ToByte((n >> 8) & 0xff));
            s.WriteByte(Convert.ToByte((n >> 16) & 0xff));
            s.WriteByte(Convert.ToByte((n >> 24) & 0xff));
        }

        public static int ReadInt(Stream s)
        {
            int n = 0;
            n |= (Read(s) << 0);
            n |= (Read(s) << 8);
            n |= (Read(s) << 16);
            n |= (Read(s) << 24);
            return n;
        }

        public static void WriteLong(Stream os, long n)
        {
            os.WriteByte((byte)(n >> 0));
            os.WriteByte((byte)(n >> 8));
            os.WriteByte((byte)(n >> 16));
            os.WriteByte((byte)(n >> 24));
            os.WriteByte((byte)(n >> 32));
            os.WriteByte((byte)(n >> 40));
            os.WriteByte((byte)(n >> 48));
            os.WriteByte((byte)(n >> 56));
        }

        public static long ReadLong(Stream s)
        {
            long n = 0;
            n |= ((Read(s) & 0xFFL) << 0);
            n |= ((Read(s) & 0xFFL) << 8);
            n |= ((Read(s) & 0xFFL) << 16);
            n |= ((Read(s) & 0xFFL) << 24);
            n |= ((Read(s) & 0xFFL) << 32);
            n |= ((Read(s) & 0xFFL) << 40);
            n |= ((Read(s) & 0xFFL) << 48);
            n |= ((Read(s) & 0xFFL) << 56);
            return n;
        }

        public static void WriteString(Stream s, String t)
        {
            byte[] b = Encoding.UTF8.GetBytes(t);
            WriteLong(s, b.Length);
            s.Write(b, 0, b.Length);
        }

        public static String ReadString(Stream s)
        {
            int n = (int)ReadLong(s);
            byte[] b = StreamToBytes(s, n);
            return Encoding.UTF8.GetString(b);
        }

        public static void WriteStringStringMap(Dictionary<String, String> map, Stream s)
        {
            if (map != null)
            {
                WriteInt(s, map.Count);
                foreach (KeyValuePair<String, String> entry in map)
                {
                    WriteString(s, entry.Key);
                    WriteString(s, entry.Value);
                }
            }
            else
            {
                WriteInt(s, 0);
            }
        }

        public static Dictionary<String, String> ReadStringStringMap(Stream s)
        {
            int size = ReadInt(s);
            Dictionary<String, String> result = new Dictionary<string, string>(size);
            for (int i = 0; i < size; i++)
            {
                String key = ReadString(s);
                String value = ReadString(s);
                result.Add(key, value);
            }
            return result;
        }

        #endregion
    }
}