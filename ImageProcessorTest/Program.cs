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
    private const int num_frames = 2;                                           // Number of frames to swap out
    private enum TexFlags : int { free = 0, ready = 1, busy = 2 };              // The possible states for a texture
    private byte[][] images = { new byte[image_size], new byte[image_size] };   // The frames used to store images as bytes
    private int frame_ind = 0;                                                  // The index of the current frame we're working on
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
            //Debug.Log(ByteArrayToString(received_bytes));
            received_bytes_ptr = 0;
        }
        num_packets++;
        return received_bytes[received_bytes_ptr++];
    }

    // Function for populating byte array with JPEG bytes
    // Needs to be run on another thread, in the background.
    void Build_Images_JPEG()
    {
        while (true)
        {
            byte b1 = GetStreamByte();
            byte b2 = GetStreamByte();

            // Start working on making a new image
            if (b1 == 0xff && b2 == 0xd8 && image_state_arr[frame_ind] == TexFlags.free)
            {
                // Initialising image
                int byte_count = 0;
                images[frame_ind] = new byte[image_size];
                images[frame_ind][byte_count++] = 0xff;
                images[frame_ind][byte_count++] = 0xd8;

                // Building image
                int end_flag = 0;
                while (byte_count < image_size)
                {
                    b1 = GetStreamByte();
                    images[frame_ind][byte_count++] = b1;
                    if (b1 == 0xff && end_flag == 0)
                        end_flag++;
                    else if (b1 == 0xd9 && end_flag == 1)
                        break;
                    else
                        end_flag = 0;
                }

                // Completing image
                if (byte_count == image_size - 1)
                {
                    images[frame_ind][image_size - 2] = 0xff;
                    images[frame_ind][image_size - 1] = 0xd9;
                }
                image_state_arr[frame_ind] = TexFlags.ready;
                frame_ind = (frame_ind + 1) % num_frames;
                return;
            }
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
            Console.WriteLine("Exception caught in process: {0}", ex);
            return false;
        }
    }

    static void Main(string[] args)
    {
        Console.WriteLine("CLIENT\n=====================");

        Client c = new Client();

        if (c.ConnectToStreamService())
        {
            c.Build_Images_JPEG();
        }
        ByteArrayToFile(@"C:\Work Experience\JPEG_Images", c.received_bytes);
    }
}