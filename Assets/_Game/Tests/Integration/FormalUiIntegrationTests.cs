#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Presentation;

namespace Spotlight.Tests.Integration
{
    public sealed class FormalUiIntegrationTests
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/UI/UI_Game.prefab";
        private GameObject host;
        private GameView view;
        private GameUiReferences prefab;
        private ModuleComposition runtime;
        private string directory;
        private readonly List<GameObject> temporaryObjects = new List<GameObject>();

        [SetUp] public void Setup()
        {
            PrototypeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            Assert.NotNull(catalog, "Generate the real catalog before running formal UI tests.");
            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.NotNull(prefabObject, "Missing formal UI prefab: " + PrefabPath);
            prefab = prefabObject.GetComponent<GameUiReferences>();
            Assert.NotNull(prefab, "The formal prefab root requires GameUiReferences.");
            string error;
            Assert.IsTrue(prefab.Validate(out error), error);
            directory = Path.Combine(Path.GetTempPath(), "Spotlight_FormalUi_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            runtime = new ModuleComposition(catalog.BuildCatalogData(), directory);
            host = new GameObject("Formal UI integration controller");
            view = host.AddComponent<GameView>();
            view.UiPrefab = prefab;
            view.Bind(runtime.ViewContext);
        }

        [TearDown] public void Cleanup()
        {
            if (view != null) view.Unbind();
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            foreach (GameObject obj in temporaryObjects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            temporaryObjects.Clear();
            if (runtime != null) runtime.Dispose();
            runtime = null; view = null; host = null;
            if (directory != null && Directory.Exists(directory))
            {
                string absolute = Path.GetFullPath(directory);
                string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.IsTrue(absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(Path.GetFileName(absolute).StartsWith("Spotlight_FormalUi_", StringComparison.Ordinal));
                Directory.Delete(absolute, true);
            }
            directory = null;
        }

        private GameUiReferences Ui()
        {
            GameUiReferences[] refs = host.GetComponentsInChildren<GameUiReferences>(true);
            Assert.AreEqual(1, refs.Length, "Bind must create exactly one formal UI instance.");
            return refs[0];
        }
        private CommandContext Command() { return runtime.Core.Commands.Create(runtime.Core.Transactions.Revision); }
        private void Pump() { runtime.Advance(0); view.RefreshAll(); }
        private static void Click(Transform parent, string label)
        {
            foreach (Button button in parent.GetComponentsInChildren<Button>(true))
            {
                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == label)
                {
                    Assert.IsTrue(button.interactable, "Button is disabled: " + label);
                    button.onClick.Invoke(); return;
                }
            }
            Assert.Fail("Missing actual UI button: " + label);
        }
        private void EnterBuildUsingUi()
        {
            Click(Ui().ActionsRoot, "故事模式"); Pump();
            Assert.AreEqual(GamePhase.DayEvent, runtime.Flow.GetSnapshot().Phase);
            DayEventSnapshot day = runtime.Gameplay.DayEvents.GetSnapshot();
            Assert.NotNull(day.State); Assert.IsFalse(day.State.Resolved); Assert.Greater(day.Options.Count, 0);
            Click(Ui().ModalRoot, day.Options[0].Text); Pump();
            Assert.AreEqual(GamePhase.Build, runtime.Flow.GetSnapshot().Phase);
            if (!runtime.Flow.GetSnapshot().TutorialShown && !string.IsNullOrEmpty(runtime.Catalog.GetSettings().TutorialText))
            {
                Click(Ui().ModalRoot, "开始部署"); Pump();
                Assert.IsTrue(runtime.Flow.GetSnapshot().TutorialShown);
            }
        }
        private void BoardClick(CellCoord cell)
        {
            // Invoke the same controller entry used by the input path; no deployment is recreated here.
            MethodInfo method = typeof(GameView).GetMethod("HandleBoardClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? typeof(GameView).GetMethod("OnBoardCellClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, "GameView needs its actual board-click entry for input integration tests.");
            method.Invoke(view, new object[] { cell });
        }

        [Test] public void FormalPrefabProvidesOneCanvasAndOneControllerAcrossRepeatedBinding()
        {
            GameUiReferences ui = Ui();
            Assert.AreSame(prefab, view.UiPrefab);
            Assert.AreNotSame(prefab, ui);
            Assert.AreSame(ui.Canvas, host.GetComponentInChildren<Canvas>(true));
            Assert.AreEqual(1, host.GetComponentsInChildren<Canvas>(true).Length);
            Assert.AreEqual(1, host.GetComponentsInChildren<GameView>(true).Length);
            view.Bind(runtime.ViewContext); view.Bind(runtime.ViewContext);
            Assert.AreEqual(1, host.GetComponentsInChildren<Canvas>(true).Length);
            Assert.AreEqual(1, host.GetComponentsInChildren<GameUiReferences>(true).Length);
            Assert.AreEqual(6, Ui().ElementButtons.Length);
            Assert.AreEqual(2, Ui().ItemButtons.Length);
        }

        [Test] public void UiDeploymentAndCommittedHealthChangesRefreshActualFormalWidgets()
        {
            EnterBuildUsingUi();
            long gold = runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold");
            long water = runtime.Core.Resources.GetQuantity(ResourceBucket.Hand, "elm.water");
            Click(Ui().ActionsRoot, "建造攻击塔"); BoardClick(new CellCoord(2, 1)); Pump();
            Ui().ElementButtons[0].onClick.Invoke(); BoardClick(new CellCoord(2, 2)); Pump();
            Assert.AreEqual(gold - 20, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
            Assert.AreEqual(water - 1, runtime.Core.Resources.GetQuantity(ResourceBucket.Hand, "elm.water"));
            Assert.That(Ui().GoldText.text, Does.Contain((gold - 20).ToString()));
            Assert.That(Ui().HandTexts[0].text, Does.Contain((water - 1).ToString()));
            OperationResult result = runtime.Core.Transactions.TryCommit(new StateMutationBatch
            {
                Context = Command(), Actors = new[] { new ActorMutation { Kind = ActorMutationKind.SetHealth, ActorId = runtime.Core.World.SpringId, CurrentHp = 63, MaxHp = 100 } }
            });
            Assert.AreEqual(OperationState.Committed, result.State, result.Error + " / " + result.MessageKey);
            runtime.Advance(0);
            Assert.That(Ui().SpringHpText.text, Does.Contain("63"));
            Assert.That(Ui().SpringHpText.text, Does.Contain("100"));
            if (Ui().SpringHpBar != null)
            {
                float expected = Mathf.Lerp(Ui().SpringHpBar.minValue, Ui().SpringHpBar.maxValue, .63f);
                Assert.AreEqual(expected, Ui().SpringHpBar.value, .001f);
            }
        }

        [Test] public void HealthBarFillGeometryMatchesZeroHalfAndFullOfActualSlot()
        {
            Slider bar = Ui().SpringHpBar;
            Assert.NotNull(bar, "The formal UI prefab must provide the spring health bar.");
            Assert.NotNull(bar.fillRect);
            RectTransform slot = bar.fillRect.parent as RectTransform;
            Assert.NotNull(slot);
            Vector3[] corners = new Vector3[4];
            foreach (float fraction in new[] { 0f, .5f, 1f })
            {
                bar.value = Mathf.Lerp(bar.minValue, bar.maxValue, fraction);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(Ui().Canvas.transform as RectTransform);
                Canvas.ForceUpdateCanvases();
                bar.fillRect.GetWorldCorners(corners);
                float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
                foreach (Vector3 corner in corners)
                {
                    float x = slot.InverseTransformPoint(corner).x;
                    minimum = Mathf.Min(minimum, x); maximum = Mathf.Max(maximum, x);
                }
                Assert.Greater(slot.rect.width, 0, "Canvas layout must produce a measurable slider slot.");
                Assert.AreEqual(slot.rect.width * fraction, maximum - minimum, .5f, "Actual fill width at fraction " + fraction);
                Assert.GreaterOrEqual(minimum, slot.rect.xMin - .5f, "Fill must stay inside the slot.");
                Assert.LessOrEqual(maximum, slot.rect.xMax + .5f, "Fill must stay inside the slot.");
            }
        }

        [Test] public void StaticPauseAndSpeedButtonsUseRealClockWithoutReleasingExternalPause()
        {
            EnterBuildUsingUi();
            Guid external = runtime.Clock.AcquirePause(PauseReason.Modal);
            Ui().PauseButton.onClick.Invoke(); Pump();
            if (Ui().PauseButtonText != null) Assert.That(Ui().PauseButtonText.text, Does.Contain(runtime.Clock.GetSnapshot().Reasons.Count == 2 ? "继续" : "暂停"));
            Assert.AreEqual(2, runtime.Clock.GetSnapshot().Reasons.Count);
            Ui().PauseButton.onClick.Invoke(); Pump();
            if (Ui().PauseButtonText != null) Assert.That(Ui().PauseButtonText.text, Does.Contain(runtime.Clock.GetSnapshot().Reasons.Count == 2 ? "继续" : "暂停"));
            Assert.AreEqual(1, runtime.Clock.GetSnapshot().Reasons.Count);
            Assert.AreEqual(PauseReason.Modal, runtime.Clock.GetSnapshot().Reasons[0]);
            Ui().DoubleSpeedButton.onClick.Invoke(); Pump(); Assert.AreEqual(GameSpeed.Double, runtime.Clock.GetSnapshot().Speed);
            if (Ui().SpeedText != null) { Assert.That(Ui().SpeedText.text, Does.Contain("2")); Assert.That(Ui().SpeedText.text, Does.Contain("暂停")); }
            Ui().NormalSpeedButton.onClick.Invoke(); Assert.AreEqual(GameSpeed.Normal, runtime.Clock.GetSnapshot().Speed);
            Ui().ItemButtons[1].onClick.Invoke(); Assert.AreEqual(2, runtime.Clock.GetSnapshot().Reasons.Count);
            Ui().CancelSelectionButton.onClick.Invoke(); Assert.AreEqual(1, runtime.Clock.GetSnapshot().Reasons.Count);
            runtime.Clock.ReleasePause(external); Assert.IsFalse(runtime.Clock.GetSnapshot().IsPaused);
        }

        [Test] public void RebindKeepsExternalListenerAndOnlyOneBusinessListenerThenUnbindReleasesOwnership()
        {
            EnterBuildUsingUi();
            Button.ButtonClickedEvent oldClick = Ui().PauseButton.onClick; int externalClicks = 0;
            oldClick.AddListener(delegate { externalClicks++; });
            view.Bind(runtime.ViewContext); view.Bind(runtime.ViewContext);
            oldClick.Invoke(); Assert.AreEqual(1, externalClicks);
            Assert.IsFalse(runtime.Clock.GetSnapshot().IsPaused, "Replaced UI must detach its business listener.");
            Button.ButtonClickedEvent currentClick = Ui().PauseButton.onClick;
            currentClick.AddListener(delegate { externalClicks++; }); currentClick.Invoke();
            Assert.AreEqual(2, externalClicks);
            Assert.AreEqual(1, runtime.Clock.GetSnapshot().Reasons.Count, "A click must toggle player pause once, not once per Bind.");
            Guid externalPause = runtime.Clock.AcquirePause(PauseReason.Modal);
            view.Unbind();
            Assert.AreEqual(1, runtime.Clock.GetSnapshot().Reasons.Count);
            Assert.AreEqual(PauseReason.Modal, runtime.Clock.GetSnapshot().Reasons[0]);
            currentClick.Invoke(); Assert.AreEqual(3, externalClicks);
            Assert.AreEqual(1, runtime.Clock.GetSnapshot().Reasons.Count, "Unbound UI must not call the clock.");
            runtime.Clock.ReleasePause(externalPause);
        }

        [Test] public void PhaseRefreshReleasesOnlyViewPauseWithoutMutatingTransactionalState()
        {
            EnterBuildUsingUi(); Ui().ItemButtons[1].onClick.Invoke();
            Assert.IsTrue(runtime.Clock.GetSnapshot().IsPaused);
            Guid external = runtime.Clock.AcquirePause(PauseReason.Modal);
            long revision = runtime.Core.Transactions.Revision;
            runtime.Events.Publish(new PhaseChangedEvent { OldPhase = GamePhase.Build, NewPhase = GamePhase.Night, DayIndex = 1 });
            bool listenerSawOwnedPause = false;
            using (runtime.Events.Subscribe<PhaseChangedEvent>(delegate { listenerSawOwnedPause = runtime.Clock.GetSnapshot().IsPaused; }))
            {
                runtime.Events.Flush();
            }
            Assert.IsTrue(listenerSawOwnedPause, "UI must preserve the external modal pause during phase refresh.");
            Assert.AreEqual(revision, runtime.Core.Transactions.Revision);
            view.RefreshAll();
            Assert.AreEqual(1, runtime.Clock.GetSnapshot().Reasons.Count);
            Assert.AreEqual(PauseReason.Modal, runtime.Clock.GetSnapshot().Reasons[0]);
            runtime.Clock.ReleasePause(external);
            Click(Ui().ActionsRoot, "开始这一夜"); Pump();
            Assert.AreEqual(GamePhase.Night, runtime.Flow.GetSnapshot().Phase);
            Assert.That(Ui().PhaseText != null ? Ui().PhaseText.text : Ui().TitleText.text, Does.Contain("夜"));
        }

        [Test] public void InvalidRequiredReferenceReportsFieldAndLeavesNoBoundCanvasOrPause()
        {
            view.Unbind();
            GameUiReferences invalid = UnityEngine.Object.Instantiate(prefab);
            temporaryObjects.Add(invalid.gameObject); invalid.TitleText = null;
            string error; Assert.IsFalse(invalid.Validate(out error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
            Assert.IsTrue(error.Contains("TitleText") || error.Contains("标题"), error);
            view.UiPrefab = invalid;
            Assert.Throws<InvalidOperationException>(delegate { view.Bind(runtime.ViewContext); });
            Assert.AreEqual(0, host.GetComponentsInChildren<Canvas>(true).Length);
            Assert.IsFalse(runtime.Clock.GetSnapshot().IsPaused);
            view.UiPrefab = prefab; view.Bind(runtime.ViewContext);
            Assert.AreEqual(1, host.GetComponentsInChildren<Canvas>(true).Length, "A failed bind must be recoverable without duplicate UI.");
        }

        [Test] public void RepeatedBindUnbindAndDestroyDoNotLeakOwnedEventSystemsOrDestroyExternalSystem()
        {
            view.Unbind();
            FieldInfo ownedField = typeof(GameView).GetField("ownedEvents", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(ownedField);
            HashSet<int> baseline = SceneEventSystemIds();
            for (int i = 0; i < 4; i++)
            {
                view.Bind(runtime.ViewContext);
                GameObject owned = ownedField.GetValue(view) as GameObject;
                if (owned != null) Assert.IsTrue(owned.transform.IsChildOf(host.transform), "Owned EventSystem must belong to the controller hierarchy.");
                view.Unbind();
                Assert.IsTrue(owned == null, "Unbind must destroy the exact EventSystem this view created.");
                Assert.IsNull(ownedField.GetValue(view), "Unbind must clear its ownership reference.");
                Assert.IsTrue(baseline.SetEquals(SceneEventSystemIds()), "Bind/Unbind must leave existing scene EventSystems unchanged.");
            }
            view.Bind(runtime.ViewContext);
            GameObject ownedAtDestroy = ownedField.GetValue(view) as GameObject;
            UnityEngine.Object.DestroyImmediate(host); host = null; view = null;
            Assert.IsTrue(ownedAtDestroy == null, "Destroy must release an owned EventSystem without a prior Unbind.");
            Assert.IsTrue(baseline.SetEquals(SceneEventSystemIds()));
            host = new GameObject("Formal UI external system controller"); view = host.AddComponent<GameView>(); view.UiPrefab = prefab;
            GameObject externalObject = new GameObject("Formal UI external EventSystem", typeof(EventSystem));
            temporaryObjects.Add(externalObject);
            EventSystem external = externalObject.GetComponent<EventSystem>();
            {
                HashSet<int> withExternal = SceneEventSystemIds();
                view.Bind(runtime.ViewContext);
                Assert.IsNull(ownedField.GetValue(view), "A view must use the available external EventSystem.");
                view.Unbind();
                UnityEngine.Object.DestroyImmediate(host); host = null; view = null;
                Assert.IsTrue(external != null && external.gameObject == externalObject);
                Assert.IsTrue(withExternal.SetEquals(SceneEventSystemIds()), "Destroy must preserve external EventSystems and add no orphan.");
            }

        }
        private static HashSet<int> SceneEventSystemIds()
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (EventSystem system in Resources.FindObjectsOfTypeAll<EventSystem>())
                if (system != null && system.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(system)) ids.Add(system.GetInstanceID());
            return ids;
        }
    }

    public sealed class FormalUiBootstrapTests
    {
        private GameObject host, cameraObject;
        private Scene scene, previous;
        private string directory;
        private readonly List<KeyValuePair<GameObject, bool>> previousRoots = new List<KeyValuePair<GameObject, bool>>();

        [UnityTest] public IEnumerator BootstrapUsesFormalPrefabAndRendersRealDeploymentHud()
        {
            yield return new EnterPlayMode();
            previous = SceneManager.GetActiveScene();
            previousRoots.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (GameObject obj in SceneManager.GetSceneAt(i).GetRootGameObjects()) previousRoots.Add(new KeyValuePair<GameObject, bool>(obj, obj.activeSelf));
            foreach (KeyValuePair<GameObject, bool> state in previousRoots) if (state.Key != null) state.Key.SetActive(false);
            scene = SceneManager.CreateScene("Spotlight_FormalUiNative_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(scene);
            PrototypeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/UI_Game.prefab");
            Assert.NotNull(catalog); Assert.NotNull(prefab);
            GameUiReferences references = prefab.GetComponent<GameUiReferences>(); Assert.NotNull(references);
            directory = Path.Combine(Path.GetTempPath(), "Spotlight_FormalUiNative_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            cameraObject = new GameObject("Formal UI native camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>(); camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            host = new GameObject("Formal UI native bootstrap"); host.SetActive(false);
            GameBootstrap bootstrap = host.AddComponent<GameBootstrap>();
            bootstrap.CatalogAsset = catalog; bootstrap.UiPrefab = references;
            bootstrap.WorldCamera = camera; bootstrap.SaveDirectoryOverride = directory;
            host.SetActive(true); yield return null; yield return null; yield return null;
            Assert.NotNull(bootstrap.Services);
            Assert.AreEqual(1, host.GetComponentsInChildren<GameUiReferences>(true).Length);
            Assert.AreEqual(1, host.GetComponentsInChildren<GameView>(true).Length);
            GameView view = host.GetComponent<GameView>(); Assert.AreSame(references, view.UiPrefab);
            GameUiReferences ui = host.GetComponentInChildren<GameUiReferences>(true); Assert.NotNull(ui);
            Assert.AreEqual(1, ui.GetComponentsInChildren<Canvas>(true).Length);
            Assert.AreSame(ui.Canvas, ui.GetComponentInChildren<Canvas>(true));
            foreach (Transform child in host.GetComponentsInChildren<Transform>(true)) Assert.AreNotEqual("Game UI", child.name, "Formal binding must not create prototype Game UI.");
            ClickLabel(ui.ActionsRoot, "故事模式"); yield return null; yield return null;
            ModuleComposition runtime = bootstrap.Services;
            Assert.AreEqual(GamePhase.DayEvent, runtime.Flow.GetSnapshot().Phase);
            DayEventSnapshot day = runtime.Gameplay.DayEvents.GetSnapshot();
            Assert.Greater(day.Options.Count, 0);
            ClickLabel(ui.ModalRoot, day.Options[0].Text); yield return null; yield return null;
            Assert.AreEqual(GamePhase.Build, runtime.Flow.GetSnapshot().Phase);
            if (!runtime.Flow.GetSnapshot().TutorialShown && !string.IsNullOrEmpty(runtime.Catalog.GetSettings().TutorialText))
            { ClickLabel(ui.ModalRoot, "开始部署"); yield return null; }
            MethodInfo click = typeof(GameView).GetMethod("HandleBoardClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? typeof(GameView).GetMethod("OnBoardCellClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(click, "Use the actual input-controller board-click entry.");
            long gold = runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold");
            long water = runtime.Core.Resources.GetQuantity(ResourceBucket.Hand, "elm.water");
            ClickLabel(ui.ActionsRoot, "建造攻击塔"); click.Invoke(view, new object[] { new CellCoord(2, 1) });
            ui.ElementButtons[0].onClick.Invoke(); click.Invoke(view, new object[] { new CellCoord(2, 2) });
            yield return null; yield return null; yield return null;
            Assert.AreEqual(gold - 20, runtime.Core.Resources.GetQuantity(ResourceBucket.Inventory, "res.gold"));
            Assert.AreEqual(water - 1, runtime.Core.Resources.GetQuantity(ResourceBucket.Hand, "elm.water"));
            Assert.That(ui.GoldText.text, Does.Contain((gold - 20).ToString()));
            Assert.That(ui.HandTexts[0].text, Does.Contain((water - 1).ToString()));
            Assert.That(ui.SpringHpText.text, Does.Contain("100"));
            Assert.AreEqual(1, host.GetComponentsInChildren<GameUiReferences>(true).Length);
            Assert.AreEqual(1, ui.GetComponentsInChildren<Canvas>(true).Length);
            Canvas.ForceUpdateCanvases(); yield return null;
            AssertRenderedText(ui.TitleText, "Formal title");
            AssertRenderedText(ui.SpringHpText, "Spring health text");
            FocusGameWindow(); NativeScreenshotProbe probe = host.AddComponent<NativeScreenshotProbe>(); probe.Begin();
            for (int frame = 0; frame < 120 && !probe.Completed; frame++) yield return null;
            Assert.IsTrue(probe.Completed, "Screenshot did not reach the render boundary within 120 frames.");
            Assert.IsNull(probe.Error); Assert.NotNull(probe.Image);
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/Formal-UI-preview.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, probe.Image.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(probe.Image); probe.Image = null; }
            LogAssert.NoUnexpectedReceived();
        }
        private static void AssertRenderedText(Text text, string label)
        {
            Assert.NotNull(text, label);
            Assert.IsTrue(text.isActiveAndEnabled, label + " must be visible.");
            Assert.Greater(text.canvasRenderer.GetAlpha(), 0, label + " must not be transparent.");
            Assert.Greater(text.cachedTextGenerator.vertexCount, 4, label + " has no rendered glyph vertices; check font line height and text bounds.");
            Assert.Greater(text.cachedTextGenerator.characterCountVisible, 0, label + " needs visible glyph characters.");
        }
        private static void ClickLabel(Transform root, string label)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                Text text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == label) { Assert.IsTrue(button.interactable); button.onClick.Invoke(); return; }
            }
            Assert.Fail("Missing formal UI button: " + label);
        }
        private static void FocusGameWindow()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("UnityEditor.GameView");
                if (type != null) { EditorWindow.GetWindow(type, false, "Game", true).Focus(); return; }
            }
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (host != null) UnityEngine.Object.Destroy(host);
            if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
            yield return null;
            foreach (KeyValuePair<GameObject, bool> state in previousRoots) if (state.Key != null) state.Key.SetActive(state.Value);
            previousRoots.Clear();
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (scene.IsValid() && scene.isLoaded)
            { AsyncOperation unload = SceneManager.UnloadSceneAsync(scene); if (unload != null) yield return unload; }
            if (directory != null && Directory.Exists(directory))
            {
                string absolute = Path.GetFullPath(directory);
                string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.IsTrue(absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(Path.GetFileName(absolute).StartsWith("Spotlight_FormalUiNative_", StringComparison.Ordinal));
                Directory.Delete(absolute, true);
            }
            host = cameraObject = null; directory = null;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
#endif
