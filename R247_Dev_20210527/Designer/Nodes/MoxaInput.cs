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
    [NodeInfo("USB", "Moxa Input Module")]
    public class MoxaInput : BaseNode
    {
        static MoxaInput()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<MoxaInput>));
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
                //int ret = MXIO_CS.MXEIO_E1K_Connect(System.Text.Encoding.UTF8.GetBytes(SelectedDevice.IPAddress), Port, Timeout, hConnection, System.Text.Encoding.UTF8.GetBytes(Password));
                //CheckErr(ret, "MXEIO_E1K_Connect");
                //if (ret == MXIO_CS.MXIO_OK)
                //    Console.WriteLine("MXEIO_E1K_Connect Success.");
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
                int PORTS_COUNT = 2;
                for (int i = 0; i < PORTS_COUNT; i++)
                {
                    switch (i)
                    {
                        case 0:
                            ushort[] wSetDI_DIMode = new ushort[8];
                            ret = MXIO_CS.E1K_DI_SetModes(hConnection[0], 0, 8, wSetDI_DIMode);
                            Nodes.MoxaInput.CheckErr(ret, "E1K_DI_SetModes");
                            break;
                        case 1:
                            for(int j=0;j<8; j++)
                            {
                                if (!DIOPortStatus[j])
                                {
                                    ret = MXIO_CS.E1K_DI_SetModes(hConnection[0], (byte)(8 + j), 1, new ushort[] { 0 });
                                    Nodes.MoxaInput.CheckErr(ret, "E1K_DI_SetModes");
                                }
                            }
                            break;
                            
                    }
                }
            }
            
            
        }
        public override void Dispose()
        {
            base.Dispose();
            ret = MXIO_CS.MXEIO_Disconnect(hConnection[0]);
            CheckErr(ret, "MXEIO_Disconnect");
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
                    Windows.MoxaInput wd = new Windows.MoxaInput(this);
                    wd.ShowDialog();
                    break;
            }
        }
        public static object iolock = new object();

        public override void Run(object context)
        {

            Result.OnNext(RunInside(Enable.Value));

        }

        public MoxaInput(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            this.Name = "MOXA Input";
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
                CheckErr(ret, "MXEIO_Init");
                if (ret == MXIO_CS.MXIO_OK)
                    Console.WriteLine("MXEIO_Init Success.");
            }
            catch (Exception ex)
            {
                App.GlobalLogger.LogError("MOXA Input", "Cannot initialize");
            }


        }
        public NetworkInterfaceInfo SelectedInterface { get; set; }
        public BitArray DIOPortStatus;
        public ushort Port { get; set; } = 502;
        public int[] hConnection = new int[1];
        public string Password { get; set; } = "";
        public uint Timeout { get; set; } = 1000;
        int ret;
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
                    if (DIOPortStatus[item.Index])      //check if this bit of port 1 is DO or DI,  
                    {
                        continue;                       //true: DO,false: DI
                    }
                }
                uint[] dwGetDIValue = new uint[1];
                ret = MXIO_CS.E1K_DI_Reads(hConnection[0], (byte)(item.PortIndex * 8 + item.Index), 1, dwGetDIValue);
                CheckErr(ret, "E1K_DI_Reads");
                if ((dwGetDIValue[0] > 0) != item.State)
                {
                    result = false;
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
        public static void CheckErr(int iRet, string szFunctionName)
        {
            string szErrMsg = "MXIO_OK";

            if (iRet != MXIO_CS.MXIO_OK)
            {

                switch (iRet)
                {
                    case MXIO_CS.ILLEGAL_FUNCTION:
                        szErrMsg = "ILLEGAL_FUNCTION";
                        break;
                    case MXIO_CS.ILLEGAL_DATA_ADDRESS:
                        szErrMsg = "ILLEGAL_DATA_ADDRESS";
                        break;
                    case MXIO_CS.ILLEGAL_DATA_VALUE:
                        szErrMsg = "ILLEGAL_DATA_VALUE";
                        break;
                    case MXIO_CS.SLAVE_DEVICE_FAILURE:
                        szErrMsg = "SLAVE_DEVICE_FAILURE";
                        break;
                    case MXIO_CS.SLAVE_DEVICE_BUSY:
                        szErrMsg = "SLAVE_DEVICE_BUSY";
                        break;
                    case MXIO_CS.EIO_TIME_OUT:
                        szErrMsg = "EIO_TIME_OUT";
                        break;
                    case MXIO_CS.EIO_INIT_SOCKETS_FAIL:
                        szErrMsg = "EIO_INIT_SOCKETS_FAIL";
                        break;
                    case MXIO_CS.EIO_CREATING_SOCKET_ERROR:
                        szErrMsg = "EIO_CREATING_SOCKET_ERROR";
                        break;
                    case MXIO_CS.EIO_RESPONSE_BAD:
                        szErrMsg = "EIO_RESPONSE_BAD";
                        break;
                    case MXIO_CS.EIO_SOCKET_DISCONNECT:
                        szErrMsg = "EIO_SOCKET_DISCONNECT";
                        break;
                    case MXIO_CS.PROTOCOL_TYPE_ERROR:
                        szErrMsg = "PROTOCOL_TYPE_ERROR";
                        break;
                    case MXIO_CS.SIO_OPEN_FAIL:
                        szErrMsg = "SIO_OPEN_FAIL";
                        break;
                    case MXIO_CS.SIO_TIME_OUT:
                        szErrMsg = "SIO_TIME_OUT";
                        break;
                    case MXIO_CS.SIO_CLOSE_FAIL:
                        szErrMsg = "SIO_CLOSE_FAIL";
                        break;
                    case MXIO_CS.SIO_PURGE_COMM_FAIL:
                        szErrMsg = "SIO_PURGE_COMM_FAIL";
                        break;
                    case MXIO_CS.SIO_FLUSH_FILE_BUFFERS_FAIL:
                        szErrMsg = "SIO_FLUSH_FILE_BUFFERS_FAIL";
                        break;
                    case MXIO_CS.SIO_GET_COMM_STATE_FAIL:
                        szErrMsg = "SIO_GET_COMM_STATE_FAIL";
                        break;
                    case MXIO_CS.SIO_SET_COMM_STATE_FAIL:
                        szErrMsg = "SIO_SET_COMM_STATE_FAIL";
                        break;
                    case MXIO_CS.SIO_SETUP_COMM_FAIL:
                        szErrMsg = "SIO_SETUP_COMM_FAIL";
                        break;
                    case MXIO_CS.SIO_SET_COMM_TIME_OUT_FAIL:
                        szErrMsg = "SIO_SET_COMM_TIME_OUT_FAIL";
                        break;
                    case MXIO_CS.SIO_CLEAR_COMM_FAIL:
                        szErrMsg = "SIO_CLEAR_COMM_FAIL";
                        break;
                    case MXIO_CS.SIO_RESPONSE_BAD:
                        szErrMsg = "SIO_RESPONSE_BAD";
                        break;
                    case MXIO_CS.SIO_TRANSMISSION_MODE_ERROR:
                        szErrMsg = "SIO_TRANSMISSION_MODE_ERROR";
                        break;
                    case MXIO_CS.PRODUCT_NOT_SUPPORT:
                        szErrMsg = "PRODUCT_NOT_SUPPORT";
                        break;
                    case MXIO_CS.HANDLE_ERROR:
                        szErrMsg = "HANDLE_ERROR";
                        break;
                    case MXIO_CS.SLOT_OUT_OF_RANGE:
                        szErrMsg = "SLOT_OUT_OF_RANGE";
                        break;
                    case MXIO_CS.CHANNEL_OUT_OF_RANGE:
                        szErrMsg = "CHANNEL_OUT_OF_RANGE";
                        break;
                    case MXIO_CS.COIL_TYPE_ERROR:
                        szErrMsg = "COIL_TYPE_ERROR";
                        break;
                    case MXIO_CS.REGISTER_TYPE_ERROR:
                        szErrMsg = "REGISTER_TYPE_ERROR";
                        break;
                    case MXIO_CS.FUNCTION_NOT_SUPPORT:
                        szErrMsg = "FUNCTION_NOT_SUPPORT";
                        break;
                    case MXIO_CS.OUTPUT_VALUE_OUT_OF_RANGE:
                        szErrMsg = "OUTPUT_VALUE_OUT_OF_RANGE";
                        break;
                    case MXIO_CS.INPUT_VALUE_OUT_OF_RANGE:
                        szErrMsg = "INPUT_VALUE_OUT_OF_RANGE";
                        break;
                }

                Console.WriteLine("Function \"{0}\" execution Fail. Error Message : {1}\n", szFunctionName, szErrMsg);

                if (iRet == MXIO_CS.EIO_TIME_OUT || iRet == MXIO_CS.HANDLE_ERROR)
                {
                    ////To terminates use of the socket
                    //MXIO_CS.MXEIO_Exit();
                    //Console.WriteLine("Press any key to close application\r\n");
                    //Console.ReadLine();
                    //Environment.Exit(1);
                }
            }
        }
    }
    public class E1212Device: IHalconDeserializable
    {
        public void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }
        public void Load(DeserializeFactory item)
        {

            HelperMethods.LoadParam(item, this);
        }
        public string ID { get; set; }
        public string IPAddress { get; set; } = "192.168.127.254";
        public string MACAddress { get; set; }
        public override string ToString()
        {
            return ID+'@'+IPAddress;
        }
    }
}
