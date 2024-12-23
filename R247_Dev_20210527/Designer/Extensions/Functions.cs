using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NOVisionDesigner.Designer.Extensions
{
    public static class Functions
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        /// <summary>
        /// Create random file name that not exist with length in directory
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static string RandomFileName(string directory,int fileLength = 8)
        {
            var filename = RandomString(8);
            var newfile = System.IO.Path.Combine(directory, filename);
            while (true)
            {
                if (System.IO.File.Exists(newfile))
                {
                    filename = RandomString(fileLength);
                    newfile = System.IO.Path.Combine(directory, filename);
                }
                else
                {
                    break;
                }
            }
            return newfile;

        }
        /// <summary>
        /// Create random file name that not exist with name increase in number
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static string NewFileName(string directory,string extension="png",string prefix = "")
        {
            //DirectoryInfo d = new DirectoryInfo(directory);
            int fileCount = Directory.GetFiles(directory, "*."+extension).Length;
            var newfile = System.IO.Path.Combine(directory, prefix+fileCount.ToString()+"."+extension);
            while (true)
            {
                if (System.IO.File.Exists(newfile))
                {
                    fileCount = fileCount+1;
                    newfile = System.IO.Path.Combine(directory, prefix + fileCount.ToString() + "." + extension);
                }
                else
                {
                    break;
                }
            }
            return newfile;

        }
        public static string RandomHalconColor()
        {
            const string chars = "abcdef0123456789";
            return "#" + new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray()) + "ff";
        }
        public static (int row, int col) GetDisplayPosition(TextPosition DisplayPosition, int r1, int c1, int r2, int c2)
        {
            switch (DisplayPosition)
            {
                case TextPosition.Bottom:
                    return (r2, c1);
                case TextPosition.Top:
                    return (r1, c1);
                case TextPosition.Left:
                    return (r2, c1);
                default:
                    return (r1, c2);
            }
        }
        public static (int row, int col) GetDisplayPosition(TextPosition DisplayPosition, double r1, double c1, double r2, double c2)
        {
            switch (DisplayPosition)
            {
                case TextPosition.Bottom:
                    return ((int)r2, (int)c1);
                case TextPosition.Top:
                    return ((int)r1, (int)c1);
                case TextPosition.Left:
                    return ((int)r2, (int)c1);
                default:
                    return ((int)r1, (int)c2+1);
            }
        }
        public static (int row, int col) GetDisplayPosition(TextPosition DisplayPosition, double row, double col, double phi, double length1, double length2)
        {
            double r1 = row - length2;
            double c1 = col - length1;
            double r2 = row + length2;
            double c2 = col + length1;
            switch (DisplayPosition)
            {
                case TextPosition.Bottom:
                    return ((int)r2, (int)c1);
                case TextPosition.Top:
                    return ((int)r1, (int)c1);
                case TextPosition.Left:
                    return ((int)r2, (int)c1);
                default:
                    return ((int)r1, (int)c2+1);
            }
        }
        /// <summary>
        /// crop image to region with smallest rectangle and fill background with fillValue, if region is null then return orginal image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="region"></param>
        /// <param name="fillValue"></param>
        /// <returns></returns>
        public static HImage CropImageWithRegion(HImage image, HRegion region, int fillValue = 0, bool reduceDomain = false,bool fillRegion =true)
        {
            if(image== null)
            {
                return null;
            }
            if (region != null )
            {
                region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                row1 = Math.Max(0, row1);
                col1 = Math.Max(0, col1);
                var imageCroped = image.CropRectangle1(row1, col1, row2, col2);
                imageCroped.GetImageSize(out int w, out int h);
                if (fillRegion)
                {
                    HRegion diffrg = new HRegion(0, 0.0, h, w);
                    var subRg = diffrg.Difference(region.MoveRegion(-row1, -col1));
                    imageCroped.OverpaintRegion(subRg, new HTuple(fillValue, fillValue, fillValue), "fill");
                }
                
                if (reduceDomain)
                {
                    imageCroped = imageCroped.ReduceDomain(region.MoveRegion(-row1, -col1));
                }
                return imageCroped;
            }
            else
            {
                return image;
            }

        }
        /// <summary>
        /// crop image to region with smallest rectangle and fill background with fillValue, if region is null then return orginal image.
        /// If region area outside of image then fill the outside region with fillValue
        /// </summary>
        /// <param name="image"></param>
        /// <param name="region"></param>
        /// <param name="fillValue"></param>
        /// <returns></returns>
        public static HImage CropImageWithRegionTranslate(HImage image, HRegion region, int fillValue = 0,bool reduceDomain=false,bool fillRegion=true)
        {
            if (region != null)
            {
                
                region.SmallestRectangle1(out int row1, out int col1, out int row2, out int col2);
                if(row1 >= 0 & col1 >= 0)
                {
                    return CropImageWithRegion(image, region, fillValue, reduceDomain,fillRegion);
                }
                HHomMat2D translate = new HHomMat2D();
                translate= translate.HomMat2dTranslate(-(double)row1, -col1);

                HImage imageCroped = image.AffineTransImageSize(translate, "bilinear", col2 - col1, row2 - row1);
                //row1 = Math.Max(0, row1);
                //col1 = Math.Max(0, col1);
                //var imageCroped = image.CropRectangle1(row1, col1, row2, col2);
                imageCroped.GetImageSize(out int w, out int h);
                if (fillRegion)
                {
                    HRegion diffrg = new HRegion(0, 0.0, h, w);
                    var subRg = diffrg.Difference(region.MoveRegion(-row1, -col1));
                    imageCroped.OverpaintRegion(subRg, new HTuple(fillValue, fillValue, fillValue), "fill");
                }
                
                if (reduceDomain)
                {
                    imageCroped = imageCroped.ReduceDomain(region.MoveRegion(-row1, -col1));
                }
                //imageCroped.WriteImage("png", 0, "D:/test");
                return imageCroped;
            }
            else
            {
                return image;
            }

        }
        /// <summary>
        /// return null if region are non empty or null otherwise return union region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static HRegion GetNoneEmptyRegion(HRegion region)
        {
            if (region != null)
            {
                if (region.Area.Length == 0)
                {
                    return null;
                }
                else if (region.Area == 0)
                {
                    return null;
                }
                else
                {
                    return region.Union1();
                }
            }
            else
            {
                return null;
            }
        }
        // <summary>
        /// return null if region are non empty or null otherwise return union region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static HImage GetNoneEmptyHImage(ValueNodeInputViewModel<HImage> ImagePort)
        {
            if (ImagePort.Value == null)
            {
                return null;
            }
            else
            {
                if (ImagePort.Value.IsInitialized())
                {
                    return ImagePort.Value.Clone();
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Overlay image with color region using Alpha channel (opacity). Not that this function will modify the image data directly.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="region"></param>
        /// <param name="color"></param>
        public static void PaintImageOverlay(HImage image,HRegion region,System.Windows.Media.Color color)
        {
            HImage image_base = image.ScaleImage((double)(1 - (double) (color.A) / 255), 0);
            HImage image_overlay = image.PaintRegion(region, new HTuple((int)color.R, color.G, color.B), "fill");
            image_overlay = image_overlay.ScaleImage((double)color.A / 255, 0.0);
            image_overlay = image_base.AddImage(image_overlay, 1, 0.0).ReduceDomain(region);

            image.OverpaintGray(image_overlay);
        }
        /// <summary>
        /// Overlay image with color region using Alpha channel (opacity). Not that this function will return new image.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="region"></param>
        /// <param name="color"></param>
        public static HImage PaintImage(HImage image, HRegion region, System.Windows.Media.Color color)
        {
            HImage image_base = image.ScaleImage((double)(1 - (double)(color.A) / 255), 0);
            HImage image_overlay = image.PaintRegion(region, new HTuple((int)color.R, color.G, color.B), "fill");
            image_overlay = image_overlay.ScaleImage((double)color.A / 255, 0.0);
            image_overlay = image_base.AddImage(image_overlay, 1, 0.0).ReduceDomain(region);

            return image_overlay.PaintGray(image);
        }
        public static HImage EditImageWithWindow(ImageFilmstrip imageInfo,Window owner=null)
        {
            if (imageInfo != null)
            {
                HImage image = null;
                try
                {
                    image = new HImage(imageInfo.FullPath);
                }
                catch (Exception ex)
                {

                }

                if (image != null)
                {
                    PaintImageWindow wd = new PaintImageWindow(image);
                    wd.Owner = owner;
                    if (wd.ShowDialog() == true)
                    {
                        try
                        {
                            image = wd.Image;
                            //window_display.HalconWindow.AttachBackgroundToWindow(image);
                            var extension = System.IO.Path.GetExtension(imageInfo.FullPath);
                            image.WriteImage(extension.Substring(1), 0, System.IO.Path.ChangeExtension(imageInfo.FullPath, null));
                            return image;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(owner, "Error", ex.Message);
                        }


                    }
                }
            }

            return null;
        }
    }
}
