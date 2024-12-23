using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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
using DevExpress.Xpf.Core;
using DynamicData;
using DynamicData.Binding;
using NOVisionDesigner.Designer;
using ReactiveUI;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for TotalHMIWindow.xaml
    /// </summary>
    public partial class TotalHMIWindow : ThemedWindow
    {
        public TotalHMIWindow(NOVisionDesigner.ViewModel.VisionModelManager modelManager)
        {
            InitializeComponent();
            if (modelManager != null)
            {
                modelManager.VisionModels.ToObservableChangeSet().Transform(x => x.Designer.HMI).ToCollection().BindTo(this, x => x.hmiList.ItemsSource);
            }
            this.Owner = Application.Current.MainWindow;
            
        }
    }
}
