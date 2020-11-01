using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using System.Reflection;
using EasyNetQ.Internals;

namespace EasyNetQ.Tests
{
    public class ReflectionHelpersTests
    {
        [Fact]
        public void ShouldGetAttributes()
        {
            Assert.True(typeof(TestAttributedClass).GetAttributes<OneTestAttribute>().Any());
            Assert.True(typeof(TestAttributedClass).GetAttributes<AnotherTestAttribute>().Any());
        }

        [Fact]
        public void ShouldGetAttribute()
        {
            Assert.NotNull(typeof(TestAttributedClass).GetAttribute<OneTestAttribute>());
            Assert.NotNull(typeof(TestAttributedClass).GetAttribute<AnotherTestAttribute>());
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class OneTestAttribute : Attribute
    {
        public int Value
        {
            get { return 1; }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AnotherTestAttribute : Attribute
    {
        public int Value
        {
            get { return 1; }
        }
    }

    [OneTest, AnotherTest]
    public class TestAttributedClass
    {
    }
}