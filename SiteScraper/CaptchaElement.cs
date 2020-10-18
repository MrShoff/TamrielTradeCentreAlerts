using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SiteScraper
{
    public class CaptchaElement
    {
        public string XPath { get; set; }
        public string iFrameXPath { get; set; }
        public string SubmitXPath { get; set; }



        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MOUSEMOVE = 0x0001;


        private static readonly Size actualScreenSize = new Size(2560, 1440);
        public static void DoMouseClick(uint dx, uint dy)
        {
            uint x = (uint) (dx * 65536 * ((double)actualScreenSize.Width / 2560));
            uint y = (uint) (dy * 65536 * ((double)actualScreenSize.Height / 1440));

            mouse_event(MOUSEEVENTF_MOUSEMOVE, x, y, 0, 0);
            Thread.Sleep(500);
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }
    }
}
