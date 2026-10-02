using NUnit.Framework;
namespace Spotlight.Tests
{
    public sealed class PBoundaryNUnitTests
    {
        [Test] public void CombatTransactionalAndDeferredBoundaries() { CombatBoundaryTests.Run(); }
        [Test] public void ElementPlannerBoundaries() { ElementBoundaryTests.Run(); }
    }
}
