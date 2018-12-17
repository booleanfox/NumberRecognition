using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.IO.Ports;


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
        }

        private IVideoSource videoSource;
        private FilterInfoCollection videoDevicesList;

        private MagicEye processor = new MagicEye();


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
            pictureBox2.Image = processor.showPicture();
            //pictureBox2.Image = processor.recogniseNumber();
            //            pictureBox2.Image = processor.recogniseNumber();

            // отладочный лейбл, для угла
            label1.Text = processor.rotateAngle().ToString();

            var newBitmap = new Bitmap(pictureBox2.Image.Width, pictureBox2.Image.Height);
            var graphics = Graphics.FromImage(newBitmap);
            graphics.TranslateTransform((float)pictureBox2.Image.Width / 2, (float)pictureBox2.Image.Height / 2);
            graphics.RotateTransform(processor.rotateAngle());
            graphics.TranslateTransform(-(float)pictureBox2.Image.Width / 2, -(float)pictureBox2.Image.Height / 2);
            graphics.DrawImage(pictureBox2.Image, new Point(0, 0));

            Bitmap pic;
            AForge.Imaging.Filters.ExtractBiggestBlob extractFfilter = new AForge.Imaging.Filters.ExtractBiggestBlob();
            pic = extractFfilter.Apply(newBitmap);
            pictureBox2.Image = pic;
        }
    }
}
