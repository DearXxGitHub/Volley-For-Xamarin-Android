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
using Java.IO;

namespace VolleyCSharp.ToolBox
{
    public class DiskBasedCache : ICache
    {
        private Dictionary<String, CacheHeader> mEntries = new Dictionary<string, CacheHeader>(16);
        private long mTotalSize = 0;
        private File mRootDirectory;
        private int mMaxCacheSizeInBytes;
        public static int DEFAULT_DISK_USAGE_BYTES = 5 * 1024 * 1024;
        public static float HYSTERESIS_FACTOR = 0.9F;
        public static int CACHE_MAGIC = 0x20150306;

        public DiskBasedCache(File rootDirectory, int maxCacheSizeInBytes)
        {
            this.mRootDirectory = rootDirectory;
            this.mMaxCacheSizeInBytes = maxCacheSizeInBytes;
        }

        public DiskBasedCache(File rootDirectory)
            : this(rootDirectory, DEFAULT_DISK_USAGE_BYTES) { }

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

                File file = GetFileForKey(key);
                CountingInputStream cis = null;
                try
                {
                    cis = new CountingInputStream(new FileInputStream(file));
                    CacheHeader.ReadHeader(cis);
                    byte[] data = StreamToBytes(cis, file.Length - cis.BytesRead);
                    return entry.ToCacheEntry(data);
                }
                catch (Java.IO.IOException e)
                {
                    VolleyLog.D("{0}:{1}", file.AbsolutePath, e.ToString());
                }
                catch (Java.Lang.NegativeArraySizeException e)
                {
                    VolleyLog.D("{0}:{1}", file.AbsolutePath, e.ToString());
                }
                finally
                {
                    if (cis != null)
                    {
                        try
                        {
                            cis.Close();
                        }
                        catch (Java.IO.IOException ioe)
                        {
                        }
                    }
                }
                return null;
            }
        }

        public void Put(string key, Entry entry)
        {
            lock (this)
            {
                PruneIfNeeded(entry.Data.Length);
                File file = GetFileForKey(key);
                try
                {
                    FileOutputStream fos = new FileOutputStream(file);
                    CacheHeader e = new CacheHeader(key, entry);
                    bool success = e.WriteHeader(fos);
                    if (!success)
                    {
                        fos.Close();
                        VolleyLog.D("Failed to write header for {0}", file.AbsolutePath);
                        throw new Java.IO.IOException();
                    }
                    fos.Write(entry.Data);
                    fos.Close();
                    PutEntry(key, e);
                    return;
                }
                catch (Java.IO.IOException) { }
                bool deleted = file.Delete();
                if (!deleted)
                {
                    VolleyLog.D("Could not clean up file {0}", file.AbsolutePath);
                }
            }
        }

        public void Initialize()
        {
            lock (this)
            {
                if (!mRootDirectory.Exists())
                {
                    if (!mRootDirectory.Mkdirs())
                    {
                        VolleyLog.E("Unable to create cache dir {0}", mRootDirectory.AbsolutePath);
                    }
                    return;
                }

                File[] files = mRootDirectory.ListFiles();
                if (files == null)
                {
                    return;
                }
                foreach (File file in files)
                {
                    BufferedInputStream fis = null;
                    try
                    {
                        fis = new BufferedInputStream(new FileInputStream(file));
                        CacheHeader entry = CacheHeader.ReadHeader(fis);
                        entry.Size = file.Length();
                        PutEntry(entry.Key, entry);
                    }
                    catch (Java.IO.IOException e)
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
                            if (fis != null)
                            {
                                fis.Close();
                            }
                        }
                        catch (Java.IO.IOException) { }
                    }
                }
            }
        }

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

        public void Remove(string key)
        {
            lock (this)
            {
                bool deleted = GetFileForKey(key).Delete();
                RemoveEntry(key);
                if (!deleted)
                {
                    VolleyLog.D("Could not delete cache entry for key={0},filename={1}", key, GetFilenameForKey(key));
                }
            }
        }

        public void Clear()
        {
            lock (this)
            {
                File[] files = mRootDirectory.ListFiles();
                if (files != null)
                {
                    foreach (File file in files)
                    {
                        file.Delete();
                    }
                }
                mEntries.Clear();
                mTotalSize = 0;
                VolleyLog.D("Cache cleared.");
            }
        }

        private String GetFilenameForKey(String key)
        {
            int firstHalfLength = key.Length / 2;
            String locakFilename = Java.Lang.String.ValueOf(key.Substring(0, firstHalfLength).GetHashCode());
            locakFilename += Java.Lang.String.ValueOf(key.Substring(firstHalfLength).GetHashCode());
            return locakFilename;
        }

        public File GetFileForKey(String key)
        {
            return new File(mRootDirectory, GetFilenameForKey(key));
        }

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
                bool deleted = GetFileForKey(e.Key).Delete();
                if (deleted)
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
            mEntries.Add(key, entry);
        }

        public static byte[] StreamToBytes(InputStream @in, int length)
        {
            byte[] bytes = new byte[length];
            int count, pos = 0;
            while (pos < length && ((count = @in.Read(bytes, pos, length - pos)) != -1))
            {
                pos += count;
            }
            if (pos != length)
            {
                throw new Java.IO.IOException("Expected " + length + " bytes,read " + pos + " bytes");
            }
            return bytes;
        }

        public static int Read(InputStream @is)
        {
            int b = @is.Read();
            if (b == -1)
            {
                throw new Java.IO.EOFException();
            }
            return b;
        }

        public static void WriteInt(OutputStream os, int n)
        {
            os.Write((n >> 0) & 0xff);
            os.Write((n >> 8) & 0xff);
            os.Write((n >> 16) & 0xff);
            os.Write((n >> 24) & 0xff);
        }

        public static int ReadInt(InputStream @is)
        {
            int n = 0;
            n |= (Read(@is) << 0);
            n |= (Read(@is) << 8);
            n |= (Read(@is) << 16);
            n |= (Read(@is) << 24);
            return n;
        }

        public static void WriteLong(OutputStream os, long n)
        {
            os.Write((byte)(n >> 0));
            os.Write((byte)(n >> 8));
            os.Write((byte)(n >> 16));
            os.Write((byte)(n >> 24));
            os.Write((byte)(n >> 32));
            os.Write((byte)(n >> 40));
            os.Write((byte)(n >> 48));
            os.Write((byte)(n >> 56));
        }

        public static long ReadLong(InputStream @is)
        {
            long n = 0;
            n |= ((Read(@is) & 0xFFL) << 0);
            n |= ((Read(@is) & 0xFFL) << 8);
            n |= ((Read(@is) & 0xFFL) << 16);
            n |= ((Read(@is) & 0xFFL) << 24);
            n |= ((Read(@is) & 0xFFL) << 32);
            n |= ((Read(@is) & 0xFFL) << 40);
            n |= ((Read(@is) & 0xFFL) << 48);
            n |= ((Read(@is) & 0xFFL) << 56);
            return n;
        }

        public static void WriteString(OutputStream os, String s)
        {
            byte[] b = Encoding.UTF8.GetBytes(s);
            WriteLong(os, b.Length);
            os.Write(b, 0, b.Length);
        }

        public static String ReadString(InputStream @is)
        {
            int n = (int)ReadLong(@is);
            byte[] b = StreamToBytes(@is, n);
            return Encoding.UTF8.GetString(b);
        }

        public static void WriteStringStringMap(Dictionary<String, String> map, OutputStream os)
        {
            if (map != null)
            {
                WriteInt(os, map.Count);
                foreach (KeyValuePair<String, String> entry in map)
                {
                    WriteString(os, entry.Key);
                    WriteString(os, entry.Value);
                }
            }
            else
            {
                WriteInt(os, 0);
            }
        }

        public static Dictionary<String, String> ReadStringStringMap(InputStream @is)
        {
            int size = ReadInt(@is);
            Dictionary<String, String> result = new Dictionary<string, string>(size);

        }
    }
}