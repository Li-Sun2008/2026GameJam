using UnityEngine;
using UnityEngine.UI;
namespace Spotlight.Presentation
{
    /// <summary>Inspector exercise only. This component never obtains gameplay services.</summary>
    public sealed class ButtonPractice : MonoBehaviour
    {
        [Tooltip("将练习按钮拖到这里。")]
        public Button PracticeButton;
        [Tooltip("将提示文字拖到这里。")]
        public Text HintText;
        public string ClickHint = "按钮连接成功！";
        private Button subscribed;
        private void OnEnable()
        {
            subscribed=PracticeButton;
            if(subscribed!=null)subscribed.onClick.AddListener(ShowHint);
        }
        private void OnDisable()
        {
            if(subscribed!=null)subscribed.onClick.RemoveListener(ShowHint);
            subscribed=null;
        }
        public void ShowHint()
        {
            if(HintText!=null)HintText.text=ClickHint;
        }
    }
}
