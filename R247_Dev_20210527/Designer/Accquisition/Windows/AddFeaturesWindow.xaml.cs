using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using NOVisionDesigner.Designer.Accquisition;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.SimpleView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reflection;
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

namespace NOVisionDesigner.Designer.Accquisition.Windows
{
    /// <summary>
    /// Interaction logic for AddFeaturesWindow.xaml
    /// </summary>
    public partial class AddFeaturesWindow : ThemedWindow, INotifyPropertyChanged
    {
        BaseVimbaInterface camera;
        ICollectionView current_list, all_list;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public AddFeaturesWindow(BaseVimbaInterface camera)
        {
            this.camera = camera;
            InitializeComponent();
            lst_current_features.ItemsSource = camera.ListCommonFeatures;
            lst_all_features.ItemsSource = camera.ListFeatures;
            current_list = CollectionViewSource.GetDefaultView(lst_current_features.ItemsSource);
            current_list.Filter = CurrentFilter;
            all_list = CollectionViewSource.GetDefaultView(lst_all_features.ItemsSource);
            all_list.Filter = AllFilter;
            var fswCreated = Observable.FromEvent<EditValueChangedEventHandler, EditValueChangedEventArgs>(handler =>
            {
                EditValueChangedEventHandler fsHandler = (sender, e) =>
                {
                    handler(e);
                };

                return fsHandler;
            },
             fsHandler => {
                 txt_search_current.EditValueChanged += fsHandler;
                 txt_search_all.EditValueChanged += fsHandler;
             },
             fsHandler => {
                 txt_search_current.EditValueChanged -= fsHandler;
                 txt_search_all.EditValueChanged -= fsHandler;
             });
            fswCreated.Throttle(TimeSpan.FromMilliseconds(200)).ObserveOn(Application.Current.Dispatcher).Subscribe(e =>
            {
                if((e.Source as TextEdit).Name == "txt_search_current")
                    current_list.Refresh();
                else if((e.Source as TextEdit).Name == "txt_search_all")
                    all_list.Refresh();

            });
        }
        private bool CurrentFilter(object item)
        {
            return (item as CustomVimbaFeature).Name.ToLower().Contains(txt_search_current.Text.ToLower());
        }
        private bool AllFilter(object item)
        {
            return (item as CustomVimbaFeature).Name.ToLower().Contains(txt_search_all.Text.ToLower());
        }
        

        private void b_Drop(object sender, DragEventArgs e)
        {
            var source = e.Data.GetData("Source") as CustomVimbaFeature;
            if (source != null)
            {
                int newIndex = lst_current_features.Items.IndexOf((sender as FrameworkElement).DataContext);
                var list = lst_current_features.ItemsSource as ObservableCollection<CustomVimbaFeature>;
                list.RemoveAt(list.IndexOf(source));
                list.Insert(newIndex, source);
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var data = new DataObject();
                data.SetData("Source", (sender as FrameworkElement).DataContext);
                DragDrop.DoDragDrop(sender as DependencyObject, data, DragDropEffects.Move);
                e.Handled = true;
            }
        }

        private void b_PreviewMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void btn_up_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            var selectedItem = b.DataContext as CustomVimbaFeature;
            if (selectedItem != null)
            {
                var index = camera.ListCommonFeatures.IndexOf(selectedItem);
                if (index > 0)
                {
                    camera.ListCommonFeatures.Move(index, index - 1);
                }

            }
        }

        private void btn_down_Click(object sender, RoutedEventArgs e)
        {
            
            Button b = (Button)sender;
            var selectedItem = b.DataContext as CustomVimbaFeature;
            if (selectedItem != null)
            {
                var index = camera.ListCommonFeatures.IndexOf(selectedItem);
                if (index < camera.ListCommonFeatures.Count - 1)
                {
                    camera.ListCommonFeatures.Move(index, index + 1);

                }

            }
        }

        private void btn_remove_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            var selectedItem = b.DataContext as CustomVimbaFeature;
            if (selectedItem != null)
            {
                camera.ListCommonFeatures.Remove(selectedItem);
            }
        }

        private void ThemedWindow_Closed(object sender, EventArgs e)
        {
            //camera.ListCommonFeatures.Clear();
            //if (camera == null) return;
            //foreach (var f in camera.CommonFeatures)
            //{
            //    var item = camera.ListFeatures.FirstOrDefault(x => x.Name == f);
            //    if (item != null)
            //        camera.ListCommonFeatures.Add(item);
            //}
        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            var selectedItem = b.DataContext as CustomVimbaFeature;
            if (selectedItem != null && !camera.ListCommonFeatures.Contains(selectedItem))
            {
                camera.ListCommonFeatures.Add(selectedItem);
            }
        }
    }

}
