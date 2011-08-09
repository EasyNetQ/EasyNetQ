using System;
using System.ComponentModel;
using System.Configuration.Install;

namespace EasyNetQ.Hosepipe.SetupActions
{
    [RunInstaller(true)]
    public class HosepipeInstaller : Installer
    {
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            var targetDirectory = Context.Parameters["targetdir"];
            if(targetDirectory == null)
            {
                throw new InstallException("Target directory not set");
            }
            SetPath(TrimPath(targetDirectory));
        }

        private void SetPath(string path)
        {
            var pathEnv = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine);
            Console.Out.WriteLine("pathEnv = {0}", pathEnv);
            var newPathEnv = pathEnv + ";" + path;
            Environment.SetEnvironmentVariable("path", newPathEnv, EnvironmentVariableTarget.Machine);
        }

        public string TrimPath(string path)
        {
            path = path.Trim();
            if(path.EndsWith(@"\"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            return path;
        }
    }
}