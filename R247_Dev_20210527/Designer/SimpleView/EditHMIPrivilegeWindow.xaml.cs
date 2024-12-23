using NOVisionDesigner.Data;
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

namespace NOVisionDesigner.Designer.SimpleView
{
    /// <summary>
    /// Interaction logic for EditHMIPrivilegeWindow.xaml
    /// </summary>
    public partial class EditHMIPrivilegeWindow : DevExpress.Xpf.Core.ThemedWindow
    {
        List<RoleView> ListRoles = new List<RoleView>();
        NodeBinding node;
        public EditHMIPrivilegeWindow(NodeBinding node)
        {
            InitializeComponent();
            this.node = node;
            using (UserContext context = new UserContext())
            {
                ListRoles=context.Roles.Select(x => new RoleView() { Name = x.Name }).ToList();
            }
            lst_role_permissions.ItemsSource = ListRoles;
            foreach(var item in ListRoles.Where(x => node.Role.Contains(x.Name))){
                item.IsEnabled = true;
            }
            

        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                node.Role = (lst_role_permissions.ItemsSource as List<RoleView>).Where(x => x.IsEnabled).Select(x=>x.Name).ToList();
                if (node.Role.Contains(ViewModel.UserViewModel.Instance.CurrentUser.Role.Name))
                {
                    node.IsEditable = true;
                }
                else
                {
                    node.IsEditable = false;
                }
                this.Close();
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public class RoleView
        {
            public string Name { get; set; }
            public bool IsEnabled { get; set; } = false;
        }
    }
}
