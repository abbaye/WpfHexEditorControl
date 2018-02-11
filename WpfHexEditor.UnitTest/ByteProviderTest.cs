using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfHexaEditor.Core.Bytes;

namespace HexEditUnitTest
{
    [TestClass]
    public class ByteProviderTest
    {
        [TestMethod]
        public void GetByteCountTest()
        {
            var bp = new ByteProvider(@"C:\TEST\TEST.xls");

            var lenght = bp.Length;

            var watch = new Stopwatch();

            watch.Start();
            var byteCount = bp.GetByteCount().Sum();
            watch.Stop();

            Console.WriteLine($@"duration {watch.Elapsed}");
            Console.WriteLine($@"File lenght {lenght}");

            Assert.AreEqual(lenght, byteCount, "The number of byte need to be equal!");
        }
    }
}
