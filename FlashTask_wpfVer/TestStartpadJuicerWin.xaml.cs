using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.IO.Ports;
using System.Diagnostics;

namespace FlashTask_wpf
{
    /// <summary>
    /// Interaction logic for TestStartpadJuicerWin.xaml
    /// </summary>
    public partial class TestStartpadJuicerWin : Window
    {
        public delegate void UpdateTextCallback(string message);

        MainWindow parent;
        private bool BtnStartState, BtnStopState;

        SerialPort serialPort_IO8;
        Thread Thread_Listen2SerialPortIO8;

        public TestStartpadJuicerWin(MainWindow parentWindow)
        {
            InitializeComponent();

            parent = parentWindow;
            DisableBtnStartStop();

            BindSerialPortNames();

            serialPort_IO8 = new SerialPort();
        }

        private void BindSerialPortNames()
        {
            string[] ports = SerialPort.GetPortNames();
            string portIO8 = SerialPortIO8.Locate_serialPortIO8();
            for(int i=0; i< ports.Length; i++)
            {
                string port = ports[i];
                cboPort.Items.Add(port);
                if (portIO8 != "" && port == portIO8)
                    cboPort.SelectedIndex = i;
            }
            if(portIO8=="")
            {
                btn_OpenStartpad.IsEnabled = false;
                btn_CloseStartpad.IsEnabled = false;
                btn_StartJuicer.IsEnabled = false;
                btn_CloseJuicer.IsEnabled = false;

            }
        }

        private void DisableBtnStartStop()
        {
            BtnStartState = parent.btn_start.IsEnabled;
            BtnStopState = parent.btn_stop.IsEnabled;
            parent.btn_start.IsEnabled = false;
            parent.btn_stop.IsEnabled = false;
        }

        private void ResumeBtnStartStop()
        {
            parent.btn_start.IsEnabled = BtnStartState;
            parent.btn_stop.IsEnabled = BtnStopState;
        }

        private void Listen2SerialPortIO8()
        {
            Stopwatch touchedWatch = new Stopwatch();
            Stopwatch totalWatch = new Stopwatch();
            totalWatch.Start();
            ReadStartpad readStartpad = ReadStartpad.Yes;
            while (readStartpad == ReadStartpad.Yes)
            {
                // Read Startpad Voltage 
                serialPort_IO8.WriteLine("Z");

                // extract and parse the start pad voltage 
                string str_Voltage = serialPort_IO8.ReadExisting();

                // Update Startpad Voltage 
                txt_StartpadVol.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateStartpadVol),
                    new object[] { str_Voltage });

                Thread.Sleep(100);
            }
        }

        private void UpdateStartpadVol(string message)
        {
            txt_StartpadVol.Text = message;
        }

        private void UpdateStartpadDigital(string message)
        {
            txt_StartpadDigital.Text = message;
        }

        private void BtnOpenStartpad_Click(object sender, RoutedEventArgs e)
        {
            btn_OpenStartpad.IsEnabled = false;
            btn_CloseStartpad.IsEnabled = true;

            SerialPortIO8.Open_serialPortIO8(serialPort_IO8, cboPort.Text, 115200);

            // listen2Startpad thread
            Thread_Listen2SerialPortIO8 = new Thread(new ThreadStart(Listen2SerialPortIO8));
            Thread_Listen2SerialPortIO8.Start();
        }

        private void BtnCloseStartpad_Click(object sender, RoutedEventArgs e)
        {
            btn_OpenStartpad.IsEnabled = true;
            btn_CloseStartpad.IsEnabled = false;

            if (Thread_Listen2SerialPortIO8.IsAlive)
                Thread_Listen2SerialPortIO8.Abort();
            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();
        }

        private void BtnOpenJuicer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCloseJuicer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            ResumeBtnStartStop();
        }
    }
}
