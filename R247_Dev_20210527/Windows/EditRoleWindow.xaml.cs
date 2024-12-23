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
    /// Interaction logic for EditRoleWindow.xaml
    /// </summary>
    public partial class EditRoleWindow : ThemedWindow
    {
        List<Role> roles_list;
        public bool IsSubmit { get; set; }
        public RoleViewModel RoleViewModel { get; set; }
        public List<Permission> Permissions { get; set; }
        public EditRoleWindow()
        {
            InitializeComponent();
        }
        public EditRoleWindow(RoleViewModel role, List<Permission> permissions,List<Role> roles_list)
        {
            InitializeComponent();
            this.DataContext = role;
            this.RoleViewModel = role;
            this.Permissions = permissions;
            this.roles_list = roles_list;
            cmb_permissions.ItemsSource= this.Permissions;
        }
        private void btn_submit_Click(object sender, RoutedEventArgs e)
        {
            if (!RoleViewModel.IsEdit)
            {
                if (roles_list.Any(x => x.Name == RoleViewModel.Name))
                {
                    DXMessageBox.Show(this, "Role name is already used.");
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
