namespace EasyNetQ.Management.Client.Model
{
    public class PermissionInfo
    {
        public string configure { get; private set; }
        public string write { get; private set; }
        public string read { get; private set; }

        private readonly User user;
        private readonly Vhost vhost;

        private const string denyAll = "^$";
        private const string allowAll = ".*";

        public PermissionInfo(User user, Vhost vhost)
        {
            this.user = user;
            this.vhost = vhost;

            configure = write = read = allowAll;
        }

        public string GetUserName()
        {
            return user.name;
        }

        public string GetVirtualHostName()
        {
            return vhost.name;
        }

        public PermissionInfo Configure(string resourcesToAllow)
        {
            configure = resourcesToAllow;
            return this;
        }

        public PermissionInfo Write(string resourcedToAllow)
        {
            write = resourcedToAllow;
            return this;
        }

        public PermissionInfo Read(string resourcesToAllow)
        {
            read = resourcesToAllow;
            return this;
        }


        public PermissionInfo DenyAllConfigure()
        {
            configure = denyAll;
            return this;
        }

        public PermissionInfo DenyAllWrite()
        {
            write = denyAll;
            return this;
        }

        public PermissionInfo DenyAllRead()
        {
            read = denyAll;
            return this;
        }
    }
}