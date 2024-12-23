using DynamicData;
using EasyModbus;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using static DALSA.SaperaLT.SapClassBasic.SapManager;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "ModbusRTUMaster", sortIndex: 5)]
    public class ModbusRTUMasterNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static ModbusRTUMasterNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ModbusRTUMasterNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> addressTrigger;
        [HMIProperty]
        public ValueNodeInputViewModel<int> AddressTrigger
        {
            get => addressTrigger;
            set
            {
                this.RaiseAndSetIfChanged(ref addressTrigger, value);
            }
        }
        public ValueNodeOutputViewModel<bool> Trigger { get; }
        public ValueNodeOutputViewModel<ModbusRTUMaster> ModbusRTUMasterOutput { get; }

        [HMIProperty("Default Editor")]
        public IReactiveCommand OpenEditor
        {
            get
            {
                return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender));
            }
        }

        private string _PortName;
        public string PortName
        {
            get { return _PortName; }
            set { _PortName = value; }
        }

        private int _SerialBaudRate;
        public int SerialBaudRate
        {
            get { return _SerialBaudRate; }
            set { _SerialBaudRate = value; }
        }

        private Parity _SerialParity;
        public Parity SerialParity
        {
            get { return _SerialParity; }
            set
            {
                _SerialParity = value;
            }
        }
        private StopBits _SerialStopBits;
        public StopBits SerialStopBits
        {
            get { return _SerialStopBits; }
            set
            {
                _SerialStopBits = value;
            }
        }
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    ModbusRTUWindow wd = new ModbusRTUWindow(PortName, SerialBaudRate, SerialParity, SerialStopBits);
                    wd.ShowDialog();
                    _PortName = wd.PortName;
                    _SerialBaudRate = wd.BaudRate;
                    _SerialParity = wd.SerialParity;
                    _SerialStopBits = wd.SerialStopBits;
                    break;
            }
        }
        public ModbusRTUMasterNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Modbus RTU Master";
            EnabledInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "IsEnabled",
                PortType = "boolean"
            };
            this.Inputs.Add(EnabledInput);

            AddressTrigger = new ValueNodeInputViewModel<int>
            {
                Name = "AddressTriger",
                PortType = "int",
                Editor = new IntegerValueEditorViewModel(),
            };
            this.Inputs.Add(AddressTrigger);

            ModbusRTUMasterOutput = new ValueNodeOutputViewModel<ModbusRTUMaster>()
            {
                Name = "ModbusRTUMaster",
                PortType = "ModbusRTUMaster",
            };
            this.Outputs.Add(ModbusRTUMasterOutput);
            Trigger = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Trigger",
                PortType = "boolean",
            };
            this.Outputs.Add(Trigger);

            this.WhenAnyValue(x => x.EnabledInput.Value).Subscribe(d =>
            {
                if (EnabledInput.Value == true)
                {
                    if (modbusRTUMaster == null)
                    {
                        modbusRTUMaster = new ModbusRTUMaster();
                    }
                    modbusRTUMaster?.Listen(PortName, SerialBaudRate, SerialParity, SerialStopBits);
                    modbusRTUMaster.WhenAnyValue(x => x.OutTrigger).Subscribe(x =>
                    {
                        Trigger.OnNext(modbusRTUMaster.OutTrigger);
                    });
                    modbusRTUMaster.AddressTrigger = this.AddressTrigger.Value;
                }
                else
                {
                    modbusRTUMaster?.StopListen();
                }
            });
            this.WhenAnyValue(x => x.AddressTrigger.Value).Subscribe(d => {
                if (modbusRTUMaster != null)
                {
                    modbusRTUMaster.AddressTrigger = this.AddressTrigger.Value;
                }
            }
            );

        }

        public override void OnInitialize()
        {
        }

        public override void Run(object context)
        {
            if (modbusRTUMaster == null)
            {
                modbusRTUMaster = new ModbusRTUMaster();
            }
            if (modbusRTUMaster != null)
            {
                if (modbusRTUMaster.masterListened == false)
                {
                    modbusRTUMaster.Listen(PortName, SerialBaudRate, SerialParity, SerialStopBits);
                }
                ModbusRTUMasterOutput.OnNext(modbusRTUMaster);
            }
            else
            {
                ModbusRTUMasterOutput.OnNext(null);
            }
        }
        public ModbusRTUMaster modbusRTUMaster { get; set; }
    }
    public class ModbusRTUMaster : ReactiveObject
    {

        ModbusServer master;
        public bool masterListened { get; set; } = false;
        public int AddressTrigger { get; set; }

        public bool serverConnected = false;
        bool _outTrigger;
        public bool OutTrigger
        {
            get
            {
                return _outTrigger;
            }
            set
            {

                this.RaiseAndSetIfChanged(ref _outTrigger, value);
            }
        }
        public ModbusRTUMaster()
        {
            master = new ModbusServer();
            master.HoldingRegistersChanged += new ModbusServer.HoldingRegistersChangedHandler(HoldingRegistersChanged);
            master.CoilsChanged += new ModbusServer.CoilsChangedHandler(CoilsChanged);
        }
        public void Listen(string port_name, int baud_rate, Parity parity, StopBits stop_bit)
        {
            try
            {
                master.SerialPort = port_name;
                master.Parity = parity;
                master.Baudrate = baud_rate;
                master.StopBits = stop_bit;

                master.Listen();
                
                masterListened = true;
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }
        public void StopListen()
        {
            if (masterListened)
            {
                master.StopListening();
                masterListened = false;
            }
        }

        private void CoilsChanged(int address, int numberOfRegisters)
        {
            if (masterListened)
            {
                if (address == AddressTrigger)
                {
                    OutTrigger = true;
                    Thread.Sleep(200);
                    OutTrigger = false;
                }
            }
        }
        private void HoldingRegistersChanged(int register, int numberOfRegisters)
        {
            if (masterListened)
            {

            }
        }
        
    }
}
