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
            var obj = ReflectionHelpers.CreateInstance<ClassWithDefaultConstuctor>();
            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.GetType() == typeof(ClassWithDefaultConstuctor));
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
        public void ShouldCreateClassWithSingleParameterConstructor()
        {
            var obj = ReflectionHelpers.CreateInstance(typeof(ClassWithOneParameterConstructor), 1);
            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.GetType() == typeof(ClassWithOneParameterConstructor));
        }

        [Test]
        [ExpectedException(typeof(MissingMethodException))]
        public void ShouldFailToCreateClassWithoutSingleParameterConstructor()
        {
            ReflectionHelpers.CreateInstance(typeof(ClassWithDefaultConstuctor), 1);
        }

        [Test, Explicit("Fails on build server.")]
        public void ShouldPerformFasterThanActivatorSingleParameter()
        {
            // warmup
            for (var i = 0; i < 10; ++i)
            {
                Activator.CreateInstance(typeof(ClassWithOneParameterConstructor), new object[] { 1 });
                ReflectionHelpers.CreateInstance(typeof(ClassWithOneParameterConstructor), 1);
            }
            // warmup
            var count = 0;
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                var obj = ReflectionHelpers.CreateInstance(typeof(ClassWithOneParameterConstructor), 1) as ClassWithOneParameterConstructor;
                count += obj.Value;
            }
            stopWatch.Stop();
            var creatorTime = stopWatch.Elapsed;
            stopWatch.Reset();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                var obj = Activator.CreateInstance(typeof(ClassWithOneParameterConstructor), new object[] { 1 }) as ClassWithOneParameterConstructor;
                count += obj.Value;
            }
            stopWatch.Stop();
            var activator = stopWatch.Elapsed;
            Assert.IsTrue(creatorTime < activator);
            Assert.AreEqual(2000000, count);
        }

        [Test]
        public void ShouldCreateClassWithDualParameterConstructor()
        {
            var obj = ReflectionHelpers.CreateInstance(typeof(ClassWithTwoParametersConstructor), 1, 2);
            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.GetType() == typeof(ClassWithTwoParametersConstructor));
        }

        [Test]
        [ExpectedException(typeof(MissingMethodException))]
        public void ShouldFailToCreateClassWithoutDualParameterConstructor()
        {
            ReflectionHelpers.CreateInstance(typeof(ClassWithDefaultConstuctor), 1, 2);
        }

        [Test, Explicit("Fails on build server.")]
        public void ShouldPerformFasterThanActivatorDualParameter()
        {
            // warmup
            for (var i = 0; i < 10; ++i)
            {
                Activator.CreateInstance(typeof(ClassWithTwoParametersConstructor), new object[] { 1, 2 });
                ReflectionHelpers.CreateInstance(typeof(ClassWithTwoParametersConstructor), 1, 2);
            }
            // warmup
            var count = 0;
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                var obj = ReflectionHelpers.CreateInstance(typeof(ClassWithTwoParametersConstructor), 1, 2) as ClassWithTwoParametersConstructor;
                count += obj.Value1 + obj.Value2;
            }
            stopWatch.Stop();
            var creatorTime = stopWatch.Elapsed;
            stopWatch.Reset();

            stopWatch.Start();
            for (var i = 0; i < 1000000; ++i)
            {
                var obj = Activator.CreateInstance(typeof(ClassWithTwoParametersConstructor), new object[] { 1, 2 }) as ClassWithTwoParametersConstructor;
                count += obj.Value1 + obj.Value2;
            }
            stopWatch.Stop();
            var activator = stopWatch.Elapsed;
            Assert.IsTrue(creatorTime < activator);
            Assert.AreEqual(6000000, count);
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

    public class ClassWithOneParameterConstructor
    {
        public ClassWithOneParameterConstructor(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }
    }

    public class ClassWithTwoParametersConstructor
    {
        public ClassWithTwoParametersConstructor(int value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public int Value1 { get; private set; }
        public int Value2 { get; private set; }
    }
}