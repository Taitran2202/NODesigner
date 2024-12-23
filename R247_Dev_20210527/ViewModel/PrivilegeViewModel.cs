using NOVisionDesigner.Helper;
using NOVisionDesigner.Windows;
using NOVisionDesigner.Windows.HelperWindows;
using ReactiveUI;
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
using System.Windows.Input;
using System.Windows.Media;

namespace NOVisionDesigner.ViewModel
{
    public class PrivilegeViewModel : ReactiveObject
    {
        static PrivilegeViewModel _instance;
        public static PrivilegeViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PrivilegeViewModel();
                }
                return _instance;
            }
        }
        
        public void EnableAll()
        {
            CanOpenDesigner = true;
            CanOpenDesignerList = true;
            CanOpenRecord = true;
            CanOpenTagManager = true;
            CanEditHMISetting = true;
            CanCloseVisionView = true;

        }
        bool _can_close_vision_view;
        public bool CanCloseVisionView
        {
            get
            {
                return _can_close_vision_view;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _can_close_vision_view, value);
            }
        }
        bool _can_open_designer;
        public bool CanOpenDesigner
        {
            get
            {
                return _can_open_designer;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _can_open_designer, value);
            }
        }

        bool _can_open_designer_list;
        public bool CanOpenDesignerList
        {
            get
            {
                return _can_open_designer_list;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _can_open_designer_list, value);
            }
        }
        bool _can_open_record;
        public bool CanOpenRecord
        {
            get
            {
                return _can_open_record;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _can_open_record, value);
            }
        }
        bool _can_open_tag_manager;
        public bool CanOpenTagManager
        {
            get
            {
                return _can_open_tag_manager;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _can_open_tag_manager, value);
            }
        }
        bool _can_edit_hmi_setting;
        public bool CanEditHMISetting
        {
            get
            {
                return _can_edit_hmi_setting;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _can_edit_hmi_setting, value);
            }
        }
    }
}
