using HalconDotNet;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Misc;
using System;
using System.Collections.Generic;
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

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for RecorderView.xaml
    /// </summary>
    public partial class RecorderView : UserControl
    {
        RecordViewWindow _current_window;
        public RecordViewWindow view_window
        {
            get
            {
                if (_current_window == null)
                {
                    _current_window = new RecordViewWindow(this);
                    _current_window.Closed += _current_window_Closed;
                }
                return _current_window;
            }
        }

        private void _current_window_Closed(object sender, EventArgs e)
        {
            _current_window = null;
        }

        List<RadioButton> rad_button = new List<RadioButton>();
        Recorder recorder;
        public RecorderView()
        {
            InitializeComponent();
            //view_window = new RecordViewWindow(this);
            MaxRecord = App.AppSetting.MaxTempRecordImage;
        }
        public void OnAddQueue(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => rad_button[(int)sender].Visibility = Visibility.Visible));

        }                                    
        private int max_record;

        public int MaxRecord
        {
            get
            {
                return max_record;
            }

            set
            {
                max_record = value;
                Recorder.max_record = value;
                if (rad_button.Count < value)
                {
                    Add(value - rad_button.Count);
                }
                else
                {
                    Remove(rad_button.Count - value);
                }
            }
        }
        public void Close()
        {
            //view_window?.Close();
        }
        public void Add(int number)
        {
            for (int i = 0; i < number; i++)
            {
                RadioButton rad = new RadioButton();
                rad.Visibility = Visibility.Hidden;
                rad.Content = i;
                rad.Checked += RadSelected;
                rad.Click += OnRadClick;
                rad_button.Add(rad);
                panel.Children.Add(rad);
            }
        }
        public void OnRadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((!view_window.IsActive) & ((bool)(sender as RadioButton).IsChecked))
                {

                    RadSelected(sender, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void Remove(int number)
        {
            for (int i = 0; i < number; i++)
            {
                int last = rad_button.Count - 1;
                rad_button.RemoveAt(last);
                panel.Children.RemoveAt(last);
            }
        }
        int current_index = 0;
        public void RadSelected(object sender, EventArgs e)
        {
            try
            {
                recorder.IsView = true;
                current_index = rad_button.IndexOf((RadioButton)sender);
                view_window.Show();
                view_window.Topmost = true;
                //view_window.WindowState = WindowState.Normal;
                view_window.ShowResult(recorder.GetResult(current_index));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void SelectRadNext()
        {
            try
            {
                recorder.IsView = true;
                if (recorder.ResultRecoderQueue.Count > current_index + 1)
                    rad_button[current_index + 1].IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void SelectRadPrevious()
        {
            try
            {
                recorder.IsView = true;
                if (recorder.ResultRecoderQueue.Count > current_index)
                    if (current_index > 0)
                        rad_button[current_index - 1].IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            // popup.IsOpen = true;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            // window = window_display.HalconWindow;
        }
        private void btn_save_all_image_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                System.Windows.Forms.FolderBrowserDialog diag = new System.Windows.Forms.FolderBrowserDialog();
                diag.RootFolder = Environment.SpecialFolder.DesktopDirectory;
                //diag.SelectedPath = MainWindow.path_record_ram;
                if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    string record_path = diag.SelectedPath;
                    recorder.IsSaving = true;
                    foreach (InspectionResult result in recorder.ResultRecoderQueue)
                    {
                        result.image.WriteImage("bmp", 0, record_path + "\\" + DateTime.Now.ToString("yyyyMMdd_HHmmssffff"));
                    }
                    recorder.IsSaving = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public Recorder Recorder
        {
            get
            {
                return recorder;
            }

            set
            {
                recorder = value;
                
                recorder.OnAddQueue += OnAddQueue;
                var count = recorder.ResultRecoderQueue.Count;
                if (count > 0)
                {
                    for(int i = 0; i < count; i++)
                    {
                        rad_button[i].Visibility = Visibility.Visible;
                    }
                }
                
            }
        }
        
    }
    public class Recorder
    {
        public bool IsView = false;
        public int max_record = 20;
        public EventHandler OnAddQueue;
        public Queue<InspectionResult> ResultRecoderQueue;
        private InspectionResult LastSelected = null;
        private InspectionResult WaitToDispose = null;
        public bool IsSaving = false;
        public Recorder()
        {
            ResultRecoderQueue = new Queue<InspectionResult>();
            max_record = App.AppSetting.MaxTempRecordImage;
        }
        public void Add(InspectionResult result)
        {
            if (IsSaving)
                return;
            if (ResultRecoderQueue.Count > max_record - 1)
            {
                if (!IsView)
                {

                    ResultRecoderQueue.Dequeue().Dispose();
                    ResultRecoderQueue.Enqueue(result);
                }
                else
                {
                    InspectionResult temp = ResultRecoderQueue.Dequeue();
                    if (temp != LastSelected)
                    {
                        temp.Dispose();
                    }
                    else
                    {
                        WaitToDispose = LastSelected;
                    }
                    ResultRecoderQueue.Enqueue(result);
                }
            }
            else
            {
                OnAddQueue?.Invoke(ResultRecoderQueue.Count, null);
                ResultRecoderQueue.Enqueue(result);
            }


        }
        public InspectionResult GetResult(int index)
        {
            if (WaitToDispose != null)
            {
                if (WaitToDispose == LastSelected)
                {

                    WaitToDispose.Dispose();
                    WaitToDispose = null;

                }
            }
            InspectionResult value = ResultRecoderQueue.ElementAt(index);
            LastSelected = value;
            return value;
        }

    }

    //public interface IDisplayable : IDisposable
    //{
    //    void Display(HWindow display);
    //}
    //public class DisplayObject : IDisplayable
    //{
    //    public void Dispose()
    //    {
    //        //display_object?.Dispose();
    //    }
    //    public string color = "red";
    //    public HObject display_object = null;
    //    public DisplayObject(string color, HObject display_object)
    //    {
    //        this.color = color;
    //        this.display_object = display_object;
    //    }
    //    public void Display(HWindow display)
    //    {
    //        if (display_object != null)
    //        {
    //            display.SetColor(color);
    //            display.DispObj(display_object);
    //        }
    //    }
    //}
    //public class DisplayText : IDisplayable
    //{
    //    public void Dispose()
    //    {

    //    }
    //    public string color = "black";
    //    public string box_color = "white";
    //    public string message = "";
    //    double row, col;
    //    double fontsize = 12;
    //    public DisplayText(string color, string message, string box_color, double row, double col, double fontsize)
    //    {
    //        this.fontsize = fontsize;
    //        this.color = color;
    //        this.box_color = box_color;
    //        this.message = message;
    //        this.row = row; this.col = col;
    //    }
    //    public void Display(HWindow display)
    //    {
    //        display.SetFont("default-Normal-" + fontsize.ToString());
    //        display.DispText(message, "image", row, col, color, "box_color", box_color);
    //        display.SetFont("default-Normal-12");
    //    }
    //}
    //public class DisplayLine : IDisplayable
    //{
    //    public void Dispose()
    //    {

    //    }
    //    public string color = "green";
    //    double row1, col1, row2, col2;
    //    double size = 2;
    //    public DisplayLine(string color, double row1, double col1, double row2, double col2, double size)
    //    {
    //        this.size = size;
    //        this.color = color;
    //        this.row1 = row1;
    //        this.row2 = row2;
    //        this.col1 = col1;
    //        this.col2 = col2;
    //    }
    //    public void Display(HWindow display)
    //    {
    //        display.SetLineWidth(size);
    //        display.SetColor(color);
    //        display.DispLine(row1, col1, row2, col2);
    //        display.SetLineWidth(1);
    //    }
    //}
}
