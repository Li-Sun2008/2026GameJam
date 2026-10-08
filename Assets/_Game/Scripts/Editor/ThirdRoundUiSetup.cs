using System;
using Spotlight.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spotlight.Editor
{
    /// <summary>Prefab-only, repeatable H1 migration. Never modifies scenes or gameplay data.</summary>
    public static class ThirdRoundUiSetup
    {
        private const string TutorialPath="Assets/_Game/Prefabs/UI/Panels/UI_TutorialPanel.prefab";
        private const string EventPath="Assets/_Game/Prefabs/UI/Panels/UI_DayEventPanel.prefab";
        private const string GamePath="Assets/_Game/Prefabs/UI/UI_Game.prefab";
        private const string FontPath="Assets/_Game/Art/UI/Fonts/NotoSansCJK/NotoSansCJKsc-Regular.otf";

        [MenuItem("聚光灯/第三轮/迁移正式教程与事件面板")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("请退出 Play 并等待编译完成。");
            MigratePanel(TutorialPath,false);MigratePanel(EventPath,true);
            GameObject root=PrefabUtility.LoadPrefabContents(GamePath);
            try
            {
                GameUiReferences refs=root.GetComponent<GameUiReferences>();
                if(refs==null)throw new InvalidOperationException("UI_Game 缺少 GameUiReferences。");
                // Preserve every existing HUD reference; only bind the new optional assets and repair SpeedText.
                refs.TutorialPanelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPath).GetComponent<TutorialPanelReferences>();
                refs.DayEventPanelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(EventPath).GetComponent<DayEventPanelReferences>();
                if(refs.SpeedText==null)throw new InvalidOperationException("UI_Game 缺少 SpeedText。");
                RectTransform speed=refs.SpeedText.rectTransform;
                speed.SetParent(refs.Canvas.transform,false);
                speed.anchorMin=speed.anchorMax=new Vector2(0,1);speed.pivot=new Vector2(0,1);
                speed.anchoredPosition=new Vector2(1040,-609);speed.sizeDelta=new Vector2(220,36);
                refs.SpeedText.horizontalOverflow=HorizontalWrapMode.Wrap;refs.SpeedText.verticalOverflow=VerticalWrapMode.Truncate;
                refs.SpeedText.fontSize=18;refs.SpeedText.text="1 倍";refs.SpeedText.raycastTarget=false;
                string error;if(!refs.Validate(out error))throw new InvalidOperationException(error);
                PrefabUtility.SaveAsPrefabAsset(root,GamePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            Debug.Log("H1 面板迁移完成：保留原路径/GUID、中文字体/配色与其余 HUD 引用；未修改场景。");
        }

        private static void MigratePanel(string path,bool eventPanel)
        {
            GameObject loaded=PrefabUtility.LoadPrefabContents(path),replacement=null;
            try
            {
                // Capture the supplied artwork's font and colors before normalizing its static preview hierarchy.
                Font font=AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                if(font==null)throw new InvalidOperationException("缺少项目 Noto 中文字体。");
                Color textColor=new Color(.94f,.96f,1),cardColor=new Color(.13f,.19f,.28f),buttonColor=new Color(.21f,.34f,.46f);
                foreach(Text text in loaded.GetComponentsInChildren<Text>(true))
                    if(text.name=="TitleText"){textColor=text.color;break;}
                foreach(Button button in loaded.GetComponentsInChildren<Button>(true))
                    if(button.targetGraphic!=null){buttonColor=button.targetGraphic.color;break;}
                foreach(Image image in loaded.GetComponentsInChildren<Image>(true))
                    if(image.GetComponent<Button>()==null&&image.color.a>.5f){cardColor=image.color;break;}
                // Prefer the already normalized card on repeated runs rather than sampling its backdrop.
                foreach(Image image in loaded.GetComponentsInChildren<Image>(true))
                    if(image.name=="Card"){cardColor=image.color;break;}
                GameObject root=loaded;
                if(!(loaded.transform is RectTransform))
                {
                    // Transform cannot be replaced in-place. Save a fresh RectTransform content root at the
                    // existing asset path through PrefabUtility, which preserves the existing .meta GUID.
                    replacement=new GameObject("Root",typeof(RectTransform));root=replacement;
                }
                else
                {
                    for(int i=root.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                    foreach(Component component in root.GetComponents<Component>())
                        if(!(component is Transform))UnityEngine.Object.DestroyImmediate(component);
                }
                root.name="Root";RectTransform rect=(RectTransform)root.transform;
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                Image shade=root.AddComponent<Image>();shade.color=new Color(.02f,.04f,.08f,.88f);shade.raycastTarget=true;
                RectTransform card=Rect(root.transform,"Card",220,70,840,610);card.gameObject.AddComponent<Image>().color=cardColor;
                Text title=TextAt(card,"TitleText",eventPanel?"今日事件":"守护指南",font,textColor,30,40,20,760,48);
                ScrollRect body=Scroll(card,"BodyScroll",40,80,760,eventPanel?210:390);
                Text bodyText=TextAt(body.content,"BodyText","正式内容由当前游戏状态填入。",font,textColor,21,0,0,740,40);
                bodyText.alignment=TextAnchor.UpperLeft;
                LayoutElement bodyLayout=bodyText.gameObject.AddComponent<LayoutElement>();bodyLayout.minHeight=40;
                VerticalLayoutGroup bodyGroup=body.content.gameObject.AddComponent<VerticalLayoutGroup>();
                bodyGroup.childControlWidth=true;bodyGroup.childControlHeight=true;bodyGroup.childForceExpandWidth=true;bodyGroup.childForceExpandHeight=false;
                ContentSizeFitter bodyFit=body.content.gameObject.AddComponent<ContentSizeFitter>();bodyFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                if(eventPanel)
                {
                    ScrollRect options=Scroll(card,"OptionsScroll",40,310,760,260);options.content.name="OptionsRoot";
                    VerticalLayoutGroup group=options.content.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=10;group.childControlWidth=true;group.childControlHeight=true;group.childForceExpandWidth=true;group.childForceExpandHeight=false;
                    options.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                    Button template=MakeButton(options.content,"OptionButtonTemplate","选项",font,textColor,buttonColor,0,0,740,52);
                    LayoutElement layout=template.gameObject.AddComponent<LayoutElement>();layout.minHeight=52;
                    // A preferred-height layout permits long choices to wrap; inactive template contributes no row.
                    VerticalLayoutGroup buttonGroup=template.gameObject.AddComponent<VerticalLayoutGroup>();buttonGroup.padding=new RectOffset(12,12,8,8);buttonGroup.childForceExpandHeight=false;buttonGroup.childControlHeight=true;buttonGroup.childControlWidth=true;
                    template.gameObject.SetActive(false);
                    DayEventPanelReferences refs=root.AddComponent<DayEventPanelReferences>();refs.Root=rect;refs.TitleText=title;refs.BodyText=bodyText;refs.BodyScroll=body;refs.OptionsRoot=options.content;refs.OptionButtonTemplate=template;
                    string error;if(!refs.Validate(out error))throw new InvalidOperationException(error);
                }
                else
                {
                    Button confirm=MakeButton(card,"ConfirmButton","开始部署",font,textColor,buttonColor,270,510,300,52);
                    TutorialPanelReferences refs=root.AddComponent<TutorialPanelReferences>();refs.Root=rect;refs.TitleText=title;refs.BodyText=bodyText;refs.ConfirmButton=confirm;
                    string error;if(!refs.Validate(out error))throw new InvalidOperationException(error);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{if(replacement!=null)UnityEngine.Object.DestroyImmediate(replacement);PrefabUtility.UnloadPrefabContents(loaded);}
        }

        private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            RectTransform rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }
        private static Text TextAt(Transform parent,string name,string value,Font font,Color color,int size,float x,float y,float width,float height)
        {
            Text text=Rect(parent,name,x,y,width,height).gameObject.AddComponent<Text>();text.text=value;text.font=font;text.fontSize=size;text.color=color;text.raycastTarget=false;text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        private static Button MakeButton(Transform parent,string name,string value,Font font,Color textColor,Color color,float x,float y,float width,float height)
        {
            RectTransform rect=Rect(parent,name,x,y,width,height);Image image=rect.gameObject.AddComponent<Image>();image.color=color;
            Button button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Text label=TextAt(rect,"Label",value,font,textColor,20,12,0,width-24,height);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        private static ScrollRect Scroll(Transform parent,string name,float x,float y,float width,float height)
        {
            RectTransform root=Rect(parent,name,x,y,width,height);root.gameObject.AddComponent<Image>().color=new Color(0,0,0,.01f);
            ScrollRect scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            RectTransform viewport=Rect(root,"Viewport",0,0,width,height);viewport.gameObject.AddComponent<RectMask2D>();
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            scroll.viewport=viewport;scroll.content=Rect(viewport,"Content",0,0,width,height);scroll.verticalNormalizedPosition=1;return scroll;
        }
    }
}
