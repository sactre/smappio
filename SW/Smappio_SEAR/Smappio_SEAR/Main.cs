﻿using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;

namespace Smappio_SEAR
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            txtPath.Text = filePath;
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
        }

        #region Members
        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        BluetoothManager bluetoothManager;
        string deviceName = "smappio_PCM";
        private string filePath = "../../AudioSamples/";
        private List<Int32> _fileInts = new List<int>();
        List<byte> _bytes = new List<byte>();
        Stopwatch sw = new Stopwatch();
        long elapsedMilliseconds = 0;
        #region SoundParameters

        static int _sampleRate = 16000;
        static int _seconds = 5;
        static int _bytesDepth = 3;
        static int _bitDepth = _bytesDepth * 8;        
        
        
        private bool _notified;
        private float _baudRate = 900000; //(_sampleRate * _bitDepth) * 1.2f;

        #endregion

        //Stopwatch sp = new Stopwatch();


        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        #endregion

        #region Transfering methods
        private void btnWifi_Click(object sender, EventArgs e)
        {
            //logica para Wi-fi
        }

        private void btnUSB_Click(object sender, EventArgs e)
        {
            EnableFeatures();
            //Silicon Labs CP210x USB to UART Bridge
            try
            {
                serialPort.PortName = "COM4";//BluetoothHelper.GetBluetoothPort("Silicon Labs CP210x USB to UART Bridge");
                serialPort.BaudRate = Convert.ToInt32(_baudRate);
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;

                if (!serialPort.IsOpen)
                    serialPort.Open();
                lblNotification.Text = "Started";
                serialPort.DataReceived += SerialPort_DataReceived;
            }
            catch (Exception ex)
            {
                serialPort.Dispose();
                return;
            }
        }        

        private void btnBluetooth_Click(object sender, EventArgs e)
        {
            EnableFeatures();
            try
            {
                bluetoothManager = new BluetoothManager();

                serialPort.PortName = BluetoothHelper.GetBluetoothPort(deviceName);
                serialPort.BaudRate = Convert.ToInt32(_baudRate);
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;

                if (!serialPort.IsOpen)
                    serialPort.Open();

                lblNotification.Text = "Started";
                serialPort.DataReceived += SerialPort_DataReceived;
            }
            catch (Exception ex)
            {
                serialPort.Dispose();
                return;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if(sw.ElapsedMilliseconds < (_seconds * 1000)) //_bytes.Count <= (_sampleRate * _bytesDepth * _seconds)
            {                                
                var bufferSize = serialPort.BytesToRead;

                //USED FOR PRINTING INT32 IN TEXTBOX
                //if (bufferSize <= 4)
                //    return;

                byte[] data = new byte[bufferSize];
                serialPort.Read(data, 0, bufferSize);

                if (!sw.IsRunning)
                    sw.Start();

                // LOGIC FOR PRINTING THE VALUES IN THE TEXTBOX.
                //int i = 0;
                //Int32 temp = 0;
                //while (i <= (bufferSize - 4))
                //{
                //    temp = (Int32)BitConverter.ToInt32(data, i);

                //    SetTextBox(temp.ToString());

                //    i += 4;
                //}

                this._bytes.AddRange(data);                
            }
            else if(!_notified)
            {
                //lblNotification.Text = "Finish";
                sw.Stop();
                elapsedMilliseconds = sw.ElapsedMilliseconds;
                SetNotificationLabel("Finished");
                _notified = true;
            }            
        } 
        #endregion

        #region TextBox_Methods
        delegate void SetTextCallback(string text);
        private void SetTextBox(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.

            if (this.txtSerialData.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTextBox);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtSerialData.AppendText(text + " ");
            }
        }

        delegate void SetSetNotificationLabelCallback(string text);
        private void SetNotificationLabel(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.

            if (this.txtSerialData.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetNotificationLabel);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                lblNotification.Text = text;
            }
        }

        #endregion

        #region Save file methods
        private void btnSave_Click(object sender, EventArgs e)
        {
            int samplesReceived = _bytes.Count / _bytesDepth;

            long sampleRate = samplesReceived / (elapsedMilliseconds / 1000);
            lblSampleRate.Text = sampleRate.ToString();

            long bitRate = (sampleRate * _bytesDepth * 8) / 1000;


            lblTime.Text = elapsedMilliseconds.ToString();
            lblSamplesReceived.Text = samplesReceived.ToString();
            lblBitRate.Text = bitRate.ToString();

            //little endian!   
            string absolutePath = Path.GetFullPath(filePath);
            string fileName = $"{DateTime.Now.ToString("ddhhmmss")}.wav";
            string fullPath = Path.Combine(absolutePath, fileName);
            File.WriteAllBytes(fullPath, _bytes.ToArray());

            // LOGIC FOR PRINTING THE VALUES IN THE TEXTBOX.

            //var data = new byte[4];
            //for (int i = 0; i <= 200; i++)
            //{
            //    data[i % 4] = _bytes[i];
            //    if (i % 4 == 3)
            //    {
            //        var temp = (Int32)BitConverter.ToInt32(data, 0);

            //        SetTextBox(temp.ToString());
            //    }
            //}


            if (serialPort.IsOpen)
            {
                serialPort.DiscardInBuffer();
                serialPort.Close();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearContents();
        }

        #endregion

        #region private methods
        private void EnableFeatures()
        {
            btnSave.Enabled = true;
        }

        private void ClearContents()
        {
            lblBitRate.Text = lblBitRate.Text = lblSampleRate.Text = lblSamplesReceived.Text = lblTime.Text = "";
            sw = new Stopwatch();

            this._bytes.Clear();

            if (serialPort.IsOpen)
                serialPort.Close();
        } 
        #endregion
    }
}
