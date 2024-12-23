using DevExpress.Xpf.Core;
using NOVisionDesigner.Designer.Nodes;
using ReactiveUI;
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
    /// Interaction logic for RecordSettingWindow.xaml
    /// </summary>
    public partial class RecordSettingWindow : ThemedWindow
    {
        public RecordSettingWindow(RecordImage model)
        {
            InitializeComponent();
            this.DataContext = model;
            model.WhenAnyValue(x => x.DateFolderFormat).Subscribe(x =>
              {
                  //validate format
                  try
                  {
                      txt_format_validation.Content = DateTime.Now.ToString(x);
                  }catch (Exception ex)
                  {
                      txt_format_validation.Content = "Error format";
                  }
                  
              });
            model.WhenAnyValue(x => x.ImageNameFormat).Subscribe(x =>
            {
                //validate format
                try
                {
                    txt_image_format_validation.Content = DateTime.Now.ToString(x);
                }
                catch (Exception ex)
                {
                    txt_image_format_validation.Content = "Error format";
                }

            });

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if(result== System.Windows.Forms.DialogResult.OK)
                {
                    (this.DataContext as RecordImage).MainDir = dialog.SelectedPath;
                }
            }
            
        }

        private void btn_open_namesub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.Combine( (this.DataContext as RecordImage).MainDir, (this.DataContext as RecordImage).NameSub));
        }

        private void btn_open_namepass_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe",(this.DataContext as RecordImage).PassDir);
        }

        private void btn_open_namefail_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", (this.DataContext as RecordImage).FailDir);
        }
        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_apply_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
