using AVT.VmbAPINET;
using DevExpress.Data.Helpers;
using DevExpress.Data.ODataLinq.Helpers;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Grid;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using HalconDotNet;
using Microsoft.Win32;
using MySqlX.XDevAPI.Common;
using NOVisionDesigner.Designer.Accquisition.Windows.UniquaView;
using NOVisionDesigner.Designer.Misc;
using OpenCvSharp;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for GigEVisionVimbaSettingWindow.xaml
    /// </summary>
    public partial class GigEVisionVimbaSettingWindow : ThemedWindow, INotifyPropertyChanged
    {
        public GigEVisionVimbaSettingWindow(BaseVimbaInterface model)
        {
            this.model = model;
            InitializeComponent();
            this.DataContext = this;

            view = CollectionViewSource.GetDefaultView(model.ListFeatures);
            view.Filter = TextFilter;
            view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            lst_features_control.ViewModel = model;
            lst_features_control.lst_features1.ItemsSource = model.ListFeatures;
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
        BaseVimbaInterface model;
        ICollectionView view;
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

        string _loadingMessage = "Loading...";
        public string LoadingMessage
        {
            get
            {
                return _loadingMessage;
            }
            set
            {
                if (_loadingMessage != value)
                {
                    _loadingMessage = value;
                    RaisePropertyChanged("LoadingMessage");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }



        private void cmb_visibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (view == null) return;
            //view.Filter = VisibilityFilter;
            view.Refresh();

        }
        private bool VisibilityFilter(object item)
        {
            switch (cmb_visibility.SelectedIndex)
            {
                case 0:
                    return (item as CustomVimbaFeature).Visibility.Equals(VmbFeatureVisibilityType.VmbFeatureVisibilityBeginner);
                case 1:
                    return 
                        (item as CustomVimbaFeature).Visibility.Equals(VmbFeatureVisibilityType.VmbFeatureVisibilityBeginner) || 
                        (item as CustomVimbaFeature).Visibility.Equals(VmbFeatureVisibilityType.VmbFeatureVisibilityExpert);
                case 2:
                    return true;
                default:
                    return true;
            }

        }
        private void ThemedWindow_Closed(object sender, EventArgs e)
        {
            var list = model.ListCommonFeatures.Select(x => x.Name).ToList();
            model.ListCommonFeatures.Clear();
            if (model == null) return;
            foreach (var name in list)
            {
                var feature = model.ListFeatures.FirstOrDefault(x => x.Name == name);
                if (feature != null)
                    model.ListCommonFeatures.Add(feature);
            }

        }

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            if (model.camera == null) return;
            model.camera.LoadSaveSettingsSetup(VmbFeaturePersistType.VmbFeaturePersistAll, 2, 4);
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "XML (*.xml)|*.xml|All Files (*.*)|*.*";
            dialog.AddExtension = true;
            dialog.FileName = model.GetDevice() + ".xml";
            IsLoading = true;
            if (dialog.ShowDialog() == true)
            {
                Task.Run(() =>
                {
                    try
                    {

                        model.camera.SaveCameraSettings(dialog.FileName);
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
                    if (model.camera == null) return;
                    model.camera.LoadSaveSettingsSetup(VmbFeaturePersistType.VmbFeaturePersistAll, 2, 4);
                    try
                    {
                        model.camera.LoadCameraSettings(open.FileName);
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
        
        //private void txtFilter_EditValueChanged(object sender, EditValueChangedEventArgs e)
        //{
        //    //view.Refresh();
        //}
        private bool TextFilter(object item)
        {
            if (txtFilter.Text == null) return true;
            return VisibilityFilter(item)&& (item as CustomVimbaFeature).Name.ToLower().Contains(txtFilter.Text.ToString().ToLower());
        }
    }
}
