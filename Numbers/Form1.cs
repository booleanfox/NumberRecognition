using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.IO;

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

        static int blockcount = 50;
        static int sensors_count =blockcount*blockcount + 1;
        static int layer_count = sensors_count * 2;
        static int digit_count = 10;
        private AForge.Neuro.ActivationNetwork net = new AForge.Neuro.ActivationNetwork(new AForge.Neuro.SigmoidFunction(), sensors_count, layer_count, layer_count,digit_count);
        private AForge.Neuro.Learning.BackPropagationLearning backprog;



        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            processor.ProcessImage((Bitmap)eventArgs.Frame.Clone());

            if(processor.original!= null)
            pictureBox1.Image = processor.original;

            if (processor.number != null)
                pictureBox2.Image = processor.number;
           

            

         
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
                    //pictureBox1.Image.Dispose();
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
                //pictureBox1.Image.Dispose();
            }
            videoSource = null;
            
        }

     



        private void train()
        {

           var Paths0  = Directory.GetFiles("C:\\Users\\boole\\Desktop\\training\\0");
           var Paths1 = Directory.GetFiles("C:\\Users\\boole\\Desktop\\training\\1");
           var Paths2 = Directory.GetFiles("C:\\Users\\boole\\Desktop\\training\\2");
           var Paths3 = Directory.GetFiles("C:\\Users\\boole\\Desktop\\training\\3");
           var Paths4 = Directory.GetFiles("C:\\Users\\boole\\Desktop\\training\\4");

           


        }


        private void Form1_Load(object sender, EventArgs e)
        {
            CloseOpenVideoSource();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            processor.ThresholdValue = (float)trackBar1.Value / 100;
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            train();
        }

   
        private void button3_Click(object sender, EventArgs e)
        {
            label1.Text = processor.Angle.ToString() + "\n" + processor.BlobCount.ToString() + "\n";
            
            var img = processor.GetInput( out double[] inp, blockcount);
            var res = net.Compute(inp);
            int ind = 0;
            double mx = res[0];
            for (int i = 1; i < res.Length; i++)
            {
                if (res[i]> mx){
                    ind = i;
                    mx = res[i];
                }
            }
            ResLabel.Text = ind.ToString();
            pictureBox3.Image = img.ToManagedImage();
            pictureBox3.Invalidate();
        }
    }
}
