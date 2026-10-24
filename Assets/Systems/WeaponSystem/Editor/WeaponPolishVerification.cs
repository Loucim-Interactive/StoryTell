#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace StoryTell.Weapons.Editor
{
    [InitializeOnLoad]
    public static class WeaponPolishVerification
    {
        private static TestRunnerApi runner;
        static WeaponPolishVerification()
        {
            if (SessionState.GetBool("WeaponPolishVerificationRunning", false)) Register();
        }
        public static void ApplyAndVerify()
        {
            Level1WeaponPolishInstaller.Apply();
            SessionState.SetBool("WeaponPolishVerificationRunning", true);
            Register();
            File.WriteAllText("Library/WeaponPolish-verification.json", "{\"status\":\"testing\"}");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                testNames = new[] { "Level1WeaponIntegrationTests", "WeaponPolishTests", "WeaponSystemTests", "Level1PhysicalMapIntegrationTests" } }));
        }
        private static void Register() { runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results()); }
        [Serializable] private sealed class Summary { public string status; public int passed, failed; }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                SessionState.SetBool("WeaponPolishVerificationRunning", false);
                File.WriteAllText("Library/WeaponPolish-tests.xml", result.ToXml().OuterXml);
                File.WriteAllText("Library/WeaponPolish-verification.json", JsonUtility.ToJson(new Summary {
                    status = result.FailCount == 0 ? "passed" : "failed", passed = result.PassCount, failed = result.FailCount }, true));
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(result.FailCount == 0 ? 0 : 1);
            }
        }
    }
}
#endif
