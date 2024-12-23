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
using HalconDotNet;
using System.ComponentModel;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using NOVisionDesigner.Designer.Nodes;
using NOVisionDesigner.Designer.Windows;

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for StainDetectionView.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for BlobDetectionView.xaml
    /// </summary>
    public partial class MeasurementView : UserControl
    {
       
        
        public MeasurementView(MeasurementNode node)
        {
            this.measure = node;
            InitializeComponent();
            this.DataContext = measure;
            lst_measure.ItemsSource = measure.measures;
            
            //TabTipAutomation.IgnoreHardwareKeyboard = HardwareKeyboardIgnoreOptions.IgnoreAll;
            //TabTipAutomation.BindTo<MahApps.Metro.Controls.NumericUpDown>();
        }

       
       

    
        HWindow display;

        public void ReadTextComplete(string result, double area)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //   txt_actual_value.Text = area.ToString();

            }
            ));

        }
        public HWindow Display
        {
            get { return display; }
            set
            {
                display = value;
            }
        }

        public MeasurementNode measure;



        private void btn_edit_position_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Measure selected = (sender as Control).DataContext as Measure;
                if (selected != null)
                {
                    if (measure.ImageInput.Value != null)
                    {

                        EditMeasurementWindow wd = new EditMeasurementWindow(measure.ImageInput.Value.CopyImage(), selected, measure.FixtureInput.Value);
                        wd.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            measure.measures.Add(new Measure(measure.calib ));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
            Measure selected = (sender as Control).DataContext as Measure;
            if (selected != null)
            {
               if (DXMessageBox.Show("Do you want to delete " + selected.MeasureName + " ?","Delete",MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                measure.measures.Remove(selected);
            }
        }

        private void Tag_edit_PopupOpening(object sender, OpenPopupEventArgs e)
        {

        }

        //private void Tag_edit_PopupOpening(object sender, OpenPopupEventArgs e)
        //{
        //    (sender as AutoSuggestEdit).ItemsSource = TagFactory.Instance.CurrentTag;
        //}
    }

}
