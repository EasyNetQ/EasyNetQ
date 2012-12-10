// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class UserInfoTests
    {
        private UserInfo userInfo;
        private const string userName = "mike";
        private const string password = "topSecret";

        [SetUp]
        public void SetUp()
        {
            userInfo = new UserInfo(userName, password);
        }

        [Test]
        public void Should_have_correct_name_and_password()
        {
            userInfo.GetName().ShouldEqual(userName);
            userInfo.Password.ShouldEqual(password);
        }

        [Test]
        public void Should_be_able_to_add_tags()
        {
            userInfo.AddTag("administrator").AddTag("management");
            userInfo.Tags.ShouldEqual("administrator,management");
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void Should_not_be_able_to_add_the_same_tag_twice()
        {
            userInfo.AddTag("management").AddTag("management");
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void Should_not_be_able_to_add_incorrect_tags()
        {
            userInfo.AddTag("blah");
        }

        [Test]
        public void Should_have_a_default_tag_of_administrator()
        {
            userInfo.Tags.ShouldEqual("administrator");
        }
    }
}

// ReSharper restore InconsistentNaming