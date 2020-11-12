using HanseCom.WtkControl.CardManager;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CardTest
{
    public partial class MainWindow : Window
    {
        public bool connActive = false;
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public Card.SCARD_READERSTATE RdrState;
        public Card.SCARD_IO_REQUEST pioSendRequest;
        private CardIo cardIo;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonGetUid_Click(object sender, RoutedEventArgs e)
        {
            if (connectCard1())
            {
                string cardUID = cardIo.getcardUID();
                textBlockStatus.Text = $"Card-UID: {cardUID}"; //displaying on text block
            }
        }

        private bool connectCard1()
        {
            if (cardIo == null)
            {
                cardIo = new CardIo();
                cardIo.ReaderStateChanged += CardIo_ReaderStateChanged;
            }
            if (!cardIo.connectCard())
            {
                if (Dispatcher.CheckAccess())
                {
                    textBlockStatus.Text = cardIo.StatusText;
                }
                else
                {
                    Dispatcher.Invoke(() => textBlockStatus.Text = cardIo.StatusText);
                }
                return false;
            }
            return true;
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
            if (connectCard1())
            {
                DataRead = readBlock() ?? new byte[0];
                SetBytes(DataRead, TextFormat.Hex, textBoxHex);
                SetBytes(DataRead, TextFormat.Stretched, textBoxText);
            }
        }

        public byte[] readBlock()
        {
            var bytes = cardIo.ReadCardBlock(Block, KeyType, KeySlot);
            textBlockStatus.Text = cardIo.StatusText;
            textBlockSubStatus.Text = cardIo.SubStatusText;
            return bytes;
        }

        private void buttonWrite_Click(object sender, RoutedEventArgs e)
        {
            if (connectCard1())// establish connection to the card: you've declared this from previous post
            {
                writeBlock();
            }
        }

        private void writeBlock()
        {
            cardIo.WriteCardBlock(Data, Block,KeyType,KeySlot);
            textBlockStatus.Text = cardIo.StatusText;
            textBlockSubStatus.Text = cardIo.SubStatusText;
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
            cardIo.StoreKey(k, KeySlot);
            textBlockStatus.Text = cardIo.StatusText;
            textBlockSubStatus.Text = cardIo.SubStatusText;
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

        private async void buttonGetReaderState_Click(object sender, RoutedEventArgs e)
        {
            buttonGetReaderState.Content = "⌛";
            buttonGetReaderState.InvalidateVisual();
            await Task.Run(() => GetReaderStatus());
            buttonGetReaderState.Content = "Query Status";
        }

        private void GetReaderStatus()
        {
            connectCard1();
            cardIo.HandleCardStatus();
        }

        private void CardIo_ReaderStateChanged(CardIo.ReaderState readerState)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => CardIo_ReaderStateChanged(readerState));
                return;
            }
            textBlockReaderState.Text = readerState.ToString();
        }

        private void toggleKeySlot1_Click(object sender, RoutedEventArgs e)
        {
            toggleKeySlot1.Content =
                toggleKeySlot1.IsChecked == true
                ? "Key Slot 1"
                : "Key Slot 0";
        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Data = DataRead;
        }

        private void buttonCheck_Click(object sender, RoutedEventArgs e)
        {

        }

        //disconnect card reader connection
        public void Close()
        {
            //if (connActive)
            //{
            //    retCode = Card.SCardDisconnect(hCard, Card.SCARD_UNPOWER_CARD);
            //}
            //retCode = Card.SCardReleaseContext(hCard);
        }

    }


}