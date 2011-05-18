using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using EasyNetQ.Monitor.Model;

namespace EasyNetQ.Monitor.Services
{
    public interface IRigService
    {
        Rig GetRig();
        void SaveRig(Rig rig);
    }

    public class RigService : IRigService
    {
        private const string rigFileName = "easynetq.monitor.rig";

        public Rig GetRig()
        {
            var store = GetStore();
            if(store.FileExists(rigFileName))
            {
                Rig rig;
                using (var stream = store.OpenFile(rigFileName, FileMode.Open, FileAccess.Read))
                {
                    var formatter = new BinaryFormatter();
                    try
                    {
                        rig = (Rig)formatter.Deserialize(stream);
                    }
                    catch (SerializationException)
                    {
                        // if the file has been corrupted, just delete it and return a new rig
                        store.DeleteFile(rigFileName);
                        rig = new Rig();
                    }
                }
                return rig;
            }
            else
            {
                return new Rig();
            }
        }

        public void SaveRig(Rig rig)
        {
            var store = GetStore();
            if(store.FileExists(rigFileName))
            {
                store.DeleteFile(rigFileName);
            }
            using (var stream = store.OpenFile(rigFileName, FileMode.CreateNew, FileAccess.Write))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, rig);
            }
        }

        public IsolatedStorageFile GetStore()
        {
            return
                IsolatedStorageFile.GetStore(
                    IsolatedStorageScope.User | 
                    IsolatedStorageScope.Domain | 
                    IsolatedStorageScope.Assembly, 
                    null, null);
        }
    }
}