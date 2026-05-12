using System;
using System.Threading;
using System.Windows.Forms;

namespace RaceBeam
{
    internal sealed class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
