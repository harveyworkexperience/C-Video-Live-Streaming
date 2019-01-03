﻿using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Windows.Input;

namespace ImageUDPServer
{
    class Program
    {
        static private UdpClient udpserver;
        static private IPEndPoint importantEP;
        const int num_bytes = 65000;
        static bool serve_client = false;

        // Storing images as byte array
        static public string simple_image_paths = @"C:\Work Experience\JPEG_Images\img";
        static public byte[] img1 = ImageToByteArray(simple_image_paths + "5.jpg");
        static public byte[] img2 = ImageToByteArray(simple_image_paths + "3.jpg");

        // Animation images
        static public string animation_image_paths = @"C:\Work Experience\JPEG_Images\sample_animation\img";
        static public byte[][] images = LoadAnimation(animation_image_paths, "jpeg", 53);

        // Bad VR Video Images
        static public string vr_paths = @"C:\Work Experience\JPEG_Images\vr_images\img";
        static public byte[][] vr_images = null;//LoadAnimation(vr_paths, "jpeg", 1219);

        // Load a lot of images
        static byte[][] LoadAnimation(string pathname, string extension, int num_images)
        {
            byte[][] tmp_images = new byte[num_images][];
            for (int i=0; i<num_images; i++)
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
                tmp_images[i] = ImageToByteArray(tempPathName);
            }
            return tmp_images;
        }

        // Only serves one client
        static void Main(string[] args)
        {
            Console.Write("Image Data\n====================\n");
            Console.Write("img1: " + img1.Length + "\n");
            Console.Write("img2: " + img2.Length + "\n\n");

            for (int i=0; i<images.Length; i++)
            {
                Console.WriteLine("images[" + i + "]: " +images[i].Length);
            }
            Console.Write("\n");

            // Input Listener
            Thread _input_thread = new Thread(KeyInterrupt);
            _input_thread.SetApartmentState(ApartmentState.STA);
            _input_thread.Start();

            // Waiting for connection
            Console.WriteLine("SERVER\n=====================");
            udpserver = new UdpClient(11000);
            importantEP = null;
            while (true) WaitForClient();
        }

        static public byte[] ImageToByteArray(string imagepath)
        {
            Image img = Image.FromFile(imagepath);

            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        static public void WaitForClient()
        {
            var datagram = Encoding.ASCII.GetBytes("Yes we are connected.");
            var data = "Null";

            while (true)
            {
                var ep = new IPEndPoint(IPAddress.Any, 11000);
                data = Encoding.ASCII.GetString(udpserver.Receive(ref ep)); // Listen on port 11000
                Console.Write("Received data from " + ep.ToString() + "\n");
                Console.Write("data: " + data + "\n\n");

                // Replying back to important client
                if (data == "Are we connected yet?")
                {
                    udpserver.Send(datagram, datagram.Length, ep);
                    while (data != "Received response!")
                    {
                        data = Encoding.ASCII.GetString(udpserver.Receive(ref ep));
                    }
                    importantEP = ep;
                    serve_client = true;
                    ServeClientVideo();
                }
            }
        }

        static public void KeyInterrupt()
        {
            while (true)
            {
                if (Keyboard.IsKeyDown(Key.Q))
                {
                    serve_client = false;
                }
            }
        }

        static public void ServeClient()
        {
            // Sending image to important client
            Console.Write("Sending image bytes to client");
            var datagram = Encoding.ASCII.GetBytes("Sending image bytes!");
            udpserver.Send(datagram, datagram.Length, importantEP);
            while (serve_client)
            {
                int loop1_count = 0;
                int loop2_count = 0;
                Console.WriteLine("Sending image 1");
                for (int i = img1.Length; i > 0; i -= num_bytes)
                {
                    byte[] b = new byte[num_bytes];
                    if (i < num_bytes)
                        Array.Copy(img1, loop1_count * num_bytes, b, 0, i);
                    else
                        Array.Copy(img1, loop1_count * num_bytes, b, 0, num_bytes);
                    udpserver.Send(b, num_bytes, importantEP);
                    loop1_count++;
                }
                Console.WriteLine("Sending image 2");
                for (int i = img2.Length; i > 0; i -= num_bytes)
                {
                    byte[] b = new byte[num_bytes];
                    if (i < num_bytes)
                        Array.Copy(img2, loop2_count * num_bytes, b, 0, i);
                    else
                        Array.Copy(img2, loop2_count * num_bytes, b, 0, num_bytes);
                    udpserver.Send(b, num_bytes, importantEP);
                    loop2_count++;
                }
            }
        }

        static public void ServeClientVideo()
        {
            // Sending image to important client
            Console.Write("Sending image bytes to client");
            var datagram = Encoding.ASCII.GetBytes("Sending image bytes!");
            udpserver.Send(datagram, datagram.Length, importantEP);
            while (serve_client)
            {
                for (int i = 0; i<images.Length; i++)
                {
                    int loop_count = 0;
                    Console.WriteLine("Sending image "+(i+1).ToString());
                    for (int j=images[i].Length; j > 0; j -= num_bytes)
                    {
                        byte[] b = new byte[num_bytes];
                        if (j < num_bytes)
                            Array.Copy(images[i], loop_count * num_bytes, b, 0, j);
                        else
                            Array.Copy(images[i], loop_count * num_bytes, b, 0, num_bytes);
                        udpserver.Send(b, num_bytes, importantEP);
                        loop_count++;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
        static public void ServeClientVideoVR()
        {
            // Sending image to important client
            Console.Write("Sending image bytes to client");
            var datagram = Encoding.ASCII.GetBytes("Sending image bytes!");
            udpserver.Send(datagram, datagram.Length, importantEP);
            while (serve_client)
            {
                for (int i = 0; i < vr_images.Length; i++)
                {
                    int loop_count = 0;
                    Console.WriteLine("Sending image " + (i + 1).ToString());
                    for (int j = vr_images[i].Length; j > 0; j -= num_bytes)
                    {
                        byte[] b = new byte[num_bytes];
                        if (j < num_bytes)
                            Array.Copy(vr_images[i], loop_count * num_bytes, b, 0, j);
                        else
                            Array.Copy(vr_images[i], loop_count * num_bytes, b, 0, num_bytes);
                        udpserver.Send(b, num_bytes, importantEP);
                        loop_count++;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}