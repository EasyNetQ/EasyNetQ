// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class PermissionInfoTests
    {
        private PermissionInfo permissionInfo;
        private User user;
        private Vhost vhost;

        [SetUp]
        public void SetUp()
        {
            user = new User {name = "mikey"};
            vhost = new Vhost {name = "theVHostName"};
            permissionInfo = new PermissionInfo(user, vhost);
        }

        [Test]
        public void Should_return_the_correct_user_name()
        {
            permissionInfo.GetUserName().ShouldEqual(user.name);
        }

        [Test]
        public void Should_return_the_correct_vhost_name()
        {
            permissionInfo.GetVirtualHostName().ShouldEqual(vhost.name);
        }

        [Test]
        public void Should_set_default_permissions_to_allow_all()
        {
            permissionInfo.configure.ShouldEqual(".*");
            permissionInfo.write.ShouldEqual(".*");
            permissionInfo.read.ShouldEqual(".*");
        }

        [Test]
        public void Should_be_able_to_set_deny_permissions()
        {
            var permissions = permissionInfo.DenyAllConfigure().DenyAllRead().DenyAllWrite();

            permissions.configure.ShouldEqual("^$");
            permissions.write.ShouldEqual("^$");
            permissions.read.ShouldEqual("^$");
        }

        [Test]
        public void Should_be_able_to_set_arbitrary_permissions()
        {
            var permissions = permissionInfo.Configure("abc").Read("def").Write("xyz");

            permissions.configure.ShouldEqual("abc");
            permissions.write.ShouldEqual("xyz");
            permissions.read.ShouldEqual("def");
        }
    }
}

// ReSharper restore InconsistentNaming