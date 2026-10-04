using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Spotlight.Bootstrap;
using Spotlight.Presentation;

namespace Spotlight.Editor
{
    /// <summary>生成正式UI模板，只新增缺失资产；正式场景仅修改UI入口引用。</summary>
    public static class FormalUiSetupTool
    {
        public const string PrefabPath="Assets/_Game/Prefabs/UI/UI_Game.prefab";
        public const string IntegrationScenePath="Assets/_Game/Scenes/Sandbox/P5_UI_Integration.unity";
        private const string BootstrapScenePath="Assets/_Game/Scenes/00_Bootstrap.unity";
        private const string FontPath="Assets/_Game/Art/UI/Fonts/NotoSansCJK/NotoSansCJKsc-Regular.otf";
        private static bool generating;

        [MenuItem("聚光灯/生成正式UI接入模板")]
        public static void Generate()
        {
            try{GenerateForBatch();}
            catch(Exception error){Debug.LogError("正式UI模板生成停止："+error.Message);}
        }

        public static void GenerateForBatch()
        {
            if(generating)throw new InvalidOperationException("正式UI模板正在生成。");
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("请等待编译完成并退出Play后生成。");
            // 只在隔离批处理进程用已保存入口替换启动空场景；不保存该临时Untitled。
            if(Application.isBatchMode&&SceneManager.sceneCount==1&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            {
                if(!File.Exists(BootstrapScenePath))throw new FileNotFoundException("缺少批处理生成的已保存入口场景。",BootstrapScenePath);
                EditorSceneManager.OpenScene(BootstrapScenePath,OpenSceneMode.Single);
            }
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                Scene scene=SceneManager.GetSceneAt(i);
                // Unity批处理启动自带临时Untitled；它属于本批处理进程，不是用户待保存场景。
                if(Application.isBatchMode&&string.IsNullOrEmpty(scene.path))continue;
                if(scene.isLoaded&&(scene.isDirty||string.IsNullOrEmpty(scene.path)))throw new InvalidOperationException("存在未保存场景；请先处理该场景。生成器未保存或关闭用户场景。");
            }
            generating=true;
            try
            {
                PrototypeCatalogSO catalog=AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
                if(catalog==null)throw new InvalidOperationException("缺少PrototypeCatalog，请先生成2D主程工程。");
                EnsureFolder("Assets/_Game/Prefabs/UI");EnsureFolder("Assets/_Game/Scenes/Sandbox");
                GameUiReferences ui=EnsurePrefab();string error;if(!ui.Validate(out error))throw new InvalidOperationException(error);
                CreateIntegrationScene(catalog,ui);
                AssignBootstrapUi(ui);
                AssetDatabase.SaveAssets();AssetDatabase.Refresh();
                Debug.Log("正式UI接入模板已就绪：UI_Game、P5_UI_Integration和00_Bootstrap引用已接入。现有练习UI保持原样。");
            }
            finally{generating=false;}
        }

        private static GameUiReferences EnsurePrefab()
        {
            GameObject existing=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(existing!=null)
            {
                GameUiReferences refs=existing.GetComponent<GameUiReferences>();
                if(refs==null)throw new InvalidOperationException("已有UI_Game未挂GameUiReferences；保留资产，请在Inspector补引用。");
                return refs;
            }
            if(File.Exists(PrefabPath))throw new InvalidOperationException("已有UI_Game无法读取；保留资产，不覆盖。");
            Font font=AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if(font==null)throw new InvalidOperationException("缺少正式中文字体NotoSansCJKsc-Regular.otf，请先确认字体已下载并由Unity导入。");
            Scene previous=SceneManager.GetActiveScene(),work=default(Scene);
            try
            {
                work=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(work);
                GameObject root=new GameObject("UI_Game",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameUiReferences));
                GameUiReferences refs=root.GetComponent<GameUiReferences>();refs.Canvas=root.GetComponent<Canvas>();refs.Canvas.renderMode=RenderMode.ScreenSpaceOverlay;refs.Canvas.sortingOrder=50;
                CanvasScaler scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
                Panel(root.transform,"Header",0,0,1280,90,new Color(.08f,.12f,.19f,.97f));
                Panel(root.transform,"Sidebar",1020,90,260,630,new Color(.08f,.12f,.19f,.97f));
                refs.TitleText=Label(root.transform,font,"TitleText","聚光灯",24,8,300,42,25);
                refs.DayText=Label(root.transform,font,"DayText","第1天",330,13,100,28,18);
                refs.PhaseText=Label(root.transform,font,"PhaseText","白天部署",430,13,110,28,18);
                refs.SpringHpText=Label(root.transform,font,"SpringHpText","灵泉100 / 100",550,6,230,36,20);
                refs.SpringHpBar=HealthBar(root.transform,550,47,230,8);
                refs.WaveText=Label(root.transform,font,"WaveText","波次0 · 敌人0",800,14,220,28,19);
                refs.HintText=Label(root.transform,font,"HintText","白天部署，夜晚守护灵泉。",24,54,980,26,17);
                refs.GoldText=Label(root.transform,font,"GoldText","金币0",1040,104,220,30,21);
                refs.SkillText=Label(root.transform,font,"SkillText","技能1次",1040,140,220,30,18);
                refs.SelectionText=Label(root.transform,font,"SelectionText","请选择元素或塔",1040,180,220,46,16);
                Label(root.transform,font,"HandCaption","手牌 · 点击后放置",1040,238,220,24,18);
                string[] names={"水","火","土","木","风","雷"};
                for(int i=0;i<6;i++)
                {
                    refs.ElementButtons[i]=Button(root.transform,font,"ElementButton"+i,names[i]+" × 0",1040+i%3*74,272+i/3*42,68,36);
                    refs.HandTexts[i]=refs.ElementButtons[i].GetComponentInChildren<Text>();
                }
                Label(root.transform,font,"InventoryCaption","背包",1040,360,220,24,18);
                refs.ItemButtons[0]=Button(root.transform,font,"HealButton","回复药剂 × 0",1040,392,220,36);
                refs.ItemButtons[1]=Button(root.transform,font,"TowerStackButton","塔强化 × 0",1040,434,220,36);
                for(int i=0;i<2;i++)refs.ItemTexts[i]=refs.ItemButtons[i].GetComponentInChildren<Text>();
                refs.CancelSelectionButton=Button(root.transform,font,"CancelSelectionButton","取消选择",1040,484,220,34);
                refs.PauseButton=Button(root.transform,font,"PauseButton","暂停 / 继续",1040,528,220,36);refs.PauseButtonText=refs.PauseButton.GetComponentInChildren<Text>();
                refs.NormalSpeedButton=Button(root.transform,font,"NormalSpeedButton","1倍",1040,574,106,34);
                refs.DoubleSpeedButton=Button(root.transform,font,"DoubleSpeedButton","2倍",1154,574,106,34);
                refs.SpeedText=Label(root.transform,font,"SpeedText","当前1倍",1040,611,220,22,15);
                Label(root.transform,font,"ControlsHint","左键选格 · 右侧选择行动\n先部署，再开始夜晚",1040,645,220,56,16);
                refs.ActionsRoot=FullScreen(root.transform,"ActionsRoot");
                refs.ModalRoot=FullScreen(root.transform,"ModalRoot");refs.ModalRoot.SetAsLastSibling();
                string error;if(!refs.Validate(out error))throw new InvalidOperationException(error);
                GameObject prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);UnityEngine.Object.DestroyImmediate(root);
                if(prefab==null)throw new IOException("UI_Game预制体保存失败。");return prefab.GetComponent<GameUiReferences>();
            }
            finally
            {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                if(work.IsValid()&&work.isLoaded)EditorSceneManager.CloseScene(work,true);
            }
        }

        /// <summary>仅用于修复本次生成模板的血条尺寸，保留已有预制体引用与其它布局。</summary>
        public static void RepairHealthBarLayoutForBatch()
        {
            GameObject root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                GameUiReferences refs=root.GetComponent<GameUiReferences>();
                if(refs==null||refs.SpringHpBar==null||refs.SpringHpBar.fillRect==null)throw new InvalidOperationException("UI_Game未绑定灵泉血条，无法定向修复。");
                Slider slider=refs.SpringHpBar;RectTransform fill=slider.fillRect;
                fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=Vector2.zero;fill.offsetMax=Vector2.zero;
                float value=slider.value;slider.SetValueWithoutNotify(value==slider.minValue?slider.maxValue:slider.minValue);slider.SetValueWithoutNotify(value);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        /// <summary>仅修复本次生成模板的标题和灵泉栏行高，保留字体及Truncate策略。</summary>
        public static void RepairInitialHeaderLayoutForBatch()
        {
            GameObject root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                GameUiReferences refs=root.GetComponent<GameUiReferences>();
                if(refs==null||refs.TitleText==null||refs.SpringHpText==null||refs.SpringHpBar==null)throw new InvalidOperationException("UI_Game未绑定标题或灵泉栏，无法定向修复。");
                refs.TitleText.rectTransform.anchoredPosition=new Vector2(24,-8);refs.TitleText.rectTransform.sizeDelta=new Vector2(300,42);
                refs.SpringHpText.rectTransform.anchoredPosition=new Vector2(550,-6);refs.SpringHpText.rectTransform.sizeDelta=new Vector2(230,36);
                RectTransform bar=refs.SpringHpBar.GetComponent<RectTransform>();bar.anchoredPosition=new Vector2(550,-47);bar.sizeDelta=new Vector2(230,8);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        private static void CreateIntegrationScene(PrototypeCatalogSO catalog,GameUiReferences ui)
        {
            if(File.Exists(IntegrationScenePath))return;
            Scene previous=SceneManager.GetActiveScene(),scene=default(Scene);
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
                GameObject cameraObject=new GameObject("WorldCamera");cameraObject.tag="MainCamera";cameraObject.transform.position=new Vector3(0,0,-10);Camera camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();camera.orthographic=true;camera.orthographicSize=5.5f;camera.backgroundColor=new Color(.07f,.09f,.14f);
                GameBootstrap bootstrap=new GameObject("GameBootstrap").AddComponent<GameBootstrap>();bootstrap.CatalogAsset=catalog;bootstrap.WorldCamera=camera;bootstrap.UiPrefab=ui;
                if(!EditorSceneManager.SaveScene(scene,IntegrationScenePath))throw new IOException("P5_UI_Integration保存失败。");
            }
            finally
            {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            }
        }

        private static void AssignBootstrapUi(GameUiReferences ui)
        {
            if(!File.Exists(BootstrapScenePath))throw new FileNotFoundException("正式入口场景尚未生成。",BootstrapScenePath);
            Scene previous=SceneManager.GetActiveScene(),scene=default(Scene);bool opened=false;
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).path==BootstrapScenePath)scene=SceneManager.GetSceneAt(i);
            try
            {
                if(!scene.IsValid()){scene=EditorSceneManager.OpenScene(BootstrapScenePath,OpenSceneMode.Additive);opened=true;}
                GameBootstrap found=null;foreach(GameObject root in scene.GetRootGameObjects())foreach(GameBootstrap candidate in root.GetComponentsInChildren<GameBootstrap>(true))
                {if(found!=null)throw new InvalidOperationException("00_Bootstrap有多个GameBootstrap；保持场景不变，请检查入口。");found=candidate;}
                if(found==null)throw new InvalidOperationException("00_Bootstrap缺少GameBootstrap。");
                if(found.UiPrefab!=ui){found.UiPrefab=ui;EditorUtility.SetDirty(found);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene,BootstrapScenePath))throw new IOException("00_Bootstrap引用保存失败。");}
            }
            finally
            {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            }
        }

        private static void EnsureFolder(string path)
        {
            if(path=="Assets"||AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            if(Directory.Exists(path))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);else AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            GameObject go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }
        private static RectTransform FullScreen(Transform parent,string name)
        {
            RectTransform rect=Rect(parent,name,0,0,0,0);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static void Panel(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>().color=color;
        }
        private static Text Label(Transform parent,Font font,string name,string value,float x,float y,float width,float height,int size)
        {
            Text text=Rect(parent,name,x,y,width,height).gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=new Color(.94f,.96f,1);text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        private static Button Button(Transform parent,Font font,string name,string value,float x,float y,float width,float height)
        {
            RectTransform rect=Rect(parent,name,x,y,width,height);Image image=rect.gameObject.AddComponent<Image>();image.color=new Color(.21f,.34f,.46f);Button button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Text label=Label(rect,font,"Label",value,5,0,width-10,height,17);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        private static Slider HealthBar(Transform parent,float x,float y,float width,float height)
        {
            RectTransform rect=Rect(parent,"SpringHpBar",x,y,width,height);Image background=rect.gameObject.AddComponent<Image>();background.color=new Color(.1f,.18f,.23f);background.raycastTarget=false;
            RectTransform fill=FullScreen(rect,"Fill");Image image=fill.gameObject.AddComponent<Image>();image.color=new Color(.2f,.8f,1);image.raycastTarget=false;
            Slider slider=rect.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.minValue=0;slider.maxValue=100;slider.value=100;slider.interactable=false;Navigation navigation=new Navigation();navigation.mode=Navigation.Mode.None;slider.navigation=navigation;return slider;
        }
    }
}
