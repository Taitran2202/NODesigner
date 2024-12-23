using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Controls;
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

namespace NOVisionDesigner.Designer.Windows
{
    /// <summary>
    /// Interaction logic for RecordInputImageWindow.xaml
    /// </summary>
    public partial class RecordInputImageWindow : Window
    {
        public RecordInputImageWindow(ValueNodeInputViewModel<HImage> Input, string RecordDirectory)
        {
            InitializeComponent();
            AddImageForNodeControl control = new AddImageForNodeControl(Input, RecordDirectory) { 
                VerticalAlignment= VerticalAlignment.Stretch,
                HorizontalAlignment= HorizontalAlignment.Stretch
            };
            grid.Children.Add(control);
            this.Closed += (o, e) =>
            {
                control.Dispose();
            };

        }
    }
}
