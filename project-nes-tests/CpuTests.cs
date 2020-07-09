using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using project_nes;

namespace project_nes_tests
{
    [TestClass]
    public class CpuTests
    {
        [TestMethod]
        public void TestTestOpcode()
        {
            CPU cpu = new CPU();

            var temp = cpu.TestOpCode("ADC");

            Assert.AreEqual(temp, 0);




        }
    }
}
