using HalconDotNet;
using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace NOVisionDesigner.Designer.GeneralWindow
{
    /// <summary>
    /// Interaction logic for ImageGalleryWindow.xaml
    /// </summary>
    public partial class ImageGalleryWindow : Window
    {
        ObservableCollection<ImageFilmstrip> ListImage;
        public ImageGalleryWindow()
        {
            InitializeComponent();
        }
        public ImageGalleryWindow(List<HImage> images)
        {
            InitializeComponent();
        }
        public static Bitmap CreateBitmapFromHImage(HImage image)
        {
            var data= Processing.HalconUtils.HImageToByteArray(image,3,out int w,out int h);
            var bmp = new Bitmap(w, h,
                                 (w * 3 + 3) / 4 * 4,
                                 System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                                 Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
            bmp = (Bitmap)bmp.Clone(); 
            return bmp;
        }
        private void lst_view_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageFilmstrip selected = lst_view.SelectedItem as ImageFilmstrip;
            if (selected != null)
            {
                
                try
                {
                    //SaveResult();
                }
                catch (Exception ex)
                {

                }

                // HImage image = new HImage(selected.FullPath);
                //CreateWpfImage(selected.FullPath);

                //image.GetImageSize(out im_w, out im_h);
                //window_display.HalconWindow.AttachBackgroundToWindow(image);


            }

        }

        private void canvas_host_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void canvas_host_MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        private void canvas_host_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
