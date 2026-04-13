using System;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FarmSimVR.Editor
{
    public static class BatchmodeTestRunner
    {
        public static void RunEditMode()
        {
            BatchmodeTestRun.Start(TestMode.EditMode);
        }

        public static void RunPlayMode()
        {
            BatchmodeTestRun.Start(TestMode.PlayMode);
        }
    }

    internal sealed class BatchmodeTestRun : ScriptableObject, IErrorCallbacks
    {
        private const string ResultsArgName = "-batchTestResults";
        private const string FilterArgName = "-batchTestFilter";

        private static BatchmodeTestRun _activeRun;

        private TestRunnerApi _testRunnerApi;
        private TestMode _testMode;
        private string _resultsPath;
        private string _testFilter;

        internal static void Start(TestMode testMode)
        {
            if (_activeRun != null)
            {
                Debug.LogError("[BatchmodeTestRunner] A batch test run is already active.");
                EditorApplication.Exit(1);
                return;
            }

            _activeRun = CreateInstance<BatchmodeTestRun>();
            _activeRun.hideFlags = HideFlags.HideAndDontSave;
            _activeRun.Initialize(testMode);
        }

        private void Initialize(TestMode testMode)
        {
            _testMode = testMode;
            _resultsPath = ResolveResultsPath(testMode);
            _testFilter = GetCommandLineArgValue(FilterArgName);

            EnsureResultsDirectoryExists(_resultsPath);

            _testRunnerApi = CreateInstance<TestRunnerApi>();
            _testRunnerApi.RegisterCallbacks(this);

            EditorApplication.delayCall += ExecuteTests;
        }

        private void ExecuteTests()
        {
            EditorApplication.delayCall -= ExecuteTests;

            var filter = new Filter
            {
                testMode = _testMode
            };

            if (!string.IsNullOrWhiteSpace(_testFilter))
                filter.testNames = new[] { _testFilter };

            Debug.Log($"[BatchmodeTestRunner] Starting {_testMode} tests.");
            _testRunnerApi.Execute(new ExecutionSettings(filter));
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"[BatchmodeTestRunner] Loaded test tree: {testsToRun?.Name ?? "Unknown"}");
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, _resultsPath);

            Debug.Log(
                $"[BatchmodeTestRunner] {_testMode} finished: " +
                $"{result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped, " +
                $"{result.InconclusiveCount} inconclusive.");

            Complete(result.FailCount > 0 ? 1 : 0);
        }

        public void OnError(string message)
        {
            Debug.LogError("[BatchmodeTestRunner] " + message);
            WriteSyntheticFailureResults(message);
            Complete(1);
        }

        private void Complete(int exitCode)
        {
            _activeRun = null;
            EditorApplication.Exit(exitCode);
        }

        private static string ResolveResultsPath(TestMode testMode)
        {
            string configuredPath = GetCommandLineArgValue(ResultsArgName);
            if (!string.IsNullOrWhiteSpace(configuredPath))
                return configuredPath;

            string fileName = testMode == TestMode.EditMode
                ? "editmode-results.xml"
                : "playmode-results.xml";

            return Path.Combine(Directory.GetCurrentDirectory(), "TestResults", fileName);
        }

        private static void EnsureResultsDirectoryExists(string resultsPath)
        {
            string directory = Path.GetDirectoryName(resultsPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        private static string GetCommandLineArgValue(string argName)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return string.Empty;
        }

        private void WriteSyntheticFailureResults(string message)
        {
            var document = new XDocument(
                new XElement(
                    "test-run",
                    new XAttribute("total", 1),
                    new XAttribute("passed", 0),
                    new XAttribute("failed", 1),
                    new XAttribute("skipped", 0),
                    new XAttribute("inconclusive", 0),
                    new XAttribute("duration", 0),
                    new XAttribute("result", "Failed"),
                    new XElement(
                        "test-case",
                        new XAttribute("name", $"{_testMode}BatchRun"),
                        new XAttribute("fullname", $"{_testMode}BatchRun"),
                        new XAttribute("result", "Failed"),
                        new XAttribute("duration", 0),
                        new XElement(
                            "failure",
                            new XElement("message", message ?? "Unknown batch test runner error.")))));

            document.Save(_resultsPath);
        }
    }
}
