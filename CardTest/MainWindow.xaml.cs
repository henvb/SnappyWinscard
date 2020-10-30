using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CardTest
{
    public partial class MainWindow : Window
    {
        int retCode;
        int hCard;
        int hContext;
        int Protocol;
        public bool connActive = false;
        string readername = "ACS ACR122 0";      // change depending on reader
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public Card.SCARD_READERSTATE RdrState;
        public Card.SCARD_IO_REQUEST pioSendRequest;

        public MainWindow()
        {
            InitializeComponent();
            SelectDevice();
            establishContext();
        }

        public void SelectDevice()
        {
            List<string> availableReaders = this.ListReaders();
            this.RdrState = new Card.SCARD_READERSTATE();
            readername = availableReaders[0].ToString();//selecting first device
            this.RdrState.RdrName = readername;
        }

        public List<string> ListReaders()
        {
            int ReaderCount = 0;
            List<string> AvailableReaderList = new List<string>();

            //Make sure a context has been established before 
            //retrieving the list of smartcard readers.
            retCode = Card.SCardListReaders(hContext, null, null, ref ReaderCount);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                MessageBox.Show(Card.GetScardErrMsg(retCode));
                //connActive = false;
            }

            byte[] ReadersList = new byte[ReaderCount];

            //Get the list of reader present again but this time add sReaderGroup, retData as 2rd & 3rd parameter respectively.
            retCode = Card.SCardListReaders(hContext, null, ReadersList, ref ReaderCount);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                MessageBox.Show(Card.GetScardErrMsg(retCode));
            }

            string rName = "";
            int indx = 0;
            if (ReaderCount > 0)
            {
                // Convert reader buffer to string
                while (ReadersList[indx] != 0)
                {

                    while (ReadersList[indx] != 0)
                    {
                        rName = rName + (char)ReadersList[indx];
                        indx = indx + 1;
                    }

                    //Add reader name to list
                    AvailableReaderList.Add(rName);
                    rName = "";
                    indx = indx + 1;

                }
            }
            return AvailableReaderList;

        }

        internal void establishContext()
        {
            retCode = Card.SCardEstablishContext(Card.SCARD_SCOPE_SYSTEM, 0, 0, ref hContext);
            if (retCode != Card.SCARD_S_SUCCESS)
            {
                MessageBox.Show("Check your device and please restart again", "Reader not connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                connActive = false;
                return;
            }
        }

        private void buttonGetUid_Click(object sender, RoutedEventArgs e)
        {
            if (connectCard())
            {
                string cardUID = getcardUID();
                textBlockStatus.Text = $"Card-UID: {cardUID}"; //displaying on text block
            }
        }

        public bool connectCard()
        {
            connActive = true;

            retCode = Card.SCardConnect(hContext, readername, Card.SCARD_SHARE_SHARED,
                      Card.SCARD_PROTOCOL_T0 | Card.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                MessageBox.Show(Card.GetScardErrMsg(retCode), "Card not available", MessageBoxButton.OK, MessageBoxImage.Error);
                connActive = false;
                return false;
            }
            return true;
        }

        private string getcardUID()//only for mifare 1k cards
        {
            string cardUID = "";
            byte[] receivedUID = new byte[256];
            Card.SCARD_IO_REQUEST request = new Card.SCARD_IO_REQUEST();
            request.dwProtocol = Card.SCARD_PROTOCOL_T1;
            request.cbPciLength = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Card.SCARD_IO_REQUEST));
            byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 }; //get UID command      for Mifare cards
            int outBytes = receivedUID.Length;
            int status = Card.SCardTransmit(hCard, ref request, ref sendBytes[0], sendBytes.Length, ref request, ref receivedUID[0], ref outBytes);

            if (status != Card.SCARD_S_SUCCESS)
            {
                cardUID = "Error";
            }
            else
            {
                cardUID = BitConverter.ToString(receivedUID.Take(4).ToArray()).Replace("-", string.Empty).ToLower();
            }

            return cardUID;
        }

        private byte[] Key
        {
            get
            {
                return GetBytes(toggleHexKey.IsChecked == true, textBoxKey, 6);
            }
        }

        private byte Block
        {
            get
            {
                if (byte.TryParse(textBoxBlock.Text, out var b)
                    && b > 0
                    && b <= 63)
                    return b;
                textBlockSubStatus.Text = "Please enter a Block No from 0 to 63";
                throw new ArgumentException();
            }
        }

        private byte KeyType => toggleKeyTypeB.IsChecked == true ? (byte)0x61 : (byte)0x60;
        private byte KeySlot => toggleKeySlot1.IsChecked == true ? (byte)0x1 : (byte)0x0;

        private byte[] Data
        {
            get
            {
                return GetBytes(toggleHexData.IsChecked == true, textBoxData, 16);
            }

            set
            {
                SetBytes(value, toggleHexData.IsChecked == true ? TextFormat.Hex : TextFormat.Normal, textBoxData);
            }
        }

        private enum TextFormat { Hex, Normal, Stretched }

        private void SetBytes(byte[] bytes, TextFormat textFormat, TextBox textBox)
        {
            textBox.Text = bytes.Aggregate(
                "",
                (s, b) =>
              {
                  switch (textFormat)
                  {
                      case TextFormat.Hex:
                          return $"{s}{b:X2} ";
                      case TextFormat.Stretched:
                          return $"{s}{(char)b}  ";
                      case TextFormat.Normal:
                          return $"{s}{(char)b}";
                  }
                  throw new ArgumentException();
              });
        }

        public byte[] DataRead { get ; set; }

        private byte[] GetBytes(bool isChecked, TextBox textBox, int length)
        {
            textBlockStatus.Text = "";
            if (isChecked)
            {
                var words = textBox.Text.Split(' ');
                var writeBack = false;
                if (words.Length > length)
                {
                    words = words
                        .Take(length)
                        .ToArray();
                    writeBack = true;
                }
                else if (words.Length < length)
                {
                    words = words
                        .Union(
                            Enumerable.Repeat("00", length - words.Length)
                        )
                        .ToArray();
                    writeBack = true;
                }
                var bytes = new byte[length];
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];
                    if (byte.TryParse(word, NumberStyles.HexNumber, null, out byte result))
                    {
                        bytes[i] = result;
                    }
                    else
                    {
                        textBlockStatus.Text = $"Cannot parse word {i} ({word}).";
                        return null;
                    }
                }
                if (writeBack)
                {
                    textBox.Text = string.Join(" ", words);
                }
                return bytes;
            }
            else
            {
                var text = textBox.Text;
                if (text.Length < length)
                {
                    text = text + new string('\0', length - text.Length);
                }
                else if (text.Length > length)
                {
                    text = text.Substring(0, length);
                    textBox.Text = text;
                }
                return text
                    .ToCharArray()
                    .Select(c => (byte)c)
                    .ToArray();
            }
        }

        private void buttonRead_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                verifyCard();
            }
            catch(Exception ex)
            {
                textBlockStatus.Text = ex.Message;
            }
        }

        public void verifyCard()
        {
            string value = "";
            if (connectCard())
            {
                DataRead = readBlock() ?? new byte[0];
                SetBytes(DataRead, TextFormat.Hex, textBoxHex);
                SetBytes(DataRead, TextFormat.Stretched, textBoxText);
            }
        }

        public byte[] readBlock()
        {
            string tmpStr = "";
            int indx;

            string status;
            string sw = "";
            byte[] retBuff = null;
            if (authenticateBlock())
            {
                ClearBuffers();
                SendBuff[0] = 0xFF; // CLA 
                SendBuff[1] = 0xB0;// INS
                SendBuff[2] = 0x00;// P1
                SendBuff[3] = Block;// P2 : Block No.
                SendBuff[4] = 16;// Le

                SendLen = 5;
                RecvLen = SendBuff[4] + 2;

                retCode = SendAPDUandDisplay(2);

                switch (retCode)
                {
                    case -200:
                        status = "outofrangeexception";
                        break;
                    case -202:
                        status = "BytesNotAcceptable";
                        break;
                    case Card.SCARD_S_SUCCESS:
                        {
                            status = "OK";
                            int RecvLen1 = RecvLen - 2;
                            retBuff = RecvBuff.Take(RecvLen1).ToArray();
                            var swInt = RecvBuff[RecvLen1] * 0x100+ RecvBuff[RecvLen1 + 1];
                           
                            switch(swInt)
                            {
                                case 0x9000:
                                    sw = "Success";
                                    break;
                                case 0x6300:
                                    sw = "Failed";
                                    break;
                                case 0x6a81:
                                    sw = "Not supported";
                                    break;
                                default:
                                    sw = "Unexpected";
                                    break;
                            }
                            break;
                        }
                    default:
                        status = "FailRead";
                        break;
                }
            }
            else
            {
                status = "FailAuthentication";
            }
            textBlockStatus.Text = status;
            textBlockSubStatus.Text = sw;
            return retBuff;
        }

        private void buttonWrite_Click(object sender, RoutedEventArgs e)
        {
            if (connectCard())// establish connection to the card: you've declared this from previous post
            {
                writeBlock();
                Close();
            }
        }

        private void buttonStoreKey_Click(object sender, RoutedEventArgs e)
        {
            storeKey();
        }

        private void storeKey()
        {
            var k = Key;
            if (k == null)
                return;
            ClearBuffers();
            SendBuff[0] = 0xFF;                             // CLA
            SendBuff[1] = 0x82;                             // INS
            SendBuff[2] = 0x00;                             // P1: Key Structure - to memory
            SendBuff[3] = KeySlot;                          // P2: Key slot number
            SendBuff[4] = 0x06;                             // Lc: Data length
            k.CopyTo(SendBuff, 5);

            SendLen = SendBuff[4] + 5;
            RecvLen = 2;

            retCode = SendAPDUandDisplay(2);

            switch (retCode)
            {
                case Card.SCARD_S_SUCCESS:
                    textBlockStatus.Text = "OK";
                    var swInt = RecvBuff[0] * 0x100 + RecvBuff[1];
                    string sw;
                    switch (swInt)
                    {
                        case 0x9000:
                            sw = "";
                            break;
                        case 0x6300:
                            sw = "Operation failed";
                            break;
                        default:
                            sw = "Failed to load key";
                            break;
                    }
                    textBlockSubStatus.Text = sw;
                    break;
                default:
                    textBlockStatus.Text = "Error";
                    break;
            }
        }

        private void toggleHex_Click(object sender, RoutedEventArgs e)
        {
            var isKey = sender == this.toggleHexKey;

            TextBox textBox = 
                isKey
                    ? textBoxKey
                    : textBoxData;
            var toggle =
                isKey
                    ? toggleHexKey
                    : toggleHexData;

            var wasHex = toggle.IsChecked == false;
            var data = GetBytes(
                wasHex,
                textBox,
                isKey
                    ? 6
                    : 16);
            if (data == null)
            {
                toggle.IsChecked = wasHex;
                return;
            }
            if (wasHex)
            {
                var chars = data.Select(b => (char)b).ToArray();
                textBox.Text = new string(chars);
            }
            else
            {
                var words = data.Select(b => b.ToString("X2"));
                textBox.Text = string.Join(" ", words);
            }
        }

        private void toggleKeyTypeB_Click(object sender, RoutedEventArgs e)
        {
            toggleKeyTypeB.Content =
                toggleKeyTypeB.IsChecked == true
                ? "Key Type B"
                : "Key Type A";
        }

        private void textBoxData_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Data = DataRead;
        }

        private void buttonCheck_Click(object sender, RoutedEventArgs e)
        {

        }

        //submit data method
        public void writeBlock()
        {

            int indx;
            string status;
            string sw = "";
            var data = Data;
            if (data == null)
                return;
            if (authenticateBlock())
            {
                ClearBuffers();
                SendBuff[0] = 0xFF;                             // Class
                SendBuff[1] = 0xD6;                             // INS
                SendBuff[2] = 0x00;                             // P1
                SendBuff[3] = Block;           // P2 : Starting Block No.
                SendBuff[4] = 16;            // P3 : Data length
                    
                data.CopyTo(SendBuff, 5);

                SendLen = SendBuff[4] + 5;
                RecvLen = 0x02;

                retCode = SendAPDUandDisplay(2);

                if (retCode == Card.SCARD_S_SUCCESS)
                {
                    status = "OK";
                    var swInt = RecvBuff[0] * 0x100 + RecvBuff[1];
                    switch (swInt)
                    {
                        case 0x9000:
                            sw = "Success";
                            break;
                        case 0x6300:
                            sw = "Failed";
                            break;
                        case 0x6a81:
                            sw = "Not supported";
                            break;
                        default:
                            sw = "Unexpected";
                            break;
                    }
                }
                else
                {
                    sw = "fail write";

                }
                status = "OK";
            }
            else
            {
                status = "FailAuthentication";
            }
            textBlockStatus.Text = status;
            textBlockSubStatus.Text = sw;
        }

        // block authentication
        private bool authenticateBlock()
        {
            ClearBuffers();
            SendBuff[0] = 0xFF;        // CLA
            SendBuff[1] = 0x86;        // INS: Authentication
            SendBuff[2] = 0x00;        // P1 
            SendBuff[3] = 0x00;        // P2: Memory location;  P2: for stored key input
            SendBuff[4] = 0x05;        // Lc
                                       // Authenticate Data Bytes
            SendBuff[5] = 0x01;        // Byte 1: Version
            SendBuff[6] = 0x00;        // Byte 2
            SendBuff[7] = Block;       // Byte 3: Block number
            SendBuff[8] = KeyType;     // Byte 4: Key type
            SendBuff[9] = KeySlot;     // Byte 5: Key number

            SendLen = 10;
            RecvLen = 2;

            retCode = SendAPDUandDisplay(0);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                //MessageBox.Show("FAIL Authentication!");
                return false;
            }

            return true;
        }

        // clear memory buffers
        private void ClearBuffers()
        {
            long indx;

            for (indx = 0; indx <= 262; indx++)
            {
                RecvBuff[indx] = 0;
                SendBuff[indx] = 0;
            }
        }

        // send application protocol data unit : communication unit between a smart card reader and a smart card
        private int SendAPDUandDisplay(int reqType)
        {
            int indx;
            string tmpStr = "";

            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            retCode = Card.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0],
                                 SendLen, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                return retCode;
            }

            else
            {
                try
                {
                    tmpStr = "";
                    switch (reqType)
                    {
                        case 0:
                            for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            if ((tmpStr).Trim() != "90 00")
                            {
                                //MessageBox.Show("Return bytes are not acceptable.");
                                return -202;
                            }

                            break;

                        case 1:

                            for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                            {
                                tmpStr = tmpStr + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            if (tmpStr.Trim() != "90 00")
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }

                            else
                            {
                                tmpStr = "ATR : ";
                                for (indx = 0; indx <= (RecvLen - 3); indx++)
                                {
                                    tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                                }
                            }

                            break;

                        case 2:

                            break;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    return -200;
                }
            }
            return retCode;
        }

        //disconnect card reader connection
        public void Close()
        {
            if (connActive)
            {
                retCode = Card.SCardDisconnect(hCard, Card.SCARD_UNPOWER_CARD);
            }
            //retCode = Card.SCardReleaseContext(hCard);
        }

    }


}