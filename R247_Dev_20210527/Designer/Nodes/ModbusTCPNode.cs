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

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "ModbusTCPClient", sortIndex: 5)]
    public class ModbusTCPNode : BaseNode
    {

        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static ModbusTCPNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ModbusTCPNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Host { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Port { get; }
        public ValueNodeOutputViewModel<ModbusTCP> ModbusTCPOutput { get; }

        [HMIProperty("Default Editor")]
        public IReactiveCommand OpenEditor
        {
            get
            {
                return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender));
            }
        }



        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    //Default editor
                    break;
            }
        }
        public ModbusTCPNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "Modbus TCP";
            EnabledInput = new ValueNodeInputViewModel<bool>()
            {
                Name = "IsEnabled",
                PortType = "boolean"
            };
            this.Inputs.Add(EnabledInput);

            Host = new ValueNodeInputViewModel<string>()
            {
                Name = "Host",
                PortType = "string",
                Editor = new StringValueEditorViewModel(false)
            };
            this.Inputs.Add(Host);
            Port = new ValueNodeInputViewModel<int>()
            {
                Name = "Port",
                PortType = "int",
                Editor = new IntegerValueEditorViewModel(),
            };
            this.Inputs.Add(Port);

            ModbusTCPOutput = new ValueNodeOutputViewModel<ModbusTCP>()
            {
                Name = "ModbusTCP",
                PortType = "ModbusTCP",
            };
            this.Outputs.Add(ModbusTCPOutput);
            this.WhenAnyValue(x => x.Host).Subscribe(x =>
            {
                if (ModbusTCPClient != null)
                {
                    ModbusTCPClient.serverConnected = false;
                }
            });
            this.WhenAnyValue(x => x.Port).Subscribe(x =>
            {
                if (ModbusTCPClient != null)
                {
                    ModbusTCPClient.serverConnected = false;
                }
            });

        }

        public override void OnInitialize()
        {
        }

        public override void Run(object context)
        {
            if (ModbusTCPClient == null)
            {
                ModbusTCPClient = new ModbusTCP();
            }
            ModbusTCPClient?.Connect(Host.Value, Port.Value);
            if (ModbusTCPClient != null)
            {
                ModbusTCPOutput.OnNext(ModbusTCPClient);
            }
            else
            {
                ModbusTCPOutput.OnNext(null);
            }
        }
        public ModbusTCP ModbusTCPClient { get; set; }
    }
    public class ModbusTCP
    {

        
        public ModbusClient Client { get; set; }
        public bool serverConnected = false;

        public ModbusTCP()
        {
            Client = new ModbusClient();
        }
        string ip;
        int port;
        public void Connect(string ip, int port)
        {
            if (ip == null)
            {
                return;
            }
            if (this.ip == ip & this.port == port)
            {
                if (serverConnected)
                {
                    return;
                }
            }
            Client.IPAddress = ip;
            Client.Port = port;
            Client.Connect();
            serverConnected = true;
            this.ip = ip;
            this.port = port;
        }
        public void Disconnect()
        {
            Client.Disconnect();
        }
        public void WriteSingleCoil(int address, bool val)
        {
            Client.WriteSingleCoil(address, val);
        }
        public void WriteMultipleCoils(int address, bool[] val)
        {
            Client.WriteMultipleCoils(address, val);
        }
        public void WriteMultipleRegisters(int address, int[] val)
        {
            Client.WriteMultipleRegisters(address, val);
        }
        public void WriteSingleRegister(int registerAddress, int value)
        {
            Client.WriteSingleRegister(registerAddress, value);
        }
        public void WriteMultipleRegisters(int address, string message)
        {
            int[] messageValues = new int[message.Length];

            for (int i = 0; i < message.Length; i++)
            {
                messageValues[i] = (int)message[i];
            }
            Client.WriteMultipleRegisters(0, messageValues);
        }

        public string ReadMultiHoldingRegisters(int address, int length)
        {
            string res = "";
            int[] res_int = Client.ReadHoldingRegisters(address, length);
            for (int i = 0; i < res_int.Length; i++)
            {
                res += (char)res_int[i];
            }
            return res;
        }
    }
}
