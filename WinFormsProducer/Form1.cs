
using MessageProtocolLibrary;
using OpenCvSharp;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WinFormsProducer
{
  

    public partial class Form1 : Form
    {
        private static IPEndPoint consumerEndPoint;
        private static UdpClient udpClient = new UdpClient();
        private static string keyBase64;// = "/642GxdGe8ZZRAZ/2eHbOZz44R4VJ5gb2JIB2sku4uY="
        private static string IVBase64;// = "LlNfGMPmyzPITfv8iWn6Hw=="
        private static byte[] keyByte;
        private static byte[] IVByte;
        VideoReader? videoSource;
        ScreenReader? screenSource;
        public Form1()
        {
            InitializeComponent();
            InitializeKey();
           

            var host = Dns.GetHostEntry(Dns.GetHostName());
            comboBox1.Items.Clear();

            foreach (var item in host.AddressList.Where(ipa=>ipa.AddressFamily==AddressFamily.InterNetwork))
            {
                string adress = item.ToString();
                comboBox1.Items.Add(adress);
         //   .Select(x => x.GetAddressBytes());

            }

        }

        
        public void GenerateKey()
        {

            using Aes aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            keyByte = aes.Key;
            IVByte = aes.IV;
            keyBase64 = Convert.ToBase64String(keyByte);
            IVBase64 = Convert.ToBase64String(IVByte);

           // string[] keys = new string[2];
           // keys[0] = $"KeyBase64: {keyBase64}";
           // keys[1] = $"IVBase64: {keyBase64}";
           //await File.WriteAllLinesAsync("Keys.txt", keys);


            string[] keys = new string[2];
            keys[0] = $"KeyBase64: {keyBase64}";
            keys[1] = $"IVBase64: {IVBase64}";
             File.WriteAllLines("Keys.txt", keys);
        }

        public void InitializeKey()
        {

            try
            {
                if (File.Exists("RegisKeys.txt"))
                {

                    var keys = File.ReadAllLines("RegisKeys.txt");
                    if (keys.Length > 0)
                    {
                        if (keys[0].Contains("KeyBase64:") && keys[1].Contains("IVBase64:"))
                        {

                            keyBase64 = keys[0].Split(": ")[1];
                            IVBase64 = keys[1].Split(": ")[1];

                            if (!string.IsNullOrEmpty(keyBase64)&& !string.IsNullOrEmpty(IVBase64))
                            {

                                keyByte = Convert.FromBase64String(keyBase64);
                                IVByte = Convert.FromBase64String(IVBase64);
                                return;
                            }
                        }
                    }

                }
                //ãåíåíðèðóåì êëþ÷ åñëè íåòó

            }
            catch (Exception ex)
            {
                MessageBox.Show($"InitializeKey Âîçíèêëè îøèáêè ïðè ïîëó÷åíèè êëþ÷åé \r\n {ex.Message}");
            }

            GenerateKey();
        }


        public void stopStream()
        {

            checkBox1.Enabled = true;
            if (videoSource != null)
            {
                
                videoSource.Stop();
                videoSource = null;

            }
            if (screenSource != null)
            {
                screenSource.Stop();
                screenSource = null;

            }
        }

        //äëÿ êàìåðû
        private void StartStreamVideoDevice()
        {
            if (videoSource == null)
            {
                var consumrIp = comboBox1.Text;//ConfigurationManager.AppSettings.Get("consumerIp");
                var consumrPort = int.Parse(numericUpDown1.Value.ToString());//int.Parse(ConfigurationManager.AppSettings.Get("consumerPort"));

                consumerEndPoint = new IPEndPoint(IPAddress.Parse(consumrIp), consumrPort);


                var capture = new VideoCapture(0);
                if (capture!=null)
                {


                    videoSource = new VideoReader(capture);
                    videoSource.NewFrame += VideoSource_NewFrame;
                    videoSource.Start();
                }
                else
                {
                    MessageBox.Show("Êàìåð íà óñòðîéñòâå íå îáíàðóæåíî");
                    stopStream();
                }
            }
            else
            {
                MessageBox.Show("StartStreamVideoDevice Äëÿ íà÷àëà îñòàíîâèòå òðàíñëÿöèþ");
            }

        }
        //äëÿ ýêðàíà
        private void StartStreamScreenDevice()
        {
            try
            {
                if (screenSource == null)
                {
                    var consumrIp = comboBox1.Text;
                    var consumrPort = int.Parse(numericUpDown1.Value.ToString());

                    consumerEndPoint = new IPEndPoint(IPAddress.Parse(consumrIp), consumrPort);

                    screenSource = new ScreenReader();
                    screenSource.NewFrame += VideoSource_NewFrame;
                    screenSource.Start();


                }
                else
                {
                    MessageBox.Show("Äëÿ íà÷àëà îñòàíîâèòå òðàíñëÿöèþ");
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show($"StartStreamScreenDevice Ïðîèçîøëà îøèáêà: \r\n {ex.Message}");
            }
            
        }
        private void button1_Click(object sender, EventArgs e)
        {

            checkBox1.Enabled = false;
            if (checkBox1.Checked)
            {
                StartStreamScreenDevice();
            }
            else
            {

                StartStreamVideoDevice();
            }
            // Console.ReadKey();


        }

        private void button2_Click(object sender, EventArgs e)
        {
            stopStream();
        }
        public static byte[] Encrypt_Aes(byte[] buffer, byte[] Key, byte[] IV)
        {
            if (buffer == null || buffer.Length <= 0)
                throw new ArgumentNullException(nameof(buffer));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(buffer, 0, buffer.Length);
                        csEncrypt.FlushFinalBlock(); 
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }


        private   void VideoSource_NewFrame(Bitmap frame)
        {
            //Bitmap test = eventArgs.Frame;
            var bmp = new Bitmap(frame, 1500, 1200);//800,600

            try
            {
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    var bytes = ms.ToArray();

                    byte[] buffer = bytes;
                    if (checkBox2.Checked)
                    {
                        buffer = Encrypt_Aes(bytes, keyByte, IVByte);
                    }

                    var register = new RegisterFiles();
                    register.CreateMessage(buffer);


                    while (register.PackMessage.Count>0)
                    {
                        var messageBuffer= register.PackMessage.Dequeue();
                        if (messageBuffer != null)
                        {
                            if (messageBuffer.Length < 65500)
                            {
                                udpClient.Send(messageBuffer, messageBuffer.Length, consumerEndPoint);
                            }
                            else
                            {

                                MessageBox.Show($"VideoSource_NewFrame Âîçíèêëè íå ïðåäâèäåííûå îøèáêè ");
                            }
                                
                        }

                    }
                    



                    //  if (buffer.Length < 65500) udpClient.Send(buffer, buffer.Length, consumerEndPoint);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"VideoSource_NewFrame Âîçíèêëè íå ïðåäâèäåííûå îøèáêè {ex.Message}");
            }
        }



    }
}
