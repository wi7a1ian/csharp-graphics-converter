using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Laboratory.Graphic.Converter;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace GraphicsConverterTest
{
    [TestClass]
    //[DeploymentItem(@"TestData\", @"TestData\")]  // Copy from build output to the test deployment path. 
                                                    // Will also cause the Env.CurrentPath to point to TestResults sequential location.
    public class GraphicsConverterTest
    {

        Bitmap testColorBitmap = null;

        [TestInitialize]
        public void TestInitialize()
        {
            testColorBitmap = new Bitmap(@"TestData\ColorLogo_1.tif");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            testColorBitmap.Dispose();
            testColorBitmap = null;
        }

        [TestMethod]
        public void GraphicsConverter_ConvertToGray1bppBitmapIsWorking()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            var converter = new GraphicsConverter();

            // when
            using (var convertedBm = converter.ConvertToGray1bppBitmap(testColorBitmap))
                convertedBm.Save(tiffFile, ImageFormat.Tiff);

            // then
            using (var convertedBm = new Bitmap(tiffFile))
                Assert.IsTrue(convertedBm.PixelFormat == PixelFormat.Format1bppIndexed);

        }

        [TestMethod]
        [TestCategory("I/O")]
        public void GraphicsConverter_ConvertToMonoTiffIsWorking()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            var converter = new GraphicsConverter();

            // when
            converter.ConvertToMonoTiff(@"TestData\ColorLogo_1.tif", tiffFile);

            // then
            using (var convertedBm = new Bitmap(tiffFile))
                Assert.IsTrue(convertedBm.PixelFormat == PixelFormat.Format1bppIndexed);

        }

        [TestMethod]
        public void GraphicsConverter_SaveBitmapWithCompressionCCITT4IsWorking()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            var converter = new GraphicsConverter();

            // when
            converter.SaveBitmapWithCompressionCCITT4(testColorBitmap, tiffFile);

            // then
            using (var convertedBm = new Bitmap(tiffFile))
                Assert.IsTrue(convertedBm.PixelFormat == PixelFormat.Format1bppIndexed);

        }

        [TestMethod]
        public void GraphicsConverter_ConvertSinglepageToMultipageTiffIsWorking()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            var inputTiffList = new string[] { @"TestData\ColorLogo_1.tif", @"TestData\ColorLogo_1.tif", @"TestData\ColorLogo_1.tif" };
            var expectedCount = inputTiffList.Length;
            var converter = new GraphicsConverter();

            // when
            converter.ConvertSinglepageToMultipageTiff(inputTiffList, tiffFile);

            // then
            using (var convertedBm = Image.FromFile(tiffFile))
            {
                int pageCount = convertedBm.GetFrameCount(FrameDimension.Page);
                Assert.AreEqual(pageCount, expectedCount);
            }

        }


        #region helpers

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        #endregion helpers
    }
}