using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Castle.Windsor;
using Castle.Windsor.Installer;
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

            using(var container = new WindsorContainer())
            {
                container.Install(FromAssembly.This());

                var mainForm = container.Resolve<Main>();

                Application.Run(mainForm);
            }
        }
    }
}
