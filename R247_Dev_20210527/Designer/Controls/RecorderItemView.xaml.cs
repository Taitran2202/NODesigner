using HalconDotNet;
using Microsoft.WindowsAPICodePack.Dialogs;
using NOVisionDesigner.Designer.Keyboards;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for RecorderView.xaml
    /// </summary>
    public partial class RecorderItemView : UserControl,INotifyPropertyChanged
    {

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private string SelectionColor;
        private double _colorOpacity = 20;
        public double ColorOpacity
        {
            get { return _colorOpacity; }
            set
            {
                if (_colorOpacity != value)
                {
                    SelectionColor = AddOpacity("#00ff00ff", value / 100);
                    _colorOpacity = value;
                    RaisePropertyChanged("ColorOpacity");
                }
            }
        }
        public HWindow display;
        Recorder _recorder;
        public Recorder Recorder
        {
            get
            {
                return _recorder;
            }
            set
            {
                if (_recorder != value)
                {
                    itemgroup.Recorder = value;
                    _recorder = value;
                }
            }
        }

        InspectionResult defect;
        public DesignerHost Designer { get; set; }
        bool first_init = true;
        public string AddOpacity(string color, double opacity)
        {
            string alpha = color.Substring(7);
            int alpha_int = int.Parse(alpha, System.Globalization.NumberStyles.HexNumber);
            string result = ((int)(alpha_int * opacity)).ToString("X2");
            return color.Remove(7) + result;
        }
        public RecorderItemView()
        {
            InitializeComponent();
            SelectionColor = AddOpacity("#00ff00ff", ColorOpacity / 100); 
            itemgroup.OnSelectionChanged += OnSelectionChanged;
            this.DataContext = this;
        }
        public void OnSelectionChanged(object sender, EventArgs e)
        {
            Recorder.IsView = true;
            ShowResult(sender as InspectionResult);
        }
        public void ShowResult(InspectionResult defect)
        {
            try
            {
                this.defect = defect;
                
                display.AttachBackgroundToWindow(defect.image);
                if (first_init)
                {
                    window_display.SetFullImagePart();
                    first_init = false;
                }
                display.ClearWindow();
                if (show_graphic)
                {
                    foreach (IDisplayable disp in defect.lst_display)
                    {
                        disp?.Display(display);
                    }
                }
                
                
                display.FlushBuffer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HImage image = new HImage();
            image.GenEmptyObj();
            window_display.HalconWindow.DispImage(image);


        }
        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            display = window_display.HalconWindow;
            display.SetDraw("margin");
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                itemgroup.DisplayLatestDefect();
            }));
            
        }

        private void window_display_HMouseDown(object sender, HalconDotNet.HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (defect != null & chk_select_region.IsChecked==true)
            {
                HRegion selectedRegion = null;
                int MinArea = int.MaxValue;
                foreach (IDisplayable displayItem in defect.lst_display)
                {
                    if (displayItem is DisplayObject)
                    {
                        DisplayObject region = displayItem as DisplayObject;
                        if (region.display_object is HRegion)
                        {
                            
                            HRegion selected = (region.display_object as HRegion).SelectRegionPoint((int)e.Row, (int)e.Column);
                            if (selected.CountObj() > 0)
                            {
                                if (selected.Area < MinArea)
                                {
                                    selectedRegion = selected;
                                    MinArea = selected.Area;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                                
                            
                        }

                    }



                }
                if (selectedRegion != null)
                {
                    if (selectedRegion.CountObj() > 0)
                    {
                        var features = selectedRegion.RegionFeatures(new HTuple("area", "width", "height"));
                        lb_area.Content = Math.Round(((features[0].D) / (defect.scale_x * defect.scale_y)), 2).ToString();
                        lb_width.Content = Math.Round(((features[1].D) / (defect.scale_x * defect.scale_y)), 2).ToString();
                        lb_height.Content = Math.Round(((features[2].D) / (defect.scale_x * defect.scale_y)), 2).ToString();
                        display.ClearWindow();
                        foreach (IDisplayable disp in defect.lst_display)
                        {
                            disp?.Display(display);
                        }
                        display.SetDraw("fill");
                        display.SetColor(SelectionColor);
                        display.DispRegion(selectedRegion);
                        // selected.dis(display);
                        //for (int index = 0; index < defect.regions.Count; index++)
                        //{
                        //    display.SetColor(defect.ColorCodes[index]);
                        //    display.DispRegion(defect.regions[index].region);
                        //}

                        //display.SetDraw("fill");
                        //selected.DispObj(display);
                        //display.SetDraw("margin");
                        display.FlushBuffer();
                        //display.SetWindowParam("flush", "true");
                    }
                }
            }

        }
        private void btn_zoom_in_click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, 120);
        }
        private void btn_zoom_out_click(object sender, RoutedEventArgs e)
        {
            window_display.HZoomWindowContents(window_display.ActualWidth / 2, window_display.ActualHeight / 2, -120);
        }
        private void btn_zoom_fit_click(object sender, RoutedEventArgs e)
        {
            window_display.SetFullImagePart();
        }
        private void btn_save_image_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            //diag.InitialDirectory = MainWindow.path_record_ram;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {
                    string record_path = diag.FileName;


                    defect.image.WriteImage("tiff", 0, record_path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void Btn_save_all_Click(object sender, RoutedEventArgs e)
        {
            VirtualNumericKeyboard wd_input = new VirtualNumericKeyboard(20);
            int image_number = 20;
            if (wd_input.ShowDialog() == true)
            {
                image_number = (int)wd_input.result;
                try
                {

                    System.Windows.Forms.FolderBrowserDialog diag = new System.Windows.Forms.FolderBrowserDialog();
                    if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {

                        string record_path = diag.SelectedPath;
                        Recorder.IsSaving = true;
                        int max = Recorder.ResultRecoderQueue.Count;
                        if (max >= image_number)
                        {
                            max = image_number;
                        }
                        for (int i = 0; i < max; i++)
                        {
                            try
                            {
                                Recorder.ResultRecoderQueue.ElementAt(max - i - 1).image.WriteImage("tiff", 0, record_path + "\\" + i.ToString());
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        Recorder.IsSaving = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_previous_Click(object sender, RoutedEventArgs e)
        {
            itemgroup?.SelectRadPrevious();
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            itemgroup?.SelectRadNext();
        }


        private void btn_set_current_image_Click(object sender, RoutedEventArgs e)
        {
            if (Designer != null)
            {
                if (defect == null)
                {
                    return;
                }
                var node= Designer.Network.Nodes.Items.FirstOrDefault(x => x.ID == defect.ID);
                if (node != null)
                {
                    if (node is IImageSourceNode)
                    {
                        (node as IImageSourceNode).SetImage(defect.image);
                    }
                }
            }

        }


        private void save_image_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (defect == null)
            {
                return;
            }
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            //diag.InitialDirectory = MainWindow.path_record_ram;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {
                    string record_path = diag.FileName;


                    defect.image.WriteImage("bmp", 0, record_path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void save_all_click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            VirtualNumericKeyboard wd_input = new VirtualNumericKeyboard(20);
            int image_number = 20;
            if (wd_input.ShowDialog() == true)
            {
                image_number = (int)wd_input.result;
                try
                {

                    System.Windows.Forms.FolderBrowserDialog diag = new System.Windows.Forms.FolderBrowserDialog();
                    if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {

                        string record_path = diag.SelectedPath;
                        Recorder.IsSaving = true;
                        int max = Recorder.ResultRecoderQueue.Count;
                        if (max >= image_number)
                        {
                            max = image_number;
                        }
                        for (int i = 0; i < max; i++)
                        {
                            try
                            {
                                Recorder.ResultRecoderQueue.ElementAt(max - i - 1).image.WriteImage("bmp", 0,
                                    System.IO.Path.Combine(record_path, i.ToString()));
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        Recorder.IsSaving = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_save_all_image_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommonOpenFileDialog diag = new CommonOpenFileDialog();
                diag.IsFolderPicker = true;
                //diag.folder
                // diag.SelectedPath = acq.Record_path;
                if (diag.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string record_path = diag.FileName;
                    Recorder.IsSaving = true;
                    int max = Recorder.ResultRecoderQueue.Count;
                    //if (max >= image_number)
                    //{
                    //    max = image_number;
                    //}
                    for (int i = 0; i < max; i++)
                    {
                        try
                        {
                            Recorder.ResultRecoderQueue.ElementAt(max - i - 1).image.WriteImage("bmp", 0,
                                System.IO.Path.Combine(record_path, i.ToString()));
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    Recorder.IsSaving = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_save_selected_image_Click(object sender, RoutedEventArgs e)
        {
            if (defect == null)
            {
                return;
            }
            System.Windows.Forms.SaveFileDialog diag = new System.Windows.Forms.SaveFileDialog();
            diag.AddExtension = false;
            //diag.InitialDirectory = MainWindow.path_record_ram;
            diag.Filter = "Bitmap (*.bmp)|*.bmp|PNG (*.png)|*.png|All files (*.*)|*.*";
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                try
                {
                    
                    string record_path =diag.FileName;

                    if (diag.FilterIndex == 1)
                    {
                        defect.image.WriteImage("bmp", 0, record_path.Remove(record_path.Length-4,4));
                    }else if(diag.FilterIndex == 2)
                    {
                        defect.image.WriteImage("png", 0, record_path.Remove(record_path.Length - 4, 4));
                    }
                    else
                    {
                        defect.image.WriteImage("bmp", 0, record_path);
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }
        bool show_graphic = true;
        private void chk_show_graphic_Checked(object sender, RoutedEventArgs e)
        {
            show_graphic = true;
            if (this.defect != null)
            {
                ShowResult(this.defect);
            }
        }

        private void chk_show_graphic_Unchecked(object sender, RoutedEventArgs e)
        {
            show_graphic = false;
            if (this.defect != null)
            {
                ShowResult(this.defect);
            }
        }
    }
}
