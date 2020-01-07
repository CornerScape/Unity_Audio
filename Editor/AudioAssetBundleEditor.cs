using System.Collections.Generic;
using System.IO;
using Szn.Framework.UtilPackage;
using UnityEditor;
using UnityEngine;

namespace Szn.Framework.Audio.Editor
{
    public class AudioAssetBundleEditor : MonoBehaviour
    {
        private static string sourcePath = "Assets/Bundle/Audio";

        private struct BuildInfo
        {
            private readonly string name;
            private readonly string md5;

            public BuildInfo(string InName, string InMD5)
            {
                name = InName;
                md5 = InMD5;
            }

            public override string ToString()
            {
                return $"{name},{md5}";
            }
        }

        [MenuItem("Framework/AssetBundle/Build Audio")]
        private static void BuildAudio()
        {
            string streamPath = Path.Combine(Application.streamingAssetsPath,
                EditorUserBuildSettings.activeBuildTarget.ToString(), "Audio");
            if (Directory.Exists(streamPath)) Directory.Delete(streamPath, true);
            Directory.CreateDirectory(streamPath);

            DirectoryInfo dirInfo = new DirectoryInfo(sourcePath);
            FileInfo[] infos = dirInfo.GetFiles();
            int length = infos.Length;
            AssetBundleBuild[] levelDataList = new AssetBundleBuild[length];
            for (int i = 0; i < length; i++)
            {
                if (infos[i].Extension.Equals(".meta")) continue;

                string filename = Path.GetFileNameWithoutExtension(infos[i].Name).ToLower();
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

            WriteVersion(streamPath);
            
            AssetDatabase.Refresh();
        }

        private static void WriteVersion(string InStreamPath)
        {
            FileInfo[] buildFileInfos = new DirectoryInfo(InStreamPath).GetFiles();

            int len = buildFileInfos.Length;

            List<BuildInfo> buildInfos = new List<BuildInfo>();

            for (int i = 0; i < len; ++i)
            {
                if (buildFileInfos[i].Extension == ".manifest" || buildFileInfos[i].Extension == ".meta")
                {
                    buildFileInfos[i].Delete();
                    continue;
                }

                if (buildFileInfos[i].Name == "Audio")
                {
                    buildFileInfos[i].Delete();
                    continue;
                }

                Debug.LogError(
                    $"{buildFileInfos[i].Name},{MD5Tools.GetFileMd5(Path.Combine(InStreamPath, buildFileInfos[i].Name))}");

                buildInfos.Add(new BuildInfo(buildFileInfos[i].Name,
                    MD5Tools.GetFileMd5(Path.Combine(InStreamPath, buildFileInfos[i].Name))));
            }


            using (FileStream fs = new FileStream(Path.Combine(InStreamPath, "Version.txt"), FileMode.Create,
                FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (BuildInfo buildInfo in buildInfos)
                    {
                        sw.WriteLine(buildInfo.ToString());
                    }

                    sw.Flush();
                    sw.Close();
                }

                fs.Close();
            }
        }
    }
}