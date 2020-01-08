using System.IO;
using UnityEngine;
using Szn.Framework.UtilPackage;
#if UNITY_EDITOR
using System.Collections.Generic;

#endif

namespace Szn.Framework.Audio
{
    public static class AudioLoader
    {
#if UNITY_EDITOR
        private static Dictionary<string, string> _keyForFullname;
#endif

        public static AudioClip Load(AudioKey InAudioKey)
        {
#if UNITY_EDITOR
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!AudioConfig.USE_ASSET_BUNDLE_IN_EDITOR_B)
            {
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
            }
#endif
            string fileName = InAudioKey.ToString();
            string bundleName = fileName.ToLower();

            string localPath =
                UnityPathTools.GetPersistentDataPath(
                    $"b22f0418e8ac915eb66f829d262d14a2/{MD5Tools.GetStringMd5(bundleName)}");

            string streamPath = UnityPathTools.GetStreamingAssetsPath($"Audio/{MD5Tools.GetStringMd5(bundleName)}");

            AssetBundle ab = AssetBundle.LoadFromFile(File.Exists(localPath) ? localPath : streamPath);

            if (ab == null) return null;

            AudioClip ac = ab.LoadAsset<AudioClip>(fileName);

            ab.Unload(false);

            return ac;
        }
    }
}