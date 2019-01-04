using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

class Client
{
    // Important Variable(s)
    static private string current_dir_path = System.AppDomain.CurrentDomain.BaseDirectory;
    private const bool DEBUGMODE = false;

    // Connection Variables
    private bool connection_success = false;
    private static UdpClient udpclient = new UdpClient();
    // Localhost - 127.0.0.1
    // To check current IPv4 Address, open Command Prompt and type in ipconfig (Wireless LAN adapter WiFi)
    private static IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening
    public static byte[] received_bytes;
    private static int num_packets = 0;
    private static int received_bytes_ptr = 0;
    private static bool is_running = false;
    private static Thread _thread;

    // Frame Variables
    private const int image_size = 5000000; // Maximum number of bytes for an image
    private static byte[] image = new byte[image_size];
    private static byte[] ready_image;
    private static int img_number = 0;
    private static string tmp_image_paths = current_dir_path + @"..\..\..\JPEG_Images\temp_images\img";

    // Display Variables
    private static System.Windows.Forms.Timer timer;
    private static PictureBox pb;

    // State Variables
    private static bool[] image_used = { false, false };
    private static bool is_ready = false;

    // Synchronisation Variables
    private static Mutex mtx = new Mutex();

    // Function for connecting to a UDP stream server
    bool ConnectToStreamService()
    {
        // Connecting to service
        try
        {
            udpclient.Connect(ep);
            connection_success = true;
        }
        catch (Exception ex)
        {
            Console.Write(ex);
        }

        // Connection success
        if (connection_success)
        {
            // Waiting for response from server
            byte[] datagram = Encoding.ASCII.GetBytes("Are we connected yet?");
            udpclient.Send(datagram, datagram.Length);
            var data = Encoding.ASCII.GetString(udpclient.Receive(ref ep));
            while (data != "Yes we are connected.")
            {
                udpclient.Send(datagram, datagram.Length);
                data = Encoding.ASCII.GetString(udpclient.Receive(ref ep));
            }
            // Requesting byte stream
            datagram = Encoding.ASCII.GetBytes("Received response!");
            udpclient.Send(datagram, datagram.Length);
            data = Encoding.ASCII.GetString(udpclient.Receive(ref ep));
            while (data.Length <= 0)
            {
                udpclient.Send(datagram, datagram.Length);
                data = Encoding.ASCII.GetString(udpclient.Receive(ref ep));
            }
        }
        return connection_success;
    }

    // Function that returns the byte of the stream
    static byte GetStreamByte()
    {
        // Fetching bytes to store in received_bytes array
        if (received_bytes == null || received_bytes_ptr >= received_bytes.Length)
        {
            received_bytes = udpclient.Receive(ref ep);
            received_bytes_ptr = 0;
            num_packets++;
        }
        return received_bytes[received_bytes_ptr++];
    }

    // Function for populating byte array with JPEG bytes
    // Needs to be run on another thread, in the background.
    static void Build_Images_JPEG()
    {
        if (DEBUGMODE) Console.WriteLine("T: Building image from JPEG bytes...");
        while (is_running)
        {
            // Looking for JPEG headers
            int found_jpg_header = 0;
            byte b;
            if (DEBUGMODE) Console.WriteLine("T: Looking for JPEG headers...");
            while (true)
            {
                b = GetStreamByte();
                if (b == 0xff)
                    found_jpg_header = 1;
                else if (b == 0xd8 && found_jpg_header == 1)
                    break;
                else
                    found_jpg_header = 0;
            }

            // Start working on making a new image
            if (DEBUGMODE) Console.WriteLine("T: Found JPEG headers!");
            // Initialising image
            int byte_count = 0;
            image = new byte[image_size];
            image[byte_count++] = 0xff;
            image[byte_count++] = 0xd8;

            // Building image
            int end_flag = 0;
            if (DEBUGMODE) Console.WriteLine("T: Retrieving image bytes...");
            while (byte_count < image_size)
            {
                byte tmp_b = GetStreamByte();
                image[byte_count++] = tmp_b;
                if (tmp_b == 0xff && end_flag == 0)
                    end_flag++;
                else if (tmp_b == 0xd9 && end_flag == 1)
                    break;
                else
                    end_flag = 0;
            }
            // Completing image
            if (byte_count == image_size - 1)
            {
                image[image_size - 2] = 0xff;
                image[image_size - 1] = 0xd9;
            }

            // Storing completed image into another byte array
            else Array.Resize<byte>(ref image, byte_count);
            if (DEBUGMODE) Console.WriteLine("T: Retrieved image bytes!");
            ready_image = new byte[byte_count];
            Array.Copy(image, ready_image, byte_count);

            // Saving image
            if (DEBUGMODE) Console.WriteLine("T: Finished building image and now saving it...");
            if (DEBUGMODE) Console.WriteLine("T: " + image.Length);
            // Checking if it is possible to save image and then saving it to one of the two tmp files
            if (!ByteArrayToFile(tmp_image_paths + "0_tmp.jpg", ready_image))
                ByteArrayToFile(tmp_image_paths + "1_tmp.jpg", ready_image);
            if (DEBUGMODE) Console.WriteLine("T: Image saved!");

            // Signalling image is ready to be used
            mtx.WaitOne();
            is_ready = true;
            mtx.ReleaseMutex();

            // Resetting
            System.Threading.Thread.Sleep(50);
            continue;
        }
    }

    static public bool ByteArrayToFile(string fileName, byte[] byteArray)
    {
        try
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(byteArray, 0, byteArray.Length);
                return true;
            }
        }
        catch (Exception ex)
        {
            // Console.WriteLine("Exception caught in process: {0}", ex);
            Console.WriteLine("{0} is currently being used!", fileName);
            return false;
        }
    }

    private static void Display_Image(string imagepath)
    {
        using (Form form = new Form())
        {
            Image img = Image.FromFile(imagepath);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Size = img.Size;
            pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            try
            {
                pb.Image = img;
            }
            catch (Exception ex)
            {
                pb.Image = null;
            }
            form.Controls.Add(pb);
            form.ShowDialog();
        }
    }

    static void Main(string[] args)
    {
        // Naming title of application
        Console.Title = "Multi-Image UDP Client";

        // Setting up
        Console.WriteLine("CLIENT\n=====================");

        Client c = new Client();
        _thread = new Thread(Build_Images_JPEG);

        Console.WriteLine("Connecting to server...");
        if (c.ConnectToStreamService())
        {
            Console.WriteLine("Connected to server!");
            is_running = true;
            _thread.Start();
        }
        else
        {
            Console.WriteLine("Unable to connect to server!");
            Console.WriteLine("Exiting.");
            return;
        }

        // Setting timer
        Console.WriteLine("Starting timer...");
        timer = new System.Windows.Forms.Timer();
        timer.Interval = 50; // specify interval time for tick to be called
        timer.Tick += (sender, tick_args) =>
        {
            if (is_ready)
            {
                // Freeing up resources
                bool disposed = false;
                if (pb.Image != null)
                {
                    pb.Image.Dispose();
                    disposed = true;
                }

                // Switching image
                if (DEBUGMODE) Console.Write("\n");
                if (DEBUGMODE) Console.WriteLine("Switching image!");
                if (img_number == 0)
                {
                    try
                    {
                        pb.Image = Image.FromFile(tmp_image_paths + "0_tmp.jpg");
                        pb.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to switch image!");
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    try
                    {
                        pb.Image = Image.FromFile(tmp_image_paths + "1_tmp.jpg");
                        pb.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to switch image!");
                        Console.WriteLine(ex);
                    }
                }

                // Setting up for next frame
                img_number = (img_number + 1) % 2;
                mtx.WaitOne();
                is_ready = false;
                mtx.ReleaseMutex();

                // Forcing a redraw
                if (!disposed)
                    pb.Refresh();
                else
                    disposed = false;
            }
        };
        timer.Start();
        Console.WriteLine("Timer has started!");

        // Displaying image
        while (!is_ready) ;
        Console.WriteLine("Displaying image(s)...");
        Display_Image(tmp_image_paths + "0_tmp.jpg");

        // Closing Thread
        is_running = false;
        _thread.Join();
        Console.Write("\n");
        Console.WriteLine("Waiting 10 seconds...");
        System.Threading.Thread.Sleep(10000);

        // Cleaning up
        pb.Image.Dispose();
        Console.WriteLine("Deleting temporary images...");
        try
        {
            if (DEBUGMODE) Console.WriteLine("{0}! has been deleted!", tmp_image_paths + "0_tmp.jpg");
            File.Delete(tmp_image_paths + "0_tmp.jpg");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to delete {0}!", tmp_image_paths + "0_tmp.jpg");
            Console.WriteLine(ex);
        }
        try
        {
            if (DEBUGMODE) Console.WriteLine("{0} has been deleted!", tmp_image_paths + "1_tmp.jpg");
            File.Delete(tmp_image_paths + "1_tmp.jpg");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to delete {0}!", tmp_image_paths + "1_tmp.jpg");
            Console.WriteLine(ex);
        }
        Console.Write("Press enter to quit...");
        Console.ReadLine();
    }
}