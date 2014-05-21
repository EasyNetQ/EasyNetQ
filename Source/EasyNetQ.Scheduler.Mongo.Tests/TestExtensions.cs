using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace EasyNetQ.Scheduler.Mongo.Tests
{
    public static class TestExtensions
    {
        public static T ShouldNotBeNull<T>(this T obj)
        {
            Assert.IsNotNull(obj);
            return obj;
        }

        public static T ShouldNotBeNull<T>(this T obj, string message)
        {
            Assert.IsNotNull(obj, message);
            return obj;
        }

        public static T ShouldEqual<T>(this T actual, object expected)
        {
            Assert.AreEqual(expected, actual);
            return actual;
        }

        public static T ShouldEqual<T>(this T actual, object expected, string message)
        {
            Assert.AreEqual(expected, actual, message);
            return actual;
        }

        public static Exception ShouldBeThrownBy(this Type exceptionType, TestDelegate testDelegate)
        {
            return Assert.Throws(exceptionType, testDelegate);
        }

        public static void ShouldBe<T>(this object actual)
        {
            Assert.IsInstanceOf<T>(actual);
        }

        public static void ShouldBeNull(this object actual)
        {
            Assert.IsNull(actual);
        }

        public static void ShouldBeTheSameAs(this object actual, object expected)
        {
            Assert.AreSame(expected, actual);
        }

        public static T CastTo<T>(this object source)
        {
            return (T)source;
        }

        public static void ShouldBeTrue(this bool source)
        {
            Assert.IsTrue(source);
        }

        public static void ShouldBeTrue(this bool source, string message)
        {
            Assert.IsTrue(source, message);
        }

        public static void ShouldBeFalse(this bool source)
        {
            Assert.IsFalse(source);
        }

        public static void ShouldBeFalse(this bool source, string message)
        {
            Assert.IsFalse(source, message);
        }

        public static void ShouldBeEmpty<T>(this IEnumerable<T> collection)
        {
            Assert.AreEqual(collection.Count(), 0,
            string.Format("Expected collection to be empty, but had {0} items", collection.Count()));
        }
    }
}