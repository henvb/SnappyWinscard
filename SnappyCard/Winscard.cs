using System;
using System.Runtime.InteropServices;

namespace CardTest
{
    public class Winscard
    {
        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct SCARD_IO_REQUEST
        {
            public int dwProtocol;
            public int cbPciLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APDURec
        {
            public byte bCLA;
            public byte bINS;
            public byte bP1;
            public byte bP2;
            public byte bP3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] Data;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] SW;
            public bool IsSend;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SCARD_READERSTATE
        {
            public string RdrName;
            public int UserData;
            public int RdrCurrState;
            public int RdrEventState;
            public int ATRLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 37)]
            public byte[] ATRValue;
        }
        #endregion

        #region Memory cards

        public const string READER_NAME_NEW = @"\\?PnP?\Notification";
        public const int CT_MCU = 0x00;                   // MCU
        public const int CT_IIC_Auto = 0x01;               // IIC (Auto Detect Memory Size)
        public const int CT_IIC_1K = 0x02;                 // IIC (1K)
        public const int CT_IIC_2K = 0x03;                 // IIC (2K)
        public const int CT_IIC_4K = 0x04;                 // IIC (4K)
        public const int CT_IIC_8K = 0x05;                 // IIC (8K)
        public const int CT_IIC_16K = 0x06;                // IIC (16K)
        public const int CT_IIC_32K = 0x07;                // IIC (32K)
        public const int CT_IIC_64K = 0x08;                // IIC (64K)
        public const int CT_IIC_128K = 0x09;               // IIC (128K)
        public const int CT_IIC_256K = 0x0A;               // IIC (256K)
        public const int CT_IIC_512K = 0x0B;               // IIC (512K)
        public const int CT_IIC_1024K = 0x0C;              // IIC (1024K)
        public const int CT_AT88SC153 = 0x0D;              // AT88SC153
        public const int CT_AT88SC1608 = 0x0E;             // AT88SC1608
        public const int CT_SLE4418 = 0x0F;                // SLE4418
        public const int CT_SLE4428 = 0x10;                // SLE4428
        public const int CT_SLE4432 = 0x11;                // SLE4432
        public const int CT_SLE4442 = 0x12;                // SLE4442
        public const int CT_SLE4406 = 0x13;                // SLE4406
        public const int CT_SLE4436 = 0x14;                // SLE4436
        public const int CT_SLE5536 = 0x15;                // SLE5536
        public const int CT_MCUT0 = 0x16;                  // MCU T=0
        public const int CT_MCUT1 = 0x17;                  // MCU T=1
        public const int CT_MCU_Auto = 0x18;               // MCU Autodetect

        #endregion

        #region Scope

        /// <summary>
        /// Scope in user space
        /// </summary>
        /// <remarks>The context is a user context, and any database operations 
        /// are performed within the domain of the user.
        /// </remarks>
        public const int SCARD_SCOPE_USER = 0;

        /*===============================================================
        ' 
        '===============================================================*/

        /// <summary>
        /// Scope in terminal
        /// </summary>
        /// <remarks>The context is that of the current terminal, and any database 
        ///operations are performed within the domain of that terminal.  
        ///(The calling application must have appropriate access permissions 
        ///for any database actions.)
        /// </remarks>
        public const int SCARD_SCOPE_TERMINAL = 1;

        /// <summary>
        /// Scope in system
        /// </summary>
        ///<remarks>The context is the system context, and any database operations 
        ///are performed within the domain of the system.  (The calling
        ///application must have appropriate access permissions for any 
        ///database actions.)
        ///</remarks>
        public const int SCARD_SCOPE_SYSTEM = 2;
        #endregion

        #region State
        /// <summary>
        /// App wants status
        /// </summary>
        /// <remarks> The application is unaware of the current state, and would like 
        ///to know. The use of this value results in an immediate return
        ///from state transition monitoring services. This is represented
        ///by all bits set to zero.
        /// </remarks>
        public const int SCARD_STATE_UNAWARE = 0x00;

        /// <summary>
        /// Ignore this reader
        /// </summary>
        /// <remarks>The application requested that this reader be ignored. No other
        ///bits will be set.
        /// </remarks>
        public const int SCARD_STATE_IGNORE = 0x01;

        /// <summary>
        /// State has changed
        /// </summary>
        /// <remarks> This implies that there is a difference between the state 
        ///believed by the application, and the state known by the Service
        ///Manager.When this bit is set, the application may assume a
        ///significant state change has occurred on this reader.
        /// </remarks>
        public const int SCARD_STATE_CHANGED = 0x02;

        /// <summary>
        /// Reader unknown
        /// </summary>
        /// <remarks> This implies that the given reader name is not recognized by
        ///the Service Manager. If this bit is set, then SCARD_STATE_CHANGED
        ///and SCARD_STATE_IGNORE will also be set.
        /// </remarks>
        public const int SCARD_STATE_UNKNOWN = 0x04;

        /// <summary>
        /// Status unavailable
        /// </summary>
        /// <remarks>This implies that the actual state of this reader is not
        ///available. If this bit is set, then all the following bits are
        ///clear.
        /// </remarks>
        public const int SCARD_STATE_UNAVAILABLE = 0x08;

        /// <summary>
        /// Card removed
        /// </summary>
        /// <remarks>This implies that there is not card in the reader.  If this bit
        /// is set, all the following bits will be clear.
        /// </remarks>
        public const int SCARD_STATE_EMPTY = 0x10;

        /// <summary>
        /// Card inserted
        /// </summary>
        /// <remarks> This implies that there is a card in the reader.
        /// </remarks>
        public const int SCARD_STATE_PRESENT = 0x20;

        /// <summary>
        /// ATR matches card
        /// </summary>
        /// <remarks>This implies that there is a card in the reader with an ATR
        ///matching one of the target cards. If this bit is set,
        ///SCARD_STATE_PRESENT will also be set.  This bit is only returned
        ///on the SCardLocateCard() service.
        /// </remarks>
        public const int SCARD_STATE_ATRMATCH = 0x40;

        /// <summary>
        /// Exclusive Mode
        /// </summary>
        /// <remarks>This implies that the card in the reader is allocated for 
        ///exclusive use by another application. If this bit is set,
        ///SCARD_STATE_PRESENT will also be set.
        /// </remarks>
        public const int SCARD_STATE_EXCLUSIVE = 0x80;

        /// <summary>
        /// Shared mode
        /// </summary>
        /// <remarks>This implies that the card in the reader is in use by one or 
        ///more other applications, but may be connected to in shared mode. 
        ///If this bit is set, SCARD_STATE_PRESENT will also be set.
        /// </remarks>
        public const int SCARD_STATE_INUSE = 0x100;

        /// <summary>
        /// Unresponsive card
        /// </summary>
        /// <remarks>This implies that the card in the reader is unresponsive or not
        ///supported by the reader or software.
        /// </remarks>
        public const int SCARD_STATE_MUTE = 0x200;

        /// <summary>
        /// Unpowered card
        /// </summary>
        /// <remarks> This implies that the card in the reader has not been powered up.
        /// </remarks>
        public const int SCARD_STATE_UNPOWERED = 0x400;

        /// <summary>
        /// Exclusive mode only
        /// </summary>
        /// <remarks>This application is not willing to share this card with other 
        //applications.
        /// </remarks>

        public const int SCARD_SHARE_EXCLUSIVE = 1;

        /// <summary>
        /// Shared mode only
        /// </summary>
        /// <remarks>This application is willing to share this card with other 
        ///applications.
        /// </remarks>
        public const int SCARD_SHARE_SHARED = 2;

        /// <summary>
        /// Raw mode only
        /// </summary>
        /// <remarks>This application demands direct control of the reader, so it 
        ///is not available to other applications.
        /// </remarks>
        public const int SCARD_SHARE_DIRECT = 3;
        #endregion

        #region Disposition

        /// <summary>
        /// Do nothing on close
        /// </summary>
        /// <remarks> Don't do anything special on close
        /// </remarks>
        public const int SCARD_LEAVE_CARD = 0;

        /// <summary>
        /// Reset on close
        /// </summary>
        /// <remarks> Reset the card on close
        /// </remarks>
        public const int SCARD_RESET_CARD = 1;

        /// <summary>
        /// Power down on close
        /// </summary>
        /// <remarks> Power down the card on close
        /// </remarks> 
        public const int SCARD_UNPOWER_CARD = 2;

        /// <summary>
        /// Eject on close
        /// </summary>
        /// <remarks>Eject the card on close
        /// </remarks>
        public const int SCARD_EJECT_CARD = 3;
        #endregion

        #region ACS IOCTL class
        public const long FILE_DEVICE_SMARTCARD = 0x310000; // Reader action IOCTLs

        public const long IOCTL_SMARTCARD_DIRECT = FILE_DEVICE_SMARTCARD + 2050 * 4;
        public const long IOCTL_SMARTCARD_SELECT_SLOT = FILE_DEVICE_SMARTCARD + 2051 * 4;
        public const long IOCTL_SMARTCARD_DRAW_LCDBMP = FILE_DEVICE_SMARTCARD + 2052 * 4;
        public const long IOCTL_SMARTCARD_DISPLAY_LCD = FILE_DEVICE_SMARTCARD + 2053 * 4;
        public const long IOCTL_SMARTCARD_CLR_LCD = FILE_DEVICE_SMARTCARD + 2054 * 4;
        public const long IOCTL_SMARTCARD_READ_KEYPAD = FILE_DEVICE_SMARTCARD + 2055 * 4;
        public const long IOCTL_SMARTCARD_READ_RTC = FILE_DEVICE_SMARTCARD + 2057 * 4;
        public const long IOCTL_SMARTCARD_SET_RTC = FILE_DEVICE_SMARTCARD + 2058 * 4;
        public const long IOCTL_SMARTCARD_SET_OPTION = FILE_DEVICE_SMARTCARD + 2059 * 4;
        public const long IOCTL_SMARTCARD_SET_LED = FILE_DEVICE_SMARTCARD + 2060 * 4;
        public const long IOCTL_SMARTCARD_LOAD_KEY = FILE_DEVICE_SMARTCARD + 2062 * 4;
        public const long IOCTL_SMARTCARD_READ_EEPROM = FILE_DEVICE_SMARTCARD + 2065 * 4;
        public const long IOCTL_SMARTCARD_WRITE_EEPROM = FILE_DEVICE_SMARTCARD + 2066 * 4;
        public const long IOCTL_SMARTCARD_GET_VERSION = FILE_DEVICE_SMARTCARD + 2067 * 4;
        public const long IOCTL_SMARTCARD_GET_READER_INFO = FILE_DEVICE_SMARTCARD + 2051 * 4;
        public const long IOCTL_SMARTCARD_SET_CARD_TYPE = FILE_DEVICE_SMARTCARD + 2060 * 4;
        public const long IOCTL_SMARTCARD_ACR128_ESCAPE_COMMAND = FILE_DEVICE_SMARTCARD + 2079 * 4;
        #endregion

        #region Return values
        /// <summary>
        /// No error was encountered.
        /// </summary>
        public const uint SCARD_S_SUCCESS = 0x00000000;

        ///<summary> 
        ///An internal consistency check failed.
        /// </summary>
        public const uint SCARD_F_INTERNAL_ERROR = 0x80100001;
        /// <summary>
        /// The action was cancelled by an SCardCancel request.
        /// </summary>
        public const uint SCARD_E_CANCELLED = 0x80100002;
        /// <summary>
        /// The supplied handle was invalid. 
        /// </summary>
        public const uint SCARD_E_INVALID_HANDLE = 0x80100003;
        /// <summary>
        /// One or more of the supplied parameters could not be properly interpreted.
        /// </summary>
        public const uint SCARD_E_INVALID_PARAMETER = 0x80100004;
        /// <summary>
        /// Registry startup information is missing or invalid.
        /// </summary>
        public const uint SCARD_E_INVALID_TARGET = 0x80100005;
        /// <summary>
        /// Not enough memory available to complete this command.
        /// </summary>
        public const uint SCARD_E_NO_MEMORY = 0x80100006;
        /// <summary>
        /// An internal consistency timer has expired.
        /// </summary>
        public const uint SCARD_F_WAITED_TOO_LONG = 0x80100007;
        /// <summary>
        /// The data buffer to receive returned data is too small for the returned data.
        /// </summary>
        public const uint SCARD_E_INSUFFICIENT_BUFFER = 0x80100008;
        /// <summary>
        /// The specified reader name is not recognized.
        /// </summary>
        public const uint SCARD_E_UNKNOWN_READER = 0x80100009;
        /// <summary>
        /// The user-specified timeout value has expired.
        /// </summary>
        public const uint SCARD_E_TIMEOUT = 0x8010000A;
        /// <summary>
        /// The smart card cannot be accessed because of other connections outstanding.
        /// </summary>
        public const uint SCARD_E_SHARING_VIOLATION = 0x8010000B;
        /// <summary>
        /// The operation requires a Smart Card, but no Smart Card is currently in the device.
        /// </summary>
        public const uint SCARD_E_NO_SMARTCARD = 0x8010000C;
        /// <summary>
        /// The specified smart card name is not recognized.
        /// </summary>
        public const uint SCARD_E_UNKNOWN_CARD = 0x8010000D;
        /// <summary>
        /// The system could not dispose of the media in the requested manner.
        /// </summary>
        public const uint SCARD_E_CANT_DISPOSE = 0x8010000E;
        /// <summary>
        /// The requested protocols are incompatible with the protocol currently in use with the smart card.
        /// </summary>
        public const uint SCARD_E_PROTO_MISMATCH = 0x8010000F;
        /// <summary>
        /// The reader or smart card is not ready to accept commands.
        /// </summary>
        public const uint SCARD_E_NOT_READY = 0x80100010;
        /// <summary>
        /// One or more of the supplied parameters values could not be properly interpreted.
        /// </summary>
        public const uint SCARD_E_INVALID_VALUE = 0x80100011;
        /// <summary>
        /// The action was cancelled by the system, presumably to log off or shut down.
        /// </summary>
        public const uint SCARD_E_SYSTEM_CANCELLED = 0x80100012;
        /// <summary>
        /// An internal communications error has been detected.
        /// </summary>
        public const uint SCARD_F_COMM_ERROR = 0x80100013;
        /// <summary>
        /// An internal error has been detected, but the source is unknown.
        /// </summary>
        public const uint SCARD_F_UNKNOWN_ERROR = 0x80100014;
        /// <summary>
        /// An ATR obtained from the registry is not a valid ATR string.
        /// </summary>
        public const uint SCARD_E_INVALID_ATR = 0x80100015;
        /// <summary>
        /// An attempt was made to end a non-existent transaction. 
        /// </summary>
        public const uint SCARD_E_NOT_TRANSACTED = 0x80100016;
        /// <summary>
        /// The specified reader is not currently available for use.
        /// </summary>
        public const uint SCARD_E_READER_UNAVAILABLE = 0x80100017;
        /// <summary>
        /// The operation has been aborted to allow the server application to exit.
        /// </summary>
        public const uint SCARD_P_SHUTDOWN = 0x80100018;
        /// <summary>
        /// The PCI Receive buffer was too small.
        /// </summary>
        public const uint SCARD_E_PCI_TOO_SMALL = 0x80100019;
        /// <summary>
        /// The reader driver does not meet minimal requirements for support.
        /// </summary>
        public const uint SCARD_E_READER_UNSUPPORTED = 0x8010001A;
        /// <summary>
        /// The reader driver did not produce a unique reader name.
        /// </summary>
        public const uint SCARD_E_DUPLICATE_READER = 0x8010001B;
        /// <summary>
        /// The smart card does not meet minimal requirements for support. 
        /// </summary>
        public const uint SCARD_E_CARD_UNSUPPORTED = 0x8010001C;
        /// <summary>
        /// The Smart card resource manager is not running. 
        /// </summary>
        public const uint SCARD_E_NO_SERVICE = 0x8010001D;
        /// <summary>
        ///  The Smart card resource manager has shut down.
        /// </summary>
        public const uint SCARD_E_SERVICE_STOPPED = 0x8010001E;
        /// <summary>
        /// An unexpected card error has occurred. 
        /// </summary>
        public const uint SCARD_E_UNEXPECTED = 0x8010001F;
        /// <summary>
        /// No primary provider can be found for the smart card.
        /// </summary>
        public const uint SCARD_E_ICC_INSTALLATION = 0x80100020;
        /// <summary>
        /// The requested order of object creation is not supported.
        /// </summary>
        public const uint SCARD_E_ICC_CREATEORDER = 0x80100021;
        /// <summary>
        ///  This smart card does not support the requested feature.
        /// </summary>
        public const uint SCARD_E_UNSUPPORTED_FEATURE = 0x80100022;
        /// <summary>
        /// The identified directory does not exist in the smart card.
        /// </summary>
        public const uint SCARD_E_DIR_NOT_FOUND = 0x80100023;
        /// <summary>
        /// The identified file does not exist in the smart card. 
        /// </summary>
        public const uint SCARD_E_FILE_NOT_FOUND = 0x80100024;
        /// <summary>
        /// The supplied path does not represent a smart card directory.
        /// </summary>
        public const uint SCARD_E_NO_DIR = 0x80100025;
        /// <summary>
        /// The supplied path does not represent a smart card file.
        /// </summary>
        public const uint SCARD_E_NO_FILE = 0x80100026;
        /// <summary>
        /// Access is denied to this file.
        /// </summary>
        public const uint SCARD_E_NO_ACCESS = 0x80100027;
        /// <summary>
        ///The smart card does not have enough memory to store the information.
        /// </summary>
        public const uint SCARD_E_WRITE_TOO_MANY = 0x80100028;
        /// <summary>
        /// There was an error trying to set the smart card file object pointer.
        /// </summary>
        public const uint SCARD_E_BAD_SEEK = 0x80100029;
        /// <summary>
        /// The supplied PIN is incorrect.
        /// </summary>
        public const uint SCARD_E_INVALID_CHV = 0x8010002A;
        /// <summary>
        /// An unrecognized error code was returned from a layered component.
        /// </summary>
        public const uint SCARD_E_UNKNOWN_RES_MNG = 0x8010002B;
        /// <summary>
        /// The requested certificate does not exist.
        /// </summary>
        public const uint SCARD_E_NO_SUCH_CERTIFICATE = 0x8010002C;
        /// <summary>
        /// The requested certificate could not be obtained.
        /// </summary>
        public const uint SCARD_E_CERTIFICATE_UNAVAILABLE = 0x8010002D;
        /// <summary>
        /// Cannot find a smart card reader.
        /// </summary>
        public const uint SCARD_E_NO_READERS_AVAILABLE = 0x8010002E;
        /// <summary>
        ///  A communications error with the smart card has been detected. Retry the operation.
        /// </summary>
        public const uint SCARD_E_COMM_DATA_LOST = 0x8010002F;
        /// <summary>
        /// The requested key container does not exist on the smart card.
        /// </summary>
        public const uint SCARD_E_NO_KEY_CONTAINER = 0x80100030;
        /// <summary>
        /// The Smart Card Resource Manager is too busy to complete this operation.
        /// </summary>
        public const uint SCARD_E_SERVER_TOO_BUSY = 0x80100031;
        /// <summary>
        /// The reader cannot communicate with the card, due to ATR string configuration conflicts.
        /// </summary>
        public const uint SCARD_W_UNSUPPORTED_CARD = 0x80100065;
        /// <summary>
        /// The smart card is not responding to a reset.
        /// </summary>
        public const uint SCARD_W_UNRESPONSIVE_CARD = 0x80100066;
        /// <summary>
        /// Power has been removed from the smart card, so that further communication is not possible.
        /// </summary>
        public const uint SCARD_W_UNPOWERED_CARD = 0x80100067;
        /// <summary>
        /// The smart card has been reset, so any shared state information is invalid.
        /// </summary>
        public const uint SCARD_W_RESET_CARD = 0x80100068;
        /// <summary>
        /// The smart card has been removed, so further communication is not possible.
        /// </summary>
        public const uint SCARD_W_REMOVED_CARD = 0x80100069;
        /// <summary>
        /// Access was denied because of a security violation.
        /// </summary>
        public const uint SCARD_W_SECURITY_VIOLATION = 0x8010006A;
        /// <summary>
        /// The card cannot be accessed because the wrong PIN was presented.
        /// </summary>
        public const uint SCARD_W_WRONG_CHV = 0x8010006B;
        /// <summary>
        /// The card cannot be accessed because the maximum number of PIN entry attempts has been reached.
        /// </summary>
        public const uint SCARD_W_CHV_BLOCKED = 0x8010006C;
        /// <summary>
        /// The end of the smart card file has been reached.
        /// </summary>
        public const uint SCARD_W_EOF = 0x8010006D;
        /// <summary>
        /// The user pressed "Cancel" on a Smart Card Selection Dialog.
        /// </summary>
        public const uint SCARD_W_CANCELLED_BY_USER = 0x8010006E;
        /// <summary>
        /// No PIN was presented to the smart card.
        /// </summary>
        public const uint SCARD_W_CARD_NOT_AUTHENTICATED = 0x8010006F;

        #endregion

        #region Protocol
        /// <summary>
        /// There is no active protocol.
        /// </summary>
        public const Int16 SCARD_PROTOCOL_UNDEFINED = 0x0000;

        /// <summary>
        /// T=0 is the active protocol.
        /// </summary>
        public const Int16 SCARD_PROTOCOL_T0 = 0x0001;

        /// <summary>
        /// T=1 is the active protocol.
        /// </summary>
        public const Int16 SCARD_PROTOCOL_T1 = 0x0002;

        /// <summary>
        /// Raw is the active protocol.
        /// </summary>
        public const Int16 SCARD_PROTOCOL_RAW = 0x0004;
        #endregion

        #region Reader state
        /// <summary>
        /// Unknown state
        /// </summary>
        /// <remarks>This value implies the driver is unaware of the current 
        ///state of the reader.
        /// </remarks>
        public const int SCARD_UNKNOWN = 0;

        /// <summary>
        /// Card is absent
        /// </summary>
        /// <remarks>This value implies there is no card in the reader.
        /// </remarks>
        public const int SCARD_ABSENT = 1;

        /// <summary>
        /// Card is present
        /// </summary>
        /// <remarks>This value implies there is a card is present in the reader, 
        ///but that it has not been moved into position for use.
        /// </remarks>
        public const int SCARD_PRESENT = 2;

        /// <summary>
        /// Card not powered
        /// </summary>
        /// <remarks>This value implies there is a card in the reader in position 
        ///for use.  The card is not powered.
        /// </remarks>
        public const int SCARD_SWALLOWED = 3;

        /// <summary>
        /// Card is powered
        /// </summary>
        /// <remarks> This value implies there is power is being provided to the card, 
        ///but the Reader Driver is unaware of the mode of the card.
        /// </remarks>
        public const int SCARD_POWERED = 4;

        /// <summary>
        /// Ready for PTS
        /// </summary>
        /// <remarks> This value implies the card has been reset and is awaiting 
        ///PTS negotiation.
        /// </remarks>
        public const int SCARD_NEGOTIABLE = 5;

        /// <summary>
        /// PTS has been set
        /// </summary>
        /// <remarks>This value implies the card has been reset and specific 
        ///communication protocols have been established.
        /// </remarks>
        public const int SCARD_SPECIFIC = 6;
        #endregion

        /// <summary>
        /// Infinite timeout
        /// </summary>
        public const UInt32 INFINITE = 0xFFFFFFFF;

        #region Imported methods

        [DllImport("winscard.dll")]
        public static extern uint SCardEstablishContext(int dwScope, int pvReserved1, int pvReserved2, ref int phContext);

        [DllImport("winscard.dll")]
        public static extern uint SCardReleaseContext(int phContext);

        [DllImport("winscard.dll")]
        public static extern uint SCardConnect(int hContext, string szReaderName, int dwShareMode, int dwPrefProtocol, ref int phCard, ref int ActiveProtocol);

        [DllImport("winscard.dll")]
        public static extern uint SCardBeginTransaction(int hCard);

        [DllImport("winscard.dll")]
        public static extern uint SCardDisconnect(int hCard, int Disposition);

        [DllImport("winscard.dll")]
        public static extern uint SCardListReaderGroups(int hContext, ref string mzGroups, ref int pcchGroups);

        [DllImport("winscard.DLL", EntryPoint = "SCardListReadersA", CharSet = CharSet.Ansi)]
        public static extern uint SCardListReaders(
            int hContext,
            byte[] Groups,
            byte[] Readers,
            ref int pcchReaders
            );


        [DllImport("winscard.dll")]
        public static extern uint SCardStatus(int hCard, string szReaderName, ref int pcchReaderLen, ref int State, ref int Protocol, ref byte ATR, ref int ATRLen);

        [DllImport("winscard.dll")]
        public static extern uint SCardEndTransaction(int hCard, int Disposition);

        [DllImport("winscard.dll")]
        public static extern uint SCardState(int hCard, ref uint State, ref uint Protocol, ref byte ATR, ref uint ATRLen);

        [DllImport("WinScard.dll")]
        public static extern uint SCardTransmit(IntPtr hCard,
                                               ref SCARD_IO_REQUEST pioSendPci,
                                               ref byte[] pbSendBuffer,
                                               int cbSendLength,
                                               ref SCARD_IO_REQUEST pioRecvPci,
                                               ref byte[] pbRecvBuffer,
                                               ref int pcbRecvLength);

        [DllImport("winscard.dll")]
        public static extern uint SCardTransmit(int hCard, ref SCARD_IO_REQUEST pioSendRequest, ref byte SendBuff, int SendBuffLen, ref SCARD_IO_REQUEST pioRecvRequest, ref byte RecvBuff, ref int RecvBuffLen);

        [DllImport("winscard.dll")]
        public static extern uint SCardTransmit(int hCard, ref SCARD_IO_REQUEST pioSendRequest, ref byte[] SendBuff, int SendBuffLen, ref SCARD_IO_REQUEST pioRecvRequest, ref byte[] RecvBuff, ref int RecvBuffLen);

        [DllImport("winscard.dll")]
        public static extern uint SCardControl(int hCard, uint dwControlCode, ref byte SendBuff, int SendBuffLen, ref byte RecvBuff, int RecvBuffLen, ref int pcbBytesReturned);

        [DllImport("winscard.dll")]
        public static extern uint SCardGetStatusChange(int hContext, UInt32 TimeOut, [In, Out] SCARD_READERSTATE[] ReaderState, int ReaderCount);
        #endregion
        public static string GetScardErrMsg(uint ReturnCode)
        {
            switch (ReturnCode)
            {
                case SCARD_E_CANCELLED:
                    return ("The action was canceled by an SCardCancel request.");
                case SCARD_E_CANT_DISPOSE:
                    return ("The system could not dispose of the media in the requested manner.");
                case SCARD_E_CARD_UNSUPPORTED:
                    return ("The smart card does not meet minimal requirements for support.");
                case SCARD_E_DUPLICATE_READER:
                    return ("The reader driver didn't produce a unique reader name.");
                case SCARD_E_INSUFFICIENT_BUFFER:
                    return ("The data buffer for returned data is too small for the returned data.");
                case SCARD_E_INVALID_ATR:
                    return ("An ATR string obtained from the registry is not a valid ATR string.");
                case SCARD_E_INVALID_HANDLE:
                    return ("The supplied handle was invalid.");
                case SCARD_E_INVALID_PARAMETER:
                    return ("One or more of the supplied parameters could not be properly interpreted.");
                case SCARD_E_INVALID_TARGET:
                    return ("Registry startup information is missing or invalid.");
                case SCARD_E_INVALID_VALUE:
                    return ("One or more of the supplied parameter values could not be properly interpreted.");
                case SCARD_E_NOT_READY:
                    return ("The reader or card is not ready to accept commands.");
                case SCARD_E_NOT_TRANSACTED:
                    return ("An attempt was made to end a non-existent transaction.");
                case SCARD_E_NO_MEMORY:
                    return ("Not enough memory available to complete this command.");
                case SCARD_E_NO_SERVICE:
                    return ("The smart card resource manager is not running.");
                case SCARD_E_NO_SMARTCARD:
                    return ("The operation requires a smart card, but no smart card is currently in the device.");
                case SCARD_E_PCI_TOO_SMALL:
                    return ("The PCI receive buffer was too small.");
                case SCARD_E_PROTO_MISMATCH:
                    return ("The requested protocols are incompatible with the protocol currently in use with the card.");
                case SCARD_E_READER_UNAVAILABLE:
                    return ("The specified reader is not currently available for use.");
                case SCARD_E_READER_UNSUPPORTED:
                    return ("The reader driver does not meet minimal requirements for support.");
                case SCARD_E_SERVICE_STOPPED:
                    return ("The smart card resource manager has shut down.");
                case SCARD_E_SHARING_VIOLATION:
                    return ("The smart card cannot be accessed because of other outstanding connections.");
                case SCARD_E_SYSTEM_CANCELLED:
                    return ("The action was canceled by the system, presumably to log off or shut down.");
                case SCARD_E_TIMEOUT:
                    return ("The user-specified timeout value has expired.");
                case SCARD_E_UNKNOWN_CARD:
                    return ("The specified smart card name is not recognized.");
                case SCARD_E_UNKNOWN_READER:
                    return ("The specified reader name is not recognized.");
                case SCARD_F_COMM_ERROR:
                    return ("An internal communications error has been detected.");
                case SCARD_F_INTERNAL_ERROR:
                    return ("An internal consistency check failed.");
                case SCARD_F_UNKNOWN_ERROR:
                    return ("An internal error has been detected, but the source is unknown.");
                case SCARD_F_WAITED_TOO_LONG:
                    return ("An internal consistency timer has expired.");
                case SCARD_S_SUCCESS:
                    return ("OK");
                case SCARD_W_REMOVED_CARD:
                    return ("The smart card has been removed, so that further communication is not possible.");
                case SCARD_W_RESET_CARD:
                    return ("The smart card has been reset, so any shared state information is invalid.");
                case SCARD_W_UNPOWERED_CARD:
                    return ("Power has been removed from the smart card, so that further communication is not possible.");
                case SCARD_W_UNRESPONSIVE_CARD:
                    return ("The smart card is not responding to a reset.");
                case SCARD_W_UNSUPPORTED_CARD:
                    return ("The reader cannot communicate with the card, due to ATR string configuration conflicts.");
                default:
                    return ("?");
            }
        }
    }
}
