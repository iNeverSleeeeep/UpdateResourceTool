#if UNITY_EDITOR
using Nirvana;
using SharpJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace URT
{
    class URTTest
    {
        private static readonly string ServerUrl = @"http://172.0.6.130/init-query-qa.php";
        private static readonly string FakeServerUrl = @"http://172.0.6.130/init-query-qa-fake.php";
        private static readonly string UpdateUrl = @"http://172.0.6.130/ug01_cn/Android/AssetBundle";

        private static string UPDATE_PATH;
        private static string UPDATE_CACHE_PATH;

        public void InitPath()
        {
            URTConfig.UPDATE_PATH = Application.dataPath + "/../URTTest/Data";
            URTConfig.UPDATE_CACHE_PATH = Application.dataPath + "/../URTTest/Cache";
            FileHelper.MakeSureDirectory(URTConfig.UPDATE_PATH);
            FileHelper.MakeSureDirectory(URTConfig.UPDATE_CACHE_PATH);
        }

         [UnityTest]
        public IEnumerator GetServerConfigTest()
        {
            var success = false;
            URTEvent.RemoteTextDelegate func = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                success = true;
            };
            var worker = new GetRemoteTextWorker(ServerUrl, URTConfig.WEB_REQUEST_TIMEOUT, func);
            Boss.Instance.AddWorker(Project.RemoteText, worker);
            Boss.Instance.StartProject(Project.RemoteText);
            yield return new WaitWithTimeout(()=> !success, URTConfig.WEB_REQUEST_TIMEOUT + 1);
            Assert.AreEqual(success, true);
            Boss.Instance.StopAllProject();
        }

        [UnityTest]
        public IEnumerator GetServerConfigFailTest()
        {
            var success = false;
            URTEvent.RemoteTextDelegate func = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.ERROR);
                Assert.AreEqual(message, "404");
                success = true;
            };
            var worker = new GetRemoteTextWorker(FakeServerUrl, URTConfig.WEB_REQUEST_TIMEOUT, func);
            Boss.Instance.AddWorker(Project.RemoteText, worker);
            Boss.Instance.StartProject(Project.RemoteText);
            yield return new WaitWithTimeout(() => !success, URTConfig.WEB_REQUEST_TIMEOUT + 1);
            Assert.AreEqual(success, true);
            Boss.Instance.StopAllProject();
        }

        [UnityTest]
        public IEnumerator GetServerMD5Test()
        {
            int success = 0;

            URTEvent.RemoteTextDelegate OnServerMD5 = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                success++;
            };

            URTEvent.RemoteTextDelegate OnServerInfo = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                var json = JsonDecoder.DecodeText(message) as IDictionary<string, object>;
                var param_list = json["param_list"] as IDictionary<string, object>;
                var update_url = param_list["update_url"] as string;
                success++;

                Boss.Instance.StopProject(Project.RemoteText);
                var md5Worker = new GetRemoteTextWorker(UpdateUrl + "/file_info.txt", 3, OnServerMD5);
                Boss.Instance.AddWorker(Project.RemoteText, md5Worker);
                Boss.Instance.StartProject(Project.RemoteText);
            };



            var worker = new GetRemoteTextWorker(ServerUrl, 3, OnServerInfo);
            Boss.Instance.AddWorker(Project.RemoteText, worker);
            Boss.Instance.StartProject(Project.RemoteText);
            yield return new WaitWithTimeout(() => success < 2, URTConfig.WEB_REQUEST_TIMEOUT + 1);
            Assert.AreEqual(success, 2);
            Boss.Instance.StopAllProject();
        }

        UpdateAssetBundleProject CurrentProject;
        [UnityTest]
        public IEnumerator DownloadTest()
        {
            URTConfig.DEBUG_DOWNLOAD_FAIL = 0;
            int success = 0;
            Coroutine coroutine = null;
            InitPath();
            PlayerPrefs.SetString(URTConfig.PREF_APP_UPDATECACHE_VERSION, "0.0.0.0");
            FileHelper.DeleteFile(URTConfig.UPDATE_PATH + "/" + URTConfig.MD5_FILENAME);

            URTEvent.ProjectCompleteDelegate OnUpdateProjectComplete = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                success = 4;
            };

            URTEvent.ConfirmDelegate OnConfirmUpdate = (Action ok, Action cancel, object[] args) =>
            {
                Debug.Log(string.Format("Confirm: {0} {1} {2} {3}", args[0], args[1], args[2], args[3]));
                success++;
                ok();
            };
            URTEvent.ConfirmUpdateEvent = OnConfirmUpdate;
            URTEvent.UpdateProjectCompleteEvent = OnUpdateProjectComplete;

            string md5 = null;
            URTEvent.RemoteTextDelegate OnServerMD5 = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                success++;

                md5 = message;
                CurrentProject = new UpdateAssetBundleProject();
                CurrentProject.Prepare(message, new List<string>() { UpdateUrl });
                var title = string.Format("下载测试 总大小{0}", FormatByte(CurrentProject.TotalSize));
                coroutine = Scheduler.RunCoroutine(ShowProcess(CurrentProject, title));
            };

            URTEvent.RemoteTextDelegate OnServerInfo = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                var json = JsonDecoder.DecodeText(message) as IDictionary<string, object>;
                var param_list = json["param_list"] as IDictionary<string, object>;
                var update_url = param_list["update_url"] as string;
                success++;

                Boss.Instance.StopProject(Project.RemoteText);
                var md5Worker = new GetRemoteTextWorker(UpdateUrl + "/file_info.txt", 3, OnServerMD5);
                Boss.Instance.AddWorker(Project.RemoteText, md5Worker);
                Boss.Instance.StartProject(Project.RemoteText);
            };
            
            var worker = new GetRemoteTextWorker(ServerUrl, 3, OnServerInfo);
            Boss.Instance.AddWorker(Project.RemoteText, worker);
            Boss.Instance.StartProject(Project.RemoteText);
            yield return new WaitWithTimeout(() => success < 4, URTConfig.WEB_REQUEST_TIMEOUT + 20);
            Assert.AreEqual(success, 4);
            Boss.Instance.StopAllProject();
            if (coroutine != null)
                Scheduler.KillCoroutine(coroutine);
            yield return null;
            foreach (var file in CheckFiles(md5))
            {
                Debug.LogWarning("File Size Error " + file);
            }
            Assert.AreEqual(CheckFiles(md5).Count, 0);
            EditorUtility.ClearProgressBar();
        }
        [UnityTest]
        public IEnumerator ContinueDownloadTest()
        {
            URTConfig.DEBUG_DOWNLOAD_FAIL = 0.5;
            int success = 0;
            Coroutine coroutine = null;
            InitPath();
            PlayerPrefs.SetString(URTConfig.PREF_APP_UPDATECACHE_VERSION, "0.0.0.0");
            FileHelper.DeleteFile(URTConfig.UPDATE_PATH + "/" + URTConfig.MD5_FILENAME);

            URTEvent.ProjectCompleteDelegate OnUpdateProjectComplete = (ErrorCode code, string message) =>
            {
                Debug.Log("OnUpdateProjectComplete " + code);
                success = 4;
            };

            URTEvent.ConfirmDelegate OnConfirmUpdate = (Action ok, Action cancel, object[] args) =>
            {
                Debug.Log(string.Format("Confirm: {0} {1} {2} {3}", args[0], args[1], args[2], args[3]));
                success++;
                ok();
            };
            URTEvent.ConfirmUpdateEvent = OnConfirmUpdate;
            URTEvent.UpdateProjectCompleteEvent = OnUpdateProjectComplete;

            string md5 = null;
            URTEvent.RemoteTextDelegate OnServerMD5 = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                success++;

                md5 = message;
                CurrentProject = new UpdateAssetBundleProject();
                CurrentProject.Prepare(message, new List<string>() { UpdateUrl });
                var title2 = string.Format("下载测试 总大小{0}", FormatByte(CurrentProject.TotalSize));
                coroutine = Scheduler.RunCoroutine(ShowProcess(CurrentProject, title2));
            };

            URTEvent.RemoteTextDelegate OnServerInfo = (ErrorCode code, string message) =>
            {
                Assert.AreEqual(code, ErrorCode.SUCCESS);
                var json = JsonDecoder.DecodeText(message) as IDictionary<string, object>;
                var param_list = json["param_list"] as IDictionary<string, object>;
                var update_url = param_list["update_url"] as string;
                success++;

                Boss.Instance.StopProject(Project.RemoteText);
                var md5Worker = new GetRemoteTextWorker(UpdateUrl + "/file_info.txt", 3, OnServerMD5);
                Boss.Instance.AddWorker(Project.RemoteText, md5Worker);
                Boss.Instance.StartProject(Project.RemoteText);
            };

            var worker = new GetRemoteTextWorker(ServerUrl, 3, OnServerInfo);
            Boss.Instance.AddWorker(Project.RemoteText, worker);
            Boss.Instance.StartProject(Project.RemoteText);
            Debug.Log("StartProject");
            yield return new WaitWithTimeout(() => success < 4, URTConfig.WEB_REQUEST_TIMEOUT + 20);
            Debug.Log("StartProject Finish");
            Assert.AreEqual(success, 4);
            Boss.Instance.StopAllProject();
            if (coroutine != null)
                Scheduler.KillCoroutine(coroutine);
            
            EditorUtility.ClearProgressBar();
            yield return null;
            yield return null;
            URTConfig.DEBUG_DOWNLOAD_FAIL = 0;

            CurrentProject.Retry();
            Debug.Log("StartProject Retry " + CurrentProject.Process);
            var title = string.Format("断点续传测试 总大小:{0} 总剩余:{1}", FormatByte(CurrentProject.TotalSize), FormatByte(CurrentProject.TotalSize - CurrentProject.CurrentSize));
            coroutine = Scheduler.RunCoroutine(ShowProcess(CurrentProject, title));
            yield return new WaitWithTimeout(() => CurrentProject.Process < 1, URTConfig.WEB_REQUEST_TIMEOUT + 20);
            if (coroutine != null)
                Scheduler.KillCoroutine(coroutine);
            EditorUtility.ClearProgressBar();
            yield return null;
            foreach (var file in CheckFiles(md5))
            {
                Debug.LogWarning("File Size Error " + file);
            }
            Assert.AreEqual(CheckFiles(md5).Count, 0);
            Debug.Log("End! " + CurrentProject.Process);
        }

        private List<string> CheckFiles(string md5)
        {
            var files = new List<string>();
            var abFileInfos = MD5Helper.GetMD5DictionaryFromTextDummy(md5);
            foreach (var fileInfo in abFileInfos)
            {
                var size = FileHelper.FileSize(URTConfig.UPDATE_CACHE_PATH + "/" + fileInfo.Key);
                if (size != fileInfo.Value.compressedSize)
                {
                    files.Add(string.Format("File:{0} ServerSize:{1} LocalSize:{2}", fileInfo.Key, fileInfo.Value.compressedSize, size));
                }
            }
            return files;
        }

        private IEnumerator ShowProcess(UpdateAssetBundleProject project, string title)
        {
            while (true)
            {
                var process = project.Process;
                EditorUtility.DisplayProgressBar(title, string.Format("当前进度：{0:P}({1}) 下载失败个数:{2}", process, FormatByte(project.CurrentSize), project.FailCount), process);
                if (process >= 1)
                    break;
                yield return null;
            }
            EditorUtility.ClearProgressBar();
        }

        public static string FormatByte(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes.ToString() + "B";
            }

            long num = bytes / 1024;
            if (num < 1024)
            {
                return string.Format("{0:F2}KB", bytes * 1.0f / 1024);
            }

            return string.Format("{0:F2}MB", bytes * 1.0f / 1048576);

        }
    }

    public class WaitWithTimeout : CustomYieldInstruction
    {
        float timeout;
        Func<bool> func;

        public override bool keepWaiting
        {
            get
            {
                timeout -= Time.deltaTime;
                if (timeout < 0)
                    return false;
                return func();
            }
        }

        public WaitWithTimeout(Func<bool> func, float timeout)
        {
            this.func = func;
            this.timeout = timeout;
        }
    }
}

#endif
