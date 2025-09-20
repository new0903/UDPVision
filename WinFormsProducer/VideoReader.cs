using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WinFormsProducer
{
    public class VideoReader
    {
        private int fps = 50;

        private CancellationTokenSource? cts;
        public VideoCapture Capture;
        public event Action<Bitmap> NewFrame;


        public VideoReader(VideoCapture capture)
        {
            Capture = capture;
        }

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

            var speed = 1000 / fps;

            Mat frame = new Mat();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    Capture.Read(frame);
                    if (frame.Empty())
                        break;
                    Bitmap newFrame = BitmapConverter.ToBitmap(frame);
                   // var bitmap = new Bitmap(newFrame, 640, 480);
                    NewFrame?.Invoke(newFrame);


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
                Capture.Dispose();

                cts = null;

            }
        }




    }
}
