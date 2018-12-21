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

            net = new Accord.Neuro.ActivationNetwork(new Accord.Neuro.BipolarSigmoidFunction(), sensors_count, sensors_count*3, sensors_count,100 , digit_count);
            backprog = new Accord.Neuro.Learning.ParallelResilientBackpropagationLearning(net);
            nguyen = new Accord.Neuro.NguyenWidrow(net);
            nguyen.Randomize();
            
           
        }

        private IVideoSource videoSource;
        private FilterInfoCollection videoDevicesList;

        private MagicEye processor = new MagicEye();

        string path_to_sampe_dir  = "C:\\Users\\je_day\\Desktop\\";
        static int blockcount = 28;
        static int sensors_count =blockcount*blockcount;
        static int layer_count = sensors_count * 3;
        static int digit_count = 10;
        static int count_per_type =100;
        private double max_error = 0.2;
        private int epochs = 10;
        private Accord.Neuro.ActivationNetwork net;
        private Accord.Neuro.Learning.ParallelResilientBackpropagationLearning backprog;
        Accord.Neuro.NguyenWidrow nguyen;

        private Accord.DataSets.MNIST MNIST = new Accord.DataSets.MNIST();

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

            var r = new Random();
            double[][] input = new double[count_per_type*digit_count][];
            int[] res = new int[count_per_type * digit_count];
            double[][] output = new double[count_per_type * digit_count][];

            
            System.Tuple<Accord.Math.Sparse<double>, int>[] sample = MNIST.Training.Item1.Where((v, j) => MNIST.Training.Item2[j] == 0).Take(count_per_type).Select(s => Tuple.Create(s, 0)).ToArray();
            for (int i = 1; i < digit_count; i++)
            {
                 sample=sample.Concat( MNIST.Training.Item1.Where((v, j) => MNIST.Training.Item2[j] == i).Take(count_per_type).Select(s => Tuple.Create(s, i))).ToArray();             
            }
            sample = sample.OrderBy(d => r.Next()).ToArray();
            for (int i = 0; i < sample.Length; i++)
            {
                input[i] = sample[i].Item1.ToDense(sensors_count).Select(d=> d / 255.0).ToArray();
                res[i] = sample[i].Item2;
                output[i] = new double[digit_count].Select((d, ind) => ind == res[i] ? 1.0: 0).ToArray();

            }


            double error = 1;
             int iter = epochs;
             while (error > max_error && iter > 0) {
                error = backprog.RunEpoch(input, output);
                /*for (int i = 0; i < input.Length; i++)
                    
                {
                    var e = backprog.Run(input[i], output[i]);
                    error += e * e;
                    progressBar1.Value = (int)((double)i/input.Length*progressBar1.Maximum);
                }*/
                
                error = Math.Sqrt(error / input.Length);
                label2.Text = error.ToString();
                iter--;
                 
            }

            double guessed  =0;
            for (int i = 0; i < input.Length; i++)
            {
                net.Compute(input[i]);
                if (net.Output.Max() == net.Output[res[i]])
                    guessed++;
            }
            guessed /= input.Length;
            label2.ResetText();
            label2.Text += guessed.ToString();
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
            processor.GetInput( out double[] inp, blockcount);
            net.Compute(inp);
            var res = net.Output;
            int ind = 0;
            double mx = res[0];
            for (int i = 1; i < res.Length; i++)
            {
                if (res[i]> mx){
                    ind = i;
                    mx = res[i];
                }
            }
            ResLabel.Text = ind.ToString()+"\n" +String.Join("\n",net.Output.Select(d => d.ToString()));
           
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            double val = trackBar2.Value / 1000.0;
            net.SetActivationFunction(new Accord.Neuro.BipolarSigmoidFunction(val));
        }

     

      

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            count_per_type = (int)numericUpDown1.Value;
        }



        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            epochs = (int)numericUpDown2.Value;
        }



        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            double val = trackBar4.Value / 1000.0;
            max_error = val;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            nguyen.Randomize();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            train();
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
    }
}
