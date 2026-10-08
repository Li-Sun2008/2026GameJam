using UnityEngine;
using UnityEngine.UI;

namespace Spotlight.Presentation
{
    public sealed class DayEventPanelReferences : MonoBehaviour
    {
        public RectTransform Root;
        public Text TitleText, BodyText;
        public ScrollRect BodyScroll;
        public RectTransform OptionsRoot;
        public Button OptionButtonTemplate;
        public bool Validate(out string error)
        {
            if(Root==null){error="缺少 Root";return false;}
            if(TitleText==null){error="缺少 TitleText";return false;}
            if(BodyText==null){error="缺少 BodyText";return false;}
            if(BodyScroll==null){error="缺少 BodyScroll";return false;}
            if(OptionsRoot==null){error="缺少 OptionsRoot";return false;}
            if(OptionButtonTemplate==null){error="缺少 OptionButtonTemplate";return false;}
            if(!PanelReferenceValidation.ValidateRoot(this,Root,out error))return false;
            foreach(Component c in new Component[]{TitleText,BodyText,BodyScroll,OptionsRoot,OptionButtonTemplate})
                if(c.transform==Root||!c.transform.IsChildOf(Root)){error=c.name+" 必须属于 Root 子层级";return false;}
            if(BodyScroll.viewport==null||BodyScroll.content==null||!BodyScroll.viewport.IsChildOf(Root)||BodyScroll.content==BodyScroll.viewport||!BodyScroll.content.IsChildOf(BodyScroll.viewport)||!BodyText.transform.IsChildOf(BodyScroll.content))
            {error="BodyScroll content/viewport 与 BodyText 层级无效";return false;}
            if(!OptionButtonTemplate.transform.IsChildOf(OptionsRoot)||OptionButtonTemplate.gameObject.activeSelf||OptionButtonTemplate.GetComponentsInChildren<Text>(true).Length!=1)
            {error="OptionButtonTemplate 必须在 OptionsRoot 内、inactive 且包含一个 Text";return false;}
            Text label=OptionButtonTemplate.GetComponentInChildren<Text>(true);
            if(label.font==null||!label.enabled||label.raycastTarget){error="OptionButtonTemplate Text 需要有效字体、enabled 并关闭 raycastTarget";return false;}
            if(OptionButtonTemplate.onClick.GetPersistentEventCount()!=0){error="OptionButtonTemplate 持久 OnClick 必须为空";return false;}
            error=null;return true;
        }
    }
}
