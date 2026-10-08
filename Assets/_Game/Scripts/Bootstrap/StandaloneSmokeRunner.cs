using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Bootstrap
{
    /// <summary>Explicit smoke experiment. Real commands; only GameBootstrap advances simulation.</summary>
    public sealed class StandaloneSmokeRunner : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool succeeded; public string utc,platform,error,limitation,isolatedSaveDirectory;
            public List<string> checks=new List<string>(); public RuntimeQaRecorder.Snapshot finalSnapshot;
            public RuntimeQaRecorder.Quantity[] initialResources;
            public RuntimeQaRecorder.Snapshot nightOne,nightTwo;
            public RuntimeQaRecorder.Settlement[] production;
        }
        static string isolatedDirectory;
        GameBootstrap bootstrap; ModuleComposition services; readonly Report report=new Report(); bool finished;
        public static string PrepareIsolatedSaveDirectory()
        {
            if(!RuntimeQaRecorder.HasArgument("-spotlightSmoke"))return null;
            if(isolatedDirectory==null){isolatedDirectory=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"Spotlight_R3_Smoke_"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(isolatedDirectory);}
            return isolatedDirectory;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if(RuntimeQaRecorder.HasArgument("-spotlightSmoke"))new GameObject("Spotlight Explicit Player Smoke").AddComponent<StandaloneSmokeRunner>();
        }
        IEnumerator Start()
        {
            IEnumerator experiment=ExecuteSmoke();
            while(true)
            {
                bool more;object current=null;
                try { more=experiment.MoveNext();if(more)current=experiment.Current; }
                catch(Exception error) { Finish(error.ToString());more=false; }
                if(!more)yield break;yield return current;
            }
        }
        IEnumerator ExecuteSmoke()
        {
            report.utc=DateTime.UtcNow.ToString("o");report.platform=Application.platform.ToString();report.isolatedSaveDirectory=isolatedDirectory;
            report.limitation="Seed 456; zero-based Day1 towers (1,1)/(3,1)/(5,1)/(5,3)/(7,1), water (1,2), existing tower_stack on (5,1). Day2 existing heal on Spring, add tower (6,3) using real drops. First legal event. Real commands/Bootstrap Update; no HP overrides or UI/material validation. Temp saves retained. Success requires an executed report.";
            yield return null;
            bootstrap=UnityEngine.Object.FindObjectOfType<GameBootstrap>();
            if(bootstrap==null||bootstrap.Services==null){Finish("Real Bootstrap initialization failed.");yield break;}
            services=bootstrap.Services;
            if(string.IsNullOrEmpty(isolatedDirectory)||bootstrap.SaveDirectoryOverride!=isolatedDirectory){Finish("Save isolation failed.");yield break;}
            if(!Commit("new run",services.Flow.TryNewRun(Command(),new NewRunRequest {LevelId="level.prototype",Mode=GameMode.Story,Seed=456})))yield break;
            yield return null;if(!ChooseEvent())yield break;yield return null;
            RuntimeQaRecorder qa=bootstrap.GetComponent<RuntimeQaRecorder>();if(qa!=null)report.initialResources=qa.Capture().resources;
            if(!Commit("dismiss tutorial",services.Flow.TryDismissTutorial(Command())))yield break;
            foreach(int x in new[]{1,3,5,7})if(!Commit("tower ("+x+",1)",services.Core.Deployment.TryPlace(Command(),new PlacementRequest {Kind=OccupantKind.Tower,DefinitionId="tower.basic",Cell=new CellCoord(x,1)})))yield break;
            if(!Commit("tower (5,3)",services.Core.Deployment.TryPlace(Command(),new PlacementRequest {Kind=OccupantKind.Tower,DefinitionId="tower.basic",Cell=new CellCoord(5,3)})))yield break;
            if(!Commit("water (1,2)",services.Core.Deployment.TryPlace(Command(),new PlacementRequest {Kind=OccupantKind.ElementBlock,DefinitionId="elm.water",Cell=new CellCoord(1,2)})))yield break;
            EntityId enhanced=new EntityId();foreach(CellSnapshot cell in services.Core.Board.GetSnapshot().Cells)if(cell.Cell.X==5&&cell.Cell.Y==1)enhanced=cell.OccupantId;
            if(!Commit("existing tower_stack (5,1)",services.Gameplay.Items.TryUse(Command(),new ItemUseRequest {ItemId="item.tower_stack",Target=enhanced})))yield break;
            yield return null;if(!VerifySaveLoad())yield break;
            if(!Commit("start night 1",services.Flow.TryStartNight(Command())))yield break;
            Guid pause=services.Clock.AcquirePause(PauseReason.Player);ClockSnapshot before=services.Clock.GetSnapshot();
            yield return null;yield return null;yield return null;
            ClockSnapshot after=services.Clock.GetSnapshot();services.Clock.ReleasePause(pause);
            if(before.GameTime!=after.GameTime||before.TickIndex!=after.TickIndex||!after.IsPaused){Finish("Pause changed clock/ticks.");yield break;}report.checks.Add("pause holds clock/ticks across three frames");
            if(!Commit("2x",services.Clock.TrySetSpeed(Command(),GameSpeed.Double)))yield break;
            double gameStart=services.Clock.GetSnapshot().GameTime;float realStart=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-realStart<1f&&services.Flow.GetSnapshot().Phase==GamePhase.Night)yield return null;
            double gameDelta=services.Clock.GetSnapshot().GameTime-gameStart;float realDelta=Time.realtimeSinceStartup-realStart;
            if(services.Flow.GetSnapshot().Phase!=GamePhase.Night||gameDelta<realDelta*1.5||gameDelta>realDelta*2.5){Finish("2x clock measurement failed: game="+gameDelta+", real="+realDelta);yield break;}report.checks.Add("2x game delta="+gameDelta+", real="+realDelta);
            for(int night=1;night<=2;night++)
            {
                float deadline=Time.realtimeSinceStartup+180f;
                while(services.Flow.GetSnapshot().Phase==GamePhase.Night&&Time.realtimeSinceStartup<deadline)yield return null;
                if(services.Flow.GetSnapshot().Phase!=GamePhase.NightResult){Finish("Night "+night+" reached "+services.Flow.GetSnapshot().Phase+" or timeout; layout unproven.");yield break;}
                report.checks.Add("real night "+night+" reached NightResult");
                if(qa!=null){if(night==1)report.nightOne=qa.Capture();else report.nightTwo=qa.Capture();}
                if(night==2)break;
                RuntimeQaRecorder recorder=bootstrap.GetComponent<RuntimeQaRecorder>();if(recorder!=null)recorder.Observe();
                if(!Commit("confirm production",services.Flow.TryConfirmNightResult(Command())))yield break;
                yield return null;if(qa!=null)report.production=qa.Capture().settlements;
                if(!ChooseEvent())yield break;yield return null;
                if(!Commit("existing Spring heal Day2",services.Gameplay.Items.TryUse(Command(),new ItemUseRequest {ItemId="item.heal",Target=services.Core.World.SpringId})))yield break;
                if(!Commit("Day2 tower (6,3) from gold drops",services.Core.Deployment.TryPlace(Command(),new PlacementRequest {Kind=OccupantKind.Tower,DefinitionId="tower.basic",Cell=new CellCoord(6,3)})))yield break;
                if(!VerifySaveLoad())yield break;
                if(!Commit("start night 2",services.Flow.TryStartNight(Command())))yield break;
                if(!Commit("night 2 2x",services.Clock.TrySetSpeed(Command(),GameSpeed.Double)))yield break;
            }
            Finish(null);
        }
        bool ChooseEvent()
        {
            if(services.Flow.GetSnapshot().Phase==GamePhase.Build)return true;
            if(services.Flow.GetSnapshot().Phase!=GamePhase.DayEvent)return Fail("Unexpected event phase.");
            DayEventSnapshot day=services.Gameplay.DayEvents.GetSnapshot();if(day.State==null||day.Options==null)return Fail("Missing event snapshot.");
            foreach(EventOptionDefinition option in day.Options)
            {
                OperationResult result=services.Gameplay.DayEvents.TryChoose(Command(),day.State.InstanceId,option.Id);
                if(result.State==OperationState.Committed){report.checks.Add("day "+day.State.DayIndex+" event "+day.State.DefinitionId+" / "+option.Id);return true;}
            }
            return Fail("No legal event option committed.");
        }
        bool VerifySaveLoad()
        {
            if(!Commit("save isolated checkpoint",services.Saves.SaveCheckpoint(Command(),SaveReason.Manual)))return false;
            SaveReadResult read=services.Saves.ReadCheckpoint();if(read.Error!=ErrorCode.None||read.Data==null)return Fail("Read failed: "+read.Error);
            string before=Resources();int day=services.Flow.GetSnapshot().DayIndex;
            if(!Commit("return menu",services.Flow.TryReturnToMenu(Command())))return false;
            if(!Commit("continue checkpoint",services.Flow.TryLoadRun(Command(),read.Data)))return false;
            if(services.Flow.GetSnapshot().DayIndex!=day||before!=Resources())return Fail("Save/load changed day/resources.");
            report.checks.Add("save/continue preserves day/resources");return true;
        }
        string Resources()
        {
            var values=new List<string>();foreach(ResourceBucket bucket in Enum.GetValues(typeof(ResourceBucket)))foreach(ResourceAmount r in services.Core.Resources.GetSnapshot(bucket))values.Add(bucket+":"+r.DefinitionId+"="+r.Amount);values.Sort(StringComparer.Ordinal);return string.Join(";",values.ToArray());
        }
        CommandContext Command(){return services.Core.Commands.Create(services.Core.Transactions.Revision);}
        bool Commit(string name,OperationResult result){if(result.State!=OperationState.Committed)return Fail(name+": "+result.State+" / "+result.Error);report.checks.Add(name);return true;}
        bool Fail(string error){Finish(error);return false;}
        void Finish(string error)
        {
            if(finished)return;finished=true;report.error=error;report.succeeded=error==null;
            if(bootstrap!=null){RuntimeQaRecorder recorder=bootstrap.GetComponent<RuntimeQaRecorder>();if(recorder!=null)report.finalSnapshot=recorder.Capture();}
            try
            {
                string path=RuntimeQaRecorder.Argument("-spotlightSmokeReport");
                if(string.IsNullOrEmpty(path))path=Path.Combine(Application.persistentDataPath,"QA","R3","smoke-report.json");
                else if(!Path.IsPathRooted(path))throw new ArgumentException("Smoke report must be an absolute path.");
                path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("Smoke report: "+path);
            }
            catch(Exception e){Debug.LogError("Smoke report failed: "+e);report.succeeded=false;}
            Application.Quit(report.succeeded?0:1);
        }
    }
}
