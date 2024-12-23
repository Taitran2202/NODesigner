using ControlzEx.Standard;
using DynamicData;
using HalconDotNet;
using NodeNetwork.Toolkit.ValueNode;
using NOVisionDesigner.Designer.Editors;
using NOVisionDesigner.Designer.Misc;
using NOVisionDesigner.Designer.NodeViews;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NOVisionDesigner.Designer.Nodes
{
    [NodeInfo("Communication", "SLMP Client", sortIndex: 5)]
    public class SLMPNode : BaseNode
    {
        public override void Save(HFile file)
        {
            HelperMethods.SaveParam(file, this);
        }

        public override void Load(DeserializeFactory item)
        {
            HelperMethods.LoadParam(item, this);
        }
        static SLMPNode()
        {
            Splat.Locator.CurrentMutable.Register(() => new NodeViewWithEditor(), typeof(IViewFor<SLMPNode>));
        }

        public ValueNodeInputViewModel<bool> EnabledInput { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<string> Host { get; }
        [HMIProperty]
        public ValueNodeInputViewModel<int> Port { get; }
        public ValueNodeOutputViewModel<SLMP> SLMPOutput { get; }

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
        public SLMPNode(DesignerHost designer, string dir, string id) : base(designer, dir, id)
        {
            Name = "SLMP";
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

            SLMPOutput = new ValueNodeOutputViewModel<SLMP>()
            {
                Name = "SLMP",
                PortType = "SLMP",
            };
            this.Outputs.Add(SLMPOutput);
            this.WhenAnyValue(x => x.Host).Subscribe(x =>
            {
                if (_slmp != null)
                {
                    _slmp.serverConnected = false;
                }
            });
            this.WhenAnyValue(x => x.Port).Subscribe(x =>
            {
                if (_slmp != null)
                {
                    _slmp.serverConnected = false;
                }
            });

        }

        public override void OnInitialize()
        {
        }

        public override void Run(object context)
        {
            if (_slmp == null)
            {
                _slmp = new SLMP();
            }
            if (_slmp.serverConnected == false)
            {
                _slmp?.Connect(Host.Value, Port.Value);
            }
            if (_slmp != null)
            {
                SLMPOutput.OnNext(_slmp);
            }
            else
            {
                SLMPOutput.OnNext(null);
            }
        }
        public SLMP _slmp { get; set; }
    }

    public class SLMP
    {
        string host;
        int port;
        SlmpClient plc;
        public bool serverConnected { get; set; } = false;
        public SLMP()
        {

        }
        public void Connect(string host, int port)
        {
            if (host == null)
            {
                return;
            }
            if (this.host == host & this.port == port)
            {
                if (serverConnected)
                {
                    return;
                }

            }
            SlmpConfig cfg = new SlmpConfig(host, port)
            {
                ConnTimeout = 1000,
                RecvTimeout = 1000,
                SendTimeout = 1000,
            };
            plc = new SlmpClient(cfg);
            plc.Connect();
            serverConnected = true;
            this.host = host;
            this.port = port;
        }
        /// <summary>
        /// Reads a single Bit from a given `BitDevice` and returns a `bool`.
        /// </summary>
        /// <param name="addr">The device address as a string.</param>
        public bool ReadBitDevice(string addr)
        {
            return plc.ReadBitDevice(addr);
        }
        /// <summary>
        /// Reads a single Bit from a given `BitDevice` and returns a `bool`.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Bit address.</param>
        public bool ReadBitDevice(Device device, ushort addr)
        {
            return plc.ReadBitDevice(device, addr);
        }
        /// <summary>
        /// Reads from a given `BitDevice` and returns an array of `bool`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        /// <returns></returns>
        public bool[] ReadBitDevice(string addr, ushort count)
        {
            return plc.ReadBitDevice(addr, count);
        }
        /// <summary>
        /// Reads from a given `BitDevice` and returns an array of `bool`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The bit device.</param>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        public bool[] ReadBitDevice(Device device, ushort addr, ushort count)
        {
            return plc.ReadBitDevice(device, addr, count);
        }
        /// <summary>
        /// Reads a single Word from a the given `WordDevice` and returns an `ushort`.
        /// </summary>
        /// <param name="addr">The device address as a string.</param>
        public ushort ReadWordDevice(string addr)
        {
            return plc.ReadWordDevice(addr);
        }
        /// <summary>
        /// Reads a single Word from a the given `WordDevice` and returns an `ushort`.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Word address.</param>
        public ushort ReadWordDevice(Device device, ushort addr)
        {
            return plc.ReadWordDevice(device, addr);
        }
        /// <summary>
        /// Reads from a given `WordDevice` and returns an array of `ushort`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Start address as a string.</param>
        /// <param name="count">Number of registers to read.</param>
        public ushort[] ReadWordDevice(string addr, ushort count)
        {
            return plc.ReadWordDevice(addr, count);
        }
        /// <summary>
        /// Reads from a given `WordDevice` and returns an array of `ushort`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        public ushort[] ReadWordDevice(Device device, ushort addr, ushort count)
        {
            return plc.ReadWordDevice((Device)device, addr, count);
        }

        /// <summary>
        /// Reads a string with the length `len` from the specified `WordDevice`. Note that
        /// this function reads the string at best two chars, ~500 times in a second.
        /// Meaning it can only read ~1000 chars per second.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Starting address of the null terminated string as a string.</param>
        /// <param name="len">Length of the string.</param>
        string ReadString(string addr, ushort len)
        {
            return plc.ReadString(addr, len);
        }
        /// <summary>
        /// Reads a string with the length `len` from the specified `WordDevice`. Note that
        /// this function reads the string at best two chars, ~500 times in a second.
        /// Meaning it can only read ~1000 chars per second.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="addr">Starting address of the null terminated string.</param>
        /// <param name="len">Length of the string.</param>
        public string ReadString(Device device, ushort addr, ushort len)
        {
            return plc.ReadString((Device)device, addr, len);
        }
        /// <summary>
        /// Writes a single `Bit` to a given `BitDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteBitDevice(string addr, bool data)
        {
            plc.WriteBitDevice(addr, data);
        }
        /// <summary>
        /// Writes an array of `bool`s to a given `BitDevice`.
        /// note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="data">data to be written into the remote device.</param>
        public void WriteBitDevice(string addr, bool[] data)
        {
            plc.WriteBitDevice(addr, data);
        }
        /// <summary>
        /// Writes a single `Bit` to a given `BitDevice`.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteBitDevice(Device device, ushort addr, bool data)
        {
            plc.WriteBitDevice((Device)device, addr, data);
        }
        /// <summary>
        /// writes an array of `bool`s to a given `bitdevice`.
        /// note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">the bitdevice to write.</param>
        /// <param name="addr">starting address.</param>
        /// <param name="data">data to be written into the remote device.</param>
        public void WriteBitDevice(Device device, ushort addr, bool[] data)
        {
            plc.WriteBitDevice(device, addr, data);
        }
        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(string addr, ushort data)
        {
            plc.WriteWordDevice(addr, data);
        }
        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(string addr, ushort[] data)
        {
            plc.WriteWordDevice(addr, (ushort)data.Length);
        }
        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(Device device, ushort addr, ushort data)
        {
            plc.WriteWordDevice(device, addr, data);
        }
        /// <summary>
        /// Writes an array of `ushort`s to a given `WordDevice`.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(Device device, ushort addr, ushort[] data)
        {
            plc.WriteWordDevice((Device)device, addr, data);
        }
        /// <summary>
        /// Writes the given string to the specified device as a null terminated string.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="text">The string to write.</param>
        public void WriteString(string addr, string text)
        {
            plc.WriteString(addr, text);
        }
        /// <summary>
        /// Writes the given string to the specified device as a null terminated string.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="text">The string to write.</param>
        public void WriteString(Device device, ushort addr, string text)
        {
            plc.WriteString((Device)device, addr, text);
        }
    }

    public partial class SlmpClient
    {
        /// <summary>
        /// Writes a single `Bit` to a given `BitDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteBitDevice(string addr, bool data)
        {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteBitDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes an array of `bool`s to a given `BitDevice`.
        /// note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="data">data to be written into the remote device.</param>
        public void WriteBitDevice(string addr, bool[] data)
        {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteBitDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes a single `Bit` to a given `BitDevice`.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteBitDevice(Device device, ushort addr, bool data)
        {
            WriteBitDevice(device, addr, new bool[] { data });
        }

        /// <summary>
        /// writes an array of `bool`s to a given `bitdevice`.
        /// note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">the bitdevice to write.</param>
        /// <param name="addr">starting address.</param>
        /// <param name="data">data to be written into the remote device.</param>
        public void WriteBitDevice(Device device, ushort addr, bool[] data)
        {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Bit)
                throw new ArgumentException("provided device is not a bit device");

            ushort count = (ushort)data.Length;
            List<bool> listData = data.ToList();
            List<byte> encodedData = new List<byte>();

            // If the length of `data` isn't even, add a dummy
            // `false` to make the encoding easier. It gets ignored on the station side.
            if (count % 2 != 0)
                listData.Add(false);

            for (int i = 0; i < listData.Count; i += 2)
            {
                bool[] chunk = new bool[2];
                chunk[0] = listData[i];
                chunk[1] = (i + 1 < listData.Count) ? listData[i + 1] : false;
                byte value = (byte)(Convert.ToByte(chunk[0]) << 4 | Convert.ToByte(chunk[1]));
                encodedData.Add(value);
            }

            SendWriteDeviceCommand(device, addr, count, encodedData.ToArray());
            ReceiveResponse();
        }

        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="addr">Device address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(string addr, ushort data)
        {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteWordDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes an array of `ushort`s to a given `WordDevice`.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(string addr, ushort[] data)
        {
            Tuple<Device, ushort> tdata = DeviceMethods.ParseDeviceAddress(addr);
            WriteWordDevice(tdata.Item1, tdata.Item2, data);
        }

        /// <summary>
        /// Writes a single `ushort` to a given `WordDevice`.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(Device device, ushort addr, ushort data)
        {
            WriteWordDevice(device, addr, new ushort[] { data });
        }

        /// <summary>
        /// Writes an array of `ushort`s to a given `WordDevice`.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">The WordDevice to write.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="data">Data to be written into the remote device.</param>
        public void WriteWordDevice(Device device, ushort addr, ushort[] data)
        {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Word)
                throw new ArgumentException("provided device is not a word device");

            ushort count = (ushort)data.Length;
            List<byte> encodedData = new List<byte>();

            foreach (ushort word in data)
            {
                encodedData.Add((byte)(word & 0xff));
                encodedData.Add((byte)(word >> 0x8));
            }

            SendWriteDeviceCommand(device, addr, count, encodedData.ToArray());
            ReceiveResponse();
        }

        /// <summary>
        /// Writes the given string to the specified device as a null terminated string.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="addr">Starting address in string format.</param>
        /// <param name="text">The string to write.</param>
        public void WriteString(string addr, string text)
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            WriteString(data.Item1, data.Item2, text);
        }

        /// <summary>
        /// Writes the given string to the specified device as a null terminated string.
        /// Note that there's a limit on how many registers can be written at a time.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="addr">Starting address.</param>
        /// <param name="text">The string to write.</param>
        public void WriteString(Device device, ushort addr, string text)
        {
            // add proper padding to the string
            text += new string('\0', 2 - (text.Length % 2));
            List<ushort> result = new List<ushort>();

            byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(text.ToCharArray());
            for (int i = 0; i < asciiBytes.Length; i += 2)
            {
                byte[] chunk = new byte[2];
                chunk[0] = asciiBytes[i];
                chunk[1] = (i + 1 < asciiBytes.Length) ? asciiBytes[i + 1] : (byte)0;
                ushort value = (ushort)(chunk[1] << 8 | chunk[0]);
                result.Add(value);
            }

            WriteWordDevice(device, addr, result.ToArray());
        }
    }
    public partial class SlmpClient
    {
        /// <summary>
        /// Reads a single Bit from a given `BitDevice` and returns a `bool`.
        /// </summary>
        /// <param name="addr">The device address as a string.</param>
        public bool ReadBitDevice(string addr)
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadBitDevice(data.Item1, data.Item2);
        }

        /// <summary>
        /// Reads from a given `BitDevice` and returns an array of `bool`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        /// <returns></returns>
        public bool[] ReadBitDevice(string addr, ushort count)
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadBitDevice(data.Item1, data.Item2, count);
        }

        /// <summary>
        /// Reads a single Bit from a given `BitDevice` and returns a `bool`.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Bit address.</param>
        public bool ReadBitDevice(Device device, ushort addr)
        {
            return ReadBitDevice(device, addr, 1)[0];
        }

        /// <summary>
        /// Reads from a given `BitDevice` and returns an array of `bool`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The bit device.</param>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        public bool[] ReadBitDevice(Device device, ushort addr, ushort count)
        {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Bit)
                throw new ArgumentException("provided device is not a bit device");

            SendReadDeviceCommand(device, addr, count);
            List<byte> response = ReceiveResponse();
            List<bool> result = new List<bool>();

            response.ForEach(delegate (byte a) {
                result.Add((a & 0x10) != 0);
                result.Add((a & 0x01) != 0);
            });

            return result.GetRange(0, count).ToArray();
        }

        /// <summary>
        /// Reads a single Word from a the given `WordDevice` and returns an `ushort`.
        /// </summary>
        /// <param name="addr">The device address as a string.</param>
        public ushort ReadWordDevice(string addr)
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadWordDevice(data.Item1, data.Item2);
        }

        /// <summary>
        /// Reads from a given `WordDevice` and returns an array of `ushort`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Start address as a string.</param>
        /// <param name="count">Number of registers to read.</param>
        public ushort[] ReadWordDevice(string addr, ushort count)
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadWordDevice(data.Item1, data.Item2, count);
        }

        /// <summary>
        /// Reads a single Word from a the given `WordDevice` and returns an `ushort`.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Word address.</param>
        public ushort ReadWordDevice(Device device, ushort addr)
        {
            return ReadWordDevice(device, addr, 1)[0];
        }

        /// <summary>
        /// Reads from a given `WordDevice` and returns an array of `ushort`s.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The word device.</param>
        /// <param name="addr">Start address.</param>
        /// <param name="count">Number of registers to read.</param>
        public ushort[] ReadWordDevice(Device device, ushort addr, ushort count)
        {
            if (DeviceMethods.GetDeviceType(device) != DeviceType.Word)
                throw new ArgumentException("provided device is not a word device");

            SendReadDeviceCommand(device, addr, count);
            List<byte> response = ReceiveResponse();
            List<ushort> result = new List<ushort>();

            // if the length of the response isn't even
            // then the response is invalid and we can't
            // construct an array of `ushort`s from it
            if (response.Count % 2 != 0)
                throw new InvalidDataException("While reading words: data section of the response is uneven");

            // word data is received in little endian format
            // which means the lower byte of a word comes first
            // and upper byte second
            
            for (int i = 0; i < response.Count; i += 2)
            {
                byte[] chunk = response.GetRange(i, Math.Min(2, response.Count - i)).ToArray();
                ushort value = (ushort)(chunk[1] << 8 | chunk[0]);
                result.Add(value);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Reads a string with the length `len` from the specified `WordDevice`. Note that
        /// this function reads the string at best two chars, ~500 times in a second.
        /// Meaning it can only read ~1000 chars per second.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="addr">Starting address of the null terminated string as a string.</param>
        /// <param name="len">Length of the string.</param>
        public string ReadString(string addr, ushort len)
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadString(data.Item1, data.Item2, len);
        }

        /// <summary>
        /// Reads a string with the length `len` from the specified `WordDevice`. Note that
        /// this function reads the string at best two chars, ~500 times in a second.
        /// Meaning it can only read ~1000 chars per second.
        /// Note that there's a limit on how many registers can be read at a time.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="addr">Starting address of the null terminated string.</param>
        /// <param name="len">Length of the string.</param>
        public string ReadString(Device device, ushort addr, ushort len)
        {
            ushort wordCount = (ushort)((len % 2 == 0 ? len : len + 1) / 2);
            List<char> buffer = new List<char>();

            foreach (ushort word in ReadWordDevice(device, addr, wordCount))
            {
                buffer.Add((char)(word & 0xff));
                buffer.Add((char)(word >> 0x8));
            }

            return string.Join("", buffer.GetRange(0, len));
        }

        /// <summary>
        /// Read from a `WordDevice` to create a C# structure.
        /// The target structure can only contain very primitive data types.
        /// </summary>
        /// <typeparam name="T">The `Struct` to read.</typeparam>
        /// <param name="addr">Starting address of the structure data in the string format.</param>
        public T? ReadStruct<T>(string addr) where T : struct
        {
            Tuple<Device, ushort> data = DeviceMethods.ParseDeviceAddress(addr);
            return ReadStruct<T>(data.Item1, data.Item2);
        }

        /// <summary>
        /// Read from a `WordDevice` to create a C# structure.
        /// The target structure can only contain very primitive data types.
        /// </summary>
        /// <typeparam name="T">The `Struct` to read.</typeparam>
        /// <param name="device">The device to read from..</param>
        /// <param name="addr">Starting address of the structure data.</param>
        public T? ReadStruct<T>(Device device, ushort addr) where T : struct
        {
            Type structType = typeof(T);
            ushort[] words = ReadWordDevice(
                device, addr, (ushort)SlmpStruct.GetStructSize(structType));

            return SlmpStruct.FromWords(structType, words) as T?;
        }
    }
    public partial class SlmpClient
    {
        /// <summary>
        /// This `HEADER` array contains the shared (header) data between
        /// commands that are supported in this library.
        /// </summary>
        private readonly byte[] HEADER = {
            0x50, 0x00,     // subheader: no serial no.
            0x00,           // request destination network no.
            0xff,           // request destination station no.
            0xff, 0x03,     // request destination module I/O no.: 0x03ff (own station)
            0x00,           // request destination multidrop station no.
        };

        private SlmpConfig _config;
        private TcpClient _client;
        private NetworkStream _stream;

        /// <summary>Initializes a new instance of the <see cref="SlmpClient" /> class.</summary>
        /// <param name="cfg">The config.</param>
        public SlmpClient(SlmpConfig cfg)
        {
            _config = cfg;
            _client = new TcpClient();
        }

        /// <summary>Connects to the address specified in the config.</summary>
        /// <exception cref="System.TimeoutException">connection timed out</exception>
        public void Connect()
        {
            _client = new TcpClient();

            if (!_client.ConnectAsync(_config.Address, _config.Port).Wait(_config.ConnTimeout))
                throw new TimeoutException("connection timed out");

            // connection is successful
            _client.SendTimeout = _config.SendTimeout;
            _client.ReceiveTimeout = _config.RecvTimeout;

            _stream = _client.GetStream();
        }

        /// <summary>
        /// Attempt to close the socket connection.
        /// </summary>
        public void Disconnect()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
            if (_client.Connected)
                _client.Client.Shutdown(SocketShutdown.Both);
            _client.Close();
        }

        private bool InternalIsConnected()
        {
            return _stream != null && _client.Connected;
        }

        /// <summary>
        /// Query the connection status.
        /// </summary>
        public bool IsConnected()
        {
            return InternalIsConnected() && SelfTest();
        }

        /// <summary>
        /// Issue a `SelfTest` command.
        /// </summary>
        public bool SelfTest()
        {
            try
            {
                SendSelfTestCommand();
                List<byte> response = ReceiveResponse();

                return response.Count == 6 &&
                       response.SequenceEqual(new byte[] { 0x04, 0x00, 0xde, 0xad, 0xbe, 0xef });
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        /// <summary>This function exists because `NetworkStream` doesn't have a `recv_exact` method.</summary>
        /// <param name="count">Number of bytes to receive.</param>
        private byte[] ReceiveBytes(int count)
        {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            int offset = 0, toRead = count;
            int read;
            byte[] buffer = new byte[count];

            while (toRead > 0 && (read = _stream.Read(buffer, offset, toRead)) > 0)
            {
                toRead -= read;
                offset += read;
            }
            if (toRead > 0) throw new EndOfStreamException();

            return buffer;
        }

        /// <summary>Receives the response and returns the raw response data.</summary>
        /// <returns>Raw response data</returns>
        private List<byte> ReceiveResponse()
        {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            // read a single byte to determine
            // if a serial no. is included or not
            int value = _stream.ReadByte();
            byte[] hdrBuf;
            switch (value)
            {
                // handle the case where we receive EOF
                // from the network stream
                case -1:
                    throw new EndOfStreamException("received EOF from the network stream");
                // if value is 0xd0, there's no serial no. included
                // in the response
                case 0xd0:
                    hdrBuf = ReceiveBytes(8);
                    break;
                // if value is 0xd4, there's a serial no. included
                // in the response
                case 0xd4:
                    hdrBuf = ReceiveBytes(12);
                    break;
                // in the case where we receive some other data, we mark it
                // as invalid and throw an `Exception`
                default:
                    throw new InvalidDataException($"while reading respoonse header: invalid start byte received: {value}");
            }

            // calculate the response data length
            int dataSize = (hdrBuf[hdrBuf.Length - 1] << 8) | hdrBuf[hdrBuf.Length - 2];
            List<byte> responseBuffer = ReceiveBytes(dataSize).ToList();

            // if the encode isn't `0` then we know that we hit an error.
            int endCode = responseBuffer[1] << 8 | responseBuffer[0];
            if (endCode != 0)
                throw new SLMPException(endCode);

            responseBuffer.RemoveRange(0, 2);
            return responseBuffer;
        }
        public enum Command
        {
            DeviceRead = 0x0401,
            DeviceWrite = 0x1401,
            SelfTest = 0x0619,
        }
        /// <summary>Sends the read device command.</summary>
        /// <param name="device">The target device.</param>
        /// <param name="adr">The address</param>
        /// <param name="cnt">The count.</param>
        private void SendReadDeviceCommand(dynamic device, ushort adr, ushort cnt)
        {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            List<byte> rawRequest = HEADER.ToList();

            ushort cmd = (ushort)Command.DeviceRead;
            ushort sub = DeviceMethods.GetSubcommand(device);

            rawRequest.AddRange(new byte[]{
                // request data length (in terms of bytes): fixed size (12) for the read command
                0x0c, 0x00,
                // monitoring timer. TODO: make this something configurable instead of hard-coding it.
                0x00, 0x00,
                (byte)(cmd & 0xff), (byte)(cmd >> 0x8),
                (byte)(sub & 0xff), (byte)(sub >> 0x8),
                (byte)(adr & 0xff), (byte)(adr >> 0x8),
                0x00,
                (byte)device,
                (byte)(cnt & 0xff), (byte)(cnt >> 0x8),
            });
            byte[] req = rawRequest.ToArray();
            _stream.Write(req, 0, req.Length);
        }

        /// <summary>
        /// Sends the write device command.
        /// </summary>
        /// <param name="device">The target device</param>
        /// <param name="adr">The address.</param>
        /// <param name="cnt">Number of data points.</param>
        /// <param name="data">Data itself.</param>
        private void SendWriteDeviceCommand(dynamic device, ushort adr, ushort cnt, byte[] data)
        {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            List<byte> rawRequest = HEADER.ToList();

            ushort cmd = (ushort)Command.DeviceWrite;
            ushort sub = DeviceMethods.GetSubcommand(device);
            ushort len = (ushort)(data.Length + 0x000c);

            rawRequest.AddRange(new byte[]{
                // request data length (in terms of bytes): (12 + data.Length)
                (byte)(len & 0xff), (byte)(len >> 0x8),
                // monitoring timer. TODO: make this something configurable instead of hard-coding it.
                0x00, 0x00,
                (byte)(cmd & 0xff), (byte)(cmd >> 0x8),
                (byte)(sub & 0xff), (byte)(sub >> 0x8),
                (byte)(adr & 0xff), (byte)(adr >> 0x8),
                0x00,
                (byte)device,
                (byte)(cnt & 0xff), (byte)(cnt >> 0x8),
            });
            rawRequest.AddRange(data);

            byte[] req = rawRequest.ToArray();
            _stream.Write(req, 0, req.Length);
        }

        /// <summary>
        /// Sends the `SelfTest` command.
        /// </summary>
        private void SendSelfTestCommand()
        {
            if (!InternalIsConnected())
                throw new NotConnectedException();

            List<byte> rawRequest = HEADER.ToList();
            ushort cmd = (ushort)Command.SelfTest;
            ushort sub = 0x0000;

            rawRequest.AddRange(new byte[]{
                // request data length (in terms of bytes): fixed size (12) for the read command
                0x0c, 0x00,
                // monitoring timer. TODO: make this something configurable instead of hard-coding it.
                0x00, 0x00,
                (byte)(cmd & 0xff), (byte)(cmd >> 0x8),
                (byte)(sub & 0xff), (byte)(sub >> 0x8),
                0x04, 0x00,
                0xde, 0xad, 0xbe, 0xef
            });


            byte[] req = rawRequest.ToArray();
            _stream.Write(req, 0, req.Length);
        }
    }
    public enum DeviceType
    {
        Bit,
        Word
    }

    /// <summary>
    /// This enum encodes the supported devices that is available to operate on.
    /// </summary>
    public enum Device
    {
        D = 0xa8,
        W = 0xb4,
        R = 0xaf,
        Z = 0xcc,
        ZR = 0xb0,
        SD = 0xa9,

        X = 0x9c,
        Y = 0x9d,
        M = 0x90,
        L = 0x92,
        F = 0x93,
        V = 0x94,
        B = 0xa0,
        SM = 0x91,
    }

    public class DeviceMethods
    {
        /// <summary>
        /// Gets the subcommand for a given `(Bit/Word)Device`.
        /// </summary>
        
        public static ushort GetSubcommand(Device device)
        {
            switch (DeviceMethods.GetDeviceType(device))
            {
                case DeviceType.Bit:
                    return 0x0001;
                case DeviceType.Word:
                    return 0x0000;
                default:
                    throw new ArgumentException("invalid device type provided");
            }
        }
        /// <summary>
        /// This helper function will return either `DeviceType.Word` or `DeviceType.Bit`
        /// for a given `device`.
        /// </summary>
        /// <returns>DeviceType</returns>
        /// <exception cref="ArgumentException"></exception>
        
        public static DeviceType GetDeviceType(Device device)
        {
            switch (device)
            {
                case Device.D:
                case Device.W:
                case Device.R:
                case Device.Z:
                case Device.ZR:
                case Device.SD:
                    return DeviceType.Word;
                case Device.X:
                case Device.Y:
                case Device.M:
                case Device.L:
                case Device.F:
                case Device.V:
                case Device.B:
                case Device.SM:
                    return DeviceType.Bit;
                default:
                    throw new ArgumentException("invalid device");
            }
        }
        /// <summary>
        /// Helper function to get a `Device` from a given string.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool FromString(string device, out Device value)
        {
            return Enum.TryParse<Device>(device, true, out value);
        }

        /// <summary>
        /// Helper function to parse strings in the form `{DeviceName}{DeviceAddress}`.
        /// </summary>
        /// <returns>Tuple<Device, ushort></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Tuple<Device, ushort> ParseDeviceAddress(string address)
        {
            Regex rx = new Regex(@"([a-zA-Z]+)(\d+)");
            Match match = rx.Match(address);

            if (match.Groups.Count < 3)
                throw new ArgumentException($"couldn't parse device address: {address}");

            string sdevice = match.Groups[1].Value;
            string saddr = match.Groups[2].Value;

            if (!FromString(sdevice, out Device device)) throw new ArgumentException($"invalid device provided: {sdevice}");
            if (!UInt16.TryParse(saddr, out ushort uaddr)) throw new ArgumentException($"invalid address provided: {saddr}");

            return Tuple.Create((Device)device, uaddr);
        }
    }
    public class SlmpConfig
    {
        /// <summary>
        /// IP address of the target SLMP-compatible device
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// The port that SLMP server is configured to run on.
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Connection timeout.
        /// </summary>
        public int ConnTimeout { get; set; } = 1000;
        /// <summary>
        /// Receive timeout.
        /// </summary>
        public int RecvTimeout { get; set; } = 1000;
        /// <summary>
        /// Send timeout.
        /// </summary>
        public int SendTimeout { get; set; } = 1000;

        /// <summary>
        /// Initialize a new `SlmpConfig` class
        /// </summary>
        public SlmpConfig(string address, int port)
        {
            Address = address;
            Port = port;
        }
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SlmpStringAttribute : Attribute
    {
        /// <summary>
        /// Length of the string.
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// Number of the device words that the string occupies.
        /// </summary>
        public int WordCount => Length % 2 == 0 ? Length / 2 + 1 : (Length + 1) / 2;
    }

    /// <summary>
    /// Functionality related to encoding/decoding struct.
    /// </summary>
    public static class SlmpStruct
    {
        /// <summary>
        /// This function returns the size of the struct in terms of
        /// device words. (16 bit values)
        /// Int16, UInt16, Boolean size: 1 word (2 bytes)
        /// Int32, UInt32 size: 2 word (4 bytes)
        /// String, user defined size, see `SlmpStringAttribute`
        /// </summary>
        /// <param name="structType">Type of the structure.</param>
        public static int GetStructSize(Type structType)
        {
            int size = 0;
            var fieldTypes = structType.GetFields();

            foreach (var field in fieldTypes)
                switch (field.FieldType.Name)
                {
                    case "Int16":
                    case "UInt16":
                    case "Boolean":
                        size++;
                        break;
                    case "Int32":
                    case "UInt32":
                        size += 2;
                        break;
                    case "String":
                        SlmpStringAttribute attr = field
                            .GetCustomAttributes<SlmpStringAttribute>()
                            .SingleOrDefault();
                        if (attr == default(SlmpStringAttribute))
                            throw new ArgumentException("please add a `SlmpStringAttribute` to the string.");

                        size += attr.WordCount;
                        break;
                    default:
                        throw new ArgumentException($"unsupported type: {field.FieldType.Name}");
                }

            return size;
        }

        /// <summary>
        /// This function builds a `Type` structure from a given byte array.
        /// </summary>
        /// <param name="structType">Type of the target structure.</param>
        /// <param name="words">Structure data in the form of an `ushort` array.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object FromWords(Type structType, ushort[] words)
        {
            if (words.Length != GetStructSize(structType))
                return null;

            object structObject = Activator.CreateInstance(structType);
            if (structObject == null)
                return null;

            int index = 0;
            var fields = structType.GetFields();

            foreach (var field in fields)
            {
                switch (field.FieldType.Name)
                {
                    case "Int16":
                        field.SetValue(structObject, (Int16)words[index]);
                        index++;
                        break;
                    case "UInt16":
                        field.SetValue(structObject, (UInt16)words[index]);
                        index++;
                        break;
                    case "Boolean":
                        field.SetValue(structObject, words[index] != 0);
                        index++;
                        break;
                    case "Int32":
                        field.SetValue(
                            structObject, (Int32)((words[index + 1] << 16) | words[index]));
                        index += 2;
                        break;
                    case "UInt32":
                        field.SetValue(
                            structObject, (UInt32)((words[index + 1] << 16) | words[index]));
                        index += 2;
                        break;
                    case "String":
                        SlmpStringAttribute attr = field
                            .GetCustomAttributes<SlmpStringAttribute>()
                            .SingleOrDefault();
                        if (attr == default(SlmpStringAttribute))
                            throw new ArgumentException("please add a SlmpStringAttribute to the string.");

                        List<char> buffer = new List<char>();
                        for (int i = index; i < index + attr.WordCount; i++)
                        {
                            ushort word = words[i];
                            buffer.Add((char)(word & 0xff));
                            buffer.Add((char)(word >> 0x8));
                        }
                        field.SetValue(
                            structObject, string.Join("", buffer.GetRange(0, attr.Length)));
                        index += attr.WordCount;
                        break;
                    default:
                        throw new ArgumentException($"unsupported type: {field.FieldType.Name}");
                }
            }

            return structObject;
        }
    }
    /// <summary>
    /// This exception is thrown in the case where `send` and `recv` data
    /// functions are called but there's no valid connection to operate on.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class NotConnectedException : Exception
    {
        public NotConnectedException()
            : base("not connected to a server") { }
    }

    /// <summary>
    /// This exception is thrown to indicate that the library is trying
    /// to process invalid data.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class InvalidDataException : Exception
    {
        public InvalidDataException(string message)
            : base(message) { }
    }

    /// <summary>
    /// This exception encapsulates the SLMP End Code for the further
    /// inspection of the error happened in the server (PLC/SLMP-compatible device) side.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class SLMPException : Exception
    {
        public int SLMPEndCode { get; set; }
        public SLMPException(int endCode)
            : base($"Received non-zero SLMP EndCode: {endCode:X4}H")
        {
            SLMPEndCode = endCode;
        }
    }
}
