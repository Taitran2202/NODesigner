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
    [NodeInfo("Communication", "TCPClient", Icon: "Designer/icons/icons8-brightness-100.png",sortIndex:5)]
    public class TCPClientNode : BaseNode
    {
        
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static TCPClientNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<TCPClientNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Host { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Port { get; }
        public ValueNodeOutputViewModel<TcpClient> TCPClientOutput { get; }

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
        public TCPClientNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
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

            TCPClientOutput = new ValueNodeOutputViewModel<TcpClient>()
            {
                Name = "TCPClient",
                PortType = "TcpClient",
            };
            this.Outputs.Add(TCPClientOutput);

        }
        bool serverConnected = false;
        TcpClient Client { get; set; }
        void Connect()
        {
            if (serverConnected)
            {
                return;
            }
            Client = new TcpClient(Host.Value,Port.Value);
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
                TCPClientOutput.OnNext(Client);
            }
        }

    }
}
