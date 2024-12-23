using DevExpress.Xpf.Core;
using NOVisionDesigner.Data;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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
    /// Interaction logic for EditUserWindow.xaml
    /// </summary>
    public partial class EditUserWindow : ThemedWindow
    {
        List<User> users;
        
        public bool IsSubmit { get; set; }
        public UserViewModel1 UserViewModel1 { get; set; }  
        public EditUserWindow(UserViewModel1 user, List<Role> roles_list, List<User> users)
        {
            InitializeComponent();
            UserViewModel1= user;
            this.DataContext = user;
            this.users = users;
            cmb_role.ItemsSource= roles_list;
            user.Role = roles_list.FirstOrDefault(x => x.ID == user.Role?.ID);
        }

        private void btn_submit_Click(object sender, RoutedEventArgs e)
        {
            if (!UserViewModel1.IsEdit)
            {
                if (users.Any(x => x.UserName == UserViewModel1.Username))
                {
                    DXMessageBox.Show(this, "Username is already used.");
                    return;
                }
            }
            IsSubmit = true;
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            IsSubmit= false;
            Close();
        }
    }
}
