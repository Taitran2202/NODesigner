using DynamicData;
using HalconDotNet;
using Newtonsoft.Json;
using NodeNetwork.Toolkit.ValueNode;
using NumSharp;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using NOVisionDesigner.Designer.Windows;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using static NOVisionDesigner.Designer.Windows.AugmentationEditorWindow;
using Microsoft.ML.OnnxRuntime;
using System.Linq;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using NetMQ.Sockets;
using NetMQ;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "ZeroMQ")]
    public class ZeroMQNode : BaseNode
    {
        
        public override void OnLoadComplete()
        {
            //segmentation.ReloadRecipe();
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);    
        }
        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item,this);
        }
        #region Properties
        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    
                    break;
            }

        }
        //[HMIProperty("Anomaly Editor")]
        //public IReactiveCommand OpenEditor
        //{
        //    get { return ReactiveCommand.Create((Control sender) => OnCommand("editor", sender)); }
        //}
        [HMIProperty]
        public ValueNodeInputViewModel<string> Message { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Server { get; }
        [HMIProperty]
        public ValueNodeOutputViewModel<bool> SendResult { get; }
        #endregion

        static ZeroMQNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<ZeroMQNode>));
        }

        public ZeroMQNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {

            Message = new ValueNodeInputViewModel<string>()
            {
                Name = "Message",
                Editor = new StringValueEditorViewModel(false),
                PortType = "string"
            };
            this.Inputs.Add(Message);
            Server = new ValueNodeInputViewModel<string>()
            {
                Name = "Server",
                Editor = new StringValueEditorViewModel(false),
                PortType = "string"
            };
            this.Inputs.Add(Server);

            SendResult = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Send Result",
                PortType = "boolean",
                Editor = new DefaultOutputValueEditorViewModel<bool>()
               
            };
            this.Outputs.Add(SendResult);

            Server.WhenAnyValue(x => x.Value).Subscribe(x =>
            {
                if (x != null)
                {
                    serverConnected = false;
                }
            });

        }
        bool serverConnected = false;
        PushSocket client;
        void  Connect()
        {
            if (serverConnected)
            {
                return;
            }
            client = new PushSocket();
            client.Connect(Server.Value);
            serverConnected = true;
        }
        public override void OnInitialize()
        {            
        }

        public override void Run(object context)
        {
            Connect();

            if (client != null)
            {
                SendResult.Value.OnNext(client.TrySendFrame(Message.Value));
            }
        }

        
    }
}
