using DevExpress.Xpf.Core;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
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

namespace NOVisionDesigner.Windows
{
    /// <summary>
    /// Interaction logic for LoginScreen.xaml
    /// </summary>
    public partial class LoginScreen : Window, INotifyPropertyChanged
    {
        public LoginScreen()
        {
            InitializeComponent();
            this.DataContext = this;
            IsLoggedIn = false;
        }
        public UserViewModel UserViewModel { get; } = UserViewModel.Instance;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string _userName;
        public bool IsLoggedIn;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    RaisePropertyChanged("UserName");
                }
            }
        }
        bool _is_loading;
        public bool IsLoading
        {
            get
            {
                return _is_loading;
            }
            set
            {
                if (_is_loading != value)
                {
                    _is_loading = value;
                    RaisePropertyChanged("IsLoading");
                }
            }
        }

        string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    RaisePropertyChanged("Password");
                }
            }
        }
        private void btn_expander_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Login();
            
        }
        void Login()
        {
            Task.Run(() =>
            {
                IsLoading = true;
                try
                {
                    if (UserName != null & Password != null)
                    {
                        if (UserViewModel.LogIn(UserName, Password))
                        {
                            IsLoggedIn = true;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                this.Close();
                            });
                        }
                        else
                        {
                            IsLoading = false;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                DXMessageBox.Show(this, "Wrong username or password!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            });

                        }
                    }
                    else
                    {
                        IsLoading = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show(this, "Username or password cannot be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                }
                catch (Exception ex)
                {
                    IsLoading = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                IsLoading = false;
            });
        }
        private void HyperlinkEdit_RequestNavigation(object sender, DevExpress.Xpf.Editors.HyperlinkEditRequestNavigationEventArgs e)
        {
            //this.Close();
            SignUpScreen wd = new SignUpScreen();
            wd.ShowDialog();
        }

        private void btn_view_password_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_password_mouseDown_Click(object sender, MouseButtonEventArgs e)
        {
            txt_password.IsPassword = false;
        }

        private void btn_password_mouseUp_Click(object sender, MouseButtonEventArgs e)
        {
            txt_password.IsPassword = true;
        }

        private void txt_user_name_onKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key== Key.Enter)
            {
                Login();
            }
        }
    }
}

