using System.Collections.Generic;
using UnityEngine;

namespace Szn.Framework.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public static AudioManager Instance
        {
            get
            {
                if (null == _instance)
                {
                    GameObject go = new GameObject(nameof(AudioManager));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioManager>();
                }

                return _instance;
            }
        }

        private readonly Dictionary<AudioKey, AudioClip> audioClipPools =
            new Dictionary<AudioKey, AudioClip>((int) AudioKey.Max, new AudioKeyEqualityComparer());

        private readonly List<AudioSource> effectAudioSourcePools =new List<AudioSource>((int)AudioKey.Max * 2);
        
        private const string MUSIC_SWITCH_PREF_KEY_S = "MusicSwitchPrefKey";
        private bool musicSwitch;

        public bool MusicSwitch
        {
            get => musicSwitch;
            set
            {
                musicSwitch = value;
                PlayerPrefs.SetInt(MUSIC_SWITCH_PREF_KEY_S, musicSwitch ? 1 : 0);
                if(musicSwitch) ResumeMusic();
                else StopMusic();
            }
        }

        private const string MUSIC_VOLUME_PREF_KEY_S = "MusicVolumePrefKey";
        private float musicVolume;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                if (value > 1)
                {
                    value = 1;
                    Debug.LogError($"The music volume can not be greater than 1.\nPlease set it within the range of [0, 1].\nIt will be set to 1.");
                }
                else if (value < 0)
                {
                    value = 0;
                    Debug.LogError($"The music volume can not be less than 0.\nPlease set it within the range of [0, 1].\nIt will be set to 0.");
                }
                musicVolume = value;
                PlayerPrefs.SetFloat(MUSIC_VOLUME_PREF_KEY_S, musicVolume);
            }
        }
        
        private const string EFFECT_SWITCH_PREF_KEY_S = "EffectSwitchPrefKey";
        private bool effectSwitch;

        public bool EffectSwitch
        {
            get => effectSwitch;
            set
            {
                effectSwitch = value;
                PlayerPrefs.SetInt(EFFECT_SWITCH_PREF_KEY_S, value ? 1 : 0);
            }
        }

        private const string EFFECT_VOLUME_PREF_KEY_S = "EffectVolumePrefKEy";
        private float effectVolume;

        public float EffectVolume
        {
            get => effectVolume;
            set
            {
                if (value > 1)
                {
                    value = 1;
                    Debug.LogError($"The sound effect volume can not be greater than 1.\nPlease set it within the range of [0, 1].\nIt will be set to 1.");
                }
                else if (value < 0)
                {
                    value = 0;
                    Debug.LogError($"The sound effect volume can not be less than 0.\nPlease set it within the range of [0, 1].\nIt will be set to 0.");
                }
                effectVolume = value;
                PlayerPrefs.SetFloat(EFFECT_VOLUME_PREF_KEY_S, effectVolume);
            }
        }
        
        [SerializeField]private AudioKey currentMusic = AudioKey.Max;

        private AudioSource musicAudioSource;
        
        private void Awake()
        {
            musicSwitch = PlayerPrefs.GetInt(MUSIC_SWITCH_PREF_KEY_S, 1) == 1;
            effectSwitch = PlayerPrefs.GetInt(EFFECT_SWITCH_PREF_KEY_S, 1) == 1;

            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_PREF_KEY_S, 1);
            effectVolume = PlayerPrefs.GetFloat(EFFECT_VOLUME_PREF_KEY_S, 1);
            
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.volume = musicVolume;
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            
        }

        public void StartUpdater()
        {
            void OnAudioResUpdateCompleted(string InMsg)
            {
                audioClipPools.Clear();
                // ReSharper disable once DelegateSubtraction
                AudioConfig.UpdateCompletedCallbackAction -= OnAudioResUpdateCompleted;
            }

            AudioConfig.UpdateCompletedCallbackAction += OnAudioResUpdateCompleted;
            AudioUpdater.Start(this);
            
        }

        private AudioSource GetEffectAudioSource(GameObject InBindGameObj = null)
        {
            if (InBindGameObj == null)
            {
                int count = effectAudioSourcePools.Count;
                int index = -1;
                for (int i = 0; i < count; i++)
                {
                    if (!effectAudioSourcePools[i].isPlaying)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    GameObject child = new GameObject(count.ToString());
                    child.transform.SetParent(transform);
                    AudioSource audioSource = child.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = false;
                    effectAudioSourcePools.Add(audioSource);
                    return audioSource;
                }

                return effectAudioSourcePools[index];
            }
            
            
            AudioSource bindAudioSource = InBindGameObj.GetComponent<AudioSource>();

            if (bindAudioSource == null)
            {
                bindAudioSource = InBindGameObj.AddComponent<AudioSource>();
                bindAudioSource.playOnAwake = false;
                bindAudioSource.loop = false;
            }

            return bindAudioSource;
        }
        
        private void StopMusic()
        {
            if(musicAudioSource.isPlaying) musicAudioSource.Stop();
        }
        
        private void ResumeMusic()
        {
            if(currentMusic != AudioKey.Max) PlayMusic(currentMusic);
        }

        public void Mute()
        {
            EffectSwitch = false;
            MusicSwitch = false;
            
            StopMusic();
            int count = effectAudioSourcePools.Count;
            for (int i = 0; i < count; i++)
            {
                effectAudioSourcePools[i].Stop();
            }
        }

        public void Resume()
        {
            MusicSwitch = true;
            EffectSwitch = true;
            ResumeMusic();
        }
        
        public void PlayMusic(AudioKey InAudioKey, bool InIsLoop = true, bool InIsRestart = false)
        {
            if (InAudioKey == AudioKey.Max)
            {
                Debug.LogError("Music audio clip can not named 'MAX'.");
                return;
            }
            
            if(!musicSwitch) return;

            musicAudioSource.loop = InIsLoop;

            if (currentMusic == InAudioKey)
            {
                if(InIsRestart) musicAudioSource.Play(0);
                return;
            }
            
            currentMusic = InAudioKey;
            if(!musicSwitch) return;

            if (!audioClipPools.TryGetValue(InAudioKey, out var audioClip))
            {
                audioClip = AudioLoader.Load(InAudioKey);
                audioClipPools.Add(InAudioKey, audioClip);
            }

            if (null == audioClip)
            {
                Debug.LogError($"Not found audio source named : {InAudioKey}");
                return;
            }

            musicAudioSource.clip = audioClip;
            musicAudioSource.Play();
        }

        public void PlayEffect(AudioKey InAudioKey, GameObject InBindGameObj = null)
        {
            if (InAudioKey == AudioKey.Max)
            {
                Debug.LogError("Effect audio clip can not named 'MAX'.");
                return;
            }
            
            if(!effectSwitch) return;

            audioClipPools.TryGetValue(InAudioKey, out var audioClip);
            if(null == audioClip)
            {
                audioClip = AudioLoader.Load(InAudioKey);
                audioClipPools.Add(InAudioKey, audioClip);
            }

            if (null == audioClip)
            {
                Debug.LogError($"Not found audio source named : {InAudioKey}");
                return;
            }

            AudioSource audioSource = GetEffectAudioSource(InBindGameObj);

            audioSource.clip = audioClip;
            audioSource.Play();
            
//            float time = audioClip.length;
//            Destroy(audioClip, time);
        }
    }
}