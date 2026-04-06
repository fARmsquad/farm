# EditMode Test Scaffold

```csharp
using NUnit.Framework;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class [SystemName]Tests
    {
        // System under test
        private [SystemType] _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new [SystemType]();
        }

        [Test]
        public void [Method]_[Scenario]_[ExpectedResult]()
        {
            // Arrange

            // Act

            // Assert
            Assert.Fail("Not implemented — RED phase");
        }
    }
}
```

## Notes
- EditMode tests go in Assets/Tests/EditMode/
- Test Core/ classes only — no UnityEngine references
- Use NUnit Assert, not UnityEngine.Assertions
- One test class per system under test
