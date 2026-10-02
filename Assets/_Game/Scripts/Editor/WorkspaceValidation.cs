using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Spotlight.Editor
{
    /// <summary>在已打开的编辑器内运行检查，不关闭编辑器或切换用户正在编辑的场景。</summary>
    [InitializeOnLoad]
    public static class WorkspaceValidation
    {
        private static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification"));
        private static TestRunnerApi runner;
        private static bool running;
        private const string RunningKey = "Spotlight.WorkspaceValidation.Running";
        static WorkspaceValidation()
        {
            running = SessionState.GetBool(RunningKey, false);
            EditorApplication.delayCall += EnsureRunner;
            EditorApplication.update += CheckRequest;
        }

        private static void EnsureRunner()
        {
            if (runner != null) return;
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
        }

        [MenuItem("聚光灯/运行主程 EditMode 检查")]
        public static void RunEditMode()
        {
            if (running || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Root);
            running = true;
            SessionState.SetBool(RunningKey, true);
            EnsureRunner();
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[]
            {
                "Spotlight.Core.Tests", "Spotlight.Enemies.Tests", "Spotlight.Gameplay.Tests", "Spotlight.Integration.Tests"
            } }));
        }

        private static void CheckRequest()
        {
            if (running || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string setup = Path.Combine(Root, "request-setup.txt");
            if (File.Exists(setup))
            {
                File.Delete(setup);
                ProjectSetupTool.Generate();
                return;
            }
            string path = Path.Combine(Root, "request-editmode.txt");
            if (!File.Exists(path)) return;
            File.Delete(path);
            RunEditMode();
        }

        [Serializable]
        private sealed class Report
        {
            public string FinishedUtc;
            public int Passed, Failed, Skipped;
            public string[] Failures;
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { running = true; SessionState.SetBool(RunningKey, true); }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                List<string> failures = new List<string>();
                Collect(result, failures);
                Report report = new Report { FinishedUtc = DateTime.UtcNow.ToString("o"), Passed = result.PassCount, Failed = result.FailCount, Skipped = result.SkipCount, Failures = failures.ToArray() };
                File.WriteAllText(Path.Combine(Root, "Unity-EditMode-results.json"), JsonUtility.ToJson(report, true));
                running = false;
                SessionState.SetBool(RunningKey, false);
                Debug.Log("聚光灯主程检查：通过 " + report.Passed + "，失败 " + report.Failed + "。报告：Docs/Verification/Unity-EditMode-results.json");
            }
            private static void Collect(ITestResultAdaptor result, List<string> failures)
            {
                if (!result.HasChildren && result.ResultState.StartsWith("Failed", StringComparison.Ordinal)) failures.Add(result.FullName + "\n" + result.Message + "\n" + result.StackTrace);
                if (result.HasChildren) foreach (ITestResultAdaptor child in result.Children) Collect(child, failures);
            }
        }
    }
}
