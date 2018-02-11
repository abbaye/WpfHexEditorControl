using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfHexaEditor.Core;

namespace HexEditUnitTest
{
    [TestClass]
    public class CaretTest
    {
        [TestMethod]
        public void CaretVisibilityPositionTest()
        {
            var caret = new Caret();

            var left = caret.Left;
            var top = caret.Top;

            Assert.AreEqual(left, 0);
            Assert.AreEqual(top, 0);
        }
    }
}
