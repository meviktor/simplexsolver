using NUnit.Framework;
using simplexapi.Common.Extensions;
using simplexapi.Common.Models;

namespace simplexapi.Common.Test
{
    public class VariableExtensionsTests
    {
        private Variable _x1;
        
        [SetUp]
        public void SetUp()
        {
            _x1 = new Variable { Name = "x", Index = 1 };
        }

        [Test]
        public void GreaterOrEqualThenZeroRangeTest()
        {
            Assert.AreEqual("+1x1 >= 0", _x1.GreaterOrEqualThanZeroRange().ToString());
        }
    }
}
