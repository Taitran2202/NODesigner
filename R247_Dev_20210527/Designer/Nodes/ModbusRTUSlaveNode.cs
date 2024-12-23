
using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using EasyModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NOVisionDesigner.Windows;
using System.IO.Ports;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "ModbusRTUSlave", sortIndex: 5)]
    public class ModbusRTUSlaveNode : BaseNode
    {

        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static ModbusRTUSlaveNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ModbusRTUSlaveNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<ModbusRTUSlave> ModbusRTUOutput { get; }

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
        public ModbusRTUSlaveNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Modbus RTU Slave";
            EnabledInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "IsEnabled",
                PortType = "boolean"
            };
            this.Inputs.Add(EnabledInput);

            

            ModbusRTUOutput = new ValueNodeOutputViewModel<ModbusRTUSlave>()
            {
                Name = "ModbusRTUSlave",
                PortType = "ModbusRTUSlave",
            };
            this.Outputs.Add(ModbusRTUOutput);
            

        }

        public override void OnInitialize()
        {
        }

        public override void Run(object context)
        {
            if (modbusRTUSlave == null)
            {
                modbusRTUSlave = new ModbusRTUSlave();
            }
            if (modbusRTUSlave != null)
            {
                if (modbusRTUSlave.IsConnect() == false)
                {
                    modbusRTUSlave.Connect(PortName, SerialBaudRate, SerialParity, SerialStopBits);
                }
                ModbusRTUOutput.OnNext(modbusRTUSlave);
            }
            else
            {
                ModbusRTUOutput.OnNext(null);
            }
        }
        public ModbusRTUSlave modbusRTUSlave { get; set; }
    }
    public class ModbusRTUSlave
    {

        ModbusClient master;
       
        public bool serverConnected = false;

        public ModbusRTUSlave()
        {
            master = new ModbusClient() { ConnectionTimeout = 1000, NumberOfRetries = 2 };
        }
        public void Connect(string port_name, int baud_rate, Parity parity, StopBits stop_bit)
        {
            try
            {
                master.SerialPort = port_name;
                master.Parity = parity;
                master.Baudrate = baud_rate;
                master.StopBits = stop_bit;

                master.Connect();
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }
        public bool IsConnect()
        {
            return master.Connected;
        }
        public void Disconnect()
        {
            master.Disconnect();
        }
        public void WriteSingleCoil(int address, bool val)
        {
            master.WriteSingleCoil(address, val);
        }
        public void WriteMultipleCoils(int address, bool[] val)
        {
            master.WriteMultipleCoils(address, val);
        }
        public void WriteMultipleRegisters(int address, int[] val)
        {
            master.WriteMultipleRegisters(address, val);
        }
        public void WriteSingleRegister(int registerAddress, int value)
        {
            master.WriteSingleRegister(registerAddress, value);
        }
        public void WriteMultipleRegisters(int address, string message)
        {
            int[] messageValues = new int[message.Length];

            for (int i = 0; i < message.Length; i++)
            {
                messageValues[i] = (int)message[i];
            }
            master.WriteMultipleRegisters(0, messageValues);
        }

        public string ReadMultiHoldingRegisters(int address, int length)
        {
            string res = "";
            int[] res_int = master.ReadHoldingRegisters(address, length);
            for (int i = 0; i < res_int.Length; i++)
            {
                res += res_int[i].ToString();
            }
            return res;
        }
    }
}
