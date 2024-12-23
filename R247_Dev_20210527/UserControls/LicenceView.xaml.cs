using System;
using System.Collections.Generic;
using System.IO;
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

namespace NOVisionDesigner.UserControls
{
    /// <summary>
    /// Interaction logic for LicenceView.xaml
    /// </summary>
    public partial class LicenceView : UserControl
    {
        public LicenceView()
        {
            InitializeComponent();
            lb_key.Text = LicenceManager.Licence.Instance.LicenceKey;
            try
            {
                LoadTextDocument(System.IO.Path.Combine(MainWindow.AppPath, "eula.txt"));
                
            }catch(Exception ex)
            {
                App.GlobalLogger.LogError("Licence view", ex.Message);
            }
            
        }
        private void LoadTextDocument(string fileName)

        {

            TextRange range;

            System.IO.FileStream fStream;

            if (System.IO.File.Exists(fileName))

            {

                //range = new TextRange(txt_eula.Document.ContentStart, txt_eula.Document.ContentEnd);

                //fStream = new System.IO.FileStream(fileName, System.IO.FileMode.OpenOrCreate);

                //range.Load(fStream, System.Windows.DataFormats.Text);

                //fStream.Close();
                txt_eula.Text= File.ReadAllText(fileName);

            }

        }
    }
}
