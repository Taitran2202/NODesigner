using DATABUILDERAXLibEx;
using DynamicData;
using HalconDotNet;
using MySql.Data.MySqlClient;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("PLC", "Keyence")]
    public class KeyencePLCNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static KeyencePLCNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<KeyencePLCNode>));
        }

        public ValueNodeInputViewModel<bool> TriggerInput { get; }
        public ValueNodeOutputViewModel<PLCDeviceModel> Connection { get; }

        #region Properties
        [HMIProperty("Open database setting")]
        public IReactiveCommand OpenEditor
        {
          get { return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender)); }
        }
        [HMIProperty("IpAddress")]
        public string IpAddress
        {
            get
            {
                return PLC.IPAddress;
            }
            set
            {
                PLC.IPAddress = value;
            } 
        }
        [HMIProperty("Port")]
        public string Port
        {
            get
            {
                return PLC.Port;
            }
            set
            {
                PLC.Port = value;
            }
        }
        [HMIProperty("PLC Type")]
        public string PLCType
        {
            get
            {
                return PLC.PLCType;
            }
            set
            {
                PLC.PLCType = value;
            }
        }
        #endregion

        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    PropertiesWindow wd = new PropertiesWindow(this);
                    wd.Show();
                    break;
            }
        }
        public PLCDeviceModel PLC { get; set; } 
        public override void OnLoadComplete()
        {
            PLC?.Connect();
            
        }
        public override void OnInitialize()
        {
            try
            {
                PLC = new PLCDeviceModel();
            }
            catch (Exception ex)
            {

            }
        }
        public override void Run(object context)
        {
            if (PLC?.IsConnected==true)
            {
                Connection.OnNext(PLC);
            }
        }
        public KeyencePLCNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "KeyencePLC Node";
            TriggerInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "TriggerInput",
                PortType = "Boolean"
            };
            this.Inputs.Add(TriggerInput);

            Connection = new ValueNodeOutputViewModel<PLCDeviceModel>()
            {
                Name = "PLC",
                PortType = "PLCDeviceModel"
            };
            this.Outputs.Add(Connection);


        }

       
    }
    public class PLCDeviceModel : INotifyPropertyChanged
    {
        #region Bindingbase
        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        private string _ip_address = "192.168.0.199";

        public string IPAddress
        {
            get
            {
                return _ip_address;
            }
            set
            {
                if (_ip_address != value)
                {
                    _ip_address = value;
                    RaisePropertyChanged("IPAddress");
                }
            }
        }

        private bool _is_connected = false;

        public bool IsConnected
        {
            get
            {
                return _is_connected;
            }
            set
            {
                if (_is_connected != value)
                {
                    _is_connected = value;
                    RaisePropertyChanged("IsConnected");
                }
            }
        }

        private string _plc_type = "DBPLC_DKV7K";
        private string _port = "8500";

        public string Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    RaisePropertyChanged("Port");
                }
            }
        }

        public string PLCType
        {
            get
            {
                return _plc_type;
            }
            set
            {
                if (_plc_type != value)
                {
                    _plc_type = value;
                    RaisePropertyChanged("PLCType");
                }
            }
        }


        private string _memory_type = "DKV7K_DM";

        public string MemoryType
        {
            get
            {
                return _memory_type;
            }
            set
            {
                if (_memory_type != value)
                {
                    _memory_type = value;
                    RaisePropertyChanged("MemoryType");
                }
            }
        }

        private string _product_tag = "1111";

        public string ProductTag
        {
            get
            {
                return _product_tag;
            }
            set
            {
                if (_product_tag != value)
                {
                    _product_tag = value;
                    RaisePropertyChanged("ProductTag");
                }
            }
        }
        private string _new_pallet_tag = "1113";

        public string NewPalletTag
        {
            get
            {
                return _new_pallet_tag;
            }
            set
            {
                if (_new_pallet_tag != value)
                {
                    _new_pallet_tag = value;
                    RaisePropertyChanged("NewPalletTag");
                }
            }
        }

        private bool _new_pallet_tag_enable = false;

        public bool NewPalletTagEnable
        {
            get
            {
                return _new_pallet_tag_enable;
            }
            set
            {
                if (_new_pallet_tag_enable != value)
                {
                    _new_pallet_tag_enable = value;
                    RaisePropertyChanged("NewPalletTagEnable");
                }
            }
        }


        private string _pallet_code = "1001";

        public string PalletCodeTag
        {
            get
            {
                return _pallet_code;
            }
            set
            {
                if (_pallet_code != value)
                {
                    _pallet_code = value;
                    RaisePropertyChanged("PalletCodeTag");
                }
            }
        }

        private bool _pallet_code_enable = true;

        public bool PalletCodeEnable
        {
            get
            {
                return _pallet_code_enable;
            }
            set
            {
                if (_pallet_code_enable != value)
                {
                    _pallet_code_enable = value;
                    RaisePropertyChanged("PalletCodeEnable");
                }
            }
        }

        private string _pallet_code_length = "20";

        public string PalletCodeLength
        {
            get
            {
                return _pallet_code_length;
            }
            set
            {
                if (_pallet_code_length != value)
                {
                    _pallet_code_length = value;
                    RaisePropertyChanged("PalletCodeLength");
                }
            }
        }


        private string serial_number_tag = "1000";

        public string SerialNumberTag
        {
            get
            {
                return serial_number_tag;
            }
            set
            {
                if (serial_number_tag != value)
                {
                    serial_number_tag = value;
                    RaisePropertyChanged("SerialNumberTag");
                }
            }
        }
        private bool _serial_number_enable = true;

        public bool SerialNumberEnable
        {
            get
            {
                return _serial_number_enable;
            }
            set
            {
                if (_serial_number_enable != value)
                {
                    _serial_number_enable = value;
                    RaisePropertyChanged("SerialNumberEnable");
                }
            }
        }
        private string _serial_number_lenght = "20";

        public string SerialNumberLength
        {
            get
            {
                return _serial_number_lenght;
            }
            set
            {
                if (_serial_number_lenght != value)
                {
                    _serial_number_lenght = value;
                    RaisePropertyChanged("SerialNumberLength");
                }
            }
        }


        private string reset_counter_tag = "1112";

        public string ResetCounterTag
        {
            get
            {
                return reset_counter_tag;
            }
            set
            {
                if (reset_counter_tag != value)
                {
                    reset_counter_tag = value;
                    RaisePropertyChanged("ResetCounterTag");
                }
            }
        }

        private AxDATABUILDERAXLibEx.AxDBCommManager _plc_device = new AxDATABUILDERAXLibEx.AxDBCommManager();

        public AxDATABUILDERAXLibEx.AxDBCommManager PLCDevice
        {
            get
            {
                return _plc_device;
            }
            set
            {
                if (_plc_device != value)
                {
                    PLCDevice = value;
                    RaisePropertyChanged("PLCDevice");
                }
            }
        }


        public void Initital()
        {

            IsConnected = false;
            //PLCDevice.CreateControl();
            // MessageBox.Show("Connected successfully and Parameters has been saved successfully.", "EasyBuilder Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        Thread PLCReconnectThread, PLC_CheckConnectionThread;
        public void Connect()
        {

            try
            {
                PLCDevice.Disconnect();

                //PLCDevice = new AxDATABUILDERAXLibEx.AxDBCommManager();
                //PLCDevice.CreateControl();

                PLCDevice.Peer = IPAddress + ":" + Port;
                Enum.TryParse(PLCType, out DBPlcId my_plc);
                PLCDevice.PLC = my_plc;

                PLCDevice.Connect();
                IsConnected = true;
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("PLC Error", "Không thể kết nối tới PLC" + "\r\n" +
                                                "-Kiểm tra lại địa chỉ IP của PLC" + "\r\n" +
                                                "-Kiểm tra lại Port của PLC " + "\r\n" +
                                                "-Kiểm tra lại đường truyền" + "\r\n" +
                                                "-Khởi động lại phần mềm" + "\r\n" +
                                                "Nếu vẫn không được:" + "\r\n" +
                                                "-Khời động lại máy tính" + "\r\n" +
                                                "-Khởi động lại PLC" + "\r\n" +
                                                "------------------\r\n Error:" + "\r\n");
                return;

            }
            PLCReconnectThread?.Abort();
            PLC_CheckConnectionThread?.Abort();

            Thread.Sleep(1000);
            PLCReconnectThread = new Thread(PLC_Notify_LostConnect);
            PLCReconnectThread.Priority = ThreadPriority.Normal;
            PLCReconnectThread.Start();


            PLC_CheckConnectionThread = new Thread(PLCResetcounterCheck);
            PLC_CheckConnectionThread.Priority = ThreadPriority.BelowNormal;
            PLC_CheckConnectionThread.Start();
        }
        public int? ReadValue(string tag)
        {


            try
            {
                if (IsConnected == true)
                {
                    Array memoryTypeArray = Enum.GetValues(typeof(DBPlcDevice));
                    //int val = visionView.plcCommManager.ReadDevice((DBPlcDevice)memoryTypeArray.GetValue(Properties.Settings.Default.MemoryTypeID), txtProductTagRegister.Text);
                    //txtResultProductTag.Text = val.ToString();
                    Enum.TryParse<DBPlcDevice>(MemoryType, out var type);
                    int val1 = PLCDevice.ReadDevice(type, tag);

                    return val1;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                IsConnected = false;

                return null;
            }
        }
        public string ReadText(string tag, int len)
        {


            try
            {
                Array memoryTypeArray = Enum.GetValues(typeof(DBPlcDevice));
                //int val = visionView.plcCommManager.ReadDevice((DBPlcDevice)memoryTypeArray.GetValue(Properties.Settings.Default.MemoryTypeID), txtProductTagRegister.Text);
                //txtResultProductTag.Text = val.ToString();
                Enum.TryParse<DBPlcDevice>(MemoryType, out var type);
                PLCDevice.ReadText(type, tag, len, out string data);

                return data;
            }
            catch (Exception ex)
            {
                IsConnected = false;

                return null;
            }
        }
        public void WriteValue(string tag, int value)
        {
            try
            {
                Enum.TryParse<DBPlcDevice>(MemoryType, out var type);
                PLCDevice.WriteDevice(type, tag, value);

            }
            catch (Exception ex)
            {
                IsConnected = false;
            }
        }
        public void Remove()
        {
            IsConnected = false;
        }
        public delegate void OnResetCounterRequest(string request);
        public OnResetCounterRequest ResetCounterRequest;
        int counter = 0;
        public void PLCResetcounterCheck()
        {

            while (IsClosing == false)
            {
                if (IsConnected == true)
                {
                    Thread.Sleep(3000);
                    int? a = ReadValue(ResetCounterTag);
                    if (a == null)
                    {
                        IsConnected = false;
                    }
                    if (a == 1)
                    {
                        counter = 0;
                        ResetCounterRequest?.Invoke("");
                        WriteValue(ResetCounterTag, 0);
                    }
                }
                else
                {
                    Thread.Sleep(3000);
                }


            }

        }
        private bool is_closing = false;

        public bool IsClosing
        {
            get
            {
                return is_closing;
            }
            set
            {
                if (is_closing != value)
                {
                    is_closing = value;
                    RaisePropertyChanged("IsClosing");
                }
            }
        }
        int count1 = 0;

        public void PLC_Notify_LostConnect()
        {

            while (IsClosing == false)
            {
                int? a = ReadValue(ResetCounterTag);
                if (IsConnected == false)
                {

                    //try
                    //{

                    //    PLCDevice.Peer = IPAddress + ":" + Port;
                    //    Enum.TryParse(PLCType, out DBPlcId my_plc);
                    //    PLCDevice.PLC = my_plc;

                    //    PLCDevice.Connect();
                    //    IsConnected = true;

                    //}
                    //catch (Exception ex)
                    //{
                    if (count1 > 5)
                    {
                        App.GlobalLogger.LogError("PLC Error", "Không thể kết nối tới PLC" + "\r\n" +
                                                 "-Kiểm tra lại địa chỉ IP của PLC" + "\r\n" +
                                                 "-Kiểm tra lại Port của PLC " + "\r\n" +
                                                 "-Kiểm tra lại đường truyền" + "\r\n" +
                                                 "-Khởi động lại phần mềm" + "\r\n" +
                                                 "Nếu vẫn không được:" + "\r\n" +
                                                 "-Khời động lại máy tính" + "\r\n" +
                                                 "-Khởi động lại PLC" + "\r\n" +
                                                 "------------------\r\n Error:" + "\r\n");
                        count1 = 0;
                    }
                    count1++;

                    //}

                }
                else
                {
                    count1 = 0;
                }

                Thread.Sleep(3000);
            }

        }



    }



}
