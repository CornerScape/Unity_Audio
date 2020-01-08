using UnityEngine;
using UnityEngine.UI;

namespace Szn.Framework.Audio
{
    [ExecuteInEditMode]
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField, Header("请选择点击该按钮要播放的音效片段~")]
        private AudioKey audioKey;


        private void Awake()
        {
            Button button = GetComponent<Button>();

            if (null == button)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("Error", "该脚本必须挂在有Button组件的物体上！", "确定");
                DestroyImmediate(this);
#endif
                return;
            }
            
            button.onClick.AddListener(()=>{AudioManager.Instance.PlayEffect(audioKey);});
        }
    }
}