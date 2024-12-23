using DevExpress.Xpf.Core;
using NOVisionDesigner.Data;
using NOVisionDesigner.ViewModel;
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

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for EditPermissionWindow.xaml
    /// </summary>
    public partial class EditPermissionWindow : ThemedWindow
    {
        List<Permission> permissions;
        public bool IsSubmit { get; set; }
        public PermissionViewModel PermissionViewModel { get; set; }
        public EditPermissionWindow()
        {
            InitializeComponent();
        }
        public EditPermissionWindow(PermissionViewModel permissionViewModel, List<Permission> permissions)
        {
            InitializeComponent();
            this.PermissionViewModel = permissionViewModel;
            this.permissions = permissions;
            this.DataContext = this.PermissionViewModel;
        }
        private void btn_submit_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionViewModel.IsEdit)
            {
                if (permissions.Any(x => x.PermissionKey == PermissionViewModel.PermissionKey))
                {
                    DXMessageBox.Show(this, "Permission key is already used.");
                    return;
                }
            }
            IsSubmit = true;
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            IsSubmit = false;
            Close();
        }
    }
}
