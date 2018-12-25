using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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

            net = new Accord.Neuro.ActivationNetwork(new Accord.Neuro.BipolarSigmoidFunction(), sensors_count, sensors_count*3, sensors_count*2,sensors_count,100 , digit_count);
            backprog = new Accord.Neuro.Learning.ParallelResilientBackpropagationLearning(net);
            nguyen = new Accord.Neuro.NguyenWidrow(net);
            nguyen.Randomize();

            comboBox1.SelectedIndex = 0;
        }

        private IVideoSource videoSource;
        private FilterInfoCollection videoDevicesList;

        private MagicEye processor = new MagicEye();

        string path_to_sampe_dir  = "C:\\Users\\Ита\\Documents\\AI\\NumberRecognition\\images\\";
        static int blockcount = 28;
        static int sensors_count =blockcount*blockcount;
        static int layer_count = sensors_count * 3;
        static int digit_count = 10;
        private double max_error = 0.2;
        private int epochs = 10;
        private Accord.Neuro.ActivationNetwork net;
        private Accord.Neuro.Learning.ParallelResilientBackpropagationLearning backprog;
        Accord.Neuro.NguyenWidrow nguyen;

       // private Accord.DataSets.MNIST MNIST = new Accord.DataSets.MNIST();

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            processor.ProcessImage((Bitmap)eventArgs.Frame.Clone());

            processor.GetPicture(out Bitmap or, out Bitmap num);
          
             pictureBox1.Image = or;
             pictureBox2.Image = num;
           

            

         
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





        
        private void Form1_Load(object sender, EventArgs e)
        {
            CloseOpenVideoSource();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            processor.ThresholdValue = (float)trackBar1.Value / 1000;
           
        }

      

   
        private void button3_Click(object sender, EventArgs e)
        {
            

            net.Compute(imgToData(processor.GetImage()));
            ShowResult();
            
           
        }

      

     
        private void TrainOnOurData()
        {
            List<double[]> input = new List<double[]>();
            List <double[]> output = new List<double[]>();
          
                foreach (var d in Directory.GetDirectories(path_to_sampe_dir))
                {

                    int type = int.Parse(d.Last().ToString());
                    foreach(var file in Directory.GetFiles(d))
                    {
                        var img = AForge.Imaging.UnmanagedImage.FromManagedImage(new Bitmap(file));
                        input.Add(imgToData(img));
                        output.Add(new double[digit_count].Select((p, ind) => ind == type ? 1.0 : 0.0).ToArray());
                    }
                }
            
            net.Randomize();
            double error = double.PositiveInfinity;
            var inp = input.ToArray();
            var otp = output.ToArray();
            int iterations = 0;
            while (error > max_error && iterations < epochs)
            {
                error = backprog.RunEpoch(inp, otp) / input.Count;
                iterations++;
                
            }


        }

        private double[] imgToData(AForge.Imaging.UnmanagedImage img)
        {
            double[] res = new double[img.Width * img.Height];
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    res[i * img.Width + j] = img.GetPixel(i, j).GetBrightness(); // maybe threshold
                }
            }
            return res;
        }
      

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            epochs = (int)numericUpDown2.Value;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            nguyen.Randomize();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TrainOnOurData();
            return;
           
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult res =  saveFileDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                net.Save(saveFileDialog1.FileName);
            }


        }

        private void button6_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                net = Accord.Neuro.Network.Load(openFileDialog1.FileName) as Accord.Neuro.ActivationNetwork;

            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var r = new Random();
            processor.presave();
            processor.Save(path_to_sampe_dir + comboBox1.SelectedIndex.ToString()+"\\" + r.Next().ToString() + r.Next().ToString() + ".png");
            

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if(double.TryParse((sender as TextBox).Text, out double r)){
                max_error = r;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            learnFromScreen();
            ShowResult();
        }

        private void learnFromScreen()
        {
            double[] inp = imgToData(processor.GetImage());
            int type = comboBox1.SelectedIndex;

            backprog.Run(inp, new int[digit_count].Select((d, i) => i == type ? 1.0 : 0.0).ToArray());
            
        }

        private void ShowResult()
        {

            var res = net.Output;
            int ind = 0;
            double mx = res[0];
            for (int i = 1; i < res.Length; i++)
            {
                if (res[i] > mx)
                {
                    ind = i;
                    mx = res[i];
                }
            }
            ResLabel.Text = "Class: "+ind.ToString()+"\n";
            for (int i = 0; i < net.Output.Length; i++)
            {
                ResLabel.Text += i.ToString()+": "+ net.Output[i].ToString("F4")+ "\n";
            }
        
        }

        private void Thershold_Click(object sender, EventArgs e)
        {

        }
    }
}
