using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EasyNetQ.Monitor.Services;

namespace EasyNetQ.Monitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var rigService = new RigService();
            var rig = rigService.GetRig();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main(rig));
        }
    }
}
