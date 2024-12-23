using NOVisionDesigner.Designer.Windows;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NOVisionDesigner.Designer.PropertiesViews
{
    /// <summary>
    /// Interaction logic for TextRecognitionPropertiesView.xaml
    /// </summary>
    public partial class TextRecognitionPropertiesView : UserControl
    {
        public TextRecognitionPropertiesView(Designer.Nodes.TextRecognitionNode node)
        {
            InitializeComponent();
            this.Initialized += (o, e) =>
            {
                grid_model_prop.SelectedObject = node;
            };
            //try
            //{

            //    var Properties = new List<Property>();
            //    Properties.Add(new Property() { Name = "Warmup" });
            //    Properties.Add(new Property() { Name = "WarmupBatch" });
            //    Properties.Add(new Property() { Name = "MaxTextWidth" });
            //    Properties.Add(new Property() { Name = "ExpandRatio" });
            //    Properties.Add(new Property() { Name = "DisplayPosition" });
            //    Properties.Add(new Property() { Name = "TextForegroundColor" });

            //    grid_model_prop.PropertyDefinitionsSource = Properties;
            //}
            //catch (Exception ex)
            //{

            //}

        }
    }
}
