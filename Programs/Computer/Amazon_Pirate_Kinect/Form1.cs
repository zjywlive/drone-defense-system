using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

using Microsoft.Kinect;
using Microsoft.Kinect.Interop;

using System.Runtime.InteropServices;

using System.Threading;
using System.Threading.Tasks;

using System.Drawing.Imaging;

namespace Amazon_Pirate_Kinect
{
    public partial class Form1 : Form
    {
        static KinectSensor sensor;
        const int minDist = 500; 
        const int maxDist = 1500;
        const int threshold = 100; //These 3 vars are very important!

        int frameCount = 0;
        int numberOfTimesDetectedNothing = 0;
        int numberOfTimesDetectedSomething = 0;
        SerialPort port = new SerialPort("COM3", 9600, Parity.None);

        public Form1()
        {
            InitializeComponent();
            sensor = KinectSensor.KinectSensors[0];
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.AllFramesReady += FramesReady;
            sensor.Start();
            port.Open();
            
        }
        void FramesReady(object sender, AllFramesReadyEventArgs e)
        {
            DepthImageFrame imageFrame = e.OpenDepthImageFrame();
            ColorImageFrame rgbFrame = e.OpenColorImageFrame();

            if (imageFrame != null && rgbFrame != null)
            {
                int targetPixelDist = int.MaxValue;
                int[] targetPixelPos = { imageFrame.Width/2, imageFrame.Height/2 };

                short[] pixelData = new short[imageFrame.PixelDataLength];
                imageFrame.CopyPixelDataTo(pixelData);
                Parallel.For(0, imageFrame.Height, new ParallelOptions { MaxDegreeOfParallelism = 16 }, y =>//int x = 0; x < imageFrame.Width; x++)
                {
                    //ColorImagePoint temp = imageFrame.MapToColorImagePoint(50, y, ColorImageFormat.RgbResolution640x480Fps30);
                    for (int x = 0; x < imageFrame.Width; x++)
                    {
                        if ((ushort)((pixelData[x + y * 640]) >> 3) < maxDist && (ushort)((pixelData[x + y * 640]) >> 3) > minDist)
                        {
                            //Check if pixel is closest one
                            if ((ushort)((pixelData[x + y * 640]) >> 3) < targetPixelDist)
                            {
                                targetPixelDist = (ushort)((pixelData[x + y * 640]) >> 3); //When you think you're going to use a really long and ugly value once but it turns out you use it a bunch
                                targetPixelPos[0] = x;
                                targetPixelPos[1] = y;
                            }
                        }
                    }
                });
                pictureBox1.Image = ImageToBitmap(rgbFrame);
                Graphics drawStats = Graphics.FromImage(pictureBox1.Image);
                drawStats.FillEllipse(Brushes.Red, targetPixelPos[0], targetPixelPos[1], 10, 10);

                //Start figuring out where to move it if on a frame that is multiple of 4
                frameCount++;
                if (frameCount >= 4)
                {
                    while(port.BytesToRead > 0)
                    {
                        port.ReadByte(); //Throw the bytes away
                    }
                    frameCount = 0;
                    if (targetPixelPos[0] != imageFrame.Width / 2 && targetPixelPos[1] != imageFrame.Height / 2)
                    {
                        //Detected something
                        numberOfTimesDetectedSomething++;
                    }
                    else //No target found
                    {
                        numberOfTimesDetectedNothing++;
                    }
                    if(numberOfTimesDetectedSomething >= 3)
                    {
                        if(numberOfTimesDetectedNothing > 3) numberOfTimesDetectedNothing = 0;

                        if (targetPixelPos[1] < (imageFrame.Height / 2) - threshold)
                        {
                            yawLabel.Text = "Up";
                            port.WriteLine("U");
                            port.ReadLine();
                        }
                        else if (targetPixelPos[1] > (imageFrame.Height / 2) + threshold)
                        {
                            yawLabel.Text = "Down";
                            port.WriteLine("D");
                            port.ReadLine();
                        }
                        else
                        {
                            yawLabel.Text = "Target yaw";
                        }

                        //Pitch
                        if (targetPixelPos[0] < (imageFrame.Width / 2) - threshold)
                        {
                            pitchLabel.Text = "Left";
                            port.WriteLine("R");
                            port.ReadLine();
                        }
                        else if (targetPixelPos[0] > (imageFrame.Width / 2) + threshold)
                        {
                            pitchLabel.Text = "Right";
                            port.WriteLine("L");
                            port.ReadLine();
                        }
                        else
                        {
                            pitchLabel.Text = "Target pitch";
                        }
                    }
                    /*if(numberOfTimesDetectedNothing >= 3)
                    {
                        if(numberOfTimesDetectedSomething > 3) numberOfTimesDetectedSomething = 0; //If statement fixes it, no idea why
                        //Start scanning for drones
                        //Set yaw correctly
                        port.WriteLine("P"); //P means set for scanning edit: I should have used S
                        port.ReadLine();
                        port.WriteLine("L");
                        port.ReadLine();
                    }*/
                }

            }

        }
        Bitmap ImageToBitmap(ColorImageFrame Image)
        {
            byte[] pixeldata = new byte[Image.PixelDataLength];
            Image.CopyPixelDataTo(pixeldata);
            Bitmap bmap = new Bitmap(Image.Width, Image.Height, PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(
                new Rectangle(0, 0, Image.Width, Image.Height),
                ImageLockMode.WriteOnly,
                bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(pixeldata, 0, ptr, Image.PixelDataLength);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
    }
}
