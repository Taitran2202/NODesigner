using AVT.VmbAPINET;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using HalconDotNet;
using Microsoft.Win32;
using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static OpenCvSharp.Stitcher;

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for VimbaLiveViewWindow.xaml
    /// </summary>
    public partial class VimbaLiveViewWindow : ThemedWindow, INotifyPropertyChanged
    {
        BaseVimbaInterface Model;
        ICollectionView view;
        string trigger_mode;
        public VimbaLiveViewWindow(BaseVimbaInterface model)
        {
            InitializeComponent();
            this.DataContext = this;
            Model= model;
            lst_features_control.ViewModel = Model;
            lst_features_control.lst_features1.ItemsSource = Model.ListFeatures;
            lst_features_control1.ViewModel = Model;
            lst_features_control1.lst_features1.ItemsSource = Model.ListCommonFeatures;
            view = CollectionViewSource.GetDefaultView(Model.ListFeatures);
            view.Filter = TextFilter;
            view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            var fswCreated = Observable.FromEvent<EditValueChangedEventHandler, EditValueChangedEventArgs>(handler =>
            {
                EditValueChangedEventHandler fsHandler = (sender, e) => handler(e);
                return fsHandler;
            },
             fsHandler => txtFilter.EditValueChanged += fsHandler,
             fsHandler => txtFilter.EditValueChanged -= fsHandler);

            fswCreated.Throttle(TimeSpan.FromMilliseconds(200)).ObserveOn(Application.Current.Dispatcher)
                .Subscribe(e => view.Refresh());
            

        }
        private bool TextFilter(object item)
        {
            if (txtFilter.Text == null) return true;
            return VisibilityFilter(item) && (item as CustomVimbaFeature).Name.ToLower().Contains(txtFilter.Text.ToString().ToLower());
        }
        private bool VisibilityFilter(object item)
        {
            switch (cmb_visibility.SelectedIndex)
            {
                case 0:
                    return (item as CustomVimbaFeature).Visibility.Equals(VmbFeatureVisibilityType.VmbFeatureVisibilityBeginner);
                case 1:
                    return (item as CustomVimbaFeature).Visibility.Equals(VmbFeatureVisibilityType.VmbFeatureVisibilityBeginner) || (item as CustomVimbaFeature).Visibility.Equals(VmbFeatureVisibilityType.VmbFeatureVisibilityExpert);
                case 2:
                    return true;
                default:
                    return true;
            }

        }
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        bool _isLive = false;
        public bool IsLive
        {
            get
            {
                return _isLive;
            }
            set
            {
                if (_isLive != value)
                {
                    _isLive = value;
                    RaisePropertyChanged("IsLive");
                }
            }
        }
        int _focusValue;
        public int FocusValue
        {
            get
            {
                return _focusValue;
            }
            set
            {
                if (_focusValue != value)
                {
                    _focusValue = value;
                    RaisePropertyChanged("FocusValue");
                }
            }
        }
        int _frameID;
        public int FrameID
        {
            get
            {
                return _frameID;
            }
            set
            {
                if (_frameID != value)
                {
                    _frameID = value;
                    RaisePropertyChanged("FrameID");
                }
            }
        }
        bool _isLoading = false;
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
        }
        private void OnFrameReceived(Frame frame)
        {
            try
            {
                if (frame.ReceiveStatus == VmbFrameStatusType.VmbFrameStatusIncomplete)
                {
                    return;
                }
                HImage image = new HImage();
                
                int input_width = (int)frame.Width;
                int input_height = (int)frame.Height;
                unsafe
                {
                    fixed (byte* pointer = &frame.Buffer[0])
                    {
                        image.GenImageInterleaved((IntPtr)pointer, "rgb", input_width, input_height, 0, "byte", input_width, input_height, 0, 0, 8, 0);
                        HTuple deviation, intensity;
                        intensity = image.Intensity(image, out deviation);
                        FocusValue = (int)((deviation.D - 80) * 100 / 40);
                        window_display.HalconWindow.DispObj(image);
                        FrameID++;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(VimbaException))
                {
                    Console.WriteLine((ex as VimbaException).ToString());
                }
            }
            finally
            {
                // We make sure to always return the frame to the API
                try
                {

                    Model.camera.QueueFrame(frame);
                }
                catch (Exception ex)
                {
                    // Do nothing
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

        private void window_display_HInitWindow(object sender, EventArgs e)
        {
            //window_display.HalconWindow.SetWindowParam("background_color", "white");
        }
        bool re_run = false;
        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            
            if (Model.IsRun)
            {
                if(DXMessageBox.Show(this, "Camera running! Do you want to continue","Warning", 
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning)== MessageBoxResult.OK)
                {
                    Model.Stop();
                    re_run = true;
                }
                else
                {
                    return;
                }
                
            }
            if (Model.camera == null)
                Model.Connected = Model.Connect();
            if (Model.camera == null)
                return;
            var mode = Model.ListFeatures.FirstOrDefault(x => x.Name == "TriggerMode");
            //var source = Model.ListFeatures.FirstOrDefault(x => x.Name == "TriggerSource");
            trigger_mode = mode.EnumValue;
            //trigger_source = source.EnumValue;
            //mode.EnumValue = "Off";
            //source.EnumValue = "Freerun";
            Model.SetAndUpdateFeatureValue(mode, "Off");
            //Model.SetAndUpdateFeatureValue(source, "Freerun");
            bool error = true;
            try
            {
                // Register frame callback
                Model.camera.OnFrameReceived += this.OnFrameReceived;
                IsLive = true;
                // Start synchronous image acquisition (grab)
                Model.camera.StartContinuousImageAcquisition(3);
                error = false;
            }
            finally
            {
                if (error) Model.ReleaseCamera();
            }
            



        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Model.camera.OnFrameReceived -= this.OnFrameReceived;
                Model.camera.StopContinuousImageAcquisition();
                IsLive= false;
            }
            catch
            {
            }
        }

        private void ThemedWindow_Closed(object sender, EventArgs e)
        {

            if(IsLive)
            {
                try {
                    btn_stop_Click(null, null);
                    }catch(Exception ex)
                {

                }
                
            }
            if (trigger_mode != null)
            {
                try
                {
                    var mode = Model.ListFeatures.FirstOrDefault(x => x.Name == "TriggerMode");
                    if (mode != null)
                        Model.SetAndUpdateFeatureValue(mode, trigger_mode);
                }catch(Exception ex)
                {

                }
               
                
            }
            if (re_run)
            {
                try
                {
                    Model?.Start();
                }catch(Exception ex)
                {

                }
               
            }
            //if (trigger_source != null)
            //{
            //    var source = Model.ListFeatures.FirstOrDefault(x => x.Name == "TriggerSource");
            //    if (source != null)
            //        Model.SetAndUpdateFeatureValue(source, trigger_source);
            //}
        }

        private void cmb_visibility_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (view == null) return;
            view.Refresh();
        }

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            if (Model.camera == null) return;
            Model.camera.LoadSaveSettingsSetup(VmbFeaturePersistType.VmbFeaturePersistAll, 2, 4);
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "XML (*.xml)|*.xml|All Files (*.*)|*.*";
            dialog.AddExtension = true;
            dialog.FileName = Model.GetDevice() + ".xml";
            IsLoading = true;
            if (dialog.ShowDialog() == true)
            {
                Task.Run(() =>
                {
                    try
                    {

                        Model.camera.SaveCameraSettings(dialog.FileName);
                        DXMessageBox.Show("Save setting successfully", "Info");
                    }
                    catch (Exception)
                    {
                        DXMessageBox.Show("Cannot export setting.", "Error");
                        //throw;
                    }
                    finally { IsLoading = false; }
                });


            }
            IsLoading = false;
        }

        private void btn_import_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog open = new System.Windows.Forms.OpenFileDialog();
            IsLoading = true;

            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Task.Run(() =>
                {
                    if (Model.camera == null) return;
                    Model.camera.LoadSaveSettingsSetup(VmbFeaturePersistType.VmbFeaturePersistAll, 2, 4);
                    try
                    {
                        Model.camera.LoadCameraSettings(open.FileName);
                        DXMessageBox.Show("Load setting successfully", "Info");
                    }
                    catch (Exception)
                    {
                        DXMessageBox.Show("Cannot import setting.", "Error");
                        //throw;
                    }
                    finally { IsLoading = false; }
                });
            }
            IsLoading = false;

            //model.ListFeatures.Clear();
            //foreach (Feature f in model.camera.Features)
            //{
            //    var feature = new CustomVimbaFeature(f);
            //    model.ListFeatures.Add(feature);
            //    if (feature.Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsNone) || feature.Flags.Contains(VmbFeatureFlagsType.VmbFeatureFlagsConst))
            //    {
            //        f.OnFeatureChanged += new Feature.OnFeatureChangeHandler(FeatureChanged);
            //    }
            //}

        }
    }
}
