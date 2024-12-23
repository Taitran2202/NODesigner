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
using WindowsInput;
using WindowsInput.Native;

namespace NOVisionDesigner.Designer.Controls
{
    /// <summary>
    /// Interaction logic for FullKeyboard.xaml
    /// </summary>
    public partial class FullKeyboard : UserControl
    {
        InputSimulator sim = new InputSimulator();
        public FullKeyboard()
        {
            InitializeComponent();
            
        }
        private void btnbksp_Click(object sender, RoutedEventArgs e)
        {
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
        }

        private void btnCaps_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void key_click(object sender, RoutedEventArgs e)
        {
            sim.Keyboard.TextEntry((sender as ContentControl).Content.ToString());
        }

        private void btnSpace_Click(object sender, RoutedEventArgs e)
        {
            sim.Keyboard.KeyPress(VirtualKeyCode.SPACE);
        }

        private void btnCaps_Checked(object sender, RoutedEventArgs e)
        {
            foreach (ContentControl UI in Grid1.Children)
            {
                if (UI!=null)
                {
                    if(UI.Content is string)
                    UI.Content = UI.Content.ToString().ToUpper();
                }
            }
            foreach (ContentControl UI in grid_num.Children)
            {
                if (UI != null)
                {
                    if (UI.Content is string)
                        UI.Content = UI.Content.ToString().ToUpper();
                }
            }
        }

        private void btnCaps_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (ContentControl UI in Grid1.Children)
            {
                if (UI != null)
                {
                    if (UI.Content is string)
                    UI.Content = UI.Content.ToString().ToLower();
                }
            }
            foreach (ContentControl UI in grid_num.Children)
            {
                if (UI != null)
                {
                    if (UI.Content is string)
                        UI.Content = UI.Content.ToString().ToLower();
                }
            }
        }
    }
}
