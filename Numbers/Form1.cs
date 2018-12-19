using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;


namespace Numbers
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

             videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevicesList)
            {
                cmbVideoSource.Items.Add(videoDevice.Name);
            }
            if (cmbVideoSource.Items.Count > 0)
            {
                cmbVideoSource.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("Камера не найдена!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            backprog = new AForge.Neuro.Learning.BackPropagationLearning(net);
            
        }

        private IVideoSource videoSource;
        private FilterInfoCollection videoDevicesList;

        private MagicEye processor = new MagicEye();

        static int sensors_count = 300 * 300;
        static int layer_count = sensors_count * 2;
        static int digit_count = 10;
        private AForge.Neuro.ActivationNetwork net = new AForge.Neuro.ActivationNetwork(new AForge.Neuro.SigmoidFunction(), sensors_count, layer_count, layer_count,digit_count);
        private AForge.Neuro.Learning.BackPropagationLearning backprog;
        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            processor.ProcessImage((Bitmap)eventArgs.Frame.Clone());

            pictureBox1.Image = processor.original;

            if (processor.stopByErrors)
            {
                //  Тут на случай ошибок
                Debug.WriteLine("Stopped by fatal errors level");
            
                return;
            }

           
           
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            CloseOpenVideoSource();
        }

        void CloseOpenVideoSource()
        {
            if (videoSource == null)
            {
                videoSource = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                videoSource.Start();
                btnStart.Text = "Stop";
            }
            else
            {
                videoSource.SignalToStop();
                if (videoSource != null && videoSource.IsRunning && pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                videoSource = null;
                btnStart.Text = "Start";

            }

        }



        // Чтобы вебка не падала в обморок при неожиданном закрытии окна
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnStart.Text == "Stop")
                videoSource.SignalToStop();
            if (videoSource != null && videoSource.IsRunning && pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            videoSource = null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ProccesImageToScreen();
        }

        void ProccesImageToScreen()
        {
    

            // extract pic for cam and find blob(number)
            Bitmap pic = processor.showPicture();
            AForge.Imaging.Filters.ExtractBiggestBlob extractFfilter = new AForge.Imaging.Filters.ExtractBiggestBlob();
            pic = extractFfilter.Apply(pic);


            float angle = get_angle(pic);
            label1.Text = angle.ToString();

            // placing rotated blob on black background 
            int side = Math.Max(pic.Width * 2, pic.Height * 2);
            Bitmap enlargedbackground = new Bitmap(side, side); // create bigger backgound
            var graphics = Graphics.FromImage(enlargedbackground);
            SolidBrush mySolidBrush = new SolidBrush(Color.Black);
            graphics.FillRectangle(mySolidBrush, 0, 0, side, side); // fill it black
            graphics.TranslateTransform(enlargedbackground.Width / 2, enlargedbackground.Height / 2);
            graphics.RotateTransform(-angle);
            graphics.DrawImageUnscaled(pic, -pic.Width / 2, -pic.Height / 2); // draw blob rotated in the center
            graphics.ResetTransform();


            pic = extractFfilter.Apply(enlargedbackground); // extract blob again but rotated


            enlargedbackground = new Bitmap(300, 300);
            graphics = Graphics.FromImage(enlargedbackground);
            graphics.FillRectangle(mySolidBrush, 0, 0, 300, 300); // fill it black
            graphics.DrawImage(pic, 0, 0, 300, 300);



            pictureBox2.Image = enlargedbackground;
            pictureBox2.Invalidate();

        }


        Bitmap ProcessImageFromFile(string path, float ThresholdValue = 0.3f )
        {
            
                Bitmap bitmap = new Bitmap(path);
                //  Минимальная сторона изображения (обычно это высота)    
                //if (bitmap.Height > bitmap.Width) throw new Exception("К такой забавной камере меня жизнь не готовила!");
                int side = Math.Min(bitmap.Height, bitmap.Width);
                // обрезка фида с камера
                Bitmap original = new Bitmap(bitmap.Width, bitmap.Height);
                Rectangle cropRect = new Rectangle(0, 0, side, side);
                Graphics g = Graphics.FromImage(original);
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), cropRect, GraphicsUnit.Pixel);


                //  Конвертируем изображение в градации серого
                AForge.Imaging.Filters.Grayscale grayFilter = new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);
                var processed = grayFilter.Apply(AForge.Imaging.UnmanagedImage.FromManagedImage(original));

                //  Масштабируем изображение до 300x300 - этого достаточно
                AForge.Imaging.Filters.ResizeBilinear scaleFilter = new AForge.Imaging.Filters.ResizeBilinear(300, 300);
                original = scaleFilter.Apply(original);


 
                //  Пороговый фильтр применяем. Величина порога берётся из настроек, и меняется на форме
                AForge.Imaging.Filters.BradleyLocalThresholding threshldFilter = new AForge.Imaging.Filters.BradleyLocalThresholding();
                threshldFilter.PixelBrightnessDifferenceLimit = ThresholdValue;
                threshldFilter.ApplyInPlace(processed);



                //  Инвертируем изображение, пустота черная
                AForge.Imaging.Filters.Invert InvertFilter = new AForge.Imaging.Filters.Invert();
                InvertFilter.ApplyInPlace(processed);





                AForge.Imaging.Filters.ExtractBiggestBlob extractFfilter = new AForge.Imaging.Filters.ExtractBiggestBlob();
                processed = extractFfilter.Apply(processed);


                float angle = get_angle(processed.ToManagedImage());


            Bitmap pic = processed.ToManagedImage();

            // placing rotated blob on black background 
             side = Math.Max(processed.Width * 2, processed.Height * 2);
            Bitmap enlargedbackground = new Bitmap(side, side); // create bigger backgound
            var graphics = Graphics.FromImage(enlargedbackground);
            SolidBrush mySolidBrush = new SolidBrush(Color.Black);
            graphics.FillRectangle(mySolidBrush, 0, 0, side, side); // fill it black
            graphics.TranslateTransform(enlargedbackground.Width / 2, enlargedbackground.Height / 2);
            graphics.RotateTransform(-angle);
            graphics.DrawImageUnscaled(pic, -processed.Width / 2, -processed.Height / 2); // draw blob rotated in the center
            graphics.ResetTransform();


            pic = extractFfilter.Apply(enlargedbackground); // extract blob again but rotated


            enlargedbackground = new Bitmap(300, 300);
            graphics = Graphics.FromImage(enlargedbackground);
            graphics.FillRectangle(mySolidBrush, 0, 0, 300, 300); // fill it black
            graphics.DrawImage(pic, 0, 0, 300, 300);

            return enlargedbackground;

        }

        float get_angle(AForge.Imaging.UnmanagedImage umg)
        {
           
            int w = umg.Width;
            int h = umg.Height;
            float gcx = w / 2.0f, gcy = h / 2.0f;
            float mcx = 0, mcy = 0;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    Color clr = umg.GetPixel(i, j);
                    if (clr.R > 200 && clr.G > 200 && clr.B > 200)
                    {
                        mcx += (float)i / w;
                        mcy += (float)j / h;
                    }
                }
            double vx = gcx - mcx;
            double vy = gcy - mcy;
            return (float)(Math.Acos(vx / Math.Sqrt(vx * vx + vy * vy)) * 180 / Math.PI);


        }

        float get_angle(Bitmap blob)
        {
            var umg = AForge.Imaging.UnmanagedImage.FromManagedImage(blob);

            return get_angle(umg);
        

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CloseOpenVideoSource();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            processor.ThresholdValue = (float)trackBar1.Value / 100;
            ProccesImageToScreen();
        }
    }
}
