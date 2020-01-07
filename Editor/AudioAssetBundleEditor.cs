using System.IO;
using UnityEditor;
using UnityEngine;

namespace Szn.Framework.Audio.Editor
{
    public class AudioAssetBundleEditor : MonoBehaviour
    {
        private static string sourcePath = "Assets/Bundle/Audio";

        [MenuItem("Framework/AssetBundle/Build Audio")]
        private static void BuildAudio()
        {
            string streamPath = Path.Combine(Application.streamingAssetsPath,
                EditorUserBuildSettings.activeBuildTarget.ToString());
            if (Directory.Exists(streamPath)) Directory.Delete(streamPath, true);
            Directory.CreateDirectory(streamPath);

            DirectoryInfo dirInfo = new DirectoryInfo(sourcePath);
            FileInfo[] infos = dirInfo.GetFiles();
            int length = infos.Length;
            AssetBundleBuild[] levelDataList = new AssetBundleBuild[length];
            for (int i = 0; i < length; i++)
            {
                if (infos[i].Extension.Equals(".meta")) continue;

                string filename = "audio/" + Path.GetFileNameWithoutExtension(infos[i].Name).ToLower();
                AssetImporter importer = AssetImporter.GetAtPath(Path.Combine(sourcePath, infos[i].Name));
                importer.assetBundleName = filename;

                levelDataList[i] = new AssetBundleBuild
                {
                    assetBundleName = filename,
                    assetNames = new[] {importer.assetPath}
                };
            }

            BuildPipeline.BuildAssetBundles(streamPath, levelDataList,
                BuildAssetBundleOptions.ChunkBasedCompression,
                EditorUserBuildSettings.activeBuildTarget);

            AssetDatabase.Refresh();
        }
    }
}