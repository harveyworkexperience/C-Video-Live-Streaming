using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bb = { 0x00, 0x00, 0x99, 0xff, 0xd8, 0x11, 0xff, 0xdd, 0x39, 0xff, 0xd8, 0xff, 0xff };
            // Looking for JPEG headers
            int found_jpg_header = 0;
            bool something = false;
            byte b;
            for (int i=0; i<bb.Length; i++)
            {
                b = bb[i];
                Console.WriteLine("Looking at: {0}", b.ToString("X2"));
                if (b == 0xff)
                    found_jpg_header = 1;
                else if (b == 0xd8 && found_jpg_header == 1)
                {
                    something = true;
                    break;
                }
                else
                    found_jpg_header = 0;
            }
            Console.WriteLine(something);
            Console.ReadLine();
        }
    }
}
