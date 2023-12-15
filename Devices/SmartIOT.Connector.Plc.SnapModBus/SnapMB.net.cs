#pragma warning disable S1118 // Utility classes should not have public constructors
#pragma warning disable S101 // Types should be named in PascalCase

using System.Text;
using System.Runtime.InteropServices;

namespace SnapModbus
{
    #region [SnapModbus Constants and types]

    public class MBConsts
    {
        public static readonly int ProtoTCP = 0;  // Modbus/TCP
        public static readonly int ProtoUDP = 1;  // Modbus/TCP using UDP as transport
        public static readonly int ProtoRTUOverTCP = 2;  // Modbus RTU wrapped in a TCP Packet
        public static readonly int ProtoRTUOverUDP = 3;  // Modbus RTU wrapped in a UDP Packet

        public static readonly int FormatRTU = 0;  // Serial RTU
        public static readonly int FormatASC = 1;  // Serial ASCII

        public static readonly int FlowNONE = 0;  // No control flow
        public static readonly int FlowRTSCTS = 1;  // RTS/CTS control flow

        // Callbacks Actions
        public static readonly int cbActionRead = 0;

        public static readonly int cbActionWrite = 1;

        public static readonly int PacketLog_NONE = 0;
        public static readonly int PacketLog_IN = 1;
        public static readonly int PacketLog_OUT = 2;
        public static readonly int PacketLog_BOTH = 3;

        public static readonly int bkSnd = 0;
        public static readonly int bkRcv = 1;

        // Area ID, see xxxdev_RegisterAreaa()
        public static readonly int mbaDiscreteInputs = 0;

        public static readonly int mbaCoils = 1;
        public static readonly int mbaInputRegisters = 2;
        public static readonly int mbaHoldingRegisters = 3;

        public static readonly int mbNoError = 0;

        // Callbacks Selectors, see xxxdev_RegisterCallback()
        public static readonly int cbkDeviceEvent = 0;

        public static readonly int cbkPacketLog = 1;
        public static readonly int cbkDiscreteInputs = 2;
        public static readonly int cbkCoils = 3;
        public static readonly int cbkInputRegisters = 4;
        public static readonly int cbkHoldingRegisters = 5;
        public static readonly int cbkReadWriteRegisters = 6;
        public static readonly int cbkMaskRegister = 7;
        public static readonly int cbkFileRecord = 8;
        public static readonly int cbkExceptionStatus = 9;
        public static readonly int cbkDiagnostics = 10;
        public static readonly int cbkGetCommEventCounter = 11;
        public static readonly int cbkGetCommEventLog = 12;
        public static readonly int cbkReportServerID = 13;
        public static readonly int cbkReadFIFOQueue = 14;
        public static readonly int cbkEncapsulatedIT = 15;
        public static readonly int cbkUsrFunction = 16;
        public static readonly int cbkPassthrough = 17;

        // Parameter Selectors see xxxxxx_SetDeviceParam()
        public static readonly int par_TCP_UDP_Port = 1;

        public static readonly int par_DeviceID = 2;
        public static readonly int par_TcpPersistence = 3;
        public static readonly int par_DisconnectOnError = 4;
        public static readonly int par_SendTimeout = 5;
        public static readonly int par_SerialFormat = 6;
        public static readonly int par_AutoTimeout = 7;
        public static readonly int par_AutoTimeLimitMin = 8;
        public static readonly int par_FixedTimeout = 9;
        public static readonly int par_BaseAddress = 10;
        public static readonly int par_DevPeerListMode = 11;
        public static readonly int par_PacketLog = 12;
        public static readonly int par_InterframeDelay = 13;
        public static readonly int par_WorkInterval = 14;
        public static readonly int par_AllowSerFunOnEth = 15;
        public static readonly int par_MaxRetries = 16;
        public static readonly int par_DisconnectTimeout = 17;
        public static readonly int par_AttemptSleep = 18;
        public static readonly int par_DevicePassthrough = 19;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DeviceEvent
        {
            public IntPtr EvtTime;   // It's platform dependent (32 or 64 bit)
            public Int32 EvtSender;
            public UInt32 EvtCode;
            public ushort EvtRetCode;
            public ushort EvtParam1;
            public ushort EvtParam2;
            public ushort EvtParam3;
            public ushort EvtParam4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct XOBJECT
        {
            public UIntPtr Object;
            public UIntPtr Selector;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DeviceStatus
        {
            public Int32 LastError;
            public Int32 Status;
            public Int32 Connected;
            public UInt32 JobTime;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DeviceInfo
        {
            public Int32 Running;
            public Int32 ClientsCount;   // only for TCP
            public Int32 ClientsBlocked; // only for TCP/UDP
            public Int32 LastError;
        }

        public static readonly int MsgTextLen = 512;

        // Using Mono under Linux you should modify snapmb.dll to libsnapmb.so
        public const string SnapLibName = "snapmb.dll";
    }

    #endregion [SnapModbus Constants and types]

    #region [Library Inports]

    internal class Externals
    {
        #region[POLYMORPHIC BROKER WRAPPERS]

        [DllImport(MBConsts.SnapLibName)]
        public static extern void broker_CreateFieldController(ref MBConsts.XOBJECT Broker);

        [DllImport(MBConsts.SnapLibName)]
        public static extern void broker_CreateEthernetClient(ref MBConsts.XOBJECT Broker, int Proto, [MarshalAs(UnmanagedType.LPStr)] string Address, int Port);

        [DllImport(MBConsts.SnapLibName)]
        public static extern void broker_CreateSerialClient(ref MBConsts.XOBJECT Broker, int Format, [MarshalAs(UnmanagedType.LPStr)] string PortName, int BaudRate, byte Parity, int DataBits, int Stops, int FLow);

        [DllImport(MBConsts.SnapLibName)]
        public static extern void broker_Destroy(ref MBConsts.XOBJECT Broker);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_Connect(ref MBConsts.XOBJECT Broker);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_Disconnect(ref MBConsts.XOBJECT Broker);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_AddControllerNetDevice(ref MBConsts.XOBJECT Broker, int Proto, byte DeviceID, [MarshalAs(UnmanagedType.LPStr)] string Address, int Port);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_AddControllerSerDevice(ref MBConsts.XOBJECT Broker, int Format, byte DeviceID, [MarshalAs(UnmanagedType.LPStr)] string PortName, int BaudRate, byte Parity, int DataBits, int Stops, int FLow);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_SetLocalParam(ref MBConsts.XOBJECT Broker, byte DeviceID, int ParamIndex, int Value);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_SetRemoteDeviceParam(ref MBConsts.XOBJECT Broker, byte DeviceID, int ParamIndex, int Value);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_GetIOBuffer(ref MBConsts.XOBJECT Broker, byte DeviceID, int BufferKind, byte[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_GetDeviceStatus(ref MBConsts.XOBJECT Broker, byte DeviceID, ref MBConsts.DeviceStatus Status);

        // Modbus Functions
        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadHoldingRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, ushort[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadHoldingRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteMultipleRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, ushort[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteMultipleRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadCoils(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, byte[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadCoils(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadDiscreteInputs(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, byte[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadDiscreteInputs(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadInputRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, ushort[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadInputRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteSingleCoil(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Value);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteSingleRegister(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Value);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadWriteMultipleRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort RDAddress, ushort RDAmount, ushort WRAddress, ushort WRAmount, ushort[] pRDUsrData, ushort[] pWRUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadWriteMultipleRegisters(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort RDAddress, ushort RDAmount, ushort WRAddress, ushort WRAmount, IntPtr pRDUsrData, IntPtr pWRUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteMultipleCoils(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, byte[] Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteMultipleCoils(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_MaskWriteRegister(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ushort AND_Mask, ushort OR_Mask);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadFileRecord(ref MBConsts.XOBJECT Broker, byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, ushort[] RecData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadFileRecord(ref MBConsts.XOBJECT Broker, byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, IntPtr pRecData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteFileRecord(ref MBConsts.XOBJECT Broker, byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, ushort[] RecData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_WriteFileRecord(ref MBConsts.XOBJECT Broker, byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, IntPtr pRecData);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadFIFOQueue(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ref ushort FifoCount, ushort[] FIFO);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadFIFOQueue(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort Address, ref ushort FifoCount, IntPtr pFIFO);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReadExceptionStatus(ref MBConsts.XOBJECT Broker, byte DeviceID, ref byte Data);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_Diagnostics(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort SubFunction, ushort[] SendData, ushort[] RecvData, ushort ItemsToSend, ref ushort ItemsReceived);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_Diagnostics(ref MBConsts.XOBJECT Broker, byte DeviceID, ushort SubFunction, IntPtr pSendData, IntPtr pRecvData, ushort ItemsToSend, ref ushort ItemsReceived);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_GetCommEventCounter(ref MBConsts.XOBJECT Broker, byte DeviceID, ref ushort Status, ref ushort EventCount);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_GetCommEventLog(ref MBConsts.XOBJECT Broker, byte DeviceID, ref ushort Status, ref ushort EventCount, ref ushort MessageCount, ref ushort NumItems, byte[] Events);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_GetCommEventLog(ref MBConsts.XOBJECT Broker, byte DeviceID, ref ushort Status, ref ushort EventCount, ref ushort MessageCount, ref ushort NumItems, IntPtr pEvents);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReportServerID(ref MBConsts.XOBJECT Broker, byte DeviceID, byte[] Data, ref int DataSize);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ReportServerID(ref MBConsts.XOBJECT Broker, byte DeviceID, IntPtr pUsrData, ref int DataSize);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ExecuteMEIFunction(ref MBConsts.XOBJECT Broker, byte DeviceID, byte MEI_Type, byte[] WRUsrData, ushort WRSize, byte[] RDUsrData, ref ushort RDSize);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_ExecuteMEIFunction(ref MBConsts.XOBJECT Broker, byte DeviceID, byte MEI_Type, IntPtr pWRUsrData, ushort WRSize, IntPtr pRDUsrData, ref ushort RDSize);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_CustomFunctionRequest(ref MBConsts.XOBJECT Broker, byte DeviceID, byte UsrFunction, byte[] UsrPDUWrite, ushort SizePDUWrite, byte[] UsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_CustomFunctionRequest(ref MBConsts.XOBJECT Broker, byte DeviceID, byte UsrFunction, IntPtr pUsrPDUWrite, ushort SizePDUWrite, IntPtr pUsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_RawRequest(ref MBConsts.XOBJECT Broker, byte DeviceID, byte[] UsrPDUWrite, ushort SizePDUWrite, byte[] UsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int broker_RawRequest(ref MBConsts.XOBJECT Broker, byte DeviceID, IntPtr pUsrPDUWrite, ushort SizePDUWrite, IntPtr pUsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected);

        #endregion [Library Inports]

        #region[POLYMORPHIC DEVICE WRAPPERS]

        [DllImport(MBConsts.SnapLibName)]
        public static extern void device_CreateEthernet(ref MBConsts.XOBJECT Device, int Proto, byte DeviceID, [MarshalAs(UnmanagedType.LPStr)] string Address, int Port);

        [DllImport(MBConsts.SnapLibName)]
        public static extern void device_CreateSerial(ref MBConsts.XOBJECT Device, int Format, byte DeviceID, [MarshalAs(UnmanagedType.LPStr)] string PortName, int BaudRate, byte Parity, int DataBits, int Stops, int Flow);

        [DllImport(MBConsts.SnapLibName)]
        public static extern void device_Destroy(ref MBConsts.XOBJECT Device);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_SetParam(ref MBConsts.XOBJECT Device, int ParamIndex, int Value);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_BindEthernet(ref MBConsts.XOBJECT Device, [MarshalAs(UnmanagedType.LPStr)] string Address, int Port);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_BindSerial(ref MBConsts.XOBJECT Device, [MarshalAs(UnmanagedType.LPStr)] string PortName, int BaudRate, byte Parity, int DataBits, int Stops, int Flow);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_SetUserFunction(ref MBConsts.XOBJECT Device, byte FunctionID, int Value);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_Start(ref MBConsts.XOBJECT Device);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_Stop(ref MBConsts.XOBJECT Device);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_AddPeer(ref MBConsts.XOBJECT Device, [MarshalAs(UnmanagedType.LPStr)] string Address);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_RegisterArea(ref MBConsts.XOBJECT Device, int AreaID, IntPtr Data, UInt16 Amount);

        [DllImport(MBConsts.SnapLibName, EntryPoint = "device_CopyArea")]
        public static extern int device_CopyArea_b(ref MBConsts.XOBJECT Device, int AreaID, UInt16 Address, UInt16 Amount, byte[] Data, int CopyMode);

        [DllImport(MBConsts.SnapLibName, EntryPoint = "device_CopyArea")]
        public static extern int device_CopyArea_w(ref MBConsts.XOBJECT Device, int AreaID, UInt16 Address, UInt16 Amount, ushort[] Data, int CopyMode);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_LockArea(ref MBConsts.XOBJECT Device, int AreaID);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_UnlockArea(ref MBConsts.XOBJECT Device, int AreaID);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_RegisterCallback(ref MBConsts.XOBJECT Device, int CallbackID, IntPtr cbRequest, IntPtr usrPtr);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_PickEvent(ref MBConsts.XOBJECT Device, ref MBConsts.DeviceEvent Event);

        [DllImport(MBConsts.SnapLibName, CharSet = CharSet.Ansi)]
        public static extern int device_PickEventAsText(ref MBConsts.XOBJECT Device, StringBuilder TextEvent, int TextSize);

        [DllImport(MBConsts.SnapLibName)]
        public static extern int device_GetDeviceInfo(ref MBConsts.XOBJECT Device, ref MBConsts.DeviceInfo Info);

        #endregion

        #region [COMMON WRAPPERS]

        [DllImport(MBConsts.SnapLibName, CharSet = CharSet.Ansi)]
        public static extern IntPtr ErrorText(int Error, StringBuilder TextError, int TextSize);

        [DllImport(MBConsts.SnapLibName, CharSet = CharSet.Ansi)]
        public static extern IntPtr EventText(ref MBConsts.DeviceEvent Event, StringBuilder TextEvent, int TextSize);

        #endregion
    }

    #endregion

    #region[SnapMBBroker]

    public class SnapMBBroker
    {
        private MBConsts.XOBJECT Broker;

        public SnapMBBroker()
        {
            Externals.broker_CreateFieldController(ref Broker);
        }

        // Ethernet Client ctor
        public SnapMBBroker(int Proto, string Address, int Port)
        {
            Externals.broker_CreateEthernetClient(ref Broker, Proto, Address, Port);
        }

        // Serial Client ctor
        public SnapMBBroker(int Format, string PortName, int BaudRate, char Parity, int DataBits, int Stops, int Flow)
        {
            Externals.broker_CreateSerialClient(ref Broker, Format, PortName, BaudRate, Convert.ToByte(Parity), DataBits, Stops, Flow);
        }

        // To avoid Objects destruction in C#, we can change the behaviour of our Broker calling ChangeTo() method
        public void ChangeTo()
        {
            Externals.broker_Destroy(ref Broker);
            Externals.broker_CreateFieldController(ref Broker);
        }

        public void ChangeTo(int Proto, string Address, int Port)
        {
            Externals.broker_Destroy(ref Broker);
            Externals.broker_CreateEthernetClient(ref Broker, Proto, Address, Port);
        }

        public void ChangeTo(int Format, string PortName, int BaudRate, char Parity, int DataBits, int Stops, int Flow)
        {
            Externals.broker_Destroy(ref Broker);
            Externals.broker_CreateSerialClient(ref Broker, Format, PortName, BaudRate, Convert.ToByte(Parity), DataBits, Stops, Flow);
        }

        // Common dtor
        ~SnapMBBroker()
        {
            Externals.broker_Destroy(ref Broker);
        }

        public int Connect()
        {
            return Externals.broker_Connect(ref Broker);
        }

        public int Disconnect()
        {
            return Externals.broker_Disconnect(ref Broker);
        }

        public int AddDevice(int Proto, byte DeviceID, string Address, int Port)
        {
            return Externals.broker_AddControllerNetDevice(ref Broker, Proto, DeviceID, Address, Port);
        }

        public int AddDevice(int Format, byte DeviceID, string PortName, int BaudRate, char Parity, int DataBits, int Stops, int Flow)
        {
            return Externals.broker_AddControllerSerDevice(ref Broker, Format, DeviceID, PortName, BaudRate, Convert.ToByte(Parity), DataBits, Stops, Flow);
        }

        public int SetLocalParam(byte DeviceID, int ParamIndex, int Value)
        {
            return Externals.broker_SetLocalParam(ref Broker, DeviceID, ParamIndex, Value);
        }

        public int SetRemoteDeviceParam(byte DeviceID, int ParamIndex, int Value)
        {
            return Externals.broker_SetRemoteDeviceParam(ref Broker, DeviceID, ParamIndex, Value);
        }

        public int GetIOBuffer(byte[] Data, int BufferKind)
        {
            return Externals.broker_GetIOBuffer(ref Broker, 0, BufferKind, Data);
        }

        public int GetIOBuffer(byte DeviceID, byte[] Data, int BufferKind)
        {
            return Externals.broker_GetIOBuffer(ref Broker, DeviceID, BufferKind, Data);
        }

        public int GetDeviceStatus(byte DeviceID, ref MBConsts.DeviceStatus Status)
        {
            return Externals.broker_GetDeviceStatus(ref Broker, DeviceID, ref Status);
        }

        public int ReadHoldingRegisters(byte DeviceID, ushort Address, ushort Amount, ushort[] Data)
        {
            return Externals.broker_ReadHoldingRegisters(ref Broker, DeviceID, Address, Amount, Data);
        }

        public int ReadHoldingRegisters(byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData)
        {
            return Externals.broker_ReadHoldingRegisters(ref Broker, DeviceID, Address, Amount, pUsrData);
        }

        public int WriteMultipleRegisters(byte DeviceID, ushort Address, ushort Amount, ushort[] Data)
        {
            return Externals.broker_WriteMultipleRegisters(ref Broker, DeviceID, Address, Amount, Data);
        }

        public int WriteMultipleRegisters(byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData)
        {
            return Externals.broker_WriteMultipleRegisters(ref Broker, DeviceID, Address, Amount, pUsrData);
        }

        public int ReadCoils(byte DeviceID, ushort Address, ushort Amount, byte[] Data)
        {
            return Externals.broker_ReadCoils(ref Broker, DeviceID, Address, Amount, Data);
        }

        public int ReadCoils(byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData)
        {
            return Externals.broker_ReadCoils(ref Broker, DeviceID, Address, Amount, pUsrData);
        }

        public int ReadDiscreteInputs(byte DeviceID, ushort Address, ushort Amount, byte[] Data)
        {
            return Externals.broker_ReadDiscreteInputs(ref Broker, DeviceID, Address, Amount, Data);
        }

        public int ReadDiscreteInputs(byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData)
        {
            return Externals.broker_ReadDiscreteInputs(ref Broker, DeviceID, Address, Amount, pUsrData);
        }

        public int ReadInputRegisters(byte DeviceID, ushort Address, ushort Amount, ushort[] Data)
        {
            return Externals.broker_ReadInputRegisters(ref Broker, DeviceID, Address, Amount, Data);
        }

        public int ReadInputRegisters(byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData)
        {
            return Externals.broker_ReadInputRegisters(ref Broker, DeviceID, Address, Amount, pUsrData);
        }

        public int WriteSingleCoil(byte DeviceID, ushort Address, bool Value)
        {
            return Externals.broker_WriteSingleCoil(ref Broker, DeviceID, Address, Convert.ToUInt16(Value));
        }

        public int WriteSingleRegister(byte DeviceID, ushort Address, ushort Value)
        {
            return Externals.broker_WriteSingleRegister(ref Broker, DeviceID, Address, Value);
        }

        public int ReadWriteMultipleRegisters(byte DeviceID, ushort RDAddress, ushort RDAmount, ushort WRAddress, ushort WRAmount, ushort[] RDUsrData, ushort[] WRUsrData)
        {
            return Externals.broker_ReadWriteMultipleRegisters(ref Broker, DeviceID, RDAddress, RDAmount, WRAddress, WRAmount, RDUsrData, WRUsrData);
        }

        public int ReadWriteMultipleRegisters(byte DeviceID, ushort RDAddress, ushort RDAmount, ushort WRAddress, ushort WRAmount, IntPtr pRDUsrData, IntPtr pWRUsrData)
        {
            return Externals.broker_ReadWriteMultipleRegisters(ref Broker, DeviceID, RDAddress, RDAmount, WRAddress, WRAmount, pRDUsrData, pWRUsrData);
        }

        public int WriteMultipleCoils(byte DeviceID, ushort Address, ushort Amount, byte[] Data)
        {
            return Externals.broker_WriteMultipleCoils(ref Broker, DeviceID, Address, Amount, Data);
        }

        public int WriteMultipleCoils(byte DeviceID, ushort Address, ushort Amount, IntPtr pUsrData)
        {
            return Externals.broker_WriteMultipleCoils(ref Broker, DeviceID, Address, Amount, pUsrData);
        }

        public int MaskWriteRegister(byte DeviceID, ushort Address, ushort AND_Mask, ushort OR_Mask)
        {
            return Externals.broker_MaskWriteRegister(ref Broker, DeviceID, Address, AND_Mask, OR_Mask);
        }

        public int ReadFileRecord(byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, ushort[] RecData)
        {
            return Externals.broker_ReadFileRecord(ref Broker, DeviceID, RefType, FileNumber, RecNumber, RegsAmount, RecData);
        }

        public int ReadFileRecord(byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, IntPtr pRecData)
        {
            return Externals.broker_ReadFileRecord(ref Broker, DeviceID, RefType, FileNumber, RecNumber, RegsAmount, pRecData);
        }

        public int WriteFileRecord(byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, ushort[] RecData)
        {
            return Externals.broker_WriteFileRecord(ref Broker, DeviceID, RefType, FileNumber, RecNumber, RegsAmount, RecData);
        }

        public int WriteFileRecord(byte DeviceID, byte RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, IntPtr pRecData)
        {
            return Externals.broker_WriteFileRecord(ref Broker, DeviceID, RefType, FileNumber, RecNumber, RegsAmount, pRecData);
        }

        public int ReadFIFOQueue(byte DeviceID, ushort Address, ref ushort FifoCount, ushort[] FIFO)
        {
            return Externals.broker_ReadFIFOQueue(ref Broker, DeviceID, Address, ref FifoCount, FIFO);
        }

        public int ReadFIFOQueue(byte DeviceID, ushort Address, ref ushort FifoCount, IntPtr pFIFO)
        {
            return Externals.broker_ReadFIFOQueue(ref Broker, DeviceID, Address, ref FifoCount, pFIFO);
        }

        public int ReadExceptionStatus(byte DeviceID, ref byte Data)
        {
            return Externals.broker_ReadExceptionStatus(ref Broker, DeviceID, ref Data);
        }

        public int Diagnostics(byte DeviceID, ushort SubFunction, ushort[] SendData, ushort[] RecvData, ushort ItemsToSend, ref ushort ItemsRecvd)
        {
            return Externals.broker_Diagnostics(ref Broker, DeviceID, SubFunction, SendData, RecvData, ItemsToSend, ref ItemsRecvd);
        }

        public int Diagnostics(byte DeviceID, ushort SubFunction, IntPtr pSendData, IntPtr pRecvData, ushort ItemsToSend, ref ushort ItemsRecvd)
        {
            return Externals.broker_Diagnostics(ref Broker, DeviceID, SubFunction, pSendData, pRecvData, ItemsToSend, ref ItemsRecvd);
        }

        public int GetCommEventCounter(byte DeviceID, ref ushort Status, ref ushort EventCount)
        {
            return Externals.broker_GetCommEventCounter(ref Broker, DeviceID, ref Status, ref EventCount);
        }

        public int GetCommEventLog(byte DeviceID, ref ushort Status, ref ushort EventCount, ref ushort MessageCount, ref ushort NumItems, byte[] Events)
        {
            return Externals.broker_GetCommEventLog(ref Broker, DeviceID, ref Status, ref EventCount, ref MessageCount, ref NumItems, Events);
        }

        public int GetCommEventLog(byte DeviceID, ref ushort Status, ref ushort EventCount, ref ushort MessageCount, ref ushort NumItems, IntPtr pEvents)
        {
            return Externals.broker_GetCommEventLog(ref Broker, DeviceID, ref Status, ref EventCount, ref MessageCount, ref NumItems, pEvents);
        }

        public int ReportServerID(byte DeviceID, byte[] UsrData, ref int DataSize)
        {
            return Externals.broker_ReportServerID(ref Broker, DeviceID, UsrData, ref DataSize);
        }

        public int ReportServerID(byte DeviceID, IntPtr pUsrData, ref int DataSize)
        {
            return Externals.broker_ReportServerID(ref Broker, DeviceID, pUsrData, ref DataSize);
        }

        public int ExecuteMEIFunction(byte DeviceID, byte MEI_Type, byte[] WRUsrData, ushort WRSize, byte[] RDUsrData, ref ushort RDSize)
        {
            return Externals.broker_ExecuteMEIFunction(ref Broker, DeviceID, MEI_Type, WRUsrData, WRSize, RDUsrData, ref RDSize);
        }

        public int ExecuteMEIFunction(byte DeviceID, byte MEI_Type, IntPtr pWRUsrData, ushort WRSize, IntPtr pRDUsrData, ref ushort RDSize)
        {
            return Externals.broker_ExecuteMEIFunction(ref Broker, DeviceID, MEI_Type, pWRUsrData, WRSize, pRDUsrData, ref RDSize);
        }

        public int CustomFunctionRequest(byte DeviceID, byte UsrFunction, byte[] UsrPDUWrite, ushort SizePDUWrite, byte[] UsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected)
        {
            return Externals.broker_CustomFunctionRequest(ref Broker, DeviceID, UsrFunction, UsrPDUWrite, SizePDUWrite, UsrPDURead, ref SizePDURead, SizePDUExpected);
        }

        public int CustomFunctionRequest(byte DeviceID, byte UsrFunction, IntPtr pUsrPDUWrite, ushort SizePDUWrite, IntPtr pUsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected)
        {
            return Externals.broker_CustomFunctionRequest(ref Broker, DeviceID, UsrFunction, pUsrPDUWrite, SizePDUWrite, pUsrPDURead, ref SizePDURead, SizePDUExpected);
        }

        public int RawRequestRequest(byte DeviceID, byte UsrFunction, byte[] UsrPDUWrite, ushort SizePDUWrite, byte[] UsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected)
        {
            return Externals.broker_RawRequest(ref Broker, DeviceID, UsrPDUWrite, SizePDUWrite, UsrPDURead, ref SizePDURead, SizePDUExpected);
        }

        public int RawRequestRequest(byte DeviceID, byte UsrFunction, IntPtr pUsrPDUWrite, ushort SizePDUWrite, IntPtr pUsrPDURead, ref ushort SizePDURead, ushort SizePDUExpected)
        {
            return Externals.broker_RawRequest(ref Broker, DeviceID, pUsrPDUWrite, SizePDUWrite, pUsrPDURead, ref SizePDURead, SizePDUExpected);
        }
    }

    #endregion

    #region[SnapMBDevice]

    // Callbacks
    public delegate void TDeviceEvent(IntPtr usrPtr, ref MBConsts.DeviceEvent Event, int Size);

    public delegate void TPacketLog(IntPtr usrPtr, UInt32 Peer, int Direction, IntPtr Data, int Size);

    public delegate int TDiscreteInputsRequest(IntPtr usrPtr, ushort Address, ushort Amount, IntPtr Data);

    public delegate int TCoilsRequest(IntPtr usrPtr, int Action, ushort Address, ushort Amount, IntPtr Data);

    public delegate int TInputRegistersRequest(IntPtr usrPtr, ushort Address, ushort Amount, IntPtr Data);

    public delegate int THoldingRegistersRequest(IntPtr usrPtr, int Action, ushort Address, ushort Amount, IntPtr Data);

    public delegate int TReadWriteMultipleRegistersRequest(IntPtr usrPtr, ushort RDAddress, ushort RDAmount, IntPtr RDData, ushort WRAddress, ushort WRAmount, IntPtr WRData);

    public delegate int TMaskRegisterRequest(IntPtr usrPtr, ushort Address, ushort AND_Mask, ushort OR_Mask);

    public delegate int TFileRecordRequest(IntPtr usrPtr, int Action, ushort RefType, ushort FileNumber, ushort RecNumber, ushort RegsAmount, IntPtr Data);

    public delegate int TExceptionStatusRequest(IntPtr usrPtr, ref byte Status);

    public delegate int TDiagnosticsRequest(IntPtr usrPtr, ushort SubFunction, IntPtr RxItems, IntPtr TxItems, int ItemsSent, ref ushort ItemsRecvd);

    public delegate int TGetCommEventCounterRequest(IntPtr usrPtr, ref ushort Status, ref ushort EventCount);

    public delegate int TGetCommEventLogRequest(IntPtr usrPtr, ref ushort Status, ref ushort EventCount, ref ushort MessageCount, IntPtr Data, ref ushort EventsAmount);

    public delegate int TReportServerIDRequest(IntPtr usrPtr, IntPtr Data, ref ushort DataSize);

    public delegate int TReadFIFOQueueRequest(IntPtr usrPtr, ushort PtrAddress, IntPtr FIFOValues, ref ushort FifoCount);

    public delegate int TEncapsulatedIT(IntPtr usrPtr, byte MEI_Type, IntPtr MEI_DataReq, int ReqDataSize, IntPtr MEI_DataRes, ref ushort ResDataSize);

    public delegate int TUsrFunctionRequest(IntPtr usrPtr, byte Function, IntPtr RxPDU, int RxPDUSize, IntPtr TxPDU, ref ushort TxPDUSize);

    public delegate int TPassthroughRequest(IntPtr usrPtr, byte DeviceID, IntPtr RxPDU, int RxPDUSize, IntPtr TxPDU, ref ushort TxPDUSize);

    public class SnapMBDevice
    {
        private MBConsts.XOBJECT Device;

        protected GCHandle[] Areas = new GCHandle[4];

        private void InternalDestroy()
        {
            if (Device.Object != UIntPtr.Zero)
                Externals.device_Destroy(ref Device);
            foreach (var Item in Areas)
                if (Item.IsAllocated)
                    Item.Free();
        }

        /// <summary>
        /// Warning : use this constructor only if don't know in advance the Device Type and
        /// so you want to invoke ChangeTo() in a second time
        /// </summary>
        public SnapMBDevice()
        {
            Device.Object = UIntPtr.Zero;
            Device.Selector = UIntPtr.Zero;
        }

        public SnapMBDevice(int Proto, byte DeviceID, string Address, int Port)
        {
            Externals.device_CreateEthernet(ref Device, Proto, DeviceID, Address, Port);
        }

        public SnapMBDevice(int Format, byte DeviceID, string PortName, int BaudRate, char Parity, int DataBits, int Stops, int Flow)
        {
            Externals.device_CreateSerial(ref Device, Format, DeviceID, PortName, BaudRate, Convert.ToByte(Parity), DataBits, Stops, Flow);
        }

        ~SnapMBDevice()
        {
            InternalDestroy();
        }

        public bool Exists
        {
            get
            {
                return (Device.Object != UIntPtr.Zero);
            }
        }

        public void ChangeTo(int Proto, int DeviceID, string Address, int Port)
        {
            InternalDestroy();
            Externals.device_CreateEthernet(ref Device, Proto, (byte)DeviceID, Address, Port);
        }

        public void ChangeTo(int Format, int DeviceID, string PortName, int BaudRate, char Parity, int DataBits, int Stops, int Flow)
        {
            InternalDestroy();
            Externals.device_CreateSerial(ref Device, Format, (byte)DeviceID, PortName, BaudRate, Convert.ToByte(Parity), DataBits, Stops, Flow);
        }

        public int Bind(byte DeviceID, string Address, int Port)
        {
            return Externals.device_BindEthernet(ref Device, Address, Port);
        }

        public int Bind(byte DeviceID, string PortName, int BaudRate, char Parity, int DataBits, int Stops, int Flow)
        {
            return Externals.device_BindSerial(ref Device, PortName, BaudRate, Convert.ToByte(Parity), DataBits, Stops, Flow);
        }

        public int SetParam(int ParamIndex, int Value)
        {
            return Externals.device_SetParam(ref Device, ParamIndex, Value);
        }

        public int SetUserFunction(byte FunctionID, bool Value)
        {
            return Externals.device_SetUserFunction(ref Device, FunctionID, Convert.ToInt32(Value));
        }

        public int Start()
        {
            return Externals.device_Start(ref Device);
        }

        public int Stop()
        {
            return Externals.device_Stop(ref Device);
        }

        public int AddPeer(string Address)
        {
            return Externals.device_AddPeer(ref Device, Address);
        }

        //--------------------------------------------------------------------------------
        // Register Area
        // We need a Pinned Object to avoid that the GarbageCollector deletes the Area
        //--------------------------------------------------------------------------------
        public int RegisterArea<T>(int AreaID, ref T Data, int Amount)
        {
            if (Areas[AreaID].IsAllocated)
                Areas[AreaID].Free();
            Areas[AreaID] = GCHandle.Alloc(Data, GCHandleType.Pinned);
            int Result = Externals.device_RegisterArea(ref Device, AreaID, Areas[AreaID].AddrOfPinnedObject(), (ushort)Amount);
            if (Result != 0)
                Areas[AreaID].Free();
            return Result;
        }

        public int CopyArea(int AreaID, UInt16 Address, UInt16 Amount, byte[] Data, int CopyMode)
        {
            return Externals.device_CopyArea_b(ref Device, AreaID, Address, Amount, Data, CopyMode);
        }

        public int CopyArea(int AreaID, UInt16 Address, UInt16 Amount, ushort[] Data, int CopyMode)
        {
            return Externals.device_CopyArea_w(ref Device, AreaID, Address, Amount, Data, CopyMode);
        }

        public int LockArea(int AreaID)
        {
            return Externals.device_LockArea(ref Device, AreaID);
        }

        public int UnlockArea(int AreaID)
        {
            return Externals.device_UnlockArea(ref Device, AreaID);
        }

        public int RegisterCallback(int CallbackID, IntPtr cbRequest, IntPtr usrPtr)
        {
            return Externals.device_RegisterCallback(ref Device, CallbackID, cbRequest, usrPtr);
        }

        public bool PickEvent(ref MBConsts.DeviceEvent Event)
        {
            return Externals.device_PickEvent(ref Device, ref Event) != 0;
        }

        public bool PickEventAsText(ref string TextEvent)
        {
            StringBuilder Text = new StringBuilder(MBConsts.MsgTextLen);
            int Result = Externals.device_PickEventAsText(ref Device, Text, MBConsts.MsgTextLen);
            if (Result != 0)
                TextEvent = Text.ToString();
            return Result != 0;
        }

        public string PickEventAsText()
        {
            StringBuilder Text = new StringBuilder(MBConsts.MsgTextLen);
            if (Externals.device_PickEventAsText(ref Device, Text, MBConsts.MsgTextLen) != 0)
                return Text.ToString();
            else
                return "";
        }

        public int GetDeviceInfo(ref MBConsts.DeviceInfo Info)
        {
            return Externals.device_GetDeviceInfo(ref Device, ref Info);
        }
    }

    #endregion

    #region [Utils]

    public static class MB
    {
        public static string EventText(ref MBConsts.DeviceEvent Event)
        {
            StringBuilder Text = new StringBuilder(MBConsts.MsgTextLen);
            Externals.EventText(ref Event, Text, MBConsts.MsgTextLen);
            return Text.ToString();
        }

        public static string ErrorText(int Error)
        {
            StringBuilder Text = new StringBuilder(MBConsts.MsgTextLen);
            Externals.ErrorText(Error, Text, MBConsts.MsgTextLen);
            return Text.ToString();
        }
    }

    #endregion
}
