using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

class Client
{
    // Connection Variables
    private bool connection_success = false;
    private UdpClient udpclient = new UdpClient();
    private IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening
    public byte[] received_bytes;
    private int num_packets = 0;
    private int received_bytes_ptr = 0;

    // Frame Variables
    private const int image_size = 5000000;                                     // Number of bytes for an image
    public const int num_frames = 2;                                           // Number of frames to swap out
    private enum TexFlags : int { free = 0, ready = 1, busy = 2 };              // The possible states for a texture
    private byte[] image = new byte[image_size];
    private TexFlags[] image_state_arr = { TexFlags.free, TexFlags.free };      // The state of the frames - used for deciding on whether to overwrite them or not

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
    byte GetStreamByte()
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
    void Build_Images_JPEG()
    {
        Console.WriteLine("Building image from JPEG bytes...");
        while (true)
        {
            byte b1 = GetStreamByte();
            byte b2 = GetStreamByte();

            // Start working on making a new image
            if (b1 == 0xff && b2 == 0xd8 && image_state_arr[0] == TexFlags.free)
            {
                // Initialising image
                int byte_count = 0;
                image = new byte[image_size];
                image[byte_count++] = 0xff;
                image[byte_count++] = 0xd8;

                // Building image
                int end_flag = 0;
                while (byte_count < image_size)
                {
                    b1 = GetStreamByte();
                    image[byte_count++] = b1;
                    if (b1 == 0xff && end_flag == 0)
                        end_flag++;
                    else if (b1 == 0xd9 && end_flag == 1)
                        break;
                    else
                        end_flag = 0;
                }
                Console.WriteLine(byte_count);
                // Completing image
                if (byte_count == image_size - 1)
                {
                    image[image_size - 2] = 0xff;
                    image[image_size - 1] = 0xd9;
                }
                else Array.Resize<byte>(ref image, byte_count);
                image_state_arr[0] = TexFlags.ready;
                return;
            }
        }
    }

    public void SendMessageToServer(string msg)
    {
        var datagram = Encoding.ASCII.GetBytes(msg);
        udpclient.Send(datagram, datagram.Length);
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
            Console.WriteLine("Exception caught in process: {0}", ex);
            return false;
        }
    }

    static void Main(string[] args)
    {
        Console.WriteLine("CLIENT\n=====================");

        Client c = new Client();

        Console.WriteLine("Connecting to server...");
        if (c.ConnectToStreamService())
        {
            Console.WriteLine("Connected to server!");
            c.Build_Images_JPEG();
        }
        Console.WriteLine("Finished building image and now saving it...");
        Console.WriteLine(c.image.Length);
        ByteArrayToFile(@"C:\Work Experience\JPEG_Images\img1_tmp.jpg", c.image);
        Console.WriteLine("Image saved!");
        System.Threading.Thread.Sleep(100000);
    }
}