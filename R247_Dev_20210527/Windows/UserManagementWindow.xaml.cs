using DevExpress.Data.Linq;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using Microsoft.EntityFrameworkCore;
using NOVisionDesigner.Data;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Xml.Linq;
using NOVision.Windows;
using DevExpress.Mvvm.Native;
using System.Diagnostics;

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for UserManagementWindow.xaml
    /// </summary>
    public partial class UserManagementWindow : ThemedWindow
    {
        EntityInstantFeedbackSource _ItemsSource;
        public EntityInstantFeedbackSource ItemsSource
        {
            get
            {
                if (_ItemsSource == null)
                {
                    _ItemsSource = new EntityInstantFeedbackSource
                    {
                        KeyExpression = nameof(LogData.Id)
                    };
                    _ItemsSource.GetQueryable += (sender, e) => {
                        UserContext context = new UserContext();
                        e.QueryableSource = context.Actions.Include(a=>a.User).AsNoTracking();
                    };
                }
                return _ItemsSource;
            }
        }
        public UserManagementWindow()
        {
            InitializeComponent();
            this.DataContext = context;
            context.Roles.Load();
            grid_users.ItemsSource = context.Users.Include(u => u.Role);
            grid_user_actions.ItemsSource = ItemsSource;
            grid_roles.ItemsSource = context.Roles.Include(x => x.PermissionsLink).ThenInclude(x => x.Permission);
            if (UserViewModel.Instance.CurrentUser.Role.ID == 0 || Debugger.IsAttached)
            {
                tab_permissions.Visibility = Visibility.Visible;
            }
            else { tab_permissions.Visibility = Visibility.Collapsed; }
        }
        UserContext context = new UserContext();
        UserViewModel UserViewModel = UserViewModel.Instance;

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            context = new UserContext();
            var roles_list = context.Roles.ToList();
            var users_list = context.Users.ToList();
            var permissions_list = context.Permissions.ToList();
            switch ((tab_ctl.SelectedItem as DXTabItem).Header)
            {
                case "Users":
                    
                    var new_user = new UserViewModel1();
                    new_user.Role = roles_list.FirstOrDefault(x => x.ID == 1);
                    var edit_user_window = new EditUserWindow(new_user, roles_list,users_list);
                    edit_user_window.ShowDialog();
                    if (edit_user_window.IsSubmit)
                    {
                        new_user = edit_user_window.UserViewModel1;
                        if (UserViewModel.SignUp(new_user))
                        {
                            grid_users.RefreshData();
                            DXMessageBox.Show(this, "Create new account successfully.");
                        }
                        else
                        {
                            DXMessageBox.Show(this, "Cannot create user.");
                        }
                    }
                    break;
                case "Roles":                  
                    var new_role = new RoleViewModel();
                    var editRoleWindow = new EditRoleWindow(new_role, permissions_list, roles_list);
                    editRoleWindow.ShowDialog();
                    if(editRoleWindow.IsSubmit)
                    {
                        editRoleWindow.RoleViewModel.Permissions = editRoleWindow.cmb_permissions.SelectedItems.ToList();
                        if (UserViewModel.AddNewRole(editRoleWindow.RoleViewModel))
                        {
                            grid_roles.RefreshData();
                            DXMessageBox.Show(this, "Create new role successfully.");
                        }
                        else
                        {
                            DXMessageBox.Show(this, "Cannot create role.");
                        }
                    }
                    break;
                case "Permissions":
                    var new_permission = new PermissionViewModel();
                    var editPermissionWindow = new EditPermissionWindow(new_permission, permissions_list);
                    editPermissionWindow.ShowDialog();
                    if (editPermissionWindow.IsSubmit)
                    {
                        if(UserViewModel.AddNewPermission(editPermissionWindow.PermissionViewModel))
                        {
                            context = new UserContext();
                            grid_permissions.RefreshData();
                            DXMessageBox.Show("Create new permission successfully.");
                        }
                        else
                            DXMessageBox.Show("Cannot create permission.");
                    }
                    break;
                case "User Actions":
                    break;
                default:
                    break;
            }

        }
        private void btn_remove_users_Click(object sender, RoutedEventArgs e)
        {
            var message_result = DXMessageBox.Show("Are you sure to delete this user?", "Warning",MessageBoxButton.YesNo);
            if (message_result == MessageBoxResult.Yes)
            {
                var selected = context.Users.FirstOrDefault(x => x.ID == (grid_users.SelectedItem as User).ID);
                if (null == selected) return;
                context.Users.Remove(selected);
                context.SaveChanges();
                grid_users.RefreshData();
            }

        }

        private void btn_remove_roles_Click(object sender, RoutedEventArgs e)
        {
            var message_result = DXMessageBox.Show("Are you sure to delete this role?", "Warning",MessageBoxButton.YesNo);
            if (message_result == MessageBoxResult.Yes)
            {
                var selected = context.Roles.FirstOrDefault(x => x.ID == (grid_roles.SelectedItem as Role).ID);
                if (null == selected) return;
                context.Roles.Remove(selected);
                context.SaveChanges();
                grid_roles.RefreshData();
            }
        }

        private void btn_remove_permissions_Click(object sender, RoutedEventArgs e)
        {
            var message_result = DXMessageBox.Show("Are you sure to delete this permission?", "Warning",MessageBoxButton.YesNo);
            if (message_result == MessageBoxResult.Yes)
            {
                var selected = context.Permissions.FirstOrDefault(x => x.ID == (grid_permissions.SelectedItem as Permission).ID);
                if (null == selected) return;
                context.Permissions.Remove(selected);
                context.SaveChanges();
                grid_permissions.RefreshData();
            }
        }

        private void btn_edit_Click(object sender, RoutedEventArgs e)
        {
            context = new UserContext();
            var roles_list = context.Roles.ToList();
            var user_list = context.Users.ToList();
            var selected = grid_users.SelectedItem as User;
            if (selected == null) return;
            var edit_user = new UserViewModel1(selected);
            edit_user.IsEdit = true;
            var wd = new EditUserWindow(edit_user, roles_list, user_list);
            wd.ShowDialog();
            if (wd.IsSubmit)
            {

                if (UserViewModel.UpdateUser(wd.UserViewModel1))
                {
                    context = new UserContext();
                    grid_users.ItemsSource = context.Users.Include(u => u.Role);
                    //grid_modal_user.Visibility = Visibility.Hidden;
                    DXMessageBox.Show(this, "Update successfully!");
                }
                else
                {
                    DXMessageBox.Show(this, "Cannot update user!");
                }
            }
        }

        private void btn_edit_roles_Click(object sender, RoutedEventArgs e)
        {
            context = new UserContext();
            var roles_list = context.Roles.ToList();
            var permissions_list = context.Permissions.ToList();
            var selected = grid_roles.SelectedItem as Role;
            if (null == selected) return;
            var edit_role = new RoleViewModel(selected);
            edit_role.IsEdit = true;
            var editRoleWindow = new EditRoleWindow(edit_role, permissions_list, roles_list);
            foreach(var permit in edit_role.Permissions)
            {
                var item = editRoleWindow.Permissions.FirstOrDefault(x => x.ID == (permit as Permission).ID);
                if (item == null) continue;
                editRoleWindow.cmb_permissions.SelectedItems.Add(item);
            }
            editRoleWindow.ShowDialog();
            if (editRoleWindow.IsSubmit)
            {
                editRoleWindow.RoleViewModel.Permissions = editRoleWindow.cmb_permissions.SelectedItems.ToList();
                if (UserViewModel.UpdateRole(editRoleWindow.RoleViewModel))
                {
                    context = new UserContext();
                    grid_roles.ItemsSource = context.Roles.Include(x => x.PermissionsLink).ThenInclude(x => x.Permission);
                    DXMessageBox.Show(this, "Update role successfully.");
                }
                else
                {
                    DXMessageBox.Show(this, "Cannot update role.");
                }
            }
        }


        private void btn_edit_permissions_Click(object sender, RoutedEventArgs e)
        {
            context = new UserContext();
            var permissions_list = context.Permissions.ToList();
            var selected = grid_permissions.SelectedItem as Permission;
            if (selected == null) return;
            var edit_permission = new PermissionViewModel(selected);
            edit_permission.IsEdit = true;
            var editPermissionWindow = new EditPermissionWindow(edit_permission, permissions_list);
            editPermissionWindow.ShowDialog();
            if (editPermissionWindow.IsSubmit)
            {
                if (UserViewModel.UpdatePermission(editPermissionWindow.PermissionViewModel))
                {
                    grid_permissions.RefreshData();
                    DXMessageBox.Show("Create new permission successfully.");
                }
                else
                    DXMessageBox.Show("Cannot create permission.");
            }
        }

        

        private void grid_user_actions_Loaded(object sender, RoutedEventArgs e)
        {
            
            
            
            tbv_user_actions.BestFitColumns();
        }

        private void tbv_users_Loaded(object sender, RoutedEventArgs e)
        {
            tbv_users.BestFitColumns();
        }

        private void tbv_roles_Loaded(object sender, RoutedEventArgs e)
        {
            tbv_roles.BestFitColumns();
        }

        private void tbv_permissions_Loaded(object sender, RoutedEventArgs e)
        {
            tbv_permissions.BestFitColumns();
        }
    }
}
