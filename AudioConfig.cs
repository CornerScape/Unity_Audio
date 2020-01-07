using UnityEngine;

namespace Szn.Framework.Audio
{
    public static class AudioConfig
    {
        public static readonly string CDN_URL_S = "http://shangke-file.oss-cn-beijing.aliyuncs.com/Audio/Audio/";
        public static readonly string VERSION_LIST_S = "Version.txt";
        public const bool USE_ASSET_BUNDLE_IN_EDITOR_B = true;
        public static string Platform { get; }

        static AudioConfig()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.WindowsEditor:
#if UNITY_EDITOR
                    switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
                    {
                        case UnityEditor.BuildTarget.StandaloneWindows:
                            Platform = "Win";
                            break;

                        case UnityEditor.BuildTarget.iOS:
                            Platform = "iOS";
                            break;

                        case UnityEditor.BuildTarget.Android:
                            Platform = "Android";
                            break;
                    }
#endif
                    break;

                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.IPhonePlayer:
                    Platform = "iOS";
                    break;

                case RuntimePlatform.WindowsPlayer:
                    Platform = "Win";
                    break;

                case RuntimePlatform.Android:
                    Platform = "Android";
                    break;
            }

            Platform = "Unknown";
        }

    }
}