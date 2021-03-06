﻿using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Windows.Input;
using System.Collections.Generic;

namespace MultiClientUDPServer
{
    class Program
    {
        // Important variables
        private const bool DEBUGMODE = false;
        const int num_bytes = 65000;
        static bool serve_client = false;
        static private string current_dir_path = System.AppDomain.CurrentDomain.BaseDirectory;
        static public bool _interrupt_thread_status = false;
        static AutoResetEvent autoEvent = new AutoResetEvent(false);
        static List<UdpState> AllUDPStates = new List<UdpState>();

        // Storing images as byte array
        static public string simple_image_paths = current_dir_path + @"..\..\..\JPEG_Images\img";
        static public byte[] img1;
        static public byte[] img2;

        // Animation images
        static public string animation_image_paths = current_dir_path + @"..\..\..\JPEG_Images\sample_animation\img";
        static public byte[][] images;

        // Bad VR Video Images
        static public string vr_paths = @"C:\Work Experience\JPEG_Images\med_quality_vr\img";
        static public byte[][] vr_images;

        // Server Video Settings
        static public int Current = 1;
        static public List<Thread> allThreads = new List<Thread>();

        // Main
        static void Main(string[] args)
        {
            // Setting up bytes for server to send across
            Console.WriteLine("Setting up server ...");
            img1 = ImageToByteArray(simple_image_paths + "5.jpg");
            img2 = ImageToByteArray(simple_image_paths + "3.jpg");
            images = LoadAnimation(animation_image_paths, "jpeg", 53);
            vr_images = vr_images = LoadAnimation(vr_paths, "jpg", 1219);

            Console.WriteLine("\n\n=====================\nSERVER\n=====================");
            ReceiveMessages();

            // Input Listener
            Thread _input_thread = new Thread(KeyInterrupt);
            _input_thread.SetApartmentState(ApartmentState.STA);
            _interrupt_thread_status = true;
            _input_thread.Start();

            // Making main thread sleep indefinitely
            serve_client = true;
            while (serve_client)
            {
                autoEvent.WaitOne();
            }
            Console.WriteLine("Closing server...");

            Console.WriteLine("Startin server clean up...");

            // Cleaning up
            _interrupt_thread_status = false;
            Console.Write("Joining user input thread ...");
            _input_thread.Join();
            Console.WriteLine("User input thread has been joined!");
            serve_client = false;
            for (int i = 0; i < allThreads.Count; i++)
            {
                Console.Write("Joining thread {0}...", i);
                allThreads[i].Join();
                Console.WriteLine("Finished joining thread {0}!", i);
            }
            Console.Write("Clearing all stored UDPStates...");
            AllUDPStates.Clear();
            Console.WriteLine("Finished clearing all stored UDPStates!");
            Console.WriteLine("Finished cleaning up!");

            // Closing server final messages
            Console.Write("Server is about to be closed. Please press enter to confirm closing it...");
            Console.ReadLine();
        }

        // Handling User Inputs

        static public void KeyInterrupt()
        {
            while (_interrupt_thread_status)
            {
                if (Keyboard.IsKeyDown(Key.Q))
                {
                    serve_client = false;
                    autoEvent.Set();
                }

                else if (Keyboard.IsKeyDown(Key.D1))
                {
                    if (Current != 0)
                    {
                        Current = 0;
                    }
                }
                else if (Keyboard.IsKeyDown(Key.D2))
                {
                    if (Current != 1)
                    {
                        Current = 1;
                    }
                }
                else if (Keyboard.IsKeyDown(Key.D3))
                {
                    if (Current != 2)
                    {
                        Current = 2;
                    }
                }
            }
        }

        // UDP Server Functions
        public struct UdpState
        {
            public UdpClient u;
            public IPEndPoint ep;
        }

        public static void ReceiveCallback(IAsyncResult ar)
        {
            UdpState s = ((UdpState)(ar.AsyncState));
            AllUDPStates.Add(s);

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
            while (true)
            {
                try
                {
                    s.u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
                    break;
                }
                catch { }
            }

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
                allThreads.Add(StartServingClientThread(s));
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

            AllUDPStates.Add(s);

            Console.WriteLine("listening for connections...");
            u.BeginReceive(new AsyncCallback(ReceiveCallback), s);
        }

        // Serving Client Functions

        static public Thread StartServingClientThread(UdpState s)
        {
            Console.WriteLine("Starting new thread to serve client at: "+s.ep.ToString());
            Thread t = new Thread(() => ServeClient(s));
            t.Start();
            return t;
        }

        // Load a lot of images that make up a MJPEG
        static byte[][] LoadAnimation(string pathname, string extension, int num_images)
        {
            Console.WriteLine("Loading " + num_images + " images at path " + pathname + "XXXX." + extension + " ...");
            byte[][] tmp_images = new byte[num_images][];
            for (int i = 0; i < num_images; i++)
            {
                string tempPathName = pathname;
                if (i < 9)
                    tempPathName += "000" + (i + 1).ToString() + "." + extension;
                else if (i < 99)
                    tempPathName += "00" + (i + 1).ToString() + "." + extension;
                else if (i < 999)
                    tempPathName += "0" + (i + 1).ToString() + "." + extension;
                else
                    tempPathName += (i + 1).ToString() + "." + extension;
                Console.Write("          ");
                tmp_images[i] = ImageToByteArray(tempPathName);
            }
            Console.WriteLine("Successfully loaded " + num_images + " images at path " + pathname + "XXXX." + extension + "!");
            return tmp_images;
        }

        static public byte[] ImageToByteArray(string imagepath)
        {
            Console.Write("Loading image at {0} ... ", imagepath);
            Image img = Image.FromFile(imagepath);

            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                Console.WriteLine("Image successfully loaded!");
                return ms.ToArray();
            }
        }

        static public void ServeClient(UdpState s)
        {
            while (serve_client)
            {
                int currentlyPlaying = Current;
                if (Current == 0)
                {
                    Console.WriteLine("Now serving client simple images!");
                    ServeClientSimple(s, ref currentlyPlaying);
                }
                else if (Current == 1)
                {
                    Console.WriteLine("Now serving client animation video images!");
                    ServeClientVideo(s, ref currentlyPlaying);
                }
                else if (Current == 2)
                {
                    Console.WriteLine("Now serving client VR video images!");
                    ServeClientVideoVR(s, ref currentlyPlaying);
                }
            }
        }

        static public void ServeClientSimple(UdpState s, ref int currentlyPlaying)
        {
            // Sending image to important client
            Console.WriteLine("Sending image bytes to client at {0}", s.ep.ToString());
            var datagram = Encoding.ASCII.GetBytes("Sending image bytes!");
            try
            {
                s.u.Send(datagram, datagram.Length, s.ep);
            }
            catch
            {
                return;
            }
            while (serve_client && currentlyPlaying == Current)
            {
                int loop1_count = 0;
                int loop2_count = 0;
                if (DEBUGMODE) Console.WriteLine("Sending image 1");
                for (int i = img1.Length; i > 0; i -= num_bytes)
                {
                    byte[] b = new byte[num_bytes];
                    if (i < num_bytes)
                        Array.Copy(img1, loop1_count * num_bytes, b, 0, i);
                    else
                        Array.Copy(img1, loop1_count * num_bytes, b, 0, num_bytes);
                    if (currentlyPlaying != Current) return;
                    try
                    {
                        s.u.Send(b, num_bytes, s.ep);
                    }
                    catch
                    {
                        return;
                    }
                    loop1_count++;
                }
                if (DEBUGMODE) Console.WriteLine("Sending image 2");
                for (int i = img2.Length; i > 0; i -= num_bytes)
                {
                    byte[] b = new byte[num_bytes];
                    if (i < num_bytes)
                        Array.Copy(img2, loop2_count * num_bytes, b, 0, i);
                    else
                        Array.Copy(img2, loop2_count * num_bytes, b, 0, num_bytes);
                    if (currentlyPlaying != Current) return;
                    try
                    {
                        s.u.Send(b, num_bytes, s.ep);
                    }
                    catch
                    {
                        return;
                    }
                    loop2_count++;
                }
            }
        }

        static public void ServeClientVideo(UdpState s, ref int currentlyPlaying)
        {
            // Sending image to important client
            Console.WriteLine("Sending image bytes to client at {0}", s.ep.ToString());
            var datagram = Encoding.ASCII.GetBytes("Sending image bytes!");
            try
            {
                s.u.Send(datagram, datagram.Length, s.ep);
            }
            catch
            {
                return;
            }
            while (serve_client && currentlyPlaying == Current)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    int loop_count = 0;
                    if (DEBUGMODE) Console.WriteLine("Sending image " + (i + 1).ToString());
                    for (int j = images[i].Length; j > 0; j -= num_bytes)
                    {
                        byte[] b = new byte[num_bytes];
                        if (j < num_bytes)
                            Array.Copy(images[i], loop_count * num_bytes, b, 0, j);
                        else
                            Array.Copy(images[i], loop_count * num_bytes, b, 0, num_bytes);
                        if (currentlyPlaying != Current) return;
                        try
                        {
                            s.u.Send(b, num_bytes, s.ep);
                        }
                        catch
                        {
                            return;
                        }
                        loop_count++;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
        static public void ServeClientVideoVR(UdpState s, ref int currentlyPlaying)
        {
            // Sending image to important client
            Console.WriteLine("Sending image bytes to client at {0}", s.ep.ToString());
            var datagram = Encoding.ASCII.GetBytes("Sending image bytes!");
            try
            {
                s.u.Send(datagram, datagram.Length, s.ep);
            }
            catch
            {
                return;
            }
            while (serve_client && currentlyPlaying == Current)
            {
                for (int i = 0; i < vr_images.Length; i++)
                {
                    int loop_count = 0;
                    if (DEBUGMODE) Console.WriteLine("Sending image " + (i + 1).ToString());
                    for (int j = vr_images[i].Length; j > 0; j -= num_bytes)
                    {
                        byte[] b = new byte[num_bytes];
                        if (j < num_bytes)
                            Array.Copy(vr_images[i], loop_count * num_bytes, b, 0, j);
                        else
                            Array.Copy(vr_images[i], loop_count * num_bytes, b, 0, num_bytes);
                        if (currentlyPlaying != Current) return;
                        try
                        {
                            s.u.Send(b, num_bytes, s.ep);
                        }
                        catch
                        {
                            return;
                        }
                        loop_count++;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
