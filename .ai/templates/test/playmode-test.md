# PlayMode Test Scaffold

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FarmSimVR.Tests.PlayMode
{
    [TestFixture]
    public class [SystemName]PlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Scene setup
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Cleanup
            yield return null;
        }

        [UnityTest]
        public IEnumerator [Method]_[Scenario]_[ExpectedResult]()
        {
            // Arrange

            // Act
            yield return null;

            // Assert
            Assert.Fail("Not implemented — RED phase");
        }
    }
}
```

## Notes
- PlayMode tests go in Assets/Tests/PlayMode/
- Can use UnityEngine, MonoBehaviours, scenes
- Use UnityTest attribute + IEnumerator for async
- Use yield return null to wait one frame
