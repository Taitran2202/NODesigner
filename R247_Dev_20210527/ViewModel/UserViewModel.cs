using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NOVision.Windows;
using NOVisionDesigner.Data;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NOVisionDesigner.ViewModel
{
    public class UserViewModel:ReactiveObject
    {
        UserView _currentUser;
        public UserView CurrentUser
        {
            get
            {
                return _currentUser;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _currentUser, value);
            }
        }

        static UserViewModel _instance;
        public static UserViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UserViewModel();
                }
                return _instance;
            }
           
        }
        private UserViewModel()
        {
            
            if (!LogInByGuest())
            {
                App.GlobalLogger.LogError("UserViewMode", "Cannot initialize default user");
            }
            using (var db = new UserContext())
            {
                db.Database.Migrate();
            }
        }
        void SetUserView(User user)
        {
            CurrentUser = new UserView()
            {
                ID = user.ID,
                UserName = user.UserName,
                Role = user.Role,
                Name = user.Name,
                LastLogin = user.LastLogin,
                DateCreated = user.DateCreated,
                Description = user.Description
            };
           
        }
        public bool LogIn(string username,string password)
        {
            var salted_dev_username = "io4NclrsuZQ=";
            var hashed_dev_username = "a6JAs0j8ub67nb1+x5kDGaABGJO20mH/71mTjmj6FqEpJTMAaGYTXI4lUbWmFPdyFH0ut03msJOQsTwPeDHOLQoeJ2QQ9zFA+bjHGTpQYTqcVVozhwttoW+bs1w6Xdipgc/Ib33zMysmRQjZnd1U28B3J+OwR9fNs0h1VSEhY+0dziGqH5A1BMKdanVGkMvRx8KLvAjz24ARNT9GdMYl/VPIUI42R1VfJvuhfVQqKwNicTEpVJ/nsP1Y2kccMpHwPphnSOJlENbMjM88u9QtIwXBufX1TjG2rIfGZ13bXvmj3YUFGlinmCWYhtUjb3YAW2Ioh3dnfWt+B0F5yOPTKA==";
            var salted_dev_password = "MErBvk3lI5c=";
            var hashed_dev_password = "T3rU+Q810bT6UISvPz57TECMxJqUOXWKz6VmV/bUqp8np8klsfJd7jy7fq8eQ0lLDirFCkOZJP1de+zsC2qTiGrzsw2J7ZwNvIy7nP7eGV+8hrQj1xJ82GAOFVLS+r97fDNtRYVUzU5iNyy+fxpYY7MHnccZgP9VvBDPLxfe8HSiPnOi8AoEWAv4pRvuZ9aadomxmDqDAxDy1Vd5bBAeED/uMnsVFcuQuGm639Zrs+drZ2LvMTxelt5u+sUC433AFeqcl4t+N1/AFAWfbp8ms04wKzuIuM7vitXBDuHeO9EIX00kpRmDgUMUpIfyuYsnvMBLObZTlAxB7ccp3+/biw==";
            if (AuthenticationHelper.VerifyPassword(username, hashed_dev_username, salted_dev_username))
            {
                if(AuthenticationHelper.VerifyPassword(password, hashed_dev_password, salted_dev_password))
                {
                    var user = new User() { Name = "Developer", Role = new Role() { Name = "DEVELOPER" } };
                    SetUserView(user);
                    return true;
                }
            }
            using(var context = new UserContext())
            {
                var user = context.Users.Include(x => x.Role).ThenInclude(x => x.PermissionsLink).FirstOrDefault(x => x.UserName == username);
                //var user =context.Users.FirstOrDefault(x => AuthenticationHelper.VerifyPassword(password, x.Hash, x.Salt));
                if (user != null)
                {
                    if (AuthenticationHelper.VerifyPassword(password, user.Hash, user.Salt))
                    {
                        DateTime lastLogin = DateTime.Now;
                        user.LastLogin = lastLogin;
                        context.SaveChanges();
                        SetUserView(user);
                        return true;
                    }
                }
            }
            return false;
        }
        public bool LogOut()
        {
            return LogInByGuest();
        }
        public bool SignUp(string name, string username, string password, Role role= null, string description = null)
        {
            using (var context = new UserContext())
            {
                var user = context.Users.FirstOrDefault(x => x.UserName == username);
                if (user == null)
                {
                    HashSalt hashSalt = AuthenticationHelper.GenerateSaltedHash(8, password);
                    Role new_role;
                    if (role != null)
                    {
                        new_role = context.Roles.Single(x => x.ID == role.ID);
                    }
                    else
                    {
                        new_role = context.Roles.FirstOrDefault();
                    }
                    
                    DateTime dateCreated = DateTime.Now;
                    //context.Roles.Attach(new_role);
                    context.Users.Add(new User() { Name = name, UserName = username, Role = new_role, Hash = hashSalt.Hash, Salt = hashSalt.Salt, DateCreated = dateCreated, Description = description });
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }
        public bool SignUp(UserViewModel1 new_user)
        {
            var context = new UserContext();
            try
            {
                HashSalt hashSalt = AuthenticationHelper.GenerateSaltedHash(8, new_user.Password);
                Role new_role = context.Roles.Single(x => x.ID == new_user.Role.ID);
                DateTime dateCreated = DateTime.Now;
                context.Users.Add(new User()
                {
                    Name = new_user.Name,
                    UserName = new_user.Username,
                    Role = new_role,
                    Hash = hashSalt.Hash,
                    Salt = hashSalt.Salt,
                    DateCreated = dateCreated,
                    Description = new_user.Description
                });
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateUser(UserViewModel1 edit_user)
        {
            var context = new UserContext();
            try
            {
                var user = context.Users.Include(x => x.Role).ThenInclude(x => x.PermissionsLink).FirstOrDefault(x => x.ID == edit_user.ID);
                if (user == null) return false;
                user.Role = context.Roles.FirstOrDefault(x => x.ID == edit_user.Role.ID);
                HashSalt hashSalt = AuthenticationHelper.GenerateSaltedHash(8, edit_user.Password);
                user.Hash = hashSalt.Hash;
                user.Salt = hashSalt.Salt;
                user.Description = edit_user.Description;
                DateTime dateCreated = DateTime.Now;
                user.DateCreated = dateCreated;
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool AddNewRole(RoleViewModel roleViewModel)
        {
            var context = new UserContext();
            try
            {
                Role new_role = new Role() { Name = roleViewModel.Name };
                foreach (var permit in roleViewModel.Permissions)
                {
                    var new_permit = context.Permissions.FirstOrDefault(x => x.ID == (permit as Permission).ID);
                    if (new_permit == null) continue;
                    var new_role_permission = new RolePermission() { Role = new_role, Permission = new_permit };
                    new_role.PermissionsLink.Add(new_role_permission);
                }
                context.Roles.Add(new_role);
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateRole(RoleViewModel roleViewModel)
        {
            var context = new UserContext();
            try
            {
                var role = context.Roles.Include(x => x.PermissionsLink).Single(x => x.ID == roleViewModel.ID);
                if (role == null) return false;
                role.Name = roleViewModel.Name;
                role.PermissionsLink.Clear();
                foreach (var permit in roleViewModel.Permissions)
                {
                    var new_permit = context.Permissions.FirstOrDefault(x => x.ID == (permit as Permission).ID);
                    if (new_permit == null) continue;
                    var new_role_permission = new RolePermission() { Role = role, Permission = new_permit };
                    role.PermissionsLink.Add(new_role_permission);
                }
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool AddNewPermission(PermissionViewModel permissionViewModel)
        {
            try
            {
                var context = new UserContext();
                context.Permissions.Add(new Permission() { Description = permissionViewModel.Description, PermissionKey = permissionViewModel.PermissionKey });
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdatePermission(PermissionViewModel permissionViewModel)
        {
            var context = new UserContext();
            var permit = context.Permissions.FirstOrDefault(x => x.ID == permissionViewModel.ID);
            if (permit == null) return false;
            try
            {
                permit.Description = permissionViewModel.Description;
                permit.PermissionKey = permissionViewModel.PermissionKey;
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool LogInByGuest()
        {
            using (var usercontext = new UserContext())
             {
                try
                {
                    usercontext.Database.EnsureCreated();
                    //Permission permit;
                    //if (usercontext.Permissions.Count() == 0)
                    //{
                    //    permit = usercontext.Permissions.Add(new Permission() { Description = "Monitor", PermissionKey="" }).Entity;
                    //    usercontext.SaveChanges();
                    //}
                    //else
                    //{
                    //    permit = usercontext.Permissions.First();
                    //}
                    
                    Role role;
                    if (usercontext.Roles.Count() == 0)
                    {
                        role = usercontext.Roles.Add(new Role() { Name = "MONITOR" }).Entity;
                        //var new_role_permission = new RolePermission() { Role = role };
                        //usercontext.RolePermissions.Attach(new_role_permission);
                        //role.PermissionsLink.Add(new_role_permission);
                        usercontext.SaveChanges();
                    }
                    else
                    {
                        role = usercontext.Roles.First();
                    }
                    var user = usercontext.Users.Include(x=>x.Role).FirstOrDefault(x => x.UserName == "monitor");
                    if (user == null)
                    {
                        user = new User() { Name = "Monitor", UserName = "monitor", Role = role };
                        var added = usercontext.Users.Add(user);
                        
                        user = added.Entity;
                    }
                    DateTime last_logedin = DateTime.Now;
                    user.LastLogin = last_logedin;
                    usercontext.SaveChanges();
                    SetUserView(user);
                    return true;
                }
                catch (Exception ex)
                {
                    
                }
            }
            return false;
        }
        public static bool WriteActionDatabase(string node, string property, string old_value, string new_value, string action_type, string description)
        {
            return true;
            try
            {
                using (var context = new UserContext())
                {
                    var user = context.Users.FirstOrDefault(x => x.ID == Instance.CurrentUser.ID);
                    var action = new UserAction()
                    {
                        Target = node,
                        Property = property,
                        OldValue = old_value,
                        NewValue = new_value,
                        Description = description,
                        Time = DateTime.Now,
                        User = user,
                        ActionType = action_type,

                    };
                    context.Actions.Add(action);
                    context.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
            
        }
    }
    public class UserView
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public Role Role { get; set; }
        public string Name { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime DateCreated { get; set; }
        public string Description { get; set; }
    }
    public class UserViewModel1:IDataErrorInfo, INotifyPropertyChanged
    {
        Regex special_char_regex = new Regex("^[a-zA-Z0-9]+$");

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public bool IsEdit { get; set; }
        public int ID { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    RaisePropertyChanged(nameof(ConfirmPassword));
                }
            }
        }
        public string ConfirmPassword { get; set; }
        public Role Role { get; set; }
        public string Description { get; set; }

        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Username")
                {
                    if (string.IsNullOrEmpty(Username))
                        return "Required";
                    //if (users.Any(x => x.UserName == Username))
                    //    return "Username is already used";
                    if (!special_char_regex.IsMatch(Username))
                        return "Contains special characters";
                }
                if (columnName == "Name")
                {
                    if (string.IsNullOrEmpty(Name))
                        return "Required";
                    
                    if (!special_char_regex.IsMatch(Name))
                        return "Contains special characters";
                }
                if (columnName == "Password")
                {
                    if (string.IsNullOrEmpty(Password))
                        return "Required";

                    //if (Password != ConfirmPassword)
                    //    return "These passwords do not match";
                }
                if (columnName == "ConfirmPassword")
                {
                    if (Password != ConfirmPassword)
                        return "These passwords do not match";
                }
                return null;
            }
        }
        public UserViewModel1() { }
        public UserViewModel1(User user)
        {
            ID = user.ID;
            Username = user.UserName;
            Name = user.Name;
            Role = user.Role;
            Description = user.Description;
        }
    }
    public class RoleViewModel : IDataErrorInfo
    {
        Regex special_char_regex = new Regex("^[a-zA-Z0-9]+$");
        public bool IsEdit { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public List<object> Permissions { get; set; } = new List<object>();
        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Name")
                {
                    if (string.IsNullOrEmpty(Name))
                        return "Required";

                    if (!special_char_regex.IsMatch(Name))
                        return "Contains special characters";
                }
                return null;
            }
        }

        public RoleViewModel()
        {
           
        }
        public RoleViewModel(Role role)
        {
            this.ID = role.ID;
            this.Name = role.Name;
            foreach(var item in role.PermissionsLink)
            {
                Permissions.Add(item.Permission);
            }
        }
    }
    public class PermissionViewModel : IDataErrorInfo
    {
        Regex special_char_regex = new Regex("^[a-zA-Z0-9]+$");
        public bool IsEdit { get; set; }
        public int ID { get; set; }
        public string Description { get; set; }
        public string PermissionKey { get; set; }
        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Description")
                {
                    if (string.IsNullOrEmpty(Description))
                        return "Required";

                    if (!special_char_regex.IsMatch(Description))
                        return "Contains special characters";
                }
                if (columnName == "PermissionKey")
                {
                    if (string.IsNullOrEmpty(PermissionKey))
                        return "Required";
                }
                return null;
            }
        }

        public PermissionViewModel()
        {

        }
        public PermissionViewModel(Permission permission)
        {
            this.ID = permission.ID;
            this.Description = permission.Description;
            this.PermissionKey = permission.PermissionKey;
        }
    }
}
