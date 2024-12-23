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
using NvAPIWrapper.Display;
using System.Linq;

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for GPUView.xaml
    /// </summary>
    public partial class GPUView : UserControl
    {
        public GPUView()
        {
            InitializeComponent();
            DetectGPU();
        }
        public void DetectGPU()
        {
           var gpus =  NvAPIWrapper.GPU.PhysicalGPU.GetPhysicalGPUs();
            List<string> infomations = new List<string>();
            foreach(var gpu in gpus)
            {

                string info = gpu.FullName.ToString()+Environment.NewLine;
                info += gpu.UsageInformation.GPU+ Environment.NewLine;
                info += gpu.ArchitectInformation + Environment.NewLine;
                info += gpu.MemoryInformation + Environment.NewLine;
                infomations.Add(info);
            }
            lst_gpu.ItemsSource = infomations;
            
            
        }
    }
}
