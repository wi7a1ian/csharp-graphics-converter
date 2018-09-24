using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Laboratory.Graphic.Converter
{
    public class GraphicsConverter
    {
        private ImageCodecInfo _tiffCodec = null;
        private ImageCodecInfo _jpegCodec = null;

        public struct PixelData
        {
            public byte blue;
            public byte green;
            public byte red;
        }

        public GraphicsConverter()
        {
            _tiffCodec = GetEncoderInfo("image/tiff");
            _jpegCodec = GetEncoderInfo("image/jpeg");
        }

        /// <summary>
        /// Codec used to save tiff
        /// </summary>
        public ImageCodecInfo TiffCodec
        {
            get { return _tiffCodec; }
            set { _tiffCodec = value; }
        }

        /// <summary>
        /// Codec used to save jpeg
        /// </summary>
        public ImageCodecInfo JpegCodec
        {
            get { return _jpegCodec; }
            set { _jpegCodec = value; }
        }

        /// <summary>
        /// Converts the given 24bpp bitmap into a 1bpp bitmap.  
        /// NOTE!!!:  This fastConvert action code is duplicated in SlipSheetCreator, so any changes made here
        /// should be updated in that code as well!  It is duplicated so we have one less dependency.
        /// </summary>
        /// <param name="bitmap">a 24bpp bitmap, not sure if other formats will work</param>
        /// <returns>A 1bpp image of the original</returns>
        public Bitmap ConvertToGray1bppBitmap(Bitmap bitmap)
        {
            Rectangle rect;
            BitmapData data;
            BitmapData srcData;
            IntPtr pixels;
            IntPtr srcpBase = IntPtr.Zero;
            //IntPtr		srcpBits = IntPtr.Zero;
            uint row, col;
			int luminanceCutOff = 125;
            int width = bitmap.Width;
            int height = bitmap.Height;

            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF bounds = bitmap.GetBounds( ref unit );

            System.Drawing.Point pixelSize = new System.Drawing.Point( (int)bounds.Width,
               (int)bounds.Height );

            //Create the DESTINATION Bitmap
            Bitmap dest = new Bitmap( width, height, PixelFormat.Format1bppIndexed );
            dest.SetResolution( bitmap.HorizontalResolution, bitmap.VerticalResolution );

            //LOCK the Entire Bitmap & get the pixel pointer
            rect = new Rectangle( 0, 0, width, height );
            data = dest.LockBits( rect, ImageLockMode.WriteOnly,
               PixelFormat.Format1bppIndexed );

            // LOCK the source bitmap
            srcData = bitmap.LockBits( rect, ImageLockMode.ReadOnly, bitmap.PixelFormat );
            //this.bitmap.UnlockBits(srcData);

            pixels = data.Scan0;
            srcpBase = srcData.Scan0;

            // x is width, y is height
            System.Drawing.Point size = pixelSize;

            unsafe
            {
                //Color		colorPixel;
                byte* pBits, pDestPixel;
                byte bMask;
                double luminance;
                byte* srcpBits = null;

                //Init pointer to the Bits
                if( data.Stride > 0 )
                {
                    pBits = (byte*)pixels.ToPointer();
                }
                else
                {
                    pBits = (byte*)pixels.ToPointer() + data.Stride * ( height - 1 );
                }

                if( srcData.Stride > 0 )
                {

                    srcpBits = (byte*)srcpBase.ToPointer();
                }
                else
                {
                    srcpBits = (byte*)srcpBase.ToPointer() + srcData.Stride * ( height - 1 );
                }

                //Stride could be negative
                uint stride = (uint)Math.Abs( data.Stride );
                uint curStride = 0;
                byte* basePos = null;
                byte* offSet = null;

                PixelData* srcPixel = (PixelData*)srcpBits;
                // row = y
                for( row = 0; row < size.Y; row++ )
                {
                    basePos = (byte*)srcPixel;

                    // Get the source pixel
                    //PixelData* srcPixel = PixelAt(0, (int)row);
                    offSet = pBits + curStride;

                    // col = x
                    for( col = 0; col < size.X; col++ )
                    {
                        //colorPixel = this.bitmap.GetPixel( (int)col, (int)row );

                        //Move the DESTINATION to the correct Address / Pointer & get Pixel
                        pDestPixel = offSet + ( (int)( col / 8 ) );

                        //Determine which Bit Represents this Pixel in 1bpp format
                        bMask = (byte)( 0x0080 >> (int)( col % 8 ) );

                        //Calculate LUMINANCE to help determine if black or white pixel
						luminance = (srcPixel->red * 0.299) + (srcPixel->green * 0.587) + (srcPixel->blue
							* 0.114);
                        //						luminance = (colorPixel.R * 0.299) + (colorPixel.G * 0.587) + (colorPixel.B
                        //							* 0.114);

                        //Set to Black or White using the Color. Luminance Cut Off
                        if( luminance >= luminanceCutOff )
                            //if( srcPixel->red != 0 )
                            *pDestPixel |= (byte)bMask;        // Set Bit to 1    - White
                        else
                            *pDestPixel &= (byte)~bMask;        // Set Bit to 0 - Black

                        // move to next pixel in row
                        srcPixel++;
                    }
                    curStride += stride;
                    // Jump to the next line
                    srcPixel = (PixelData*)( basePos + srcData.Stride );
                }

            }
            dest.UnlockBits( data );
            bitmap.UnlockBits( srcData );
            return dest;

        }

        public Bitmap ConvertToGray32bppBitmap(Bitmap original)
        {
            // Create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            // Get a graphics object from the new image
            using (Graphics g = Graphics.FromImage(newBitmap))
            {

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][] 
			        {
				        new float[] {.3f, .3f, .3f, 0, 0},
				        new float[] {.59f, .59f, .59f, 0, 0},
				        new float[] {.11f, .11f, .11f, 0, 0},
				        new float[] {0, 0, 0, 1, 0},
				        new float[] {0, 0, 0, 0, 1}
			        });

                // Create some image attributes
                ImageAttributes attributes = new ImageAttributes();

                // Set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                // Draw the original image on the new image
                // Using the grayscale color matrix
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                   0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            }

            return newBitmap;
        }

        public Bitmap ConvertToGray8bppBitmap(Bitmap bmp)
        {
            int w = bmp.Width,
                h = bmp.Height,
                r, ic, oc, bmpStride, outputStride, bytesPerPixel;
            PixelFormat pfIn = bmp.PixelFormat;
            ColorPalette palette;
            Bitmap output;
            BitmapData bmpData, outputData;

            //Create the new bitmap
            output = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

            //Build a grayscale color Palette
            palette = output.Palette;
            for (int i = 0; i < 256; i++)
            {
                Color tmp = Color.FromArgb(255, i, i, i);
                palette.Entries[i] = Color.FromArgb(255, i, i, i);
            }
            output.Palette = palette;

            //No need to convert formats if already in 8 bit
            if (pfIn == PixelFormat.Format8bppIndexed)
            {
                output = (Bitmap)bmp.Clone();

                //Make sure the palette is a grayscale palette and not some other
                //8-bit indexed palette
                output.Palette = palette;

                return output;
            }

            //Get the number of bytes per pixel
            switch (pfIn)
            {
                case PixelFormat.Format24bppRgb: bytesPerPixel = 3; break;
                case PixelFormat.Format32bppArgb: bytesPerPixel = 4; break;
                case PixelFormat.Format32bppRgb: bytesPerPixel = 4; break;
                default: throw new InvalidOperationException("Image format not supported");
            }

            //Lock the images
            bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly,
                                   pfIn);
            outputData = output.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly,
                                         PixelFormat.Format8bppIndexed);
            bmpStride = bmpData.Stride;
            outputStride = outputData.Stride;

            //Traverse each pixel of the image
            unsafe
            {
                byte* bmpPtr = (byte*)bmpData.Scan0.ToPointer(),
                outputPtr = (byte*)outputData.Scan0.ToPointer();

                if (bytesPerPixel == 3)
                {
                    //Convert the pixel to it's luminance using the formula:
                    // L = .299*R + .587*G + .114*B
                    //Note that ic is the input column and oc is the output column
                    for (r = 0; r < h; r++)
                        for (ic = oc = 0; oc < w; ic += 3, ++oc)
                            outputPtr[r * outputStride + oc] = (byte)(int)
                                (0.299f * bmpPtr[r * bmpStride + ic] +
                                 0.587f * bmpPtr[r * bmpStride + ic + 1] +
                                 0.114f * bmpPtr[r * bmpStride + ic + 2]);
                }
                else //bytesPerPixel == 4
                {
                    //Convert the pixel to it's luminance using the formula:
                    // L = alpha * (.299*R + .587*G + .114*B)
                    //Note that ic is the input column and oc is the output column
                    for (r = 0; r < h; r++)
                        for (ic = oc = 0; oc < w; ic += 4, ++oc)
                            outputPtr[r * outputStride + oc] = (byte)(int)
                                ((bmpPtr[r * bmpStride + ic] / 255.0f) *
                                (0.299f * bmpPtr[r * bmpStride + ic + 1] +
                                 0.587f * bmpPtr[r * bmpStride + ic + 2] +
                                 0.114f * bmpPtr[r * bmpStride + ic + 3]));
                }
            }

            //Unlock the images
            bmp.UnlockBits(bmpData);
            output.UnlockBits(outputData);

            return output;
        }

        /// <summary>
        /// Converts the given image into a 1bpp tiff with CCITT4 compression.  
        /// </summary>
        /// <param name="srcImagePath">Path to the source image file.</param>
        /// <param name="dstImagePath">Path to the target image file.</param>
        /// <returns>True on success</returns>
        public bool ConvertToMonoTiff( string srcImagePath, string dstImagePath)
        {
            using( System.Drawing.Image im = System.Drawing.Image.FromFile( srcImagePath ) )
            //using (Bitmap bitMap = new Bitmap(im, im.Width, im.Height)) // This will loose resolution info
            using( Bitmap bitMap = new Bitmap( im.Width, im.Height, PixelFormat.Format24bppRgb ) )
            {
                bitMap.SetResolution( im.HorizontalResolution, im.VerticalResolution );

                // We have to draw the orig image onto 24bpp bitmap, otherwise the image will draw just bad.
                using( Graphics g = Graphics.FromImage( bitMap ) )
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage( im, new Rectangle( 0, 0, im.Width, im.Height ) );
                }

                // Not needed anymore
                im.Dispose();

                using (Bitmap convBitmap = ConvertToGray1bppBitmap(bitMap)) // Fast convert to 1bpp
                {
                    if (!this.SaveBitmapWithCompressionCCITT4(convBitmap, dstImagePath))
                    {
                        throw new Exception("Error occured while saving the image.");
                    }
                    else
                    {
                        convBitmap.GetLifetimeService(); // TODO: What is this for ? Do we need to check object lifetime services ?
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Converts the given image into a 1bpp tiff with CCITT4 compression.  
        /// </summary>
        /// <param name="srcImagePath">Path to the target image file.</param>
        /// <returns>True on success</returns>
        public bool ConvertToMonoTiff(string srcImagePath)
        {
            return ConvertToMonoTiff(srcImagePath, srcImagePath);
        }

        public int GetMultipageTiffPageCount(string path)
        {
            string tempPath = Path.GetTempFileName();

            File.Copy(path, tempPath);

            int pageCount = 0;
            using (Bitmap mpImg = new Bitmap(tempPath))
            {
                pageCount = mpImg.GetFrameCount(FrameDimension.Page);
            }

            File.Delete(tempPath);

            return pageCount;
        }

        //public int ConvertMultiToSinglePageTiffs(string tiffFilePath, ref string[] TiffPathArray)
        //{
        //    int resPgCount = 0;
        //    string tempPath = Path.GetTempFileName();
        //    string tiffFileExt = Path.GetExtension(tiffFilePath);
        //    string tiffFileName = Path.GetFileNameWithoutExtension(tiffFilePath);
        //    string tiffBatesPrefix = BatesNumber.GetBatesPrefixFromString(tiffFileName);
        //    long tiffBatesNumber = BatesNumber.GetBatesNumberFromString(tiffFileName);
        //    int tiffBatesPadding = BatesNumber.GetBatesPaddingFromString(tiffFileName);
        //    string currentTiffBates = string.Empty;

        //    ProtectedWin32IO.CopyFile(tiffFilePath, tempPath);

        //    TiffPathArray = null;

        //    using (Bitmap mpImg = new Bitmap(tempPath))
        //    {
        //        int pageCount = mpImg.GetFrameCount(FrameDimension.Page);

        //        // Check to see if this is a multipage tiff
        //        if (pageCount > 1)
        //        {
        //            TiffPathArray = new string[pageCount];

        //            EncoderParameters eps = new EncoderParameters(1);
        //            ImageCodecInfo[] imgCodecs = ImageCodecInfo.GetImageEncoders();
        //            EncoderParameters encParams = new EncoderParameters(1);
        //            encParams.Param[0] = new EncoderParameter(
        //                System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionCCITT4);

        //            string singleTiffFilePath = string.Empty;
        //            string newSingleTiffFilePath = string.Empty;

        //            for (int i = 0; i < pageCount; i++)
        //            {
        //                currentTiffBates = BatesNumber.FormatBates(tiffBatesNumber + i,
        //                    tiffBatesPrefix, tiffBatesPadding);

        //                try
        //                {
        //                    singleTiffFilePath = Path.GetTempFileName();
        //                }
        //                catch (Exception ex)
        //                {
        //                    throw new Exception(ex.Message);
        //                }

        //                newSingleTiffFilePath = Path.Combine(Path.GetDirectoryName(singleTiffFilePath),
        //                currentTiffBates + tiffFileExt);

        //                // Remove existing file with the same name
        //                if (File.Exists(newSingleTiffFilePath))
        //                {
        //                    try
        //                    {
        //                        ProtectedWin32IO.DeleteFile(newSingleTiffFilePath);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        throw new Exception(ex.Message);
        //                    }
        //                }
        //                ProtectedWin32IO.MoveFile(singleTiffFilePath, newSingleTiffFilePath);

        //                if (ProtectedWin32IO.FileExists(newSingleTiffFilePath))
        //                {
        //                    ++resPgCount;
        //                }

        //                // save path to created single tiff file
        //                TiffPathArray[i] = newSingleTiffFilePath;

        //                mpImg.SelectActiveFrame(FrameDimension.Page, i);
        //                using (Bitmap img = (Bitmap)mpImg.Clone())
        //                {
        //                    img.Save(newSingleTiffFilePath, imgCodecs[3], encParams); //imgCodecs[3] for Tiff
        //                }
        //            }

        //        }
        //        else
        //        {
        //            resPgCount = pageCount;
        //        }
        //    }

        //    ProtectedWin32IO.DeleteFile(tempPath);

        //    return resPgCount;
        //}

        /// <summary>
        /// Expects list of singlepage tiffs and outputs multipage tiff in the same format.
        /// </summary>
        /// <param name="tiffList"></param>
        /// <param name="outputTiff"></param>
        /// <param name="expectedPagecount"></param>
        public void ConvertSinglepageToMultipageTiff(string[] tiffList, string outputTiff, int expectedPagecount = -1, bool monoOutput = false)
        {
            var encoder = Encoder.SaveFlag;

            // Add all the tiffs to one file
            EncoderParameters encParams = new EncoderParameters(monoOutput ? 2 : 1);
            encParams.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.MultiFrame);
            if (monoOutput)
                encParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionCCITT4);

            // Create a new image to hold all the image pages.
            using (Image image = Image.FromFile(tiffList[0]))
            {
                image.Save(outputTiff, _tiffCodec, encParams);

                encParams.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.FrameDimensionPage);

                // Loop through each page and add it to the main image.
                for (int i = 1; i < tiffList.Length; i++)
                {
                    // Stop creating pages if our current page is greater than requested page count.
                    if (expectedPagecount != -1 && i >= expectedPagecount)
                    {
                        break;
                    }

                    using (Image page = Image.FromFile(tiffList[i]))
                    {
                        image.SaveAdd(page, encParams);
                    }
                }

                encParams.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.Flush);

                image.SaveAdd(encParams);
            }
        }


        public bool IsStandardTiff(string sourceFile)
        {

            Stream stream = null;
            IntPtr tiffInfo = IntPtr.Zero;
            System.Drawing.Image image = null;
            //UInt16 pageCount = 0;
            float displayWidth;
            float displayHeight;
            //float displayRatio;

            string tempFile = Path.GetTempFileName();
            File.Copy(sourceFile, tempFile);
            sourceFile = tempFile;

            try
            {
                /*try
                {
                    tiffInfo = LibTiffApi.OpenTiff(sourceFile, ref pageCount);
                    image = LibTiffApi.LoadTiffPage(tiffInfo, 0);
                }
                catch*/
                {
                    // LibTiff failed, try .NET

                    // Note:  Keep the stream open for the length of the operation in case we are dealing with multipage tiffs.
                    stream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    image = System.Drawing.Image.FromStream(stream);
                }

                // Figure out the display dimensions, so we know whether we should start
                // with landscape or portrait
                float hRes = image.HorizontalResolution; // pixels per inch
                float vRes = image.VerticalResolution; // pixels per inch
                float ratio = (float)image.Height / (float)image.Width;
                //Graphics g = Graphics.FromHwnd( IntPtr.Zero );
                float ghRes = 300.0f; // default
                float gvRes = 300.0f; // default

                // Adjust for varying horizontal-to-vertical ratio
                displayWidth = ((float)image.Width / hRes) * ghRes;
                displayHeight = ((float)image.Height / vRes) * gvRes;

                // If the resolution ratio is 1:1, use the pixel values
                if (hRes == vRes)
                {
                    displayWidth = (float)image.Width;
                    displayHeight = (float)image.Height;
                }

                if (displayHeight == 3300.0f && displayWidth == 2550.0f)
                    //no need to fix this tiff
                    return true;
                else
                    return false;

            }
            catch (Exception e)
            {
                throw new Exception("Could not create image from stream " + e.Message +
                        "\n File: " + sourceFile, e);
            }
            finally
            {
                try
                {
                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
                catch { }

                try
                {
                    if (tiffInfo != IntPtr.Zero)
                    {
                        //LibTiffApi.CloseTiff(tiffInfo);
                        tiffInfo = IntPtr.Zero;
                    }
                }
                catch { }

                try
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }

                    File.Delete(tempFile);
                }
                catch { }

            }
        }

        public void ResizeTiff(string sourceFile, string destFile)
        {
            IntPtr tiffInfo = IntPtr.Zero;
            System.Drawing.Image image = null;
            //UInt16 pageCount = 0;
            int displayWidth;
            int displayHeight;
            //float displayRatio;

            Stream stream = null;
            image = null;
            string outFileName = destFile;
            SolidBrush brush = new SolidBrush(Color.Black);

            int width = 0;
            int height = 0;

            string tempFile = Path.GetTempFileName();
            File.Copy(sourceFile, tempFile);
            sourceFile = tempFile;

            // number of pixels to buffer the edge
            int border = 40;
            int marginWidth = 0;
            try
            {
                /*try
                {
                    tiffInfo = LibTiffApi.OpenTiff(sourceFile, ref pageCount);
                    image = LibTiffApi.LoadTiffPage(tiffInfo, 0);
                }
                catch*/
                {
                    // LibTiff failed, try .NET

                    // Note:  Keep the stream open for the length of the operation in case we are dealing with multipage tiffs.
                    stream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    image = System.Drawing.Image.FromStream(stream);
                }

                //cc standard
                width = 2550;
                height = 3300;

                // width of the image minus the margins
                marginWidth = width - (border * 2);

                // Use 5% reduction, as it is an even divisor of the image dimensions
                // Scale the image to 95%
                //				scaledWidth = width - (int)(width * 0.05);
                //				scaledHeight = height - (int)(height * 0.05);

                using (Bitmap bitMap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                {

                    bitMap.SetResolution(300, 300);

                    // Creat a Graphics object using the image.
                    using (Graphics g = Graphics.FromImage(bitMap))
                    {
                        // We will be printing on white, so make the background white.
                        g.FillRectangle(Brushes.White, 0, 0, width, height);

                        float ratio = (float)image.Height / (float)image.Width;

                        //Graphics g = Graphics.FromHwnd( IntPtr.Zero );
                        //float ghRes = 300.0f; // default
                        //float gvRes = 300.0f; // default

                        // Create a rectangle that is 100% of the original image size
                        // Adjust for varying horizontal-to-vertical ratio
                        //displayWidth = ((float)image.Width / hRes) * ghRes;
                        //displayHeight = ((float)image.Height / vRes) * gvRes;
                        displayWidth = image.Width;
                        displayHeight = image.Height;

                        //// If the resolution ratio is 1:1, use the pixel values
                        //if (hRes == vRes)
                        //{
                        //  displayWidth = image.Width;
                        //  displayHeight = image.Height;
                        //}

                        Rectangle scaledRect = new Rectangle(0, 0, displayWidth, displayHeight);

                        if (displayHeight > height || displayWidth > width)
                        {
                            int finalHeight = displayHeight;
                            int finalWidth = displayWidth;

                            // make image height fit
                            finalHeight = height;

                            // calculate width
                            finalWidth = (int)((float)finalHeight / ratio);

                            if (finalWidth > width)
                            {
                                finalWidth = width;

                                // recalculate height
                                finalHeight = (int)((float)finalWidth * ratio);
                            }

                            scaledRect.Width = finalWidth;
                            scaledRect.Height = finalHeight;

                        }

                        // Draw the image in a rectangle
                        // Choose the interpolation mode explicitly, because the default
                        // mode produces a checkerboard pattern in grey-scale
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(image, scaledRect);

                        image.Dispose();

                        using (Bitmap convBitmap = ConvertToGray1bppBitmap(bitMap))
                        {
                            bitMap.Dispose();

                            if (!SaveBitmapWithCompressionCCITT4(convBitmap, outFileName))
                            {
                                throw new Exception("Error occured while saving the image.");
                            }
                            else
                            {
                                convBitmap.GetLifetimeService();
                                convBitmap.Dispose();

                                File.Delete(tempFile);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error occurred while resizing the image: " + e.Message);
            }
            finally
            {
                try
                {
                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
                catch { }

                try
                {
                    if (tiffInfo != IntPtr.Zero)
                    {
                        //LibTiffApi.CloseTiff(tiffInfo);
                        tiffInfo = IntPtr.Zero;
                    }
                }
                catch { }

                try
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }

                }
                catch { }

            }
        }

        /// <summary>
        /// Retrieves image codec
        /// </summary>
        /// <param name="mimeType">MIME type string</param>
        /// <returns></returns>
        public static ImageCodecInfo GetEncoderInfo( string mimeType )
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for( j = 0; j < encoders.Length; ++j )
            {
                if( encoders[j].MimeType == mimeType )
                    return encoders[j];
            }
            return null;
        }

        /// <summary>
        /// Saves a bitmap with CCITT4 compression
        /// </summary>
        /// <param name="bm">Bitmap to be saved</param>
        /// <param name="outFileName">Name of the image</param>
        /// <returns>True if success, otherwise false</returns>
        public bool SaveBitmapWithCompressionCCITT4(Bitmap bm, string outFileName)
        {
            using (EncoderParameters eps = new EncoderParameters(1))
            using (Stream outStream = File.Open(outFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                long l = (long)EncoderValue.CompressionCCITT4;
                eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, l);
                bm.Save(outStream, _tiffCodec, eps);
            }

            return true;
        }

        /// <summary>
        /// Saves a bitmap with JPEG 90% compression
        /// </summary>
        /// <param name="bm">Bitmap to be saved</param>
        /// <param name="outFileName">Name of the image</param>
        /// <returns>True if success, otherwise false</returns>
        public bool SaveBitmapWithCompressionJPEG90(Bitmap bm, string outFileName)
        {
            using (EncoderParameters eps = new EncoderParameters(1))
            using (Stream outStream = File.Open(outFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
                bm.Save(outStream, _jpegCodec, eps);
            }
            return true;
        }

    }
}
