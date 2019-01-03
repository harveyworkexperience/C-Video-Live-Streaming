using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Timers;

class Client
{
    private static string path = @"C:\Work Experience\JPEG_Images\sample_animation/img00";
    private static Image animated_img = Image.FromFile(@"C:\Work Experience\JPEG_Images\animation/giphy-0000.jpg");
    private static Image img1 = Image.FromFile(@"C:\Work Experience\JPEG_Images\img5.jpg");
    private static Image img2 = Image.FromFile(@"C:\Work Experience\JPEG_Images\img3.jpg");
    private static PictureBox pb;
    private static int image_state = 1;

    // Frame Variables
    private const int image_size = 5000000;                                     // Number of bytes for an image
    public const int num_frames = 2;                                            // Number of frames to swap out
    private enum TexFlags : int { free = 0, ready = 1, busy = 2 };              // The possible states for a texture
    private byte[] image = new byte[image_size];
    private TexFlags[] image_state_arr = { TexFlags.free, TexFlags.free };      // The state of the frames - used for deciding on whether to overwrite them or not

    private static System.Windows.Forms.Timer timer;

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

    private static void Display_Image(string imagepath)
    {
        using (Form form = new Form())
        {
            Image img = Image.FromFile(imagepath);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Size = img.Size;

            pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.Image = img;

            form.Controls.Add(pb);
            form.ShowDialog();
        }
    }
    static void Main(string[] main_args)
    {
        Console.WriteLine("Starting timer...");
        timer = new System.Windows.Forms.Timer();
        timer.Interval = 100; // specify interval time as you want
        timer.Tick += (sender, args) =>
        {
            Console.WriteLine("Switching image!");
            if (image_state < 10)
                pb.Image = Image.FromFile(path + "0" + image_state.ToString() + ".jpeg");
            else
                pb.Image = Image.FromFile(path + image_state.ToString() + ".jpeg");
            image_state = (image_state+1)%54;
            if (image_state == 0) image_state = 1;
            pb.Refresh();
        };
        timer.Start();
        Console.WriteLine("Timer has started!");

        Display_Image(path+"01.jpeg");

        timer.Stop();
    }
}