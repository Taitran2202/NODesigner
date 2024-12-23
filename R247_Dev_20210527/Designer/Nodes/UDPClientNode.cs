using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using NOVisionDesigner.ViewModel;
using ReactiveUI;
using System;
using System.Net.Sockets;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "UDPClient", Icon: "Designer/icons/icons8-brightness-100.png",sortIndex:5)]
    public class UDPClientNode : BaseNode
    {
        
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static UDPClientNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<UDPClientNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Host { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Port { get; }
        public ValueNodeOutputViewModel<UdpClient> UDPClientOutput { get; }

        [HMIProperty("Default Editor")]
        public IReactiveCommand OpenEditor
        {
            get 
            {
                return ReactiveCommand.Create((Control sender) => OnCommand("editor",sender));
            }
        }

        
        
        public override void OnCommand(string type,Control sender)
        {
            switch (type)
            {
                case "editor":
                    //Default editor
                    break;
            }
        }
        public UDPClientNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "TCPClient";
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

            UDPClientOutput = new ValueNodeOutputViewModel<UdpClient>()
            {
                Name = "UDPClient",
                PortType = "UDPClient",
            };
            this.Outputs.Add(UDPClientOutput);

        }
        bool serverConnected = false;
        UdpClient Client { get; set; }
        void Connect()
        {
            if (serverConnected)
            {
                return;
            }
            Client = new UdpClient(Host.Value,Port.Value);
            serverConnected = true;
        }
        public override void OnInitialize()
        {
        }

        public override void Run(object context)
        {
            Connect();
            if (Client != null)
            {
                UDPClientOutput.OnNext(Client);
            }
        }

    }
}
