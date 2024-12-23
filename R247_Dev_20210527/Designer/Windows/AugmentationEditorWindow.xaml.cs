using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for AugmentationEditorWindow.xaml
    /// </summary>
    public partial class AugmentationEditorWindow : Window
    {
        public ObservableCollection<ImageFilmstrip> ListImage;
        public ObservableCollection<ImageFilmstrip> ResultListImage = new ObservableCollection<ImageFilmstrip>();
        public string imagedir;
        public string annotationdir;

        public List<RegionMaker> OldRegionList { get; set; }
        public List<RegionMaker> NewRegionList { get; set; }

        public IEnumerable<ClassifierClass1> ClassList { get; set; }

        HWindow displayProcess;
        HWindow displayResult;
        HImage imageProcess;
        HImage imageResult;
        ImageFilmstrip selectedImage;
        public List<Augmentation> listAugmentationcs = new List<Augmentation>();

        public class Augmentation:IHalconDeserializable
        {
            public string Name { get; set; }
            public dynamic Value { get; set; }
            public void Save(HFile file)
            {
                HelperMethods.SaveParam(file, this);
            }
            public void Load(DeserializeFactory item)
            {
                HelperMethods.LoadParam(item, this);
            }
        }
        public AugmentationEditorWindow(string imagedir, string annotationdir, IEnumerable<ClassifierClass1> ClassList)
        {
            InitializeComponent();
            this.imagedir = imagedir;
            this.ClassList = ClassList;
            this.annotationdir = annotationdir;
            list_augmentation.Items.Clear();
        }

        public void LoadImageList(string dir)
        {
            var result = new ObservableCollection<ImageFilmstrip>();
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir).Where(x => x.ToLower().EndsWith(".bmp") | x.ToLower().EndsWith(".jpg") | x.ToLower().EndsWith(".png"));
                foreach (string file in files)
                {
                    result.Add(new ImageFilmstrip(file));
                }
            }
            ListImage = result;
        }
        HTuple w, h;
        public void SelectFirstImage()
        {
            selectedImage = ListImage[0];
            try
            {
                imageProcess = new HImage(selectedImage.FullPath);
                imageProcess.GetImageSize(out w, out h);
                displayProcess.AttachBackgroundToWindow(imageProcess);
                displayProcess.ClearWindow();
            }
            catch (Exception ex)
            {

            }
        }

        private void window_display_result_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                displayResult = window_display_result.HalconWindow;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                //this.Close();
            }
        }
        private void window_display_process_HInitWindow(object sender, EventArgs e)
        {
            try
            {
                displayProcess = window_display_process.HalconWindow;
                displayProcess.SetDraw("fill");
                LoadImageList(this.imagedir);
                SelectFirstImage();
                //LoadAnnotation(selectedImage);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                this.Close();
            }
        }
    
        public void SaveAnnotation(string path, List<RegionMaker> CurrentRegionList)
        {
            JObject[] data = new JObject[CurrentRegionList.Count];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new JObject();
                data[i]["x"] = CurrentRegionList[i].Region.col1;
                data[i]["y"] = CurrentRegionList[i].Region.row1;
                data[i]["w"] = CurrentRegionList[i].Region.col2 - CurrentRegionList[i].Region.col1;
                data[i]["h"] = CurrentRegionList[i].Region.row2 - CurrentRegionList[i].Region.row1;
                data[i]["annotation"] = CurrentRegionList[i].Annotation.Name;
            }
            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(data));
        }
        public void AddAugmentationList(string name, dynamic value)
        {
            listAugmentationcs.Add(new Augmentation() { Name = name,  Value = value });
            list_augmentation.Items.Add(new Augmentation() { Name = name, Value = value});
           
        }
        private void Btn_Flip_Click(object sender, RoutedEventArgs e)
        {
            box_flip.Visibility = Visibility.Visible;
            checkbox_flip_horizontal.IsChecked = false;
            checkbox_flip_vertical.IsChecked = false;
            
        }

        private void checkbox_flip_horizontal_Checked(object sender, RoutedEventArgs e)
        {

            if (checkbox_flip_horizontal.IsChecked.Value == false)
            {

                displayResult.ClearWindow();
            }
            else
            {
                checkbox_flip_vertical.IsChecked = false;
                imageResult = imageProcess.MirrorImage("column");
                displayResult.DispObj(imageResult);
            }

        }

        private void checkbox_flip_vertical_Checked(object sender, RoutedEventArgs e)
        {

            if (checkbox_flip_vertical.IsChecked.Value == false)
            {

                displayResult.ClearWindow();
            }
            else
            {
                checkbox_flip_horizontal.IsChecked = false;
                imageResult = imageProcess.MirrorImage("row");
                displayResult.DispObj(imageResult);
            }
        }

        private void Btn_GoBack_Flip_Click(object sender, RoutedEventArgs e)
        {
            box_flip.Visibility = Visibility.Hidden;
        }

        private void Btn_Apply_Flip_Click(object sender, RoutedEventArgs e)
        {
            if (checkbox_flip_horizontal.IsChecked == false & checkbox_flip_vertical.IsChecked == false)
            {
                return;
            }
            if (checkbox_flip_horizontal.IsChecked.Value == true)
            {
                AddAugmentationList("horizontal_flip", true);
            }
            else if (checkbox_flip_vertical.IsChecked.Value == true)
            {
                AddAugmentationList("vertical_flip", true);
            }
            box_flip.Visibility = Visibility.Hidden;
           
        }
        public void RemoveAugmentation(Augmentation vm)
        {
            var indexRemove = list_augmentation.Items.IndexOf(vm);
            list_augmentation.Items.RemoveAt(indexRemove);
            listAugmentationcs.RemoveAt(indexRemove);    
        }
        private void Btn_Augmentation_Remove_Click(object sender, RoutedEventArgs e)
        {
            var vm =  (sender as Button).DataContext as Augmentation;
            if (vm != null)
            {
                RemoveAugmentation(vm);
            }
        }

        private void List_Augmentation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void checkbox_brighten_Checked(object sender, RoutedEventArgs e)
        {
            //if (checkbox_brighten.IsChecked == true)
            //{
            //    checkbox_darken.IsChecked = false;
            //}
        }

        private void checkbox_darken_Checked(object sender, RoutedEventArgs e)
        {
            //if (checkbox_darken.IsChecked == true)
            //{
            //    checkbox_brighten.IsChecked = false;
            //}
        }

        private void Btn_Apply_Brightness_Click(object sender, RoutedEventArgs e)
        {
            if (ListImage == null)
            {
                return;
            }
      

            AddAugmentationList("brightness_range", brightness_range);
            box_brightness.Visibility = Visibility.Hidden;
        }

        private void Btn_GoBack_Brightnes_Click(object sender, RoutedEventArgs e)
        {
            box_brightness.Visibility = Visibility.Hidden;
        }

        private void Btn_Brightness_Click(object sender, RoutedEventArgs e)
        {
            box_brightness.Visibility = Visibility.Visible;
            
        }
        private Bitmap AdjustBrightness(Bitmap image, float brightness)
        {
            ////Old code Brightness
            //System.Drawing.Bitmap TempBitmap = image;
            //float FinalValue = (float)brightness /100.0f;
            //System.Drawing.Bitmap NewBitmap = new System.Drawing.Bitmap(TempBitmap.Width, TempBitmap.Height);
            //System.Drawing.Graphics NewGraphics = System.Drawing.Graphics.FromImage(NewBitmap);
            //float[][] FloatColorMatrix ={
            //         new float[] {1, 0, 0, 0, 0},
            //         new float[] {0, 1, 0, 0, 0},
            //         new float[] {0, 0, 1, 0, 0},
            //         new float[] {0, 0, 0, 1, 0},
            //         new float[] {FinalValue, FinalValue, FinalValue, 1, 1}
            //     };

            //System.Drawing.Imaging.ColorMatrix NewColorMatrix = new System.Drawing.Imaging.ColorMatrix(FloatColorMatrix);
            //System.Drawing.Imaging.ImageAttributes Attributes = new System.Drawing.Imaging.ImageAttributes();
            //Attributes.SetColorMatrix(NewColorMatrix);
            //NewGraphics.DrawImage(TempBitmap, new System.Drawing.Rectangle(0, 0, TempBitmap.Width, TempBitmap.Height), 0, 0, TempBitmap.Width, TempBitmap.Height, System.Drawing.GraphicsUnit.Pixel, Attributes);
            //Attributes.Dispose();
            //NewGraphics.Dispose();
            //return NewBitmap;

            ////New code Brightness
            {
                // Make the ColorMatrix.
                float b = brightness;
                ColorMatrix cm = new ColorMatrix(new float[][]
                    {
                    new float[] {b, 0, 0, 0, 0},
                    new float[] {0, b, 0, 0, 0},
                    new float[] {0, 0, b, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1},
                    });
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(cm);

                // Draw the image onto the new bitmap while applying the new ColorMatrix.
                System.Drawing.Point[] points =
                {
                new System.Drawing.Point(0, 0),
                new System.Drawing.Point(image.Width, 0),
                new System.Drawing.Point(0, image.Height),
            };
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);

                // Make the result bitmap.
                Bitmap bm = new Bitmap(image.Width, image.Height);
                using (Graphics gr = Graphics.FromImage(bm))
                {
                    gr.DrawImage(image, points, rect, GraphicsUnit.Pixel, attributes);
                }

                // Return the result.
                return bm;
            }
        }
        public static void Bitmap2HObjectBpp32(Bitmap bmp, out HObject image)
        {
            try
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);

                //PixelFormat.Format24bppRgb
                BitmapData srcBmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                HOperatorSet.GenImageInterleaved(out image, srcBmpData.Scan0, "bgrx", bmp.Width, bmp.Height, 0, "byte", 0, 0, 0, 0, -1, 0);
                bmp.UnlockBits(srcBmpData);
            }
            catch (Exception ex)
            {
                image = null;
            }
        }
        private static Bitmap MergeImage(Bitmap img1, Bitmap img2)
        {
            int width = img1.Width + img2.Width;
            int height = Math.Max(img1.Height, img2.Height);

            Bitmap img3 = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(img3);

            
            g.DrawImage(img1, new System.Drawing.Point(0, 0));
            g.DrawImage(img2, new System.Drawing.Point(img1.Width, 0));

            g.Dispose();
            img1.Dispose();
            img2.Dispose();

            return img3;  
        }
        float[] brightness_range = new float[2] { 1,1};
        private void Brightness_Slide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(selectedImage == null)
            {
                return;
            }    
            Bitmap tempImageProcess = (Bitmap)Bitmap.FromFile(selectedImage.FullPath);
            float brighten_threshold = 0;
            float darken_threshold = 0;

            brighten_threshold = (float)brighten_slide.Value;
            darken_threshold = 1-(float)darken_slide.Value/100;
      
            var image_brighten = AdjustBrightness(tempImageProcess, brighten_threshold);
            var image_darken = AdjustBrightness(tempImageProcess, darken_threshold);

            brightness_range = new float[2] { darken_threshold, brighten_threshold };
            var mergeImage = MergeImage(image_brighten, image_darken);
            displayResult.ClearWindow();
            HObject result = new HObject();
           
            Bitmap2HObjectBpp32(mergeImage, out result);
            displayResult.DispObj(result);

        }

        public HImage RotateImage(HImage image, double value)
        {
            HImage result = new HImage();
            result = image.RotateImage(value, "constant");
            return result;
        }

        private void Btn_Apply_Rotate_Click(object sender, RoutedEventArgs e)
        {
            if (ListImage == null)
            {
                return;
            }
            AddAugmentationList("rotation_range", rotate_slide.Value);
            box_rotate.Visibility = Visibility.Hidden;
            
        }
        private void Btn_GoBack_Rotate_Click(object sender, RoutedEventArgs e)
        {
            box_rotate.Visibility = Visibility.Hidden;
        }
        private void Btn_Rotate_Click(object sender, RoutedEventArgs e)
        {
            box_rotate.Visibility = Visibility.Visible;
        }

        private void Rotate_Slide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedImage == null)
            {
                return;
            }
            var temp = RotateImage(imageProcess, rotate_slide.Value);
            if (temp != null) {
                displayResult.ClearWindow();
                displayResult.DispObj(temp);
            }
           
        }
        private void Btn_Apply_Zoom_Click(object sender, RoutedEventArgs e)
        {                   
            if(ListImage==null)
            {
                return;
            }
            AddAugmentationList("zoom_range", zoom_slide.Value / 100);
            box_zoom.Visibility = Visibility.Hidden;
        }

        private void Btn_GoBack_Zoom_Click(object sender, RoutedEventArgs e)
        {
            box_zoom.Visibility = Visibility.Hidden;
        }

        private void Zoom_Slide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedImage == null)
            {
                return;
            }
            displayResult.ClearWindow();
            var scale = 1-zoom_slide.Value / 100;
            var row1 = (h / 2 - h*scale/2)<0?(HTuple)0: (h / 2 - h * scale / 2);
            var col1 = (w / 2 - w*scale/2)<0?(HTuple)0: (w / 2 - w * scale / 2);
            var row2 = (h / 2 + h * scale / 2)>h?h: (h / 2 + h * scale / 2);
            var col2 = (w / 2 + w * scale / 2)>w?w: (w / 2 + w * scale / 2);
            try
            {
                var temp = imageProcess.CropRectangle1(row1, col1, row2, col2);
                displayResult.DispObj(temp);
            }
            catch
            {

            }
        }
        private void Btn_Zoom_Click(object sender, RoutedEventArgs e)
        {
            box_zoom.Visibility = Visibility.Visible;
        } 
    }
}
                           