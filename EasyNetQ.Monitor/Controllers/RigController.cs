using System;
using EasyNetQ.Monitor.Model;
using EasyNetQ.Monitor.Services;

namespace EasyNetQ.Monitor.Controllers
{
    public interface IRigController
    {
        IVHostController CreateNewVHost(string vHostName);
        void SaveRig();
    }

    public class RigController : IRigController
    {
        private readonly Rig rig;
        private readonly IRigService rigService;
        private readonly IVHostControllerFactory vHostControllerFactory;

        public RigController(IRigService rigService, IVHostControllerFactory vHostControllerFactory)
        {
            this.rigService = rigService;
            this.rig = rigService.GetRig();
            this.vHostControllerFactory = vHostControllerFactory;
        }

        public IVHostController CreateNewVHost(string vHostName)
        {
            var vHost = rig.CreateVHost(vHostName);
            return vHostControllerFactory.Create(vHost);
        }

        public void SaveRig()
        {
            rigService.SaveRig(rig);
        }
    }
}