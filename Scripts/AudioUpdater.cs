using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Szn.Framework.UtilPackage;
using UnityEngine;
using UnityEngine.Networking;

namespace Szn.Framework.Audio
{
    public static class AudioUpdater
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            AudioManager.Instance.StartUpdater();
        }

        private struct AudioResInfo
        {
            public string Md5 { get; private set; }

            public bool NeedUpdate { get; private set; }

            public AudioResInfo(string InMd5, bool InNeedUpdate)
            {
                Md5 = InMd5;
                NeedUpdate = InNeedUpdate;
            }

            public void UpdateMd5(string InMd5)
            {
                Md5 = InMd5;
                NeedUpdate = true;
            }
        }

        public static void Start(MonoBehaviour InBindMono)
        {
            AudioConfig.UpdateProgressAction?.Invoke(0.5f, "Update audio resources start...");

            if (string.IsNullOrEmpty(AudioConfig.CDN_URL_S) || string.IsNullOrEmpty(AudioConfig.VERSION_LIST_S)) return;
            string serverPath = Path.Combine(AudioConfig.CDN_URL_S, UnityPathTools.GetPlatform(), "Audio");
            string localPath = UnityPathTools.GetPersistentDataPath("b22f0418e8ac915eb66f829d262d14a2");

            if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);

            AudioConfig.UpdateProgressAction?.Invoke(.05f, "Connect to resources server...");
            InBindMono.StartCoroutine(Download(Path.Combine(serverPath, AudioConfig.VERSION_LIST_S),
                (InResult, InHandler, InMsg) =>
                {
                    if (!InResult)
                    {
                        AudioConfig.UpdateCompletedCallbackAction?.Invoke("No resources need to be updated...");
                        return;
                    }

                    AudioConfig.UpdateProgressAction?.Invoke(.05f, "Analysis server file list...");
                    string fileContent = InHandler.text;

                    if (string.IsNullOrEmpty(fileContent))
                    {
                        AudioConfig.UpdateCompletedCallbackAction?.Invoke("No resources need to be updated...");
                        return;
                    }

                    string localVersionPath = Path.Combine(localPath, AudioConfig.VERSION_LIST_S);
                    InBindMono.StartCoroutine(LoadLocalVersion(InBindMono,
                        localVersionPath,
                        InLocalVersionDict =>
                        {
                            AudioConfig.UpdateProgressAction?.Invoke(.1f, "Analysis local file list...");

                            string[] fileList = fileContent.Split('\n');
                            int len = fileList.Length;
                            int needUpdateFile = 0;
                            for (int i = 0; i < len; i++)
                            {
                                if (string.IsNullOrEmpty(fileList[i])) continue;
                                string[] info = fileList[i].Split(',');

                                if (InLocalVersionDict.TryGetValue(info[0], out var resInfo))
                                {
                                    if (resInfo.Md5 != info[1])
                                    {
                                        InLocalVersionDict[info[0]].UpdateMd5(info[1]);
                                        ++needUpdateFile;
                                    }
                                }
                                else
                                {
                                    ++needUpdateFile;
                                    InLocalVersionDict.Add(info[0], new AudioResInfo(info[1], true));
                                }
                            }

                            if (needUpdateFile > 0)
                            {
                                InBindMono.StartCoroutine(DownloadList(serverPath, localPath,
                                    localVersionPath, InLocalVersionDict,
                                    () => { AudioConfig.UpdateProgressAction?.Invoke(.05f, "Update completed..."); }));
                            }
                            else
                            {
                                AudioConfig.UpdateCompletedCallbackAction?.Invoke(
                                    "Local resources are already up to date...");
                            }
                        }));
                }));
        }

        private static IEnumerator Download(string InUrl, Action<bool, DownloadHandler, string> InCallback)
        {
            if (null == InCallback) yield break;

            using (UnityWebRequest request = UnityWebRequest.Get(InUrl))
            {
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    InCallback.Invoke(false, null,
                        $"is network error = {request.isNetworkError}\nis http error = {request.isHttpError}\nmsg = {request.error}");
                    yield break;
                }

                InCallback.Invoke(true, request.downloadHandler, null);
            }
        }

        private static IEnumerator DownloadList(string InServerPath, string InLocalPath,
            string InVersionPath,
            Dictionary<string, AudioResInfo> InFileDict,
            Action InCompletedAction)
        {
            int count = InFileDict.Count;
            float delta = .7f / count;
            using (FileStream fs = new FileStream(InVersionPath, FileMode.Open, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (KeyValuePair<string, AudioResInfo> keyValuePair in InFileDict)
                    {
                        sw.WriteLine($"{keyValuePair.Key},{keyValuePair.Value.Md5}");
                        if (keyValuePair.Value.NeedUpdate)
                        {
                            using (UnityWebRequest request =
                                UnityWebRequest.Get(Path.Combine(InServerPath, keyValuePair.Key)))
                            {
                                yield return request.SendWebRequest();

                                if (request.isNetworkError || request.isHttpError)
                                {
                                    Debug.LogError(
                                        $"Download audio resource {keyValuePair.Key} error.\n is network error = {request.isNetworkError}\nis http error = {request.isHttpError}\nmsg = {request.error}");
                                }
                                else
                                {
                                    File.WriteAllBytes(Path.Combine(InLocalPath, keyValuePair.Key),
                                        request.downloadHandler.data);
                                }
                            }
                        }

                        AudioConfig.UpdateProgressAction?.Invoke(delta, "Downloading");
                    }

                    sw.Flush();
                    sw.Close();
                }

                fs.Close();
            }

            InCompletedAction?.Invoke();
        }

        private static IEnumerator LoadLocalVersion(MonoBehaviour InBindMono, string InPath,
            Action<Dictionary<string, AudioResInfo>> InResultAction)
        {
            if (null == InResultAction) yield break;

            Dictionary<string, AudioResInfo> versionDict = new Dictionary<string, AudioResInfo>((int) AudioKey.Max);

            if (File.Exists(InPath))
            {
                using (FileStream fs = new FileStream(InPath, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string content;
                        while (!string.IsNullOrEmpty(content = sr.ReadLine()))
                        {
                            string[] info = content.Split(',');
                            versionDict.Add(info[0], new AudioResInfo(info[1], false));
                        }

                        sr.Close();
                    }

                    fs.Close();
                }

                InResultAction.Invoke(versionDict);
            }
            else
            {
                InBindMono.StartCoroutine(Download(
                    UnityPathTools.GetStreamingAssetsUrl($"Audio/{AudioConfig.VERSION_LIST_S}"),
                    (InResult, InHandler, InMsg) =>
                    {
                        using (FileStream fs = new FileStream(InPath, FileMode.Create, FileAccess.Write))
                        {
                            if (InResult)
                            {
                                string content = InHandler.text;
                                if (!string.IsNullOrEmpty(content))
                                {
                                    string[] lines = content.Split('\n');
                                    int len = lines.Length;
                                    using (StreamWriter sw = new StreamWriter(fs))
                                    {
                                        for (int i = 0; i < len; i++)
                                        {
                                            if (string.IsNullOrEmpty(lines[i])) continue;

                                            sw.WriteLine(lines[i]);

                                            string[] info = lines[i].Split(',');
                                            versionDict.Add(info[0], new AudioResInfo(info[1], false));
                                        }

                                        sw.Flush();
                                        sw.Close();
                                    }
                                }
                            }

                            fs.Close();
                        }

                        InResultAction.Invoke(versionDict);
                    }));
            }
        }
    }
}