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
    /// Interaction logic for TypesSelectionWindow.xaml
    /// </summary>
    public partial class TypesSelectionWindow : Window
    {
        public TypesSelectionWindow()
        {
            InitializeComponent();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }
    }
    public class AssemblyType
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
    }
}
