using System;
using System.Collections.Generic;
using System.IO;
using Spotlight.Bootstrap;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Spotlight.Editor
{
    public static class ThirdRoundBuild
    {
        const string Scene="Assets/_Game/Scenes/00_Bootstrap.unity";
        [Serializable] sealed class Report { public string output,result,utc; public bool succeeded; public int warnings,errors; public string[] scenes,messages; }
        [MenuItem("聚光灯/QA/构建 Windows 第三轮")]
        public static void BuildWindowsBatch()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=RuntimeQaRecorder.Argument("-spotlightBuildOutput");
            output=string.IsNullOrEmpty(output)?Path.Combine(root,"Builds","Windows","Spotlight.exe"):Path.GetFullPath(Path.IsPathRooted(output)?output:Path.Combine(root,output));
            if(!output.StartsWith(Path.Combine(root,"Builds")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!string.Equals(Path.GetExtension(output),".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Output must be an .exe under this project's Builds directory.");
            if(!File.Exists(Path.Combine(root,Scene)))throw new FileNotFoundException("Missing Bootstrap scene",Scene);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport build=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
            var messages=new List<string>();foreach(BuildStep step in build.steps)foreach(BuildStepMessage message in step.messages)messages.Add(message.type+": "+message.content);
            var report=new Report {output=output,result=build.summary.result.ToString(),utc=DateTime.UtcNow.ToString("o"),succeeded=build.summary.result==BuildResult.Succeeded,warnings=build.summary.totalWarnings,errors=build.summary.totalErrors,scenes=new[]{Scene},messages=messages.ToArray()};
            string path=Path.Combine(Path.GetDirectoryName(output),"build-report.json");File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("Windows build report: "+path);
            if(!report.succeeded)throw new InvalidOperationException("Windows build failed: "+report.result);
        }
        [MenuItem("聚光灯/QA/查看当前运行快照")]
        public static void ViewSnapshot() { RuntimeSnapshotWindow window=EditorWindow.GetWindow<RuntimeSnapshotWindow>("运行快照（只读）");window.Refresh();window.Show(); }
    }
    public sealed class RuntimeSnapshotWindow : EditorWindow
    {
        string snapshot="请先在 00_Bootstrap 场景进入 Play。"; Vector2 scroll;
        RuntimeQaRecorder FindRecorder() { return Application.isPlaying?UnityEngine.Object.FindObjectOfType<RuntimeQaRecorder>():null; }
        public void Refresh() { RuntimeQaRecorder recorder=FindRecorder();snapshot=recorder==null?"当前没有运行中的 QA observer。":JsonUtility.ToJson(recorder.Capture(),true);Repaint(); }
        void OnGUI()
        {
            if(GUILayout.Button("刷新只读快照"))Refresh();
            using(new EditorGUI.DisabledScope(FindRecorder()==null))if(GUILayout.Button("导出 JSON 到 QA/R3/Runtime")){RuntimeQaRecorder recorder=FindRecorder();if(recorder!=null)snapshot="已导出："+recorder.Export()+"\n\n"+JsonUtility.ToJson(recorder.Capture(),true);}
            scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.SelectableLabel(snapshot,EditorStyles.textArea,GUILayout.MinHeight(600),GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
    }
}
