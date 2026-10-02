using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Spotlight.Bootstrap;
namespace Spotlight.Editor
{
    /// <summary>在独立附加场景中生成缺失文件，绝不保存或关闭用户正在编辑的场景。</summary>
    [InitializeOnLoad]
    public static class ProjectSetupTool
    {
        const string SceneRoot="Assets/_Game/Scenes/";
        const string PrefabRoot="Assets/_Game/Prefabs/Prototype/";
        const string SpritePath="Assets/_Game/Art/Prototype/WhiteSquare.png";
        const string MaterialPath="Assets/_Game/Art/Prototype/SpriteUnlit.mat";
        const string SessionKey="Spotlight.ProjectSetup.Checked.V2";
        static bool generating;
        static ProjectSetupTool(){EditorApplication.update+=AutoGenerate;}
        static void AutoGenerate()
        {
            if(generating||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||HasUnsavedScene())return;
            if(SessionState.GetBool(SessionKey,false)){EditorApplication.update-=AutoGenerate;return;}
            SessionState.SetBool(SessionKey,true);EditorApplication.update-=AutoGenerate;
            if(!File.Exists(PrototypeCatalogFactory.CatalogPath)||!File.Exists(SceneRoot+"00_Bootstrap.unity")||!File.Exists(SceneRoot+"10_Gameplay.unity")||!File.Exists(SceneRoot+"Sandbox/P2_Elements.unity")||!File.Exists(SceneRoot+"Sandbox/P3_Combat.unity")||!File.Exists(SceneRoot+"Sandbox/P4_Enemies.unity")||!File.Exists(SceneRoot+"Sandbox/P5_UI.unity")||!File.Exists("Assets/_Game/Prefabs/UI/UI_Practice.prefab"))Generate();
        }
        [MenuItem("聚光灯/生成2D主程工程")]
        public static void Generate()
        {
            if(generating||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(HasUnsavedScene()){Debug.Log("生成器等待：当前有未保存场景，保持原场景不变。");return;}
            generating=true;Scene previous=SceneManager.GetActiveScene();Scene work=default(Scene);
            try
            {
                work=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                SceneManager.SetActiveScene(work);Directory.CreateDirectory(SceneRoot);Directory.CreateDirectory(PrefabRoot);EnsureTaskFolders();Sprite sprite=EnsureSprite();Material material=EnsureMaterial();PrefabEntry[] entries=EnsurePrefabs(sprite,material);GameObject practice=EnsureUiPractice();
                PrototypeCatalogSO catalog=PrototypeCatalogFactory.CreateAssets(entries);MigrateGeneratedPrefabMappings(catalog,entries);IReadOnlyList<string> errors=catalog.Validate();if(errors.Count>0){foreach(string error in errors)Debug.LogError("Spotlight配置："+error);throw new InvalidOperationException("原型配置验证失败，请先修复Inspector配置。");}
                SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(work,true);work=default(Scene);
                CreateScene("00_Bootstrap",delegate(Scene scene){Camera camera=Camera2D();GameObject root=new GameObject("GameBootstrap");GameBootstrap bootstrap=root.AddComponent<GameBootstrap>();bootstrap.CatalogAsset=catalog;bootstrap.WorldCamera=camera;});
                CreateScene("10_Gameplay",delegate(Scene scene){Camera2D();new GameObject("GameplayWorkspace");});
                CreateScene("Sandbox/P2_Elements",delegate(Scene scene){Camera camera=Camera2D();AddGameBootstrap(catalog,camera);});
                CreateScene("Sandbox/P3_Combat",delegate(Scene scene){Camera camera=Camera2D();AddGameBootstrap(catalog,camera);});
                CreateScene("Sandbox/P4_Enemies",delegate(Scene scene){Camera camera=Camera2D();AddGameBootstrap(catalog,camera);});
                AddPreviewToUntouchedSandbox("Sandbox/P2_Elements",catalog);
                AddPreviewToUntouchedSandbox("Sandbox/P3_Combat",catalog);
                AddPreviewToUntouchedSandbox("Sandbox/P4_Enemies",catalog);
                CreateScene("Sandbox/P5_UI",delegate(Scene scene){Camera2D();Example(practice,Vector3.zero,scene);new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));});
                CreateScene("Sandbox/P5_UI_Preview",delegate(Scene scene){Camera2D();GameObject demo=new GameObject("PresentationPreview");Spotlight.Presentation.GameView view=demo.AddComponent<Spotlight.Presentation.GameView>();view.PreviewOnly=true;});
                // 主程入口必须是打包启动场景；保留其余已有场景及启用状态。
                List<EditorBuildSettingsScene> scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);string bootstrapPath=SceneRoot+"00_Bootstrap.unity";scenes.RemoveAll(delegate(EditorBuildSettingsScene item){return item.path==bootstrapPath;});scenes.Insert(0,new EditorBuildSettingsScene(bootstrapPath,true));AddBuildScene(scenes,SceneRoot+"10_Gameplay.unity");EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("聚光灯2D模板已就绪。已有配置、场景和Prefab均保留。");
            }
            catch(Exception error){Debug.LogException(error);}
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(work.IsValid()&&work.isLoaded)EditorSceneManager.CloseScene(work,true);generating=false;}
        }
        static void AddBuildScene(List<EditorBuildSettingsScene> list,string path){foreach(EditorBuildSettingsScene s in list)if(s.path==path)return;list.Add(new EditorBuildSettingsScene(path,true));}
        static void MigrateGeneratedPrefabMappings(PrototypeCatalogSO catalog,PrefabEntry[] canonical)
        {
            if(catalog.PrefabEntries==null)return;bool changed=false;
            foreach(PrefabEntry entry in catalog.PrefabEntries)
            {
                if(entry==null||entry.Prefab==null||string.IsNullOrEmpty(entry.Key))continue;
                // 只接管本生成器最初的固定路径；用户自己选的Prefab保持原引用。
                string oldPath=PrefabRoot+entry.Key+".prefab";if(AssetDatabase.GetAssetPath(entry.Prefab)!=oldPath)continue;
                GameObject replacement=Find(canonical,entry.Key);if(replacement==null||replacement==entry.Prefab)continue;
                if(AssetDatabase.GetAssetPath(replacement)!=CanonicalPrefabPath(entry.Key))continue;
                entry.Prefab=replacement;changed=true;
            }
            if(changed)EditorUtility.SetDirty(catalog);
        }
        static bool HasUnsavedScene()
        {
            for(int i=0;i<SceneManager.sceneCount;i++){Scene scene=SceneManager.GetSceneAt(i);if(scene.isLoaded&&(string.IsNullOrEmpty(scene.path)||scene.isDirty))return true;}return false;
        }
        static void AddGameBootstrap(PrototypeCatalogSO catalog,Camera camera)
        {
            GameBootstrap bootstrap=new GameObject("GameBootstrap - Play后点新游戏进入真实业务").AddComponent<GameBootstrap>();bootstrap.CatalogAsset=catalog;bootstrap.WorldCamera=camera;
            BoardRoutePreview2D preview=bootstrap.gameObject.AddComponent<BoardRoutePreview2D>();if(catalog.Levels!=null&&catalog.Levels.Length>0)preview.Level=catalog.Levels[0];
        }
        static void AddPreviewToUntouchedSandbox(string name,PrototypeCatalogSO catalog)
        {
            string path=SceneRoot+name+".unity";if(!File.Exists(path)||catalog.Levels==null||catalog.Levels.Length==0)return;
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).path==path)return; // 用户正在打开的场景不保存。
            Scene previous=SceneManager.GetActiveScene();Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                GameObject[] roots=scene.GetRootGameObjects();if(roots.Length!=2)return;GameBootstrap bootstrap=null;bool camera=false;
                foreach(GameObject root in roots)
                {
                    if(root.transform.childCount!=0)return;
                    if(root.GetComponent<GameBootstrap>()!=null)
                    {
                        bootstrap=root.GetComponent<GameBootstrap>();foreach(Component c in root.GetComponents<Component>())if(!(c is Transform)&&!(c is GameBootstrap)&&!(c is BoardRoutePreview2D))return;
                    }
                    else if(root.GetComponent<Camera>()!=null){camera=true;foreach(Component c in root.GetComponents<Component>())if(!(c is Transform)&&!(c is Camera))return;}
                    else return;
                }
                if(bootstrap==null||!camera||bootstrap.CatalogAsset!=catalog||bootstrap.GetComponent<BoardRoutePreview2D>()!=null)return;
                bootstrap.gameObject.AddComponent<BoardRoutePreview2D>().Level=catalog.Levels[0];EditorSceneManager.SaveScene(scene,path);
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static void EnsureTaskFolders()
        {
            string[] paths={"Assets/_Game/Scenes/Sandbox","Assets/_Game/Prefabs/Elements","Assets/_Game/Prefabs/Combat","Assets/_Game/Prefabs/Towers","Assets/_Game/Prefabs/Projectiles","Assets/_Game/Prefabs/Enemies","Assets/_Game/Prefabs/UI","Assets/_Game/Prefabs/Feedback","Assets/_Game/Scripts/Enemies/Runtime"};foreach(string path in paths)EnsureFolder(path);
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)||path=="Assets")return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);if(Directory.Exists(path))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);else AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static void CreateScene(string name,Action<Scene> populate)
        {
            string path=SceneRoot+name+".unity";if(File.Exists(path))return;EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));Scene previous=SceneManager.GetActiveScene();Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);populate(scene);if(!EditorSceneManager.SaveScene(scene,path))throw new IOException("Cannot save "+path);}finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static Camera Camera2D(){GameObject go=new GameObject("WorldCamera");go.tag="MainCamera";go.transform.position=new Vector3(0,0,-10);Camera camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.09f,.14f);return camera;}
        static Sprite EnsureSprite()
        {
            if(!File.Exists(SpritePath)){Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));Texture2D texture=new Texture2D(16,16,TextureFormat.RGBA32,false);Color[] pixels=new Color[256];for(int i=0;i<pixels.Length;i++)pixels[i]=Color.white;texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(SpritePath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(SpritePath);TextureImporter importer=AssetImporter.GetAtPath(SpritePath) as TextureImporter;if(importer!=null){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}}
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
        static Material EnsureMaterial()
        {
            Material material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);if(material!=null)return material;Shader shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");if(shader==null)shader=Shader.Find("Sprites/Default");if(shader==null)throw new InvalidOperationException("Missing 2D sprite shader");material=new Material(shader);Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));AssetDatabase.CreateAsset(material,MaterialPath);return material;
        }
        static GameObject EnsureUiPractice()
        {
            const string path="Assets/_Game/Prefabs/UI/UI_Practice.prefab";
            GameObject existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;if(File.Exists(path))throw new InvalidOperationException("保留已有UI_Practice，无法读取："+path);
            Font font=EnsurePracticeFont();GameObject root=new GameObject("UI_Practice",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            Canvas canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;CanvasScaler scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            Text title=PracticeText(root.transform,font,"Title","聚光灯 · 按钮接线练习",new Vector2(60,-50),new Vector2(700,55));title.fontSize=32;
            Text hint=PracticeText(root.transform,font,"HintText","点击开始按钮，测试文字会改变。",new Vector2(60,-125),new Vector2(750,45));
            GameObject buttonObject=new GameObject("PracticeButton",typeof(RectTransform),typeof(Image),typeof(Button));buttonObject.transform.SetParent(root.transform,false);RectTransform buttonRect=buttonObject.GetComponent<RectTransform>();buttonRect.anchorMin=buttonRect.anchorMax=new Vector2(0,1);buttonRect.pivot=new Vector2(0,1);buttonRect.anchoredPosition=new Vector2(60,-190);buttonRect.sizeDelta=new Vector2(260,65);Image background=buttonObject.GetComponent<Image>();background.color=new Color(.2f,.4f,.75f,1);Button button=buttonObject.GetComponent<Button>();button.targetGraphic=background;
            Text buttonText=PracticeText(buttonObject.transform,font,"Label","开始",Vector2.zero,new Vector2(260,65));buttonText.alignment=TextAnchor.MiddleCenter;
            Spotlight.Presentation.ButtonPractice practice=root.AddComponent<Spotlight.Presentation.ButtonPractice>();practice.PracticeButton=button;practice.HintText=hint;practice.ClickHint="按钮连接成功！";
            Spotlight.Presentation.ReadonlyHudView hud=root.AddComponent<Spotlight.Presentation.ReadonlyHudView>();hud.SpringHpText=PracticeText(root.transform,font,"SpringHpText","灵泉 100 / 100（演示）",new Vector2(60,-305),new Vector2(400,40));hud.GoldText=PracticeText(root.transform,font,"GoldText","金币 100（演示）",new Vector2(60,-350),new Vector2(400,40));hud.HandTexts=new Text[6];string[] labels={"水","火","土","木","风","雷"};for(int i=0;i<6;i++)hud.HandTexts[i]=PracticeText(root.transform,font,"Hand"+i,labels[i]+"：3",new Vector2(60+i*160,-410),new Vector2(140,40));
            PracticeText(root.transform,font,"PracticeInstructions","停止Play后，可在Inspector重新拖入 Button / Text 字段。\n本场景使用演示数值；正式UI另见 P5_UI_Preview 和 00_Bootstrap。",new Vector2(60,-515),new Vector2(1120,100));
            GameObject prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static Font EnsurePracticeFont()
        {
            const string path="Assets/_Game/Art/Prototype/ChineseUI.fontsettings";Font existing=AssetDatabase.LoadAssetAtPath<Font>(path);if(existing!=null)return existing;if(File.Exists(path))throw new InvalidOperationException("保留已有练习字体配置，无法读取："+path);
            Font font=Font.CreateDynamicFontFromOSFont(new string[] {"Microsoft YaHei","SimHei","Arial"},24);if(font==null)return Resources.GetBuiltinResource<Font>("Arial.ttf");font.name="ChineseUI";AssetDatabase.CreateAsset(font,path);return font;
        }
        static Text PracticeText(Transform parent,Font font,string name,string value,Vector2 position,Vector2 size)
        {
            GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;Text text=go.GetComponent<Text>();text.font=font;text.fontSize=24;text.color=Color.white;text.text=value;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        static string CanonicalPrefabPath(string key)
        {
            if(key.StartsWith("Element",StringComparison.Ordinal))return "Assets/_Game/Prefabs/Elements/"+key+".prefab";
            if(key=="TowerBasic")return "Assets/_Game/Prefabs/Towers/TWR_Basic.prefab";
            if(key=="ProjectileBasic")return "Assets/_Game/Prefabs/Projectiles/PRJ_Basic.prefab";
            if(key=="EnemyProjectile")return "Assets/_Game/Prefabs/Projectiles/PRJ_Enemy.prefab";
            if(key=="EnemyGoblinMelee")return "Assets/_Game/Prefabs/Enemies/ENM_GoblinMelee.prefab";
            if(key=="EnemyGoblinRanged")return "Assets/_Game/Prefabs/Enemies/ENM_GoblinRanged.prefab";
            if(key=="EnemySoulKing")return "Assets/_Game/Prefabs/Enemies/ENM_SoulKing.prefab";
            return PrefabRoot+key+".prefab";
        }
        static PrefabEntry[] EnsurePrefabs(Sprite sprite,Material material)
        {
            string[] keys={"ElementWater","ElementFire","ElementEarth","ElementWood","ElementWind","ElementThunder","TowerBasic","ProjectileBasic","EnemyProjectile","EnemyGoblinMelee","EnemyGoblinRanged","EnemySoulKing","view.spring"};
            Color[] colors={new Color(.15f,.6f,1),new Color(1,.3f,.15f),new Color(.7f,.5f,.2f),new Color(.2f,.75f,.35f),new Color(.6f,1,.8f),new Color(.8f,.45f,1),new Color(.7f,.8f,1),Color.yellow,new Color(1,.45f,.2f),new Color(.3f,.75f,.2f),new Color(.5f,.65f,.25f),new Color(.55f,.2f,.65f),new Color(.3f,.9f,1)};
            List<PrefabEntry> entries=new List<PrefabEntry>();for(int i=0;i<keys.Length;i++){string path=CanonicalPrefabPath(keys[i]);GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null&&File.Exists(path))throw new InvalidOperationException("保留已有Prefab，无法读取："+path);if(prefab==null){string original=PrefabRoot+keys[i]+".prefab";if(original!=path&&AssetDatabase.LoadAssetAtPath<GameObject>(original)!=null){AssetDatabase.CopyAsset(original,path);prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);}else{GameObject go=new GameObject(keys[i]);SpriteRenderer render=go.AddComponent<SpriteRenderer>();render.sprite=sprite;render.sharedMaterial=material;render.color=colors[i];go.transform.localScale=Vector3.one*(keys[i].Contains("Projectile") ? .18f : .75f);prefab=PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);}}entries.Add(new PrefabEntry {Key=keys[i],Prefab=prefab});}return entries.ToArray();
        }
        static GameObject Find(PrefabEntry[] entries,string key){foreach(PrefabEntry e in entries)if(e.Key==key)return e.Prefab;return null;}
        static void Example(GameObject prefab,Vector3 position,Scene scene){if(prefab==null)return;GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.position=position;}
    }
}

