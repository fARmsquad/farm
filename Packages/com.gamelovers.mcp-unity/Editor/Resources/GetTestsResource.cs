using System.Collections.Generic;
using System.Threading.Tasks;
using McpUnity.Services;
using Newtonsoft.Json.Linq;
using UnityEditor.TestTools.TestRunner.Api;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for getting available tests from Unity Test Runner
    /// </summary>
    public class GetTestsResource : McpResourceBase
    {
        private readonly ITestRunnerService _testRunnerService;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public GetTestsResource(ITestRunnerService testRunnerService)
        {
            Name = "get_tests";
            Description = "Gets available tests from Unity Test Runner";
            Uri = "unity://tests/{testMode}";
            IsAsync = true;
            _testRunnerService = testRunnerService;
        }
        
        /// <summary>
        /// Asynchronously fetch tests based on provided parameters
        /// </summary>
        /// <param name="parameters">Resource parameters as a JObject</param>
        /// <param name="tcs">TaskCompletionSource to set the result or exception</param>
        public override async void FetchAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            // Get filter parameters
            string testModeFilter = parameters["testMode"]?.ToObject<string>();
            List<ITestAdaptor> allTests = await _testRunnerService.GetAllTestsAsync(testModeFilter);
            var results = new JArray();
            
            foreach (ITestAdaptor test in allTests)
            {
                results.Add(new JObject
                {
                    ["name"] = test.Name,
                    ["fullName"] = test.FullName,
                    ["testMode"] = test.TestMode.ToString(),
                    ["runState"] = test.RunState.ToString()
                });
            }
            
            tcs.SetResult(new JObject
            {
                ["success"] = true,
                ["message"] = $"Retrieved {allTests.Count} tests",
                ["tests"] = results
            });
        }
    }
}
