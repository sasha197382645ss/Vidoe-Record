using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Screen_Recorder
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

            //Check if File exists:
            if (File.Exists(Environment.CurrentDirectory + "//" + "sas.txt"))
            {
                //Check and read File
                string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "//" + "sas.txt");
                outputPath = lines[0];
                if (lines[1] != null)
                {
                    Nomer = Convert.ToInt32(lines[1]);

                }
                else
                {
                    Nomer = 0;
                }
            }

            //Do Bounds for Screenrecorder
            Rectangle bounds = Screen.FromControl(this).Bounds;
            screenRec = new Screenrecorder(bounds, outputPath);


            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
            comboBox1.Items.AddRange(devices.ToArray());

        }

        // Filing variables:
        string outputPath = "";
        string outputPathOld = "";
        int Nomer = 0;
        bool Abort = false;
        bool pathSelected = false;
        string finalVidName = "Sas.mp4";
        int progres = 0;

        // Screen recorder object:
        Screenrecorder screenRec = new Screenrecorder(new Rectangle(), "");


        private void button3_Click(object sender, EventArgs e)
        {
            //Create output path:
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select an Output Folder";

            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Creat path
                outputPath = @folderBrowser.SelectedPath;
                pathSelected = true;

                //Create File
                FileStream fs = File.Create(Environment.CurrentDirectory + "//" + "sas.txt");
                fs.Close();

                //Write to File
                File.WriteAllText(Environment.CurrentDirectory + "//" + "sas.txt", outputPath + "\\" + "\n" + "");

                //Finish screen recorder object:
                Rectangle bounds = Screen.FromControl(this).Bounds;
                screenRec = new Screenrecorder(bounds, outputPath);
            }
            else
            {
                MessageBox.Show("Please select an output folder.", "Error");

            }
        }

        private void tmrRecorder_Tick(object sender, EventArgs e)
        {
            //Creat time of vidoe
            screenRec.RecordVideo();
            screenRec.RecordAudio();
            lblTimer.Text = screenRec.getElapsed();

            //Volume of Voice
            if (comboBox1.SelectedItem != null)
            {
                var device = (MMDevice)comboBox1.SelectedItem;
                progressBar1.Value = (int)(Math.Floor(device.AudioMeterInformation.MasterPeakValue * 100));
            }
        }

        public void VidName()
        {
            //Nomber of video
            Nomer++;
            finalVidName = Nomer.ToString() + ".mp4";

            //Write in File
            StreamWriter w = new StreamWriter(Environment.CurrentDirectory + "//" + "sas.txt");
            w.Write(outputPath);
            w.Write(Environment.NewLine + Nomer);
            w.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Start video recording
            bool containsMP4 = finalVidName.Contains(".mp4");
            VidName();

            //Set nomber to video
            screenRec.setVideoName(finalVidName);
            tmrRecorder.Start();

            //Start Capturing
            Thread t = new Thread(Capture);
            t.Start();

        }

        void Capture()
        {
            //Capture video from monitor
            while (Abort == false)
            {
                Bitmap vb = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                Graphics y = Graphics.FromImage(vb);
                y.CopyFromScreen(0, 0, 0, 0, vb.Size);
                pictureBox1.Image = vb;
                Thread.Sleep(100);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Stop video recording and capture
            screenRec.Stop();
            tmrRecorder.Stop();
            Abort = true;
            Application.Restart();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Clean program
            screenRec.cleanUp();
        }
    }
}
