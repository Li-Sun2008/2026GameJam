#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Presentation;

namespace Spotlight.Tests.Integration
{
    public sealed class ThirdRoundUiIntegrationTests
    {
        private GameObject host;
        private GameView view;
        private ModuleComposition runtime;
        private GameUiReferences prefab,temporaryPrefab;
        private string directory;
        [SetUp] public void Setup()
        {
            prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/UI_Game.prefab").GetComponent<GameUiReferences>();
            Assert.NotNull(prefab.TutorialPanelPrefab,"Run Spotlight.Editor.ThirdRoundUiSetup.Apply first.");
            Assert.NotNull(prefab.DayEventPanelPrefab);
            string error;Assert.IsTrue(prefab.TutorialPanelPrefab.Validate(out error),error);Assert.IsTrue(prefab.DayEventPanelPrefab.Validate(out error),error);
            directory=Path.Combine(Path.GetTempPath(),"Spotlight_ThirdRoundUi_"+Guid.NewGuid().ToString("N"));
            runtime=new ModuleComposition(AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath).BuildCatalogData(),directory);
            host=new GameObject("Third round real UI tests");view=host.AddComponent<GameView>();view.UiPrefab=prefab;view.Bind(runtime.ViewContext);
        }
        [TearDown] public void Cleanup()
        {
            if(view!=null)view.Unbind();if(host!=null)UnityEngine.Object.DestroyImmediate(host);
            if(temporaryPrefab!=null)UnityEngine.Object.DestroyImmediate(temporaryPrefab.gameObject);
            if(runtime!=null)runtime.Dispose();
            if(directory!=null&&Directory.Exists(directory))
            {
                string target=Path.GetFullPath(directory),root=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                Assert.IsTrue(target.StartsWith(root,StringComparison.OrdinalIgnoreCase));Assert.IsTrue(Path.GetFileName(target).StartsWith("Spotlight_ThirdRoundUi_"));Directory.Delete(target,true);
            }
        }
        private GameUiReferences Ui { get { return host.GetComponentInChildren<GameUiReferences>(true); } }
        private void Pump(){runtime.Advance(0);view.RefreshAll();}
        private void NewRun(){FindButton(Ui.ActionsRoot,"故事模式").onClick.Invoke();Pump();Assert.AreEqual(GamePhase.DayEvent,runtime.Flow.GetSnapshot().Phase);}
        private static Button FindButton(Transform root,string caption)
        {
            foreach(Button button in root.GetComponentsInChildren<Button>(true))
                if(button.gameObject.activeInHierarchy&&button.GetComponentInChildren<Text>(true).text==caption)return button;
            Assert.Fail("Missing visible button: "+caption);return null;
        }
        private void EnterBuild()
        {
            NewRun();DayEventSnapshot snapshot=runtime.Gameplay.DayEvents.GetSnapshot();FindButton(Ui.ModalRoot,snapshot.Options[0].Text).onClick.Invoke();Pump();
            Assert.AreEqual(GamePhase.Build,runtime.Flow.GetSnapshot().Phase);
            TutorialPanelReferences tutorial=Ui.ModalRoot.GetComponentInChildren<TutorialPanelReferences>(true);
            if(!runtime.Flow.GetSnapshot().TutorialShown&&!string.IsNullOrEmpty(runtime.Catalog.GetSettings().TutorialText))
            {
                if(tutorial!=null)Assert.AreEqual(runtime.Catalog.GetSettings().TutorialText,tutorial.BodyText.text);
                FindButton(Ui.ModalRoot,"开始部署").onClick.Invoke();Pump();Assert.IsTrue(runtime.Flow.GetSnapshot().TutorialShown);
            }
        }
        [Test] public void RealSnapshotsPopulatePanelAndRefreshDoesNotRecreateOrMultiplyListeners()
        {
            NewRun();DayEventSnapshot snapshot=runtime.Gameplay.DayEvents.GetSnapshot();DayEventPanelReferences panel=Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>();
            Assert.NotNull(panel);Assert.AreEqual(snapshot.Text,panel.BodyText.text);Assert.That(panel.TitleText.text,Does.Contain(snapshot.State.DayIndex.ToString()));
            Assert.IsFalse(panel.OptionButtonTemplate.gameObject.activeSelf);
            int count=0;foreach(Button button in panel.OptionsRoot.GetComponentsInChildren<Button>(true))if(button.gameObject.activeSelf)count++;
            Assert.AreEqual(snapshot.Options.Count,count);
            int id=panel.GetInstanceID();for(int i=0;i<5;i++)view.RefreshAll();Assert.AreEqual(id,Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>().GetInstanceID());
            int external=0;Button option=FindButton(panel.OptionsRoot,snapshot.Options[0].Text);Button.ButtonClickedEvent clicks=option.onClick;clicks.AddListener(delegate{external++;});
            option.onClick.Invoke();Pump();Assert.IsTrue(runtime.Gameplay.DayEvents.GetSnapshot().State.Resolved);Assert.AreEqual(1,external);
            long revision=runtime.Core.Transactions.Revision;clicks.Invoke();Assert.AreEqual(2,external);Assert.AreEqual(revision,runtime.Core.Transactions.Revision,"Closed panel must detach only its own listener.");
        }
        [Test] public void RepeatBindingAndRealConfirmationKeepOneCanvasAndWidePausedText()
        {
            EnterBuild();view.Bind(runtime.ViewContext);view.Bind(runtime.ViewContext);
            Assert.AreEqual(1,host.GetComponentsInChildren<Canvas>(true).Length);Assert.AreEqual(1,host.GetComponentsInChildren<GameUiReferences>(true).Length);
            Ui.PauseButton.onClick.Invoke();Ui.DoubleSpeedButton.onClick.Invoke();Pump();
            Assert.AreEqual("2 倍 · 已暂停",Ui.SpeedText.text);Assert.AreEqual(220,Ui.SpeedText.rectTransform.sizeDelta.x,.01);Assert.AreEqual(36,Ui.SpeedText.rectTransform.sizeDelta.y,.01);
            Ui.SpeedText.cachedTextGenerator.Populate(Ui.SpeedText.text,Ui.SpeedText.GetGenerationSettings(Ui.SpeedText.rectTransform.rect.size));
            Assert.GreaterOrEqual(Ui.SpeedText.cachedTextGenerator.characterCountVisible,Ui.SpeedText.text.Length);
        }
        // Presentation-only fixture expands a real snapshot; it never modifies the shipped event catalog.
        private sealed class EventProjection : IDayEventService
        {
            internal IDayEventService Real;internal DayEventSnapshot Snapshot;internal bool Queue;internal int Calls;internal CommandContext QueuedContext;
            public DayEventSnapshot GetSnapshot(){return Snapshot;}
            public OperationResult EnsureEvent(int dayIndex,GameMode mode){return Real.EnsureEvent(dayIndex,mode);}
            public OperationResult TryChoose(CommandContext context,string instanceId,string optionId)
            {Calls++;if(!Queue)return Real.TryChoose(context,instanceId,optionId);QueuedContext=context;return new OperationResult(OperationState.Queued,ErrorCode.None,context.CommandId,context.ExpectedRevision,"queued");}
        }
        private EventProjection Expand(bool queue)
        {
            NewRun();DayEventSnapshot actual=runtime.Gameplay.DayEvents.GetSnapshot();List<EventOptionDefinition> options=new List<EventOptionDefinition>();
            for(int i=0;i<7;i++)options.Add(new EventOptionDefinition{Id="qa.invalid."+i,Text="展示选项 "+i+"："+new string('长',120)});
            EventProjection projection=new EventProjection{Real=runtime.Gameplay.DayEvents,Snapshot=new DayEventSnapshot{State=actual.State,Text=new string('长',2400),Options=options},Queue=queue};
            runtime.ViewContext.DayEvents=projection;view.Bind(runtime.ViewContext);return projection;
        }
        [Test] public void LongTextAndSevenOptionsHaveScrollableContentAndReadableRows()
        {
            Expand(false);DayEventPanelReferences panel=Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>();
            Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(panel.Root);Canvas.ForceUpdateCanvases();
            Assert.Greater(panel.BodyScroll.content.rect.height,panel.BodyScroll.viewport.rect.height);
            ScrollRect options=panel.OptionsRoot.GetComponentInParent<ScrollRect>();Assert.NotNull(options);Assert.Greater(panel.OptionsRoot.rect.height,options.viewport.rect.height);
            int count=0;foreach(Button button in panel.OptionsRoot.GetComponentsInChildren<Button>())
            {
                count++;Text text=button.GetComponentInChildren<Text>();Assert.GreaterOrEqual(text.rectTransform.rect.height+1,text.preferredHeight);
            }
            Assert.AreEqual(7,count);options.verticalNormalizedPosition=0;Assert.AreEqual(0,options.verticalNormalizedPosition,.01);
        }
        [Test] public void QueuedChoiceBlocksDoubleClickAndFailurePreservesMandatoryPanelAndBoardBlock()
        {
            EventProjection projection=Expand(true);DayEventPanelReferences panel=Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>();Button button=FindButton(panel.OptionsRoot,projection.Snapshot.Options[0].Text);
            button.onClick.Invoke();button.onClick.Invoke();Assert.AreEqual(1,projection.Calls);Assert.IsFalse(button.interactable);
            runtime.Events.Publish(new DayEventChangedEvent{Snapshot=projection.Snapshot});runtime.Events.Flush();Assert.AreSame(panel,Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>());
            OperationResult rejected=projection.Real.TryChoose(projection.QueuedContext,projection.Snapshot.State.InstanceId,"qa.invalid.0");Assert.AreNotEqual(OperationState.Committed,rejected.State);
            runtime.Events.Publish(new CommandFinishedEvent{Result=rejected});runtime.Events.Flush();Assert.IsTrue(button.interactable);Assert.AreEqual(GamePhase.DayEvent,runtime.Flow.GetSnapshot().Phase);
            Assert.IsFalse(runtime.Gameplay.DayEvents.GetSnapshot().State.Resolved);Assert.AreSame(panel,Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>());
            bool visibleFailure=false;foreach(Text text in panel.GetComponentsInChildren<Text>())if(!string.IsNullOrEmpty(text.text)&&text.text!="正在处理…"&&text.text==Ui.HintText.text)visibleFailure=true;
            Assert.IsTrue(visibleFailure,"Failure should remain readable inside the modal.");
            FieldInfo blocked=typeof(GameView).GetField("blocked",BindingFlags.NonPublic|BindingFlags.Instance);Assert.IsTrue((bool)blocked.GetValue(view));
            long revision=runtime.Core.Transactions.Revision;typeof(GameView).GetMethod("HandleBoardClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new CellCoord(2,1)});Assert.AreEqual(revision,runtime.Core.Transactions.Revision);
        }
        [Test] public void NullOptionalPrefabsUseDynamicScrollableFallbackAndRealOperations()
        {
            view.Unbind();temporaryPrefab=UnityEngine.Object.Instantiate(prefab);temporaryPrefab.gameObject.SetActive(false);
            temporaryPrefab.TutorialPanelPrefab=null;temporaryPrefab.DayEventPanelPrefab=null;view.UiPrefab=temporaryPrefab;view.Bind(runtime.ViewContext);Ui.gameObject.SetActive(true);
            EnterBuild();Assert.AreEqual(1,host.GetComponentsInChildren<Canvas>(true).Length);Assert.IsNull(Ui.ModalRoot.GetComponentInChildren<DayEventPanelReferences>());
        }
    }
}
#endif
