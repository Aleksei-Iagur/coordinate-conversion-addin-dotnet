using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArcGIS.Core.Hosting;
using CoordinateConversionLibrary.Models;

namespace ProAppCoordConversionModule.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod, STAThread]
        public void TestProCoordinateGet()
        {
            Host.Initialize();

            var proGetter = new ProCoordinateGet();

            var getBase = proGetter as CoordinateGetBase;

            getBase.InputCoordinate = "44.123 -121.456";

            string coord = string.Empty;
            var result = proGetter.CanGetDD(4326, out coord);
            Assert.IsTrue(result);
        }
    }
}
