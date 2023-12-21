﻿using HslCommunication;
using HslCommunication.Profinet.LSIS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PluginInterface;
using System.IO.Ports;
using System.Net;

namespace PLC.LSIS
{
    [DriverSupported("FENet")]
    [DriverSupported("CNet")]
    [DriverInfo("XGB", "V1.0.0", "Copyright IoTGateway.net 20230220")]
    public class DeviceXGB : IDriver
    {
        public ILogger _logger { get; set; }
        private readonly string _device;

        private XGBCnet xGBCnet = null;
        private XGBFastEnet fastEnet = null;


        #region 配置参数

        [ConfigParameter("设备Id")] public string DeviceId { get; set; }

        [ConfigParameter("PLC类型")] public LSCpuInfo lSCpuInfo { get; set; } = LSCpuInfo.XGB;

        [ConfigParameter("PLC类型")] public LSTypeConnect lSTypeConnect { get; set; } = LSTypeConnect.FENet;

        [ConfigParameter("IP地址")] public string IpAddress { get; set; } = "127.0.0.1";

        [ConfigParameter("端口号")] public int Port { get; set; } = 2004;

        [ConfigParameter("Slot")] public short Slot { get; set; } = 0;

        [ConfigParameter("串口名")] public string PortName { get; set; } = "COM1";

        [ConfigParameter("波特率")] public int BaudRate { get; set; } = 9600;

        [ConfigParameter("数据位")] public int DataBits { get; set; } = 8;

        [ConfigParameter("校验位")] public Parity Parity { get; set; } = Parity.None;

        [ConfigParameter("停止位")] public StopBits StopBits { get; set; } = StopBits.One;

        [ConfigParameter("从站号")] public byte Station { get; set; } = 1;

        [ConfigParameter("超时时间ms")] public int Timeout { get; set; } = 3000;

        [ConfigParameter("最小通讯周期ms")] public uint MinPeriod { get; set; } = 3000;

        #endregion

        #region 
        /// <summary>
        /// 反射构造函数
        /// </summary>
        /// <param name="device"></param>
        /// <param name="logger"></param>
        public DeviceXGB(string device, ILogger logger)
        {
            _device = device;
            _logger = logger;

            _logger.LogInformation($"Device:[{_device}],Create()");
        }

        public bool IsConnected { get; set; }

        public bool Connect()
        {
            try
            {
                _logger.LogInformation($"Device:[{_device}],Connect()");
                if (!IsConnected)
                {
                    switch (lSTypeConnect)
                    {
                        case LSTypeConnect.FENet:
                            if (fastEnet == null || !IsConnected)
                            {
                                fastEnet = new XGBFastEnet(IpAddress, Port);
                                IsConnected = fastEnet.ConnectServer().IsSuccess;
                            }
                            break;
                        case LSTypeConnect.CNet:
                            if (xGBCnet == null || !IsConnected)
                            {
                                xGBCnet = new XGBCnet
                                {
                                    Station = Station
                                };
                                xGBCnet.SerialPortInni(sp =>
                                {
                                    sp.PortName = PortName;
                                    sp.BaudRate = BaudRate;
                                    sp.DataBits = DataBits;
                                    sp.StopBits = StopBits;
                                    sp.Parity = Parity;
                                });
                                xGBCnet.Open();
                                IsConnected = xGBCnet.IsOpen();
                            }
                            break;

                        default:
                            return IsConnected;
                    }

                }
                else
                {
                    return IsConnected;
                }
                return IsConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Device:[{_device}],Connect()");
                return IsConnected;
            }
        }

        public bool Close()
        {
            try
            {
                _logger.LogInformation($"Device:[{_device}],Close()");
                switch (lSTypeConnect)
                {
                    case LSTypeConnect.FENet:
                        if (fastEnet != null)
                        {

                            fastEnet.ConnectClose();
                            return !IsConnected;
                        }
                        break;
                    case LSTypeConnect.CNet:
                        if (xGBCnet != null)
                        {
                            xGBCnet.Close();

                            return !IsConnected;
                        }
                        break;

                    default:
                        return false;
                }
                return !IsConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Device:[{_device}],Close()");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                _logger?.LogInformation($"Device:[{_device}],Dispose()");
                switch (lSTypeConnect)
                {
                    case LSTypeConnect.FENet:
                        if (fastEnet != null)
                        {

                            fastEnet.Dispose();

                        }
                        break;
                    case LSTypeConnect.CNet:
                        if (xGBCnet != null)
                        {
                            xGBCnet.Dispose();


                        }
                        break;



                }

                // Suppress finalization.
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Device:[{_device}],Dispose()");
            }

        }
        [Method("XGB", description: "Read")]
        public DriverReturnValueModel Read(DriverAddressIoArgModel ioArg)
        {
            DriverReturnValueModel ret = new() { StatusType = VaribaleStatusTypeEnum.Good };
            if (!IsConnected)
                ret.StatusType = VaribaleStatusTypeEnum.Bad;
            else
            {
                if (ret.StatusType != VaribaleStatusTypeEnum.Good)
                    return ret;
                try
                {


                    if (ioArg.ValueType == DataTypeEnum.Bool)
                    {
                        
                        OperateResult<bool[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadBool(ioArg.Address, 1) : xGBCnet.ReadBool(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Bit)
                    {
                        OperateResult<bool[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadBool(ioArg.Address, 1) : xGBCnet.ReadBool(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Byte)
                    {
                        OperateResult<byte> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadByte(ioArg.Address) : xGBCnet.ReadByte(ioArg.Address);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Uint16)
                    {
                        OperateResult<ushort[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadUInt16(ioArg.Address, 1) : xGBCnet.ReadUInt16(ioArg.Address, 1);  
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;

                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int16)
                    {

                        OperateResult<short[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadInt16(ioArg.Address, 1) : xGBCnet.ReadInt16(ioArg.Address, 1); 
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int32)
                    {

                        OperateResult<int[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadInt32(ioArg.Address, 1) : xGBCnet.ReadInt32(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Uint32)
                    {

                        OperateResult<uint[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadUInt32(ioArg.Address, 1) : xGBCnet.ReadUInt32(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int64)
                    {

                        OperateResult<long[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadInt64(ioArg.Address, 1) : xGBCnet.ReadInt64(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Uint64)
                    {

                        OperateResult<ulong[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadUInt64(ioArg.Address, 1) : xGBCnet.ReadUInt64(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Double)
                    {

                        OperateResult<double[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadDouble(ioArg.Address, 1) : xGBCnet.ReadDouble(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Float)
                    {

                        OperateResult<float[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadFloat(ioArg.Address, 1) : xGBCnet.ReadFloat(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content[0]);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.AsciiString)
                    {

                        OperateResult<string> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadString(ioArg.Address, 1) : xGBCnet.ReadString(ioArg.Address, 1);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else
                    {
                        IsConnected = false;
                        ret.StatusType = VaribaleStatusTypeEnum.UnKnow;
                        ret.Message = "类型未定义";
                        _logger.LogError($"Device:[{_device}],[{ioArg.ValueType}]类型未定义");
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    ret.StatusType = VaribaleStatusTypeEnum.Bad;
                    ret.Message = ex.Message;
                    _logger.LogError(ex, $"Device:[{_device}],ReadRegistersBuffers(),Error");
                }
            }


            return ret;

        }
        [Method("XGB", description: "ReadMultiple")]
        public DriverReturnValueModel ReadMultiple(DriverAddressIoArgModel ioArg)
        {
            DriverReturnValueModel ret = new() { StatusType = VaribaleStatusTypeEnum.Good };



            if (!IsConnected)
                ret.StatusType = VaribaleStatusTypeEnum.Bad;
            else
            {
                if (ret.StatusType != VaribaleStatusTypeEnum.Good)
                    return ret;
                try
                {
                    string startAddress;
                    if (ioArg.Address.Contains('|'))
                    {
                        startAddress = ioArg.Address.Split('|')[0];
                        ioArg.Address = ioArg.Address.Split('|')[1];
                    }
                    var args = ioArg.Address.Split(',');

                    startAddress = args[0];
                    var length = ushort.Parse(args[1]);

                    if (ioArg.ValueType == DataTypeEnum.Bool)
                    {
                        OperateResult<bool[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadBool(startAddress, length) : xGBCnet.ReadBool(startAddress, length);
                      
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Bit)
                    {
                        OperateResult<bool[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadBool(startAddress, length) : xGBCnet.ReadBool(startAddress, length);

                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Byte)
                    {
                        OperateResult<byte[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Read(startAddress, length) : xGBCnet.Read(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Uint16)
                    {
                        OperateResult<ushort[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadUInt16(startAddress, length) : xGBCnet.ReadUInt16(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;

                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int16)
                    {

                        OperateResult<short[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadInt16(startAddress, length) : xGBCnet.ReadInt16(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int32)
                    {

                        OperateResult<int[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadInt32(startAddress, length) : xGBCnet.ReadInt32(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Uint32)
                    {

                        OperateResult<uint[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadUInt32(startAddress, length) : xGBCnet.ReadUInt32(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int64)
                    {

                        OperateResult<long[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadInt64(startAddress, length) : xGBCnet.ReadInt64(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Uint64)
                    {

                        OperateResult<ulong[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadUInt64(startAddress, length) : xGBCnet.ReadUInt64(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Double)
                    {

                        OperateResult<double[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadDouble(startAddress, length) : xGBCnet.ReadDouble(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Float)
                    {

                        OperateResult<float[]> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadFloat(startAddress, length) : xGBCnet.ReadFloat(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.AsciiString)
                    {

                        OperateResult<string> read = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.ReadString(startAddress, length) : xGBCnet.ReadString(startAddress, length);
                        if (read.IsSuccess)
                            ret.Value = Newtonsoft.Json.JsonConvert.SerializeObject(read.Content);
                        else
                        {
                            IsConnected = false;
                            ret.StatusType = VaribaleStatusTypeEnum.Bad;
                            ret.Message = $"读取失败";
                        }
                        return ret;
                    }
                    else
                    {
                        IsConnected = false;
                        ret.StatusType = VaribaleStatusTypeEnum.UnKnow;
                        ret.Message = "类型未定义";
                        _logger.LogError($"Device:[{_device}],[{ioArg.ValueType}]类型未定义");
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    ret.StatusType = VaribaleStatusTypeEnum.Bad;
                    ret.Message = ex.Message;
                    _logger.LogError(ex, $"Device:[{_device}],ReadRegistersBuffers(),Error");
                }
            }


            return ret;

        }
        [Method("XGB", description: "Write")]
        public async Task<RpcResponse> WriteAsync(string RequestId, string Method, DriverAddressIoArgModel ioArg)
        {
            RpcResponse rpcResponse = new() { IsSuccess = false };
            try
            {
                OperateResult writeResult;
                if (!IsConnected)
                    rpcResponse.Description = "设备连接已断开";
                else
                {


                    if (ioArg.ValueType == DataTypeEnum.Bool)
                    {
                        var value = ioArg.Value.ToString() == "1" || ioArg.Value.ToString().ToLower() == "true";
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.WriteCoil(ioArg.Address, value) : xGBCnet.WriteCoil(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;


                    }
                    if (ioArg.ValueType == DataTypeEnum.Bit)
                    {
                        var value = ioArg.Value.ToString() == "1" || ioArg.Value.ToString().ToLower() == "true";
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.WriteCoil(ioArg.Address, value) : xGBCnet.WriteCoil(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Byte)
                    {
                        var value = ioArg.Value.ToString();
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);

                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    if (ioArg.ValueType == DataTypeEnum.Uint16)
                    {
                        var value = ushort.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;

                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int16)
                    {
                        var value = short.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int32)
                    {

                        var value = int.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Uint32)
                    {

                        var value = uint.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Int64)
                    {

                        var value = long.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Uint64)
                    {

                        var value = ulong.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Double)
                    {

                        var value = double.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }
                    else if (ioArg.ValueType == DataTypeEnum.Float)
                    {

                        var value = float.Parse(ioArg.Value.ToString());
                        writeResult = lSTypeConnect == LSTypeConnect.FENet ? fastEnet.Write(ioArg.Address, value) : xGBCnet.Write(ioArg.Address, value);
                        rpcResponse.IsSuccess = true;
                        return rpcResponse;
                    }




                    rpcResponse.Description = $"不支持写入:{Method}";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                rpcResponse.Description = $"写入失败,[method]:{Method},[ioArg]:{ioArg},[ex]:{ex}";
                _logger.LogError(ex, $"Device:[{_device}],WriteAsync(),Error");
            }

            return rpcResponse;
        }
        #endregion

    }

    public enum LSTypeConnect
    {
        CNet = 0,
        FENet = 1,

    }
}
