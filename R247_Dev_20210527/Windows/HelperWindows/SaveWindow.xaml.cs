using DevExpress.Xpf.Core;
using NOVisionDesigner.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NOVisionDesigner.Windows.HelperWindows
{
    /// <summary>
    /// Interaction logic for SaveWindow.xaml
    /// </summary>
    public partial class SaveWindow : Window
    {
        string _text;
        public string Text
        {
            get { return _text; }
            set 
            {
                _text = value;
                //Validate();
            }

        }
        
        IStringValidator validator = null;
        public SaveWindow()
        {
            InitializeComponent();
            
        }
        bool error = false;
        string validate_error_text;
        List<string> duplicate_check_list;
        public SaveWindow(string defaultText,string title="Text Input",List<string> duplicate_check_list=null,string validatorErrorText="Error")
        {
            InitializeComponent();
            this.DataContext = this;
            txt_title.Text = title;
            
            this.validate_error_text = validatorErrorText;
            this.duplicate_check_list = duplicate_check_list;
            Text = defaultText;
            //if (validator != null)
            //{
            //    var fswCreated = Observable.FromEvent<TextChangedEventHandler, TextChangedEventArgs>(handler =>
            //    {
            //        TextChangedEventHandler fsHandler = (sender, e) =>
            //        {
            //            handler(e);
            //        };

            //        return fsHandler;
            //    },
            // fsHandler => txt_saved_name.TextChanged += fsHandler,
            // fsHandler => txt_saved_name.TextChanged -= fsHandler);
            //    fswCreated
            //            .Throttle(TimeSpan.FromMilliseconds(200)).ObserveOn(Application.Current.Dispatcher).Subscribe(e =>
            //            {
            //                Validate();

            //            });
            //    Validate();
            //}
            //txt_saved_name.Focus();
            //txt_saved_name.CaretIndex = txt_saved_name.Text.Length;
        }
        public void Validate()
        {
            if (duplicate_check_list == null) return;
            if (duplicate_check_list.Contains(Text))
            {
                    throw new ArgumentException(validate_error_text);
            }
            
            //    error = true;
            //    txt_error.Text = validate_error_text;
            //    flyout.IsOpen = true;
            //    txt_saved_name.SetResourceReference(TextBox.ForegroundProperty, "RedIOS");
            //    btn_ok.IsEnabled = false;
            //}
            //else
            //{
            //    if (error)
            //    {
            //        error = false;
            //        txt_saved_name.SetResourceReference(TextBox.ForegroundProperty, "BlackIOS");
            //        btn_ok.IsEnabled = true;
            //        flyout.IsOpen = false;
            //    }

        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            //if(string.IsNullOrEmpty(Text))
            //{
            //    DXMessageBox.Show(this,"Input is empty!","Error");
            //    return;
            //}
            if (duplicate_check_list == null)
            {
                DialogResult = true;
                this.Close();
                return;
            };
            if (duplicate_check_list.Contains(Text))
            {
                DXMessageBox.Show(this, "Job name already exists!", "Error");
                return;
            }
            //Text = txt_saved_name.Text;
            DialogResult = true;
            this.Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void FullKeyboard_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }
        public static void DeleteDirectory(string target_dir)
        {
            string[] files = System.IO.Directory.GetFiles(target_dir);
            string[] dirs = System.IO.Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);
                System.IO.File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            System.IO.Directory.Delete(target_dir, false);
        }
    }
    public interface IStringValidator
    {
        bool Validate(string txt);
    }
    public class StringListValidator : IStringValidator
    {
        List<string> list;
        public StringListValidator(List<string> list)
        {
            this.list = list;
        }
        public bool Validate(string txt)
        {
            if (list.Contains(txt))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public class VariableNameValidationRule : ValidationRule
    {
        public override System.Windows.Controls.ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string strValue = Convert.ToString(value);
            if (string.IsNullOrEmpty(strValue))
                return new System.Windows.Controls.ValidationResult(false, $"Value cannot be empty.");
            if (strValue.Contains(" "))
                return new System.Windows.Controls.ValidationResult(false, $"Value cannot contain space characters.");
            Regex regex = new Regex("^[a-zA-Z_$][a-zA-Z_$0-9]*$");
            if (!regex.IsMatch(strValue))
                return new System.Windows.Controls.ValidationResult(false, $"Incorrect variable name declaration.");
            return new System.Windows.Controls.ValidationResult(true, null);
        }
    }
}
