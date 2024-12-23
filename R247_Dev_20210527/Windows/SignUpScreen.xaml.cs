using NOVisionDesigner.Data;
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
    /// Interaction logic for SignUpScreen.xaml
    /// </summary>
    public partial class SignUpScreen : Window, INotifyPropertyChanged
    {
        public SignUpScreen()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public SignUpScreen(User user)
        {
            InitializeComponent();
            this.DataContext = this;
            _userName = user.UserName;
            _name = user.Name;
            
        }
        public UserViewModel UserViewModel { get; } = UserViewModel.Instance;
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string _reEnterPassword;
        public string ReEnterPassword
        {
            get
            {
                return _reEnterPassword;
            }
            set
            {
                if (_reEnterPassword != value)
                {
                    _reEnterPassword = value;
                    RaisePropertyChanged("ReEnterPassword");
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
        string _name;
        public string FullName
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("FullName");
                }
            }
        }
        string _userName;
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
        public ICommand BackToLoginCommand { get; set; }

        private void btn_expander_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ReEnterPasswordBoxEdit_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (Password != ReEnterPassword)
            {
                re_enter_password_edit.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("RosyBrown");
            }
            else
            {
                re_enter_password_edit.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#F2F2F2");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (FullName != null & UserName != null & Password != null & (Password == ReEnterPassword))
            {
                if (UserViewModel.SignUp(FullName, UserName, Password))
                {
                    MessageBox.Show(this, "Create account successfully!");
                    this.Close();
                }
                else
                {
                    MessageBox.Show(this, "Username is already used!");
                }
            }
            
        }

        private void HyperlinkEdit_RequestNavigation(object sender, DevExpress.Xpf.Editors.HyperlinkEditRequestNavigationEventArgs e)
        {
            this.Close();
            //LoginScreen wd = new LoginScreen();
            //wd.ShowDialog();
        }

        private void PasswordBoxEdit_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (Password != ReEnterPassword)
            {
                re_enter_password_edit.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("RosyBrown");
                btn_sign_up.IsEnabled = false;
            }
            else
            {
                re_enter_password_edit.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#F2F2F2");
                btn_sign_up.IsEnabled = true;
            }
        }
    }
}
