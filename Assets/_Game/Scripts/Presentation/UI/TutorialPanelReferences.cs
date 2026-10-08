using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Spotlight.Presentation
{
    public sealed class TutorialPanelReferences : MonoBehaviour
    {
        public RectTransform Root;
        public Text TitleText, BodyText;
        public Button ConfirmButton;
        public bool Validate(out string error)
        {
            if(Root==null) { error="缺少 Root"; return false; }
            if(TitleText==null) { error="缺少 TitleText"; return false; }
            if(BodyText==null) { error="缺少 BodyText"; return false; }
            if(ConfirmButton==null) { error="缺少 ConfirmButton"; return false; }
            if(!PanelReferenceValidation.ValidateRoot(this,Root,out error))return false;
            foreach(Component c in new Component[] {TitleText,BodyText,ConfirmButton})
                if(c.transform==Root||!c.transform.IsChildOf(Root)){error=c.name+" 必须属于 Root 子层级";return false;}
            if(ConfirmButton.onClick.GetPersistentEventCount()!=0){error="ConfirmButton 持久 OnClick 必须为空";return false;}
            error=null;return true;
        }
    }

    internal static class PanelReferenceValidation
    {
        internal static bool ValidateRoot(MonoBehaviour owner,RectTransform root,out string error)
        {
            if(root!=(owner.transform as RectTransform)){error="Root 必须是组件所在根节点的 RectTransform";return false;}
            if(owner.GetComponentsInChildren<Canvas>(true).Length!=0||owner.GetComponentsInChildren<CanvasScaler>(true).Length!=0||owner.GetComponentsInChildren<GraphicRaycaster>(true).Length!=0||owner.GetComponentsInChildren<EventSystem>(true).Length!=0)
            {error="面板不能包含 Canvas、CanvasScaler、GraphicRaycaster 或 EventSystem";return false;}
            foreach(Button button in owner.GetComponentsInChildren<Button>(true))
                if(button.onClick.GetPersistentEventCount()!=0){error=button.name+" 持久 OnClick 必须为空";return false;}
            error=null;return true;
        }
    }
}
