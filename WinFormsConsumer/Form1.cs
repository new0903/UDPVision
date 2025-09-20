using FastYolo;
using FastYolo.Model;
using MessageProtocolLibrary;

using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace WinFormsConsumer
{
    public partial class Form1 : Form
    {
        public static List<YoloItem> itemsYolo = new List<YoloItem>();
        private static string keyBase64=string.Empty; //= "/642GxdGe8ZZRAZ/2eHbOZz44R4VJ5gb2JIB2sku4uY=";
        private static string IVBase64 = string.Empty;//= "LlNfGMPmyzPITfv8iWn6Hw=="
        private static byte[] keyByte;
        private static byte[] IVByte;
        public bool IsInitializedKey = false;

        private static CancellationTokenSource? cts;
        private static CancellationTokenSource? ctsDetectObject;
        //    MemoryStream? ms;
        byte[]? msBuffer;
        private Thread _thread;


        RegisterFiles registerFiles;

        public Form1()
        {
            InitializeComponent();
            IsInitializedKey=InitializeKey();
            label2.Text = "";

            registerFiles=new RegisterFiles();
            registerFiles.RecieveMessaging += VisibleMessage;
        }

        public bool InitializeKey()
        {

            try
            {
                if (File.Exists("RegisKeys.txt"))
                {

                    var keys = File.ReadAllLines("RegisKeys.txt");

                    if (keys[0].Contains("KeyBase64:") && keys[1].Contains("IVBase64:"))
                    {

                        keyBase64 = keys[0].Split(": ")[1];
                        IVBase64 = keys[1].Split(": ")[1];
                        if (!string.IsNullOrEmpty(keyBase64)&& !string.IsNullOrEmpty(IVBase64))
                        {
                            keyByte = Convert.FromBase64String(keyBase64);
                            IVByte = Convert.FromBase64String(IVBase64);
                            IsInitializedKey = true;
                            return true;
                        }

                    }
                }
                else
                {


                    MessageBox.Show("Файл RegisKeys.txt отсутствует");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"InitializeKey Возникли не предвиденные ошибки {ex.Message}");
            }
            
            return false;
            

        }


        private void VisibleMessage(byte[] bufferMessage)
        {
            byte[] buffer = bufferMessage;
            if (IsInitializedKey && checkBox2.Checked)
            {
                buffer = Decrypt_Aes(buffer, keyByte, IVByte);

            }

            msBuffer = buffer;
          var  ms = new MemoryStream(buffer);

            //   Image finalImage = new Bitmap(ms); 172.30.115.4
            Image finalImage = new Bitmap(ms);
            if (itemsYolo.Count > 0)
            {
                Graphics graph = Graphics.FromImage(finalImage);
                Font font = new Font("Consolas", 10, FontStyle.Bold);
                SolidBrush brush = new SolidBrush(System.Drawing.Color.Red);

                foreach (YoloItem item in itemsYolo)
                {
                    Point rectPoint = new Point(item.X, item.Y);
                    Size rectSize = new Size(item.Width, item.Height);
                    Rectangle rect = new Rectangle(rectPoint, rectSize);
                    Pen pen = new Pen(System.Drawing.Color.Red, 2);
                    graph.DrawRectangle(pen, rect);
                    graph.DrawString(item.Type, font, brush, rectPoint);
                }
            }

            pictureBox1.Image = finalImage;


        }

        private async Task StartStream(CancellationToken token)
        {
            var port = int.Parse(numericUpDown1.Value.ToString());
            //     var consumrIp = comboBox1.Text;
            UdpClient? client = new UdpClient(port);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var data = await client.ReceiveAsync(token);
                    if (data.Buffer.Length > 0)
                    {
                        registerFiles.CompleteMessage(data.Buffer);
                      //  VisibleMessage(data.Buffer);

                    }
                    Text = $"Bytes recieved: {data.Buffer.Length * sizeof(byte)}";

                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {

                MessageBox.Show($"StartStream Операция завершилась с ошибкой \r\n {ex.Message}");
            }
            finally
            {

                client.Dispose();
                // client.Close();

                client = null;
                cts = null;
            }
           
        }
        private async Task DetectObjects(CancellationToken token)
        {

            /*вот эту  штуку надо сделать ассинхронно*/
            using YoloWrapper? yolo = new YoloWrapper("yolov3.cfg", "yolov3.weights", "coco.names");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    //Bitmap bmp = new Bitmap(pictureBox1.Image);
                    //var ms = new MemoryStream();
                    //bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);


                    if (msBuffer != null)
                    {
                        await Task.Run(() => {

                            var buffer = msBuffer;//ms.ToArray();
                            itemsYolo = yolo.Detect(buffer).ToList<YoloItem>();

                        },token);
                        await Task.Delay(TimeSpan.FromMilliseconds(100), token);

                    }
                    else
                    {

                        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show($"DetectObjects Операция завершилась с ошибкой \r\n {ex.Message}");
            }
            finally
            {
                ctsDetectObject = null;
                yolo.Dispose();
                itemsYolo.Clear();

            }

        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (cts==null)
            {
                if (!IsInitializedKey) IsInitializedKey = InitializeKey();


                cts = new CancellationTokenSource();

                StartStream(cts.Token);


            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (cts!= null)
            {

                cts.Cancel();


            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ctsDetectObject==null)
            {
                ctsDetectObject = new CancellationTokenSource();
                DetectObjects(ctsDetectObject.Token);
                button3.Text = "Выкл Распознование объектов в видео";
            }
            else
            {
                ctsDetectObject.Cancel();
                button3.Text = "Вкл Распознование объектов в видео";

                itemsYolo.Clear();
            }
        }



        public static byte[] Decrypt_Aes(byte[] buffer, byte[] Key, byte[] IV)
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

                ICryptoTransform encryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

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
    }
}
