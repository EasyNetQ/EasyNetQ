using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using System.Reflection;

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

        [Fact]
        public void ReflectionHelpers_CreateObject_Should_Call_Parametrized_Ctors()
        {
            Assert.Equal(string.Empty, ReflectionHelpers.CreateObject<string>());
            Assert.Equal(0, ReflectionHelpers.CreateObject<int>());
            Assert.Equal(default, ReflectionHelpers.CreateObject<DateTime>());
            Assert.Equal(default, ReflectionHelpers.CreateObject<char>());
            Assert.Null(ReflectionHelpers.CreateObject<char?>());
            Assert.True(ReflectionHelpers.CreateObject<int[]>().Length == 0);
            Assert.NotNull(ReflectionHelpers.CreateObject<object>());

            var person1 = ReflectionHelpers.CreateObject<PersonWithDefaultCtor>();
            Assert.True(person1.CtorCalledIndex == 1);

            var person2 = ReflectionHelpers.CreateObject<PersonWithParametrizedCtor>();
            Assert.True(person2.CtorCalledIndex == 2);

            var person3 = ReflectionHelpers.CreateObject<PersonWithSecondParametrizedCtor>();
            Assert.True(person3.CtorCalledIndex == 3);
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

    public abstract class PersonBase
    {
        public int CtorCalledIndex { get; set; }
    }

    public class PersonWithDefaultCtor : PersonBase
    {
        public PersonWithDefaultCtor()
        {
            CtorCalledIndex = 1;
        }

        public string Name { get; set; }

        public int Age { get; set; }

        public PersonWithDefaultCtor Parent { get; set; }
    }

    public class PersonWithParametrizedCtor : PersonBase
    {
        public PersonWithParametrizedCtor(string name, int age, PersonWithParametrizedCtor parent)
        {
            Name = name;
            Age = age;
            Parent = parent;
            CtorCalledIndex = 2;
        }

        public string Name { get; set; }

        public int Age { get; set; }

        public PersonWithParametrizedCtor Parent { get; set; }
    }

    public class PersonWithSecondParametrizedCtor : PersonWithParametrizedCtor
    {
        public PersonWithSecondParametrizedCtor(string name, int age, PersonWithParametrizedCtor parent, DateTime birthday, char[] secret, long? money) : base(name, age, parent)
        {
            Birthday = birthday;
            CtorCalledIndex = 3;
        }

        public DateTime Birthday { get; set; }
    }
}