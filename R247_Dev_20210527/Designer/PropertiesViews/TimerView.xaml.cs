using DevExpress.Xpf.Charts;
using DevExpress.Xpf.Core;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for TimerView.xaml
    /// </summary>
    public partial class TimerView : UserControl
    {
        public TimerBlock nodeTimer;
        public TimerView(TimerBlock node)
        {
            this.nodeTimer = node;
            InitializeComponent();
            this.DataContext = nodeTimer;
            //lst_timer.ItemsSource = nodeTimer.timerTools;
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            TimerTool selected = (sender as Control).DataContext as TimerTool;
            if (selected != null)
            {
                if (DXMessageBox.Show("Do you want to delete " + selected.TimerName + " ?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    //selected.timerFalling.Dispose();
                    //selected.timerRising.Dispose();
                    //selected.timerDisplay.Dispose();
                    //nodeTimer.timerTools.Remove(selected);
                }                
            }
        }

        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            //TimerTool selected = (sender as Control).DataContext as TimerTool;
            //if (selected !=null)
            //{
            //    selected.DataPoints.Clear();
            //    selected.timerFalling.Start();
            //    selected.isStop = false;
            //    selected.isStart = true;
            //    selected.timerDisplay.Start();
            //}
            //else
            //{
            //    return;
            //}
        }

        private void btn_Stop_Click(object sender, RoutedEventArgs e)
        {
            //TimerTool selected = (sender as Control).DataContext as TimerTool;
            //if (selected != null)
            //{
            //    selected.timerFalling.Stop();
            //    selected.timerRising.Stop();
            //    selected.timerDisplay.Stop();
               
            //    selected.isStop = true;
            //    selected.isStart = false;
            //}
            //else
            //{
            //    return;
            //}
        }
        private void DueTime_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
        //    TimerTool selected = (sender as Control).DataContext as TimerTool;
        //    if (selected != null)
        //    {
        //        if (selected.isStop)
        //        {
        //            selected.DueTime = Convert.ToInt32(e.NewValue);
        //            selected.timerRising.Interval = selected.DueTime;
        //        }
        //        else
        //        {
                   
        //            MessageBox.Show("Please stop timer");
        //        }     
        //    }
        }

        private void PeriodTime_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
        //    TimerTool selected = (sender as Control).DataContext as TimerTool;
        //    if (selected != null)
        //    {
        //        if (selected.isStop)
        //        {
        //            selected.PeriodTime = Convert.ToInt32(e.NewValue);
        //            selected.timerFalling.Interval = selected.PeriodTime;
        //        }
        //        else
        //        {
                    
        //            MessageBox.Show("Please stop timer");
        //        }
        //    }
        }

        private void chart_BoundDataChanged(object sender, RoutedEventArgs e)
        {
            //XYDiagram2D diagram = (sender as Diagram) as XYDiagram2D;
            //if (diagram != null)
            //{
            //    string min = diagram.ActualAxisX.GetScaleValueFromInternal(0).ToString();
            //    string max = diagram.ActualAxisX.GetScaleValueFromInternal(4).ToString();
            //    diagram.ActualAxisX.ActualVisualRange.SetMinMaxValues(min, max);
            //}
        }            
    }
}
