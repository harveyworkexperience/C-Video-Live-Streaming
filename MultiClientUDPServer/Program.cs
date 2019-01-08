using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Windows.Input;

namespace MultiClientUDPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SERVER\n=====================");
            ReceiveMessages();
            while (true) ;
        }

        public struct UdpState
        {
            public UdpClient u;
            public IPEndPoint ep;
        }

        public static void ReceiveCallback(IAsyncResult ar)
        {
            UdpState s = ((UdpState)(ar.AsyncState));

            byte[] receiveBytes = { };
            try
            {
                receiveBytes = s.u.EndReceive(ar, ref s.ep);
            }
            catch
            {
                // Re-Listening
                while (true)
                {
                    try
                    {
                        s.u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
                        break;
                    }
                    catch
                    {
                        //Console.WriteLine("Unable to re-listen. Retrying...");
                    }
                }
                return;
            }
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            // Receiving any messages
            s.u.BeginReceive(new AsyncCallback(ReceiveCallback), s);

            Console.WriteLine($"Received: {receiveString}");

            // Broadcasting image
            if (receiveString == "Send me stuff!")
            {
                byte[] datagram = Encoding.ASCII.GetBytes("Okay here you go!");
                try
                {
                    s.u.Send(datagram, datagram.Length, s.ep);
                }
                catch
                {
                    Console.WriteLine("Unable to send datagram to {0}!", s.ep.ToString());
                    return;
                }
                Thread.Sleep(500);
                while (true)
                {
                    datagram = Encoding.ASCII.GetBytes("Blah blah blah");
                    try
                    {
                        s.u.Send(datagram, datagram.Length, s.ep);
                    }
                    catch
                    {
                        Console.WriteLine("Unable to send datagram to {0}!", s.ep.ToString());
                        return;
                    }
                    Thread.Sleep(500);
                }
            }
        }

        public static void ReceiveMessages()
        {
            // Receive a message and write it to the console.
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 11000);
            UdpClient u = new UdpClient(ep);

            UdpState s = new UdpState();
            s.ep = ep;
            s.u = u;

            Console.WriteLine("listening for messages");
            u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
        }
    }
}
