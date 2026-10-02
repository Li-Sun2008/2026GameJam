#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Presentation;

namespace Spotlight.Tests.Integration
{
    public sealed class PBootstrapSmokeTests
    {
        private GameObject root, cameraObject;
        private Scene scene, previous;
        private string directory;

        [UnityTest] public IEnumerator NativeAwakeBuildsUiSpritesAndCapturesPreview()
        {
            yield return new EnterPlayMode();
            previous=SceneManager.GetActiveScene();
            scene=SceneManager.CreateScene("Spotlight_NativeSmoke_"+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(scene);
            PrototypeCatalogSO catalog=AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);Assert.NotNull(catalog);
            directory=Path.Combine(Path.GetTempPath(),"Spotlight_NativeSmoke_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            cameraObject=new GameObject("Smoke Camera");cameraObject.tag="MainCamera";Camera camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            root=new GameObject("Smoke Bootstrap");root.SetActive(false);GameBootstrap bootstrap=root.AddComponent<GameBootstrap>();bootstrap.CatalogAsset=catalog;bootstrap.WorldCamera=camera;bootstrap.SaveDirectoryOverride=directory;root.SetActive(true);
            yield return null;yield return null;yield return null;
            Assert.NotNull(bootstrap.Services,"Native Awake must construct the real service composition without startup errors");
            GameView view=root.GetComponent<GameView>();Assert.NotNull(view);Assert.NotNull(root.GetComponentInChildren<Canvas>());
            Assert.GreaterOrEqual(root.GetComponentsInChildren<Button>().Length,12);Assert.IsTrue(ContainsText(root,"聚光灯"));
            ModuleComposition services=bootstrap.Services;
            Assert.AreEqual(OperationState.Committed,services.Flow.TryNewRun(Command(services),new NewRunRequest {LevelId="level.prototype",Mode=GameMode.Story,Seed=456}).State);
            yield return null;DayEventSnapshot day=services.Gameplay.DayEvents.GetSnapshot();Assert.NotNull(day.State);Assert.Greater(day.Options.Count,0);
            Assert.AreEqual(OperationState.Committed,services.Gameplay.DayEvents.TryChoose(Command(services),day.State.InstanceId,day.Options[0].Id).State);yield return null;
            Assert.AreEqual(GamePhase.Build,services.Flow.GetSnapshot().Phase);Assert.AreEqual(OperationState.Committed,services.Flow.TryDismissTutorial(Command(services)).State);
            Assert.AreEqual(OperationState.Committed,services.Core.Deployment.TryPlace(Command(services),new PlacementRequest {Kind=OccupantKind.Tower,DefinitionId="tower.basic",Cell=new CellCoord(2,1)}).State);
            Assert.AreEqual(OperationState.Committed,services.Core.Deployment.TryPlace(Command(services),new PlacementRequest {Kind=OccupantKind.ElementBlock,DefinitionId="elm.water",Cell=new CellCoord(2,2)}).State);
            yield return null;yield return null;yield return null;
            Assert.GreaterOrEqual(root.GetComponentsInChildren<SpriteRenderer>().Length,54,"The actual renderer must create all board tiles");
            bool springSprite=false,towerSprite=false,blockSprite=false;
            foreach(GameObject sceneRoot in scene.GetRootGameObjects())foreach(SpriteRenderer sprite in sceneRoot.GetComponentsInChildren<SpriteRenderer>())
            {springSprite|=sprite.sortingOrder==10;towerSprite|=sprite.sortingOrder==20;blockSprite|=sprite.sortingOrder==5;}
            Assert.IsTrue(springSprite&&towerSprite&&blockSprite,"Real pool leases must display Spring, tower and element block in the test scene");
            Assert.IsTrue(ContainsText(root,"水 × 2"));Assert.IsTrue(ContainsText(root,"金币 80"));Assert.IsTrue(ContainsText(root,"灵泉 100 / 100"));
            Assert.AreEqual(OperationState.Committed,services.Flow.TryStartNight(Command(services)).State);Guid pause=services.Clock.AcquirePause(PauseReason.Player);ClockSnapshot before=services.Clock.GetSnapshot();
            yield return null;yield return null;yield return null;
            Assert.AreEqual(before.GameTime,services.Clock.GetSnapshot().GameTime);Assert.AreEqual(before.TickIndex,services.Clock.GetSnapshot().TickIndex);Assert.IsTrue(services.Clock.GetSnapshot().IsPaused);
            // The EditMode test iterator runs on EditorUpdate. Only a player coroutine can
            // reach the actual render boundary; the test waits with a bounded frame budget.
            FocusGameWindow();NativeScreenshotProbe probe=root.AddComponent<NativeScreenshotProbe>();probe.Begin();
            for(int frame=0;frame<120&&!probe.Completed;frame++)yield return null;
            Assert.IsTrue(probe.Completed,"The player screenshot coroutine did not reach the render boundary within 120 frames");Assert.IsNull(probe.Error);
            Texture2D screenshot=probe.Image;Assert.NotNull(screenshot);
            try{string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/2D-Prototype-preview.png"));Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,screenshot.EncodeToPNG());}
            finally{UnityEngine.Object.Destroy(screenshot);probe.Image=null;}
            services.Clock.ReleasePause(pause);LogAssert.NoUnexpectedReceived();
        }
        private static CommandContext Command(ModuleComposition services){return services.Core.Commands.Create(services.Core.Transactions.Revision);}
        private static void FocusGameWindow()
        {
            foreach(System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {Type gameView=assembly.GetType("UnityEditor.GameView");if(gameView!=null){EditorWindow.GetWindow(gameView,false,"Game",true).Focus();return;}}
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        private static bool ContainsText(GameObject target,string value){foreach(Text text in target.GetComponentsInChildren<Text>(true))if(text.text.Contains(value))return true;return false;}
        [UnityTearDown] public IEnumerator CleanupNativeScene()
        {
            if(root!=null)UnityEngine.Object.Destroy(root);if(cameraObject!=null)UnityEngine.Object.Destroy(cameraObject);yield return null;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded){AsyncOperation unload=SceneManager.UnloadSceneAsync(scene);if(unload!=null)yield return unload;}
            if(directory!=null&&Directory.Exists(directory))
            {
                string path=Path.GetFullPath(directory),temporaryRoot=Path.GetFullPath(Path.GetTempPath());
                Assert.IsTrue(path.StartsWith(temporaryRoot,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(path).StartsWith("Spotlight_NativeSmoke_",StringComparison.Ordinal));Directory.Delete(path,true);
            }
            root=cameraObject=null;directory=null;
            if(Application.isPlaying)yield return new ExitPlayMode();
        }
    }
    public sealed class NativeScreenshotProbe : MonoBehaviour
    {
        public bool Completed;
        public string Error;
        public Texture2D Image;
        public void Begin(){StartCoroutine(Capture());}
        private IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();
            try{Image=ScreenCapture.CaptureScreenshotAsTexture();}
            catch(Exception exception){Error=exception.ToString();}
            finally{Completed=true;}
        }
        private void OnDestroy(){if(Image!=null)UnityEngine.Object.Destroy(Image);}
    }
}
#endif
