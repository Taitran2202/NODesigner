using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.ComponentModel;
using NOVisionDesigner.Designer.Misc;
using DevExpress.Xpf.Charts;
using System.Collections.ObjectModel;
using NOVisionDesigner.Designer.Windows;
using Automation.BDaq;
using NOVisionDesigner.Designer.Editors;
using System.Threading;
using System.Windows.Controls;
using System.Collections;
using NOVisionDesigner.Designer.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("USB", "Moxa Output Module")]
    public class MoxaOutput : BaseNode
    {
        static MoxaOutput()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<MoxaOutput>));
        }
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {

            HelperMethods.LoadParam(item, this);
        }
        public override void OnLoadComplete()
        {
            try
            {
                InitDevice();
                    
                    
            }
            catch (Exception ex)
            {

            }
        }
        public void InitDevice()
        {
            int ret = MXIO_CS.MXEIO_E1K_Connect(System.Text.Encoding.UTF8.GetBytes(SelectedDevice.IPAddress), Port, Timeout, hConnection, System.Text.Encoding.UTF8.GetBytes(Password));
            Nodes.MoxaInput.CheckErr(ret, "MXEIO_E1K_Connect");
            if (ret == MXIO_CS.MXIO_OK)
            {
                Console.WriteLine("MXEIO_E1K_Connect Success.");
                uint[] dwMode = new uint[1];
                ret = MXIO_CS.E1K_DIO_GetIOModes(hConnection[0], 0, 8, dwMode);
                Nodes.MoxaInput.CheckErr(ret, "E1K_DIO_GetIOModes");
                DIOPortStatus = new BitArray(new byte[] { (byte)dwMode[0] }); //DIO_port_status: false: DI, true: DO
                for (int i = 0; i < 8; i++)
                {
                    if (DIOPortStatus[i])
                    {
                        ret = MXIO_CS.E1K_DO_SetModes(hConnection[0], 0, 1, new ushort[1] { 0 });
                        Nodes.MoxaInput.CheckErr(ret, "E1K_DO_SetModes");
                    }
                }
            }
            
        }
        public override void Dispose()
        {
            base.Dispose();
            ret = MXIO_CS.MXEIO_Disconnect(hConnection[0]);
            Nodes.MoxaInput.CheckErr(ret, "MXEIO_Disconnect");
            MXIO_CS.MXEIO_Exit();
        }
        public ValueNodeOutputViewModel<bool> Result { get; set; }
        public ValueNodeInputViewModel<bool> Enable { get; set; }
        //public ValueNodeInputViewModel<bool> ResultInput { get; set; }

        public ObservableCollection<BitValue> PortStates { get; set; } = new ObservableCollection<BitValue>();
        public E1212Device SelectedDevice { get; set; } = new E1212Device();


        public override void OnCommand(string type, Control sender)
        {
            switch (type)
            {
                case "editor":
                    Windows.MoxaOutput wd = new Windows.MoxaOutput(this);
                    wd.ShowDialog();
                    break;
            }
        }
        public static object iolock = new object();

        public override void Run(object context)
        {

            Result.OnNext(RunInside(Enable.Value));

        }

        public MoxaOutput(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "MOXA Output";
            Enable = new ValueNodeInputViewModel<bool>()
            {
                Name = "Enable",
                Editor = new BoolValueEditorViewModel(),
                PortType = "Boolean"
            };
            this.Inputs.Add(Enable);
            Enable.WhenAnyValue(x => x.Value).Subscribe(x =>
            {

                RunInside(x);
            });

            Result = new ValueNodeOutputViewModel<bool>()
            {
                Name = "Output",
                PortType = "Boolean"
            };
            this.Outputs.Add(Result);

            try
            {
                ret = MXIO_CS.MXEIO_Init();
                Nodes.MoxaInput.CheckErr(ret, "MXEIO_Init");
                if (ret == MXIO_CS.MXIO_OK)
                    Console.WriteLine("MXEIO_Init Success.");
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("MOXA Output", "Cannot initialize");
            }


        }

        public BitArray DIOPortStatus;
        public ushort Port { get; set; } = 502;
        public int[] hConnection = new int[1];
        public string Password { get; set; } = "";
        public uint Timeout { get; set; } = 1000;
        int ret;
        public NetworkInterfaceInfo SelectedInterface { get; set; }
        public bool RunInside(bool input)
        {
            if (!CheckAndReconnect())
            {
                return false;
            }
            if (PortStates.Count(x => x.Activation == input) == 0)
            {
                return false;
            }
            bool result = true;
            foreach (var item in PortStates)
            {
                if (item.Activation != input)
                {
                    continue;
                }
                if (item.PortIndex == 1)                //port 1 is DIO port
                {
                    if (!DIOPortStatus[item.Index])      //check if this bit of port 1 is DO or DI,  
                    {
                        continue;                       //true: DO,false: DI
                    }
                }
                if (item.Duration > 0)
                {
                    lock (iolock)
                    {
                        ret = MXIO_CS.E1K_DO_Writes(hConnection[0], (byte)(item.PortIndex * 8 + item.Index), 1, (byte)(item.State ? 1 : 0));
                        Nodes.MoxaInput.CheckErr(ret, "E1K_DO_Writes");
                    }

                    Task.Run(new Action(() =>
                    {
                        Thread.Sleep(Math.Max(20, item.Duration));
                        lock (iolock)
                        {
                            ret = MXIO_CS.E1K_DO_Writes(hConnection[0], (byte)(item.PortIndex * 8 + item.Index), 1, (byte)(!item.State ? 1 : 0));
                            Nodes.MoxaInput.CheckErr(ret, "E1K_DO_Writes");
                        }

                    }));
                }
                else
                {
                    lock (iolock)
                    {
                        ret = MXIO_CS.E1K_DO_Writes(hConnection[0], (byte)(item.PortIndex * 8 + item.Index), 1, (byte)(item.State ? 1 : 0));
                        Nodes.MoxaInput.CheckErr(ret, "E1K_DO_Writes");
                    }

                }
            }
            return result;
        }
        public bool CheckAndReconnect()
        {
            byte[] bytStatus = new byte[1];
            if (MXIO_CS.MXEIO_CheckConnection(hConnection[0], 100, bytStatus) == MXIO_CS.MXIO_OK)
            {

                if (bytStatus[0] != MXIO_CS.CHECK_CONNECTION_OK)
                {
                    ret = MXIO_CS.MXEIO_E1K_Connect(System.Text.Encoding.UTF8.GetBytes(SelectedDevice.IPAddress), Port, Timeout, hConnection, System.Text.Encoding.UTF8.GetBytes(Password));
                    Nodes.MoxaInput.CheckErr(ret, "MXEIO_E1K_Connect");
                    if (ret == MXIO_CS.MXIO_OK)
                    {
                        Console.WriteLine("MXEIO_E1K_Connect Success.");
                        return true;
                    }
                    return false;    
                }
                return true;
            }
            return false;
        }
    }

}
