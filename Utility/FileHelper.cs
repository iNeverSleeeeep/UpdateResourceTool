using System;
using System.IO;
using UnityEngine;

namespace URT
{
    public static class FileHelper
    {
        public static bool MoveFileForce(string pathFrom, string pathTo)
        {
            var overwrite = false;

            if (File.Exists(pathTo))
            {
                overwrite = true;
                File.Delete(pathTo);
            }

            File.Move(pathFrom, pathTo);
            return overwrite;
        }

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static long FileSize(string path)
        {
            long size = 0;
            try
            {
                if (FileExists(path))
                    size = new FileInfo(path).Length;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            return size;
        }

        public static void MakeSureDirectory(string path)
        {
            int start = 0;
            while (true)
            {
                var index = path.IndexOf('/', start);
                if (index < 0)
                    break;
                start = index + 1;
                var directory = path.Substring(0, index);
                if (Directory.Exists(directory) == false)
                    Directory.CreateDirectory(directory);
            }
        }

        public static FileStream GetFileStream(string path)
        {
            MakeSureDirectory(path);
            return new FileStream(path, FileMode.OpenOrCreate);
        }

        public static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    new DirectoryInfo(path).Delete(true);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void ClearDirectory(string path)
        {
            DeleteDirectory(path);
            CreateDirectory(path);
        }

        public static string ReadTextFromFile(string path, string defaultText = "")
        {
            string ret = defaultText;

            var fi = new FileInfo(path);
            if (fi.Exists)
            {
                StreamReader reader = fi.OpenText();
                ret = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
            }

            return ret;
        }

        public static void SaveTextToFile(string text, string path)
        {
            MakeSureDirectory(path);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            SaveBytesToFile(bytes, path);
        }

        public static void SaveBytesToFile(byte[] bytes, string path)
        {
            MakeSureDirectory(path);

            try
            {
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("FileHelper.SaveBytesToFile. Exception:{0}", e.Message);
            }
        }

        public static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}
