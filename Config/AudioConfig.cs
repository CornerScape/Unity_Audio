using System;

namespace Szn.Framework.Audio
{
    public static class AudioConfig
    {
        //local audio clips resources path
        public const string AUDIO_RES_ASSETS_PATH_S = "Assets/ThirdPartyPlugins/AudioManager/Sample/Bundle/Audio/";
        //server audio clips resources path
        public const string CDN_URL_S = "http://shangke-file.oss-cn-beijing.aliyuncs.com/SznTest/";
        //version.txt name convert to md5 string
        public const string VERSION_LIST_S = "45264b0d287afd9795f479a7882b3765";
        //set true use original audio clips, set false use asset bundle in editor.
        public const bool USE_ASSET_BUNDLE_IN_EDITOR_B = true;

        //update audio clips progress and msg
        public static Action<float, string> UpdateProgressAction;
        //call when update completed.
        public static Action<string> UpdateCompletedCallbackAction;
    }
    
    //all audio clips name
    //these name must be the file names of the original audio clip name in AUDIO_RES_ASSETS_PATH_S
    //Max can not be deleted and Must be the last one, because it identifies the number of audio clip files.
    public enum AudioKey
    {
        Piano,
        Piano26,
        Piano27,
        Bird02,
        Bird03,
        Bird05,
        Max    
    }
    

}