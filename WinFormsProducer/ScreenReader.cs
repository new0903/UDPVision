
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsProducer
{
    public class ScreenReader
    {
        private int fps = 50;

        private CancellationTokenSource? cts;

        public event Action<Bitmap> NewFrame;

        public void Start()
        {
            if (cts == null)
            {
                cts = new CancellationTokenSource();
                ScreenStream(cts.Token);
            }
        }
        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
            }
        }




        public async Task ScreenStream(CancellationToken token)
        {
            int width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            var speed = 1000/fps;

            try
            {
                while (!token.IsCancellationRequested)
                {

                    await Task.Run(() =>
                    {
                        using (Bitmap bmp = new Bitmap(width, height))
                        {
                            // Создаем объект Graphics из bitmap
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                // Копируем содержимое экрана в bitmap
                                g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                            }
                           // var bitmap = new Bitmap(bmp, 640, 480);
                           // var frame = new NewFrameEventArgs(bitmap);

                            NewFrame?.Invoke(bmp);
                        }
                    },token);

                    await Task.Delay(TimeSpan.FromMilliseconds(speed), token);//20
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {

                cts = null;

            }
        }


    }

}
