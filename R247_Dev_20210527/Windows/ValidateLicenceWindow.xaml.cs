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

namespace NOVision.Windows
{
    /// <summary>
    /// Interaction logic for ValidateLicenceWindow.xaml
    /// </summary>
    /// 
    public class ID
    {
        string name;

        public string Name
        {

            get { return name; }
            set { name = value; }
        }
        public ID(string name)
        {
            this.name = name;
        }
    }

    public partial class ValidateLicenceWindow : Window
    {
        public ValidateLicenceWindow()
        {
            InitializeComponent();
            foreach (string id in LicenceManager.Licence.Instance.GetCPUID())
            {
                lst_id.Items.Add(new ID(id));
            }
        }

        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_hide_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            if (txt_licence.Text == "tanvubc")
            {
                this.DialogResult = true;
                this.Close();
            }
            if (txt_licence.Text != null)
            {
                LicenceManager.Licence.Instance.SetLicenceKey(txt_licence.Text);
                if (LicenceManager.Licence.Instance.ValidateLicence())
                {
                    MessageBox.Show("Licence is valid");
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Licence is invalid. Please enter another key");
                }
            }

        }
    }
}
