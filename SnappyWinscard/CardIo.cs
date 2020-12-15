using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SnappyWinscard
{
    public class CardIo : INotifyPropertyChanged
    {
        int CurrentState;
        private uint retCode;
        private int swInt;
        const int SwOk = 0x9000;
        const int SwUnknown = -1;
        const int SwNoContext = -2;
        private ReaderState currentReaderState;
        private int hContext;
        private int hCard;
        private int Protocol;
        Winscard.SCARD_IO_REQUEST pioSendRequest;
        private string currentDevice;

        public CardIo()
        {
            Initialize();

            Task.Run(() => HandleCardStatus());
        }

        public ReaderState CurrentReaderState
        {
            get
            {
                return currentReaderState;
            }

            set
            {
                if (currentReaderState == value)
                    return;
                currentReaderState = value;
                ReaderStateChanged?.Invoke(value);
                NotifyPropertyChanged();
            }
        }

        public List<string> ListReaders()
        {
            int ReaderCount = 0;
            List<string> availableReaderList = new List<string>();

            //Make sure a context has been established before 
            //retrieving the list of smartcard readers.
            retCode = Winscard.SCardListReaders(hContext, null, null, ref ReaderCount);
            if (retCode != Winscard.SCARD_S_SUCCESS)
            {
                return null;
            }

            byte[] ReadersList = new byte[ReaderCount];

            //Get the list of reader present again but this time add sReaderGroup, retData as 2rd & 3rd parameter respectively.
            retCode = Winscard.SCardListReaders(hContext, null, ReadersList, ref ReaderCount);
            if (retCode != Winscard.SCARD_S_SUCCESS)
            {
                return null;
            }

            string rName = "";
            int indx = 0;
            if (ReaderCount <= 0)
            {
                return null;
            }
            // Convert reader buffer to string
            while (ReadersList[indx] != 0)
            {

                while (ReadersList[indx] != 0)
                {
                    rName += (char)ReadersList[indx];
                    indx += 1;
                }

                //Add reader name to list
                availableReaderList.Add(rName);
                rName = "";
                indx += 1;
            }
            return availableReaderList;

        }

        private void Initialize()
        {
            CurrentState = Winscard.SCARD_STATE_UNAWARE;
            // Connect
            pioSendRequest.dwProtocol = 0;
            pioSendRequest.cbPciLength = 8;
            retCode = Winscard.SCardEstablishContext(Winscard.SCARD_SCOPE_SYSTEM, 0, 0, ref hContext);
            if (retCode != Winscard.SCARD_S_SUCCESS)
            {
                swInt = SwNoContext;
            }
            else
                SelectDevice();
        }
        public bool ConnectCard()
        {

            retCode = Winscard.SCardConnect(hContext, currentDevice, Winscard.SCARD_SHARE_SHARED,
                      Winscard.SCARD_PROTOCOL_T0 | Winscard.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);

            swInt = 0;
            switch (retCode)
            {
                case Winscard.SCARD_S_SUCCESS:
                    break;
                case Winscard.SCARD_E_SERVICE_STOPPED:
                    Initialize();
                    goto default;
                default:
                    return false;
            }
            return true;
        }

        public void SelectDevice()
        {
            List<string> devices1 = ListReaders();
            Devices.Clear();
            if (devices1 == null)
                return;
            devices1.ForEach(d => Devices.Add(d));
            if (!devices1.Contains(currentDevice))
            {
                CurrentDevice =
                    devices1?[0].ToString(); // Select first device
            }
        }

        public ObservableCollection<string> Devices { get; } = new ObservableCollection<string>();

        public string CurrentDevice
        {
            get => currentDevice;
            set
            {
                currentDevice = value;
                NotifyPropertyChanged();
            }
        }

        private string ReaderName
        {
            get
            {
                if (currentDevice == null)
                    return Winscard.READER_NAME_NEW;
                else
                    return currentDevice;
            }
        }


        public string GetCardUID()//only for mifare 1k cards
        {
            if (ConnectCard())
            {
                string cardUID = "";
                byte[] receivedUID = new byte[256];
                Winscard.SCARD_IO_REQUEST request = new Winscard.SCARD_IO_REQUEST
                {
                    dwProtocol = Winscard.SCARD_PROTOCOL_T1,
                    cbPciLength = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Winscard.SCARD_IO_REQUEST))
                };
                byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 }; //get UID command      for Mifare cards
                int outBytes = receivedUID.Length;
                if (SCardTransmit(sendBytes, receivedUID, ref request, ref outBytes))
                {
                    cardUID = receivedUID
                        .Take(4)
                        .Aggregate(
                            "",
                            (a, b)
                                => a += b.ToString("x2"));
                }
                else
                {
                    cardUID = "Error";
                }

                return cardUID;
            }
            return null;
        }


        public string StatusText => Winscard.GetScardErrMsg(retCode);
        public string SubStatusText
        {
            get
            {
                switch (swInt)
                {
                    case 0x9000:
                        return "Success";
                    case 0x6300:
                        return "Failed";
                    case 0x6a81:
                        return "Not supported";
                    default:
                        return "Unexpected";
                }
            }
        }

        public void StoreKey(byte[] key, byte keySlot)
        {
            const byte keyLength = 6;
            if (key.Length != keyLength)
                throw new ArgumentException("Key must be 6 bytes long", nameof(key));
            var SendLen = 5 + keyLength;
            byte[] SendBuff = new byte[SendLen];
            SendBuff[0] = 0xFF;                             // CLA
            SendBuff[1] = 0x82;                             // INS
            SendBuff[2] = 0x00;                             // P1: Key Structure - to memory
            SendBuff[3] = keySlot;                          // P2: Key slot number
            SendBuff[4] = keyLength;                             // Lc: Data length
            key.CopyTo(SendBuff, 5);

            var RecvLen = 2;
            byte[] RecvBuff = new byte[RecvLen];

            SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen);
        }


        public bool AuthenticateBlock(byte Block, byte KeyType, byte KeySlot)
        {
            byte[] SendBuff = new byte[] {
                0xFF,        // CLA
                0x86,        // INS: Authentication
                0x00,        // P1 
                0x00,        // P2: Memory location,  P2: for stored key input
                0x05,        // Lc
                                           // Authenticate Data Bytes
                0x01,        // Byte 1: Version
                0x00,        // Byte 2
                Block,       // Byte 3: Block number
                KeyType,     // Byte 4: Key type
                KeySlot,     // Byte 5: Key number
            };

            byte[] RecvBuff = new byte[2];

            int RecvLen = RecvBuff.Length;

            return SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen);
        }

        private bool SCardTransmit(byte[] SendBuff, byte[] RecvBuff, ref Winscard.SCARD_IO_REQUEST pioSendRequest, ref int RecvLen)
        {
            retCode = Winscard.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0],
                                 SendBuff.Length, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);
            ReadSw(RecvBuff, RecvLen - 2);
            return retCode == Winscard.SCARD_S_SUCCESS;
        }

        private void ReadSw(byte[] buff, int i)
        {
            if (buff.Length - i < 2)
            {
                swInt = SwUnknown;
            }
            else
            {
                swInt = buff[i] * 0x100 + buff[i + 1];
            }
        }

        public byte[] ReadCardBlock(byte block, byte keyType, byte keySlot)
        {
            if (AuthenticateBlock(block, keyType, keySlot))
            {
                return ReadCardBlock1(block);
            }
            return null;
        }

        private byte[] ReadCardBlock1(byte block)
        {
            const byte blockSize = 16;
            byte[] SendBuff = new byte[5];
            SendBuff[0] = 0xFF; // CLA 
            SendBuff[1] = 0xB0;// INS
            SendBuff[2] = 0x00;// P1
            SendBuff[3] = block;// P2 : Block No.
            SendBuff[4] = blockSize;// Le

            var RecvLen = blockSize + 2;
            byte[] RecvBuff = new byte[RecvLen];


            if (SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen))
            {
                if (swInt == SwOk)
                {
                    return RecvBuff.Take(RecvLen - 2).ToArray();
                }
            }
            return null;
        }

        public void WriteCardBlock(byte[] data, byte block, byte keyType, byte keySlot)
        {

            if (data == null)
                return;
            if (AuthenticateBlock(block, keyType, keySlot))
            {
                var SendLen = 5 + (byte)data.Length;
                byte[] SendBuff = new byte[SendLen];
                SendBuff[0] = 0xFF;                             // Class
                SendBuff[1] = 0xD6;                             // INS
                SendBuff[2] = 0x00;                             // P1
                SendBuff[3] = block;           // P2 : Starting Block No.
                SendBuff[4] = (byte)data.Length;            // P3 : Data length

                data.CopyTo(SendBuff, 5);

                var RecvLen = 0x02;
                byte[] RecvBuff = new byte[RecvLen];
                SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen);
            }
        }

        public enum ReaderState { Unavailable, NoCard, CardReady }
        public event Action<ReaderState> ReaderStateChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void HandleCardStatus()
        {
            for (; ; )
            {
                var rs = new Winscard.SCARD_READERSTATE[1];
                rs[0].RdrName = ReaderName;
                rs[0].RdrCurrState = CurrentState;

                var x = Winscard.SCardGetStatusChange(hContext, Winscard.INFINITE, rs, 1);
                switch (x)
                {
                    case Winscard.SCARD_S_SUCCESS:
                        if (ReaderName == Winscard.READER_NAME_NEW)
                        {
                            SelectDevice();
                        }
                        else
                        {
                            var isPresent = (rs[0].RdrEventState & Winscard.SCARD_STATE_PRESENT) != 0;
                            var isUnavailable = (rs[0].RdrEventState & Winscard.SCARD_STATE_UNAVAILABLE) != 0;
                            ReaderState readerState;
                            if (isUnavailable)
                            {
                                readerState = ReaderState.Unavailable;
                            }
                            else if (isPresent)
                            {
                                readerState = ReaderState.CardReady;
                            }
                            else
                            {
                                readerState = ReaderState.NoCard;
                            }
                            CurrentReaderState = readerState;
                            CurrentState = rs[0].RdrEventState;
                        }
                        break;

                    case Winscard.SCARD_E_SERVICE_STOPPED:
                        Initialize();
                        break;

                    default:
                        ;
                        break;
                }
            }
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
