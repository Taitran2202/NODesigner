using NOVisionDesigner.Designer;
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

namespace NOVisionDesigner.Windows.HelperWindows
{
    /// <summary>
    /// Interaction logic for TagManagerWindow.xaml
    /// </summary>
    public partial class DisplaySettingWindow : Window
    {
        public DisplaySettingWindow(DesignerHost model)
        {
            InitializeComponent();
            this.DataContext= model.displayData;
            stack_menu_setting.DataContext = model.HMI;
            btn_save_setting.Click += (sender, e) =>
            {
                Task.Run(new Action(() =>
                {
                    model.Save(false);
                }));
                
            };
        }

        private void btn_expander_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        
    }
}
