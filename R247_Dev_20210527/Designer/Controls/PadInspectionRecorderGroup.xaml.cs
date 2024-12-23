using HalconDotNet;
using NOVisionDesigner.Designer;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Windows;
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

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for RecorderView.xaml
    /// </summary>
    public partial class PadInspectionRecorderGroup : UserControl
    {
        List<RadioButton> rad_button = new List<RadioButton>();
        PadInspectionRecorder recorder;
        public PadInspectionRecorderGroup()
        {
            InitializeComponent();
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
                if ((bool)(sender as RadioButton).IsChecked)
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
        public EventHandler OnSelectionChanged;
        public void DisplayLatestDefect()
        {
            try
            {


                if (recorder != null)
                {
                    if (recorder.ResultRecoderQueue.Count > 0)
                    {
                        rad_button[recorder.ResultRecoderQueue.Count - 1].IsChecked = true;
                    }
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"Unexpected error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }
        public void RadSelected(object sender, EventArgs e)
        {
            try
            {
                //recorder.IsView = true;
                current_index = rad_button.IndexOf((RadioButton)sender);
                //view_window.WindowState = WindowState.Normal;
                OnSelectionChanged?.Invoke(recorder.GetResult(current_index),null);
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
                //recorder.IsView = true;
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
                //recorder.IsView = true;
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
                    foreach (PadInspectionResult result in recorder.ResultRecoderQueue)
                    {
                        result.Image.WriteImage("bmp", 0, record_path + "\\" + DateTime.Now.ToString("yyyyMMdd_HHmmssffff"));
                    }
                    recorder.IsSaving = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public PadInspectionRecorder Recorder
        {
            get
            {
                return recorder;
            }

            set
            {
                recorder = value;
                MaxRecord = value.max_record;
                recorder.OnAddQueue += OnAddQueue;
                var count = recorder.ResultRecoderQueue.Count;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        rad_button[i].Visibility = Visibility.Visible;
                    }
                }

            }
        }

    }
    
}
