using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ReflectionHelpersTests
    {
        [Test]
        public void ShouldCreateClassWithDefaultConstructor()
        {
            ReflectionHelpers.CreateInstance<ClassWithDefaultConstuctor>();
        }

        [Test]
        [ExpectedException(typeof(MissingMethodException))]
        public void ShouldFailToCreateClassWithoutDefaultConstructor()
        {
            ReflectionHelpers.CreateInstance<ClassWithoutDefaultConstuctor>();
        }

        [Test, Explicit("Fails on build server.")]
        public void ShouldPerformFasterThanActivator()
        {
            // warmup
            for (var i = 0; i < 10; ++i)
            {
                Activator.CreateInstance<ClassWithDefaultConstuctor>();
                ReflectionHelpers.CreateInstance<ClassWithDefaultConstuctor>();
            }
            // warmup
            var count = 0;
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                count += ReflectionHelpers.CreateInstance<ClassWithDefaultConstuctor>().Value;
            }
            stopWatch.Stop();
            var creatorTime = stopWatch.Elapsed;
            stopWatch.Reset();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                count += Activator.CreateInstance<ClassWithDefaultConstuctor>().Value;
            }
            stopWatch.Stop();
            var activator = stopWatch.Elapsed;
            Assert.IsTrue(creatorTime < activator);
            Assert.AreEqual(2000000, count);
        }

        [Test]
        public void ShouldGetAttributes()
        {
            Assert.IsTrue(typeof(TestAttributedClass).GetAttributes<OneTestAttribute>().Any());
            Assert.IsTrue(typeof(TestAttributedClass).GetAttributes<AnotherTestAttribute>().Any());
        }

        [Test, Explicit("Fails on build server")]
        public void ShouldPerformFasterThanGetCustomAttributes()
        {
            var type = typeof(TestAttributedClass);
            // warmup
            for (var i = 0; i < 10; ++i)
            {
                type.GetCustomAttributes(typeof(OneTestAttribute), true);
                type.GetAttributes<OneTestAttribute>();
            }
            // warmup
            var count = 0;
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                count += (type.GetCustomAttributes(typeof(OneTestAttribute), true).SingleOrDefault() as OneTestAttribute).Value;
            }
            stopWatch.Stop();
            var getCustomAttributesTime = stopWatch.Elapsed;
            stopWatch.Reset();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                count += (type.GetAttributes<OneTestAttribute>().SingleOrDefault()).Value;
            }
            stopWatch.Stop();
            var getAttributesTime = stopWatch.Elapsed;
            Assert.IsTrue(getAttributesTime + getAttributesTime < getCustomAttributesTime);
            Assert.AreEqual(2000000, count);
            Console.WriteLine(getCustomAttributesTime);
            Console.WriteLine(getAttributesTime);
        }

        [Test]
        public void ShouldGetAttribute()
        {
            Assert.IsNotNull(typeof(TestAttributedClass).GetAttribute<OneTestAttribute>());
            Assert.IsNotNull(typeof(TestAttributedClass).GetAttribute<AnotherTestAttribute>());
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

    public class ClassWithDefaultConstuctor
    {
        public ClassWithDefaultConstuctor()
        {
            Value = 1;
        }

        public int Value { get; private set; }
    }

    public class ClassWithoutDefaultConstuctor
    {
        public ClassWithoutDefaultConstuctor(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }
    }
}