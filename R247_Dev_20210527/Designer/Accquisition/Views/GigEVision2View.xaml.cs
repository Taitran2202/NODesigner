using NOVisionDesigner.Designer.Windows.GigeCameraUserControl;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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

namespace NOVisionDesigner.Designer.Accquisition.Views
{
    /// <summary>
    /// Interaction logic for GigEVision2View.xaml
    /// </summary>
    public partial class GigEVision2View : UserControl,IViewFor<GigEVision2>
    {
        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(nameof(ViewModel), typeof(GigEVision2), typeof(GigEVision2View), new PropertyMetadata(null));

        public GigEVision2 ViewModel
        {
            get => (GigEVision2)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (GigEVision2)value;
        }
        public GigEVision2View()
        {
            InitializeComponent();
            //this.DataContext = ViewModel;
            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext).DisposeWith(d);
            });
            
        }
        private void btn_camera_setting_click(object sender, RoutedEventArgs e)
        {
            //if (!ViewModel.IsRun)
            //{
            //    ViewModel.Start();
            //}
            //else
            //{
            //    ViewModel.Stop();
            //}

        }

        private void Btn_select_camera_Click(object sender, RoutedEventArgs e)
        {
            DeviceListWindow wd = new DeviceListWindow();
            string selected = wd.ShowDevice();
            if (selected != "")
                ViewModel.Device = selected;
        }

        private void btn_sofware_trigger_click(object sender, RoutedEventArgs e)
        {
            ViewModel?.Trigger();
        }
        private void Btn_UserSetLoad_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.UserSetLoadEvent();
        }

        private void Btn_UserSetSave_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.UserSetSaveEvent();
        }

        private void btn_stream_status_click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] features = new string[]
                {
                    "[Stream]GevStreamDeliveredPacketCount",
                    "[Stream]GevStreamDeliverIncompleteBlocks",
                    "[Stream]GevStreamDiscardedBlockCount",
                    "[Stream]GevStreamDuplicatePacketCount",
                    "[Stream]GevStreamEngineUnderrunCount",
                    "[Stream]GevStreamFullBlockTerminatesPrev",
                    "[Stream]GevStreamIncompleteBlockCount",
                    "[Stream]GevStreamLostPacketCount",
                    "[Stream]GevStreamMaxBlockDuration",
                    "[Stream]GevStreamMaxPacketGaps",
                    "[Stream]GevStreamOversizedBlockCount",
                    "[Stream]GevStreamPacketOrderDelay",
                    "[Stream]GevStreamResendCommandCount",
                    "[Stream]GevStreamResendPacketCount",
                    "[Stream]GevStreamSeenPacketCount",
                    "[Stream]GevStreamSkippedBlockCount",
                    "[Stream]GevStreamUnavailablePacketCount"
                };
                var result = ViewModel.GetFeatures(new HalconDotNet.HTuple(features));
                string message = "";
                for(int i = 0; i < result.Length; i++)
                {
                    string value="0";
                    if(result[i].Type == HalconDotNet.HTupleType.DOUBLE)
                    {
                        value = result[i].D.ToString();
                    }
                    if (result[i].Type == HalconDotNet.HTupleType.INTEGER)
                    {
                        value = result[i].I.ToString();
                    }
                    if (result[i].Type == HalconDotNet.HTupleType.STRING)
                    {
                        value = result[i].S;
                    }
                    if (result[i].Type == HalconDotNet.HTupleType.LONG)
                    {
                        value = result[i].L.ToString();
                    }

                    message = message+ features[i]+" : " + value + Environment.NewLine;
                }
                MessageBox.Show(message);
            }
            catch(Exception ex)
            {

            }
        }

        private void btn_live_view_click(object sender, RoutedEventArgs e)
        {
            ViewModel?.LiveView();
        }
    }
}
