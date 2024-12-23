using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Helper
{
    public class User: INotifyPropertyChanged
    {
        private static User user;
        public event PropertyChangedEventHandler typechanged;


        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        string _Type = "Operator";
        public string Type
        {
            get => _Type;
            set
            {
                if (_Type != value)
                {
                    _Type = value;
                    RaisePropertyChanged("Type");
                }
            }
        }

        public static User UserInstance
        {
            get
            {
                if (user == null)
                {
                    user = new User();
                }
                return user;
            }
        }
        //public User(bool defaultvalue)
        //{
        //    if(defaultvalue==true)
        //    {
        //        Name = "Anonymous";
        //        Type = "Monitor";
        //    }
        //}

        public string Name
        {
            get { return _Name; }
            set
            {
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        public string _Name = "Anonymous";


        public string _Password = "";
        public string Password
        {
            get { return _Password; }
            set { _Password = value; RaisePropertyChanged("Password"); }
        }
        public string ID;
    }
}
