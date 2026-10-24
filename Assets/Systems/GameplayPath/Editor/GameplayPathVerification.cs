#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace StoryTell.GameplayPath.Editor
{
    [InitializeOnLoad]
    public static class GameplayPathVerification
    {
        private static TestRunnerApi runner;
        static GameplayPathVerification(){if(SessionState.GetBool("GameplayPathTesting",false))Register();}
        public static void Run()
        {
            SessionState.SetBool("GameplayPathTesting",true);Register();GameplayPathInstaller.Report("testing","Running progress, scene, navigation and runtime checks.");
            runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{"GameplayPathTests","WeaponSystemTests","PhysicalMapWorldReferenceTests","Level1PhysicalMapIntegrationTests","Level1WeaponIntegrationTests","WeaponPolishTests"}}));
        }
        private static void Register(){runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());}
        [Serializable]private sealed class Summary{public string status;public int passed,failed,skipped;}
        private sealed class Results:ICallbacks
        {
            public void RunStarted(ITestAdaptor t){} public void TestStarted(ITestAdaptor t){} public void TestFinished(ITestResultAdaptor r){}
            public void RunFinished(ITestResultAdaptor r)
            {
                SessionState.SetBool("GameplayPathTesting",false);File.WriteAllText("Library/GameplayPath-tests.xml",r.ToXml().OuterXml);
                File.WriteAllText("Library/GameplayPath-tests.json",JsonUtility.ToJson(new Summary{status=r.FailCount==0?"passed":"failed",passed=r.PassCount,failed=r.FailCount,skipped=r.SkipCount},true));
                if(!EditorApplication.isPlaying)EditorApplication.delayCall+=()=>EditorSceneManager.OpenScene(GameplayPathInstaller.ScenePath);
            }
        }
    }
}
#endif
