using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyoSharp.Device;
using MyoSharp.Communication;
using MyoSharp.Exceptions;
using MyoSharp.Poses;
using System.Net.Sockets;
using System.Net;

namespace MyoApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        TcpClient tcpClient = new TcpClient();
        NetworkStream ns;

        IChannel myoChannel;
        IHub myoHub;
        IMyo myo;
        private void Form1_Load(object sender, EventArgs e)
        {
            myoChannel = Channel.Create(ChannelDriver.Create(ChannelBridge.Create(), MyoErrorHandlerDriver.Create(MyoErrorHandlerBridge.Create())));
            myoHub = Hub.Create(myoChannel);

            myoHub.MyoConnected += MyoHub_MyoConnected;
            myoHub.MyoDisconnected += MyoHub_MyoDisconnected;

            myoChannel.StartListening();

            Task.Run(() =>
            {
                try
                {
                    tcpClient.Connect(IPAddress.Parse("192.168.137.144"), 1234);
                    tcpClient.NoDelay = true;
                    this.Invoke(new Action(() =>
                    {
                        label3.Text = "TCP Connected";
                        label3.ForeColor = Color.Green;
                    }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.GetBaseException().ToString());
                    this.Invoke(new Action(() =>
                    {
                        label3.Text = "TCP failed to connect";
                        label3.ForeColor = Color.Red;
                    }));
                }
                
            });
        }

        private void MyoHub_MyoDisconnected(object sender, MyoEventArgs e)
        {
            MessageBox.Show("disconnected");
        }

        private void MyoHub_MyoConnected(object sender, MyoEventArgs e)
        {
            this.Invoke(new Action(() => {
                label1.Text = "Myo Connected";
                label1.ForeColor = Color.Green;
            }));
            e.Myo.Vibrate(VibrationType.Medium);
            myo = e.Myo;
            e.Myo.Unlock(UnlockType.Hold);
            var pose = HeldPose.Create(e.Myo, Pose.DoubleTap, Pose.FingersSpread, Pose.Fist, Pose.Rest, Pose.WaveIn, Pose.WaveOut);
            pose.Interval = TimeSpan.FromSeconds(0.5);
            pose.Start();
            pose.Triggered += Pose_Triggered;
        }

        private void Pose_Triggered(object sender, PoseEventArgs e)
        {
            Console.WriteLine(e.Pose.ToString());

            this.Invoke(new Action(() =>
            {
                try
                {
                listBox1.Items.Add(e.Pose);
                listBox1.TopIndex = listBox1.Items.Count - 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("dakhel invoke \n"+ ex.GetBaseException().ToString());
                }
            }));
            try
            {
                byte[] buff;
                ns = tcpClient.GetStream();
                buff = Encoding.ASCII.GetBytes(e.Pose.ToString()+"\r\n");
                ns.Write(buff, 0, buff.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("kharej invoke \n" + ex.GetBaseException().ToString());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            myo.Lock();
            myoChannel.StopListening();
            myoHub.Dispose();
        }
    }
}
