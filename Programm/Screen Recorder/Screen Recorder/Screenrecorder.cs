using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

using Accord.Video.FFMPEG;

namespace Screen_Recorder
{
    class Screenrecorder
    {
        //Video variables:
        private Rectangle bounds;
        private string outputPath = "";
        private string tempPath = "";
        private string tempPathvideo = "";
        private int fileCount = 1;
        private List<string> inputImageSequence = new List<string>();

        //File variables:
        private string audioName = "mic.wav";
        private string videoName = "video.mp4";
        private string finalName = "FinalVideo.mp4";

        //Time variable:
        Stopwatch watch = new Stopwatch();

        //Audio variables:
        public static class NativeMethods
        {
            [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern int record(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        }

        //ScreenRecorder Object:
        public Screenrecorder(Rectangle b, string outPath)
        {
            //Create temporary folder for screenshots:
            CreateTempFolder("tempScreenCaps");

            //Create temporary folder for video:
            CreateTempFolderVideo("tempvideo");

            //Set variables:
            bounds = b;
            outputPath = outPath;
        }

        //Create temporary folder for video:
        private void CreateTempFolderVideo(string name)
        {
            
            //Check if a C or D drive exists:
            if (Directory.Exists("D://"))
            {
                string pathName = $"D://{name}";
                Directory.CreateDirectory(pathName);
                tempPathvideo = pathName;
            }
            else
            {
                string pathName = $"C://Documents//{name}";
                Directory.CreateDirectory(pathName);
                tempPathvideo = pathName;
            }
        }

        //Create temporary folder:
        private void CreateTempFolder(string name)
        {
            //Check if a C or D drive exists:
            if (Directory.Exists("D://"))
            {
                string pathName = $"D://{name}";
                Directory.CreateDirectory(pathName);
                tempPath = pathName;
            }
            else
            {
                string pathName = $"C://Documents//{name}";
                Directory.CreateDirectory(pathName);
                tempPath = pathName;
            }
        }

        //Change final video name:
        public void setVideoName(string name)
        {
            finalName = name;
        }

        //Delete all files and directory:
        private void DeletePath(string targetDir)
        {
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            //Delete each file:
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            //Delete the path:
            foreach (string dir in dirs)
            {
                DeletePath(dir);
            }

            Directory.Delete(targetDir, false);
        }

        //Delete all files except the one specified:
        private void DeleteFilesExcept(string targetDir, string excDir)
        {
            string[] files = Directory.GetFiles(targetDir);

            //Delete each file except specified:
            foreach (string file in files)
            {
                if (file != excDir)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }

            //Copy File to path
            var sourceDirectoryPathvidoe = Path.Combine(tempPathvideo);
            var sourceDirectoryInfovideo = new DirectoryInfo(sourceDirectoryPathvidoe);

            var targetDirectoryPathvideo = Path.Combine(outputPath);
            var targetDirectoryInfovideo = new DirectoryInfo(targetDirectoryPathvideo);


            CopyFiles(sourceDirectoryInfovideo, targetDirectoryInfovideo);
        }

        //Clean up program on crash:
        public void cleanUp()
        {
            if (Directory.Exists(tempPath))
            {
                DeletePath(tempPath);
            }
        }

        //Return elapsed time:
        public string getElapsed()
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds);
        }

        //Record video:
        public void RecordVideo()
        {
            //Keep track of time:
            watch.Start();

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    //Add screen to bitmap:
                    g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                }
                //Save screenshot:
                string name = tempPath + "//screenshot-" + fileCount + ".png";
                bitmap.Save(name, ImageFormat.Png);
                inputImageSequence.Add(name);
                fileCount++;

                //Dispose of bitmap:
                bitmap.Dispose();
            }
        }

        //Record audio:
        public void RecordAudio()
        {
            NativeMethods.record("open new Type waveaudio Alias recsound", "", 0, 0);
            NativeMethods.record("record recsound", "", 0, 0);
        }

        //Save audio file:
        private void SaveAudio()
        {
            string audioPath = "save recsound " + tempPathvideo + "//" + audioName;
            NativeMethods.record(audioPath, "", 0, 0);
            NativeMethods.record("close recsound", "", 0, 0);
        }

        //Save video file:
        private void SaveVideo(int width, int height, int frameRate)
        {
            using (VideoFileWriter vFWriter = new VideoFileWriter())
            {
                //Create new video file:
                vFWriter.Open(tempPathvideo + "//" + videoName, width, height, frameRate, VideoCodec.MPEG4);

                //Make each screenshot into a video frame:
                foreach (string imageLocation in inputImageSequence)
                {
                    Bitmap imageFrame = System.Drawing.Image.FromFile(imageLocation) as Bitmap;
                    vFWriter.WriteVideoFrame(imageFrame);
                    imageFrame.Dispose();
                }

                //Close:
                vFWriter.Close();
            }
        }

        //Copy
        private void CopyFiles(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            //Copy File
            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }

        //Combine video and audio files:
        private void CombineVideoAndAudio(string video, string audio)
        {
            //Copy FFMPEG command
            var sourceDirectoryPath = Path.Combine(Environment.CurrentDirectory, "Copy");
            var sourceDirectoryInfo = new DirectoryInfo(sourceDirectoryPath);

            var targetDirectoryPath = Path.Combine(tempPathvideo);
            var targetDirectoryInfo = new DirectoryInfo(targetDirectoryPath);

            CopyFiles(sourceDirectoryInfo, targetDirectoryInfo);

            //FFMPEG command to combine video and audio:
            string args = $"/c ffmpeg -i \"{video}\" -i \"{audio}\" -shortest {finalName}";
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                FileName = "cmd.exe",
                WorkingDirectory = tempPathvideo,
                Arguments = args
            };

            //Execute command:
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        public void Stop()
        {
            //Stop watch:
            watch.Stop();

            //Video variables:
            int width = bounds.Width;
            int height = bounds.Height;
            int frameRate = 10;

            //Save audio:
            SaveAudio();

            //Save video:
            SaveVideo(width, height, frameRate);

            //Combine audio and video files:
            CombineVideoAndAudio(videoName, audioName);

            //Delete separated video and audio files:
            DeleteFilesExcept(tempPathvideo, tempPathvideo + "\\" + finalName);

            //Delete the screenshots and temporary folder:
            DeletePath(tempPath);

            //Delete the video and temporary folder:
            DeletePath(tempPathvideo);
        }
    }
}
