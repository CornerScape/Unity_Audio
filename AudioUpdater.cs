using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Szn.Framework.UtilPackage;
using UnityEngine;

namespace Szn.Framework.Audio
{
    public static class AudioUpdater
    {
        // Start is called before the first frame update
        public static void Start()
        {
            if(string.IsNullOrEmpty(AudioConfig.CDN_URL_S) || string.IsNullOrEmpty(AudioConfig.VERSION_LIST_S)) return;
            
            
            string localPath = Path.Combine(Application.persistentDataPath,
                $"{AudioConfig.Platform}/b22f0418e8ac915eb66f829d262d14a2/");

            string streamPath = Path.Combine(Application.streamingAssetsPath, $"{AudioConfig.Platform}/Audio/");
            
            
        }
    }
}