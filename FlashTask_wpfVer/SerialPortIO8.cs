using System;
using System.Windows;
using System.IO.Ports;
using System.Threading;

namespace FlashTask_wpf
{
    class SerialPortIO8
    {
        public SerialPortIO8()
        {
        }

        public static string Locate_serialPortIO8()
        {/* 
            Locate the correct COM port used for communicating with the DLP-IO8 

            Returns:
                string serialPortIO8 Name, if located; "", false
            
             */

            string[] portNames = SerialPort.GetPortNames();
            string serialPortIO8_Name = "";
            
            foreach (string portName in portNames)
            {
                SerialPort serialPort;
                try
                {
                    serialPort = new SerialPort(portName, 115200);
                    serialPort.WriteTimeout = 100;
                    serialPort.Open();
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        // channel 1 Ping command of the DLP-IO8, return 0 or 1
                        serialPort.WriteLine("Z");
                    }
                    catch
                    {
                        break;
                    }

                    try
                    {
                        // Read exist Analog in from serialPort
                        string str_Read = serialPort.ReadExisting();
                        if (str_Read.Contains("V"))
                        {//
                            serialPortIO8_Name = portName;
                            serialPort.Close();
                            return serialPortIO8_Name;
                        }
                        Thread.Sleep(10);
                    }
                    catch { }
                }
                serialPort.Close();
            }

            return serialPortIO8_Name;
        }


        public static void Open_serialPortIO8(SerialPort serialPort_IO8, string portName, int baudRate)
        {
            try
            {
                serialPort_IO8.PortName = portName;
                serialPort_IO8.BaudRate = baudRate;
                serialPort_IO8.Open();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
