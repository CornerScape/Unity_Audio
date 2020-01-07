using System;
using System.Collections.Generic;
using System.IO;
using Szn.Framework.UtilPackage;
using Szn.Framework.Web;
using UnityEngine;

namespace Szn.Framework.Audio
{
    public static class AudioLoader
    {
        private static readonly string _platform;
        private static Dictionary<string, string> newestAudioInfo;

        private static Dictionary<string, string> _keyForFullname;

        static AudioLoader()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.IPhonePlayer:
                    _platform = "iOS";
                    break;

                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.Android:
                    _platform = "Android";
                    break;

                case RuntimePlatform.WebGLPlayer:
                    _platform = "WebGL";
                    break;
            }

            if (!string.IsNullOrEmpty(AudioConfig.CDN_URL_S) && !string.IsNullOrEmpty(AudioConfig.VERSION_LIST_S))
            {
                UnityWebTools.DownloadHandle(Path.Combine(AudioConfig.CDN_URL_S, AudioConfig.VERSION_LIST_S),
                    (InResult, InHandler, InMsg) =>
                    {
                        if (InResult)
                        {
                            string[] fileList = InHandler.text.Split('\n');
                            int count = fileList.Length;
                            newestAudioInfo = new Dictionary<string, string>(count);
                            for (int i = 0; i < count; i++)
                            {
                                if (string.IsNullOrEmpty(fileList[i])) continue;

                                string[] singleAudioInfo = fileList[i].Split(',');
                                newestAudioInfo.Add(singleAudioInfo[0], singleAudioInfo[1]);
                            }
                        }
                        else
                        {
                            Debug.LogError($"Download version.txt from 'CDN' error msg = {InMsg}.");
                        }
                    });
            }
        }

        public static AudioClip Load(AudioKey InAudioKey)
        {
#if UNITY_EDITOR
            if (_keyForFullname == null)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo("Assets/Bundle/Audio");
                FileInfo[] files = directoryInfo.GetFiles();
                int count = files.Length;
                _keyForFullname = new Dictionary<string, string>(count);
                for (int i = 0; i < count; i++)
                {
                    if (files[i].Extension == ".meta") continue;

                    string fullname = files[i].Name;
                    _keyForFullname.Add(Path.GetFileNameWithoutExtension(fullname), fullname);
                }
            }

            if (_keyForFullname.TryGetValue(InAudioKey.ToString(), out var filename))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(Path.Combine("Assets/Bundle/Audio/",
                    filename));
            }

            Debug.LogError($"Audio clip named '{InAudioKey}' not found in .");

            return null;
#else
            string fileName = InAudioKey.ToString();
            string bundleName = fileName.ToLower();
            string localName = MD5Tools.GetStringMd5(bundleName);
            string serverMd5 = null;

            newestAudioInfo?.TryGetValue(bundleName, out serverMd5);

            string dirPath = Path.Combine(Application.persistentDataPath, "b22f0418e8ac915eb66f829d262d14a2");
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            string localPath = Path.Combine(dirPath, localName);

            bool needSaveToLocal = false;
            byte[] fileBytes = null;
            if (File.Exists(localPath))
            {
                if (!string.IsNullOrEmpty(serverMd5))
                {
                    fileBytes = File.ReadAllBytes(localPath);
                    string localMd5 = MD5Tools.GetBytesMd5(fileBytes);
                    if (localMd5 != serverMd5)
                    {
                        UnityWebTools.DownloadHandle(Path.Combine(_cdnUrl, bundleName), (InResult, InHandler, InMsg) =>
                        {
                            if (InResult)
                            {
                                needSaveToLocal = true;
                                fileBytes = InHandler.data;
                            }
                            else
                            {
                                Debug.LogError($"Download audio from 'CDN' error msg = {InMsg}.");
                            }
                        });
                    }
                }
            }
            else
            {
#if UNITY_ANDROID
                bool isDownloadFinished = false;
                UnityWebTools.DownloadHandle(
                    Path.Combine("jar:file://" + Application.dataPath + $"!/assets/{_platform}/audio/", bundleName),
                    (InResult, InHandler, InMsg) =>
                    {
                        Debug.LogError(InResult);
                        Debug.LogError(Path.Combine("jar:file://" + Application.dataPath + $"!/assets/{_platform}/audio/", bundleName));
                        isDownloadFinished = true;
                        if (InResult)
                        {
                            needSaveToLocal = true;
                            fileBytes = InHandler.data;
                        }
                        else
                        {
                            Debug.LogError($"Download audio from 'StreamAssets' error msg = {InMsg}.");
                        }
                    });

                while (!isDownloadFinished)
                {
                }
                
                Debug.LogError(isDownloadFinished);
#elif UNITY_IOS
                fileBytes =
 File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, $"{_platform}/audio/{bundleName}"));
                needSaveToLocal = true;
#endif
            }

            if (null == fileBytes)
            {
                Debug.LogError("file bytes is null.");
                return null;
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            if (needSaveToLocal) File.WriteAllBytes(localPath, fileBytes);

            return AssetBundle.LoadFromMemory(fileBytes)?.LoadAsset<AudioClip>(fileName);
#endif
        }
    }
}