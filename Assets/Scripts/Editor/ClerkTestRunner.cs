#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class ClerkTestRunner
{
    [MenuItem("Clerk/Validation/Run Edit Mode Tests")]
    public static void RunEditModeTests()
    {
        TestRunnerApi api =
            ScriptableObject.CreateInstance<
                TestRunnerApi>();

        ClerkTestCallbacks callbacks =
            ScriptableObject.CreateInstance<
                ClerkTestCallbacks>();

        callbacks.hideFlags =
            HideFlags.HideAndDontSave;

        api.RegisterCallbacks(callbacks);

        Filter filter = new Filter
        {
            testMode = TestMode.EditMode,
            assemblyNames =
                new[]
                {
                    "Clerk.EditModeTests"
                }
        };

        api.Execute(
            new ExecutionSettings(filter));
    }
}

public sealed class ClerkTestCallbacks :
    ScriptableObject,
    ICallbacks
{
    public void RunStarted(ITestAdaptor testsToRun)
    {
        Debug.Log(
            "[Clerk Validation] Edit mode tests started.");
    }

    public void RunFinished(
        ITestResultAdaptor result)
    {
        string message =
            "[Clerk Validation] " +
            result.PassCount +
            " passed, " +
            result.FailCount +
            " failed, " +
            result.SkipCount +
            " skipped.";

        if (result.FailCount > 0)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    public void TestStarted(ITestAdaptor test)
    {
    }

    public void TestFinished(
        ITestResultAdaptor result)
    {
        if (result.FailCount > 0)
        {
            Debug.LogError(
                "[Clerk Validation] " +
                result.FullName +
                "\n" +
                result.Message +
                "\n" +
                result.StackTrace);
        }
    }
}
#endif
