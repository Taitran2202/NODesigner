using ReactiveUI;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using NOVisionDesigner.Services.ViewModel;
using System.Reactive.Disposables;

namespace NOVisionDesigner.Services.View
{
    /// <summary>
    /// Interaction logic for RSLogixServiceView.xaml
    /// </summary>
    public partial class RSLogixServiceView : UserControl, IViewFor<RSLogixServiceViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(RSLogixServiceViewModel), typeof(RSLogixServiceView), new PropertyMetadata(null));

        public RSLogixServiceViewModel ViewModel
        {
            get => (RSLogixServiceViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (RSLogixServiceViewModel)value;
        }
        public RSLogixServiceView()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(v => v.ViewModel).BindTo(this, v => v.DataContext).DisposeWith(d);
                
                //this.Bind(ViewModel,
                //    viewModel => viewModel.TagJob.Value,
                //    view => view.txt_tagjob.Text).DisposeWith(d);
            });
            //this.DataContext = ViewModel;
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Start();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Stop();
        }

        private void btn_add_write_tag_link_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.WriteTagList.Add(new TagLink());
        }

        private void btn_add_read_tag_link_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReadTagList.Add(new TagLink());
        }

        private void btn_remove_read_tag_link_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement).DataContext as TagLink;
            if (selected != null)
            {
                ViewModel.ReadTagList.Remove(selected); ;
            }
        }

        private void btn_remove_write_tag_link_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as FrameworkElement).DataContext as TagLink;
            if (selected != null)
            {
                ViewModel.WriteTagList.Remove(selected); ;
            }
        }

        private void btn_add_read_all_tag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = ViewModel.MyMicroLogix.GetTagList();
                MessageBox.Show(String.Join(",", data.Select(x => x.TagName)));
            }
            catch(Exception ex)
            {

            }
            
        }
    }
}
