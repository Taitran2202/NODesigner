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
using DevExpress.Xpf.Core.DataSources;
using System.Net;
using NOVisionDesigner.Designer.GroupNodes;
using System.Threading;
using System.Reactive.Linq;
using static DALSA.SaperaLT.SapClassBasic.SapManager;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "ModbusTCPServer", sortIndex: 5)]
    public class ModbusTCPServerNode : BaseNode
    {

        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static ModbusTCPServerNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ModbusTCPServerNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Host { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Port { get; }
        public ValueNodeInputViewModel<int> addressTrigger;
        public ValueNodeInputViewModel<int> AddressTrigger 
        {
            get => addressTrigger;
            set
            {
                this.RaiseAndSetIfChanged(ref addressTrigger, value);
            }
        }
        public ValueNodeOutputViewModel<ModbusTCPServer> ModbusTCPServerOutput { get; }

        public ValueNodeOutputViewModel<bool> Trigger { get; }

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
        public ModbusTCPServerNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            if (ModbusTCPServer == null)
            {
                ModbusTCPServer = new ModbusTCPServer();
            }
            Name = "Modbus TCP Server";
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

            AddressTrigger = new ValueNodeInputViewModel<int>
            {
                Name = "AddressTriger",
                PortType = "int",
                Editor = new IntegerValueEditorViewModel(),
            };
            this.Inputs.Add(AddressTrigger);
            ModbusTCPServerOutput = new ValueNodeOutputViewModel<ModbusTCPServer>()
            {
                Name = "ModbusTCPServer",
                PortType = "ModbusTCPServer",
            };
            this.Outputs.Add(ModbusTCPServerOutput);

            Trigger = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Trigger",
                PortType = "boolean",
            };
            this.Outputs.Add(Trigger);
            this.WhenAnyValue(x => x.Host).Subscribe(x =>
            {
                if (ModbusTCPServer != null)
                {
                    ModbusTCPServer.serverListened = false;
                }
            });
            this.WhenAnyValue(x => x.Port).Subscribe(x =>
            {
                if (ModbusTCPServer != null)
                {
                    ModbusTCPServer.serverListened = false;
                }
            });
            this.WhenAnyValue(x => x.EnabledInput.Value).Subscribe(d =>
            {
                if(EnabledInput.Value == true)
                {
                    if (ModbusTCPServer == null)
                    {
                        ModbusTCPServer = new ModbusTCPServer();
                    }
                    ModbusTCPServer?.Listen(Host.Value, Port.Value);
                }
                else
                {
                    ModbusTCPServer?.StopListen();
                }
            });
            this.WhenAnyValue(x => x.AddressTrigger.Value).Subscribe(d => {
                if (ModbusTCPServer != null)
                {
                    ModbusTCPServer.AddressTrigger = this.AddressTrigger.Value;
                }
            }
            );

            if (ModbusTCPServer != null)
            {
                ModbusTCPServer.WhenAnyValue(x => x.OutTrigger).Subscribe(x =>
                {
                    Trigger.OnNext(ModbusTCPServer.OutTrigger);
                });
            }
        }
        public ModbusTCPServer ModbusTCPServer { get; set; }
        public override void OnInitialize()
        {

        }

        public override void Run(object context)
        {
            
            
            if (ModbusTCPServer != null)
            {
                ModbusTCPServerOutput.OnNext(ModbusTCPServer);
            }
            else
            {
                ModbusTCPServerOutput.OnNext(null);
            }
        }
        
    }
    public class ModbusTCPServer : ReactiveObject
    {

        string ip;
        int port;
        public int AddressTrigger;

        private ModbusServer _server;
        public ModbusServer Server
        {
            get { return _server; }
            set { _server = value; }
        }
        public bool serverListened = false;
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

        public ModbusTCPServer()
        {
            Server = new ModbusServer();
            Server.HoldingRegistersChanged += new ModbusServer.HoldingRegistersChangedHandler(HoldingRegistersChanged);
            Server.CoilsChanged += new ModbusServer.CoilsChangedHandler(CoilsChanged);
        }
        public void Listen(string ip, int port)
        {
            if (ip == null)
            {
                return;
            }
            if (this.ip == ip & this.port == port)
            {
                if (serverListened)
                {
                    return;
                }
            }
            Server.LocalIPAddress = IPAddress.Parse(ip);
            Server.Port = port;
            Server.Listen();           
            serverListened = true;
            this.ip = ip;
            this.port = port;
        }
        public void StopListen()
        {
            if (serverListened)
            {
                Server.StopListening();
                serverListened = false;
            }
        }
        public short[] ReadHoldingRegisters(int address,int size)
        {
            short[] registers = new short[size];
            for(int i = address; i < address + size; i++)
            {
                registers[i] = Server.holdingRegisters[i];
            }
            
            return registers;
        }
        public bool[] ReadMultiCoils(int address, int size)
        {
            bool[] registers = new bool[size];
            for(int i = address; i < address + size; i++)
            {
                registers[i] = Server.coils[i];
            }
            return registers;
        }
        private void CoilsChanged(int address, int numberOfRegisters)
        {
            if (serverListened)
            {
                if(address == AddressTrigger)
                {
                    OutTrigger = true;
                    Thread.Sleep(20);
                    OutTrigger = false;
                }
            }
        }
        private void HoldingRegistersChanged(int register, int numberOfRegisters)
        {
            if(serverListened)
            {

            }
        }
    }
    }
