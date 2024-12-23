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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "HostLinkUDP",sortIndex:5)]
    public class HostLinkUDPNode : BaseNode
    {
        public override void Dispose()
        {
            if (HostLinkUDPClient != null)
            {
                HostLinkUDPClient.Dispose();
            }
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static HostLinkUDPNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<HostLinkUDPNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Host { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Port { get; }
        public ValueNodeOutputViewModel<HostLinkUDP> HostLinkUDPOutput { get; }

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
        public HostLinkUDPNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "HostLink UDP";
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

            HostLinkUDPOutput = new ValueNodeOutputViewModel<HostLinkUDP>()
            {
                Name = "HostLinkUDP",
                PortType = "HostLinkUDP",
            };
            this.Outputs.Add(HostLinkUDPOutput);
            this.WhenAnyValue(x => x.Host).Subscribe(x =>
            {
                if (HostLinkUDPClient != null)
                {
                    HostLinkUDPClient.Connected = false;
                }
            });
            this.WhenAnyValue(x => x.Port).Subscribe(x =>
            {
                if (HostLinkUDPClient != null)
                {
                    HostLinkUDPClient.Connected = false;
                }
            });

            EnabledInput.ValueChanged.Subscribe(x =>
            {
                if (this.NodeType == NodeType.Event)
                {
                    this.Run(null);
                }
            });
        }
        
        public override void OnInitialize()
        {
        }

        public override void Run(object context)
        {
            if (HostLinkUDPClient == null)
            {
                HostLinkUDPClient = new HostLinkUDP();
            }
            HostLinkUDPClient?.Connect(Host.Value,Port.Value);
            if (HostLinkUDPClient != null)
            {
                HostLinkUDPOutput.OnNext(HostLinkUDPClient);
            }
            else
            {
                HostLinkUDPOutput.OnNext(null);
            }
        }
        public HostLinkUDP HostLinkUDPClient { get; set; } 
    }
    public class HostLinkUDP
    {
        public void Dispose()
        {
            if (Client != null)
            {
                try
                {
                    Client.Dispose();
                }catch(Exception ex)
                {

                }
                
            }
        }
        public UdpClient Client { get; set; }
        public bool Connected { get; set; } = false;
        public HostLinkUDP()
        {
            
        }
        IPEndPoint EndPoint;
        String host;
        int port;
        public void Connect(string host,int port)
        {
            if(host == null)
            {
                return;
            }
            if(this.host==host & this.port == port)
            {
                if (Connected)
                {
                    return;
                }
            }
            try
            {
                Client = new UdpClient(host, port);
                EndPoint = new IPEndPoint(IPAddress.Any, port);
                Connected = true;
                this.host = host;
                this.port = port;
            }
            catch(Exception ex)
            {

            }
            
            
            
        }
        static byte[] EndByte = new byte[] { 0x0D, 0x0A };
        static byte SpaceByte = 0x20;
        static byte[] OKResult = new byte[] { 0x4F, 0x4B, 0x0D, 0x0A };
        public string ReadString(string Address,int MaxLength,int timeout=100)
        {

            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RDS {0}.H {1}\r", Address, MaxLength));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                var result2 = result.Split(' ').Select(x => Convert.ToChar((byte)int.Parse(x, System.Globalization.NumberStyles.HexNumber)));
                return String.Join("", result2);
            }
            else
            {
                return "";
            }
                
            
        }
        /// <summary>
        /// write 32bit signed integer
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool WriteInt(string Address, int Value,int timeout=100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("WR {0}.L {1}\r", Address, Value.ToString()));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "OK")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
                
            
        }
        /// <summary>
        /// write 32bit unsigned integer
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool WriteUInt(string Address, uint Value, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("WR {0}.D {1}\r", Address, Value.ToString()));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "OK")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


        }
        /// <summary>
        /// write 16bit signed integer
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool WriteShortValue(string Address, short Value, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("WR {0}.S {1}\r", Address, Value.ToString()));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "OK")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


        }
        /// <summary>
        /// Write 16 bit unsigned integer
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="Value"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool WriteUShortValue(string Address, ushort Value, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("WR {0}.U {1}\r", Address, Value.ToString()));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "OK")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


        }
        public bool WriteBit(string Address, bool Value,int timeout=100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("WR {0} {1}\r", Address, Value?"1":"0"));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "OK")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            
            
        }
        public bool ReadBit(string Address,int timeout=100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RD {0}\r", Address));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            if(waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "E1")
                {
                    return false;
                }
                if (int.TryParse(result, out int result2))
                {
                    if (result2 == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            
           // dataR.Remove(EndByte);
            
            
        }
        public int ReadInt(string Address,int timeout=100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RD {0}.L\r", Address));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            // dataR.Remove(EndByte);
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "E1")
                {
                    return 0;
                }
                if (int.TryParse(result, out int result2))
                {
                    return result2;

                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
            

        }
        public uint ReadUInt(string Address, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RD {0}.D\r", Address));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            // dataR.Remove(EndByte);
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "E1")
                {
                    return 0;
                }
                if (uint.TryParse(result, out uint result2))
                {
                    return result2;

                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }


        }
        public short ReadShort(string Address, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RD {0}.S\r", Address));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            // dataR.Remove(EndByte);
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "E1")
                {
                    return 0;
                }
                if (short.TryParse(result, out short result2))
                {
                    return result2;

                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }


        }
        public ushort ReadUShort(string Address, int timeout = 100)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(string.Format("RD {0}.U\r", Address));
            Client.Send(data, data.Length);
            //Thread.Sleep(200);
            var waitThread = Client.ReceiveAsync();
            // dataR.Remove(EndByte);
            if (waitThread.Wait(timeout))
            {
                var dataR = waitThread.Result.Buffer;
                var result = System.Text.Encoding.ASCII.GetString(dataR).Replace("\r\n", "");
                if (result == "E1")
                {
                    return 0;
                }
                if (ushort.TryParse(result, out ushort result2))
                {
                    return result2;

                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }


        }
    }
}
