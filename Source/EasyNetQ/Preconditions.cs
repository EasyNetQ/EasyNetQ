using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EasyNetQ
{
    /// <summary>
    /// Collection of precondition methods for qualifying method arguments.
    /// </summary>
    internal static class Preconditions
    {
        /// <summary>
        /// Ensures that <paramref name="value"/> is not null.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
        /// <param name="value">
        /// The value to check, must not be null.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="name"/> is blank.
        /// </exception>
        public static void CheckNotNull<T>(T value, string name)
        {
            // Avoid boxing here
            if (value == null)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");

                throw new ArgumentNullException(name, $"{name} must not be null");
            }
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is not null.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
        /// <param name="value">
        /// The value to check, must not be null.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="name"/> is blank.
        /// </exception>
        public static T NotNull<T>(this T value, [CallerArgumentExpression("value")] string name = "")
        {
            // Avoid boxing here
            if (value == null)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");

                throw new ArgumentNullException(name, $"{name} must not be null");
            }

            return value;
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is not null.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
        /// <param name="value">
        /// The value to check, must not be null.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="value"/>
        /// is null, must not be blank.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="name"/> or <paramref name="message"/> are
        /// blank.
        /// </exception>
        public static void CheckNotNull<T>(T value, string name, string message) where T : class
        {
            if (value == null)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentNullException(name, message);
            }
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is not blank.
        /// </summary>
        /// <param name="value">
        /// The value to check, must not be blank.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="value"/>
        /// is blank, must not be blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/>, <paramref name="name"/>, or
        /// <paramref name="message"/> are blank.
        /// </exception>
        public static void CheckNotBlank(string value, string name, string message)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name must not be blank", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("message must not be blank", nameof(message));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(message, name);
            }
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is not blank.
        /// </summary>
        /// <param name="value">
        /// The value to check, must not be blank.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> or <paramref name="name"/> are
        /// blank.
        /// </exception>
        public static string CheckNotBlank(this string value, [CallerArgumentExpression("value")] string name = "")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name must not be blank", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(string.Format("{0} must not be blank", name), name);
            }

            return value;
        }

        /// <summary>
        /// Ensures that <paramref name="collection"/> contains at least one
        /// item.
        /// </summary>
        /// <typeparam name="T">Collection item type</typeparam>
        /// <param name="collection">
        /// The collection to check, must not be null or empty.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the collection is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="collection"/>
        /// is empty, must not be blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="collection"/> is empty, or <c>null</c>.
        /// </exception>
        public static IEnumerable<T> CheckAny<T>(this IEnumerable<T> collection, string message, [CallerArgumentExpression("collection")] string name = "")
        {
            if (collection == null || !collection.Any())
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }

            return collection;
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is <c>true</c>.
        /// </summary>
        /// <param name="value">
        /// The value to check, must be <c>true</c>.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="value"/>
        /// is <c>false</c>, must not be blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> is <c>false</c>, or if <paramref name="name"/>
        /// or <paramref name="message"/> are blank.
        /// </exception>
        public static bool CheckTrue(this bool value, string message, [CallerArgumentExpression("value")] string name = "")
        {
            if (!value)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }

            return value;
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is <c>false</c>.
        /// </summary>
        /// <param name="value">
        /// The value to check, must be <c>false</c>.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="value"/>
        /// is <c>true</c>, must not be blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> is <c>true</c>, or if <paramref name="name"/>
        /// or <paramref name="message"/> are blank.
        /// </exception>
        public static bool CheckFalse(this bool value, string message, [CallerArgumentExpression("value")] string name = "")
        {
            if (value)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }

            return value;
        }

        public static string CheckShortString(this string value, [CallerArgumentExpression("value")] string name = "")
        {
            if (value.NotNull(name).Length > 255)
            {
                throw new ArgumentException(string.Format("Argument '{0}' must be less than or equal to 255 characters.", name));
            }

            return value;
        }

        public static Type CheckTypeMatches(this Type expectedType, object value, string message, [CallerArgumentExpression("value")] string name = "")
        {
            var assignable = expectedType.IsAssignableFrom(value.GetType());
            if (!assignable)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }

            return expectedType;
        }

        public static TimeSpan CheckLess(this TimeSpan value, TimeSpan maxValue, [CallerArgumentExpression("value")] string name = "")
        {
            if (value < maxValue)
                return value;
            throw new ArgumentOutOfRangeException(name, string.Format("Arguments {0} must be less than maxValue", name));
        }

        public static T CheckNull<T>(this T value, [CallerArgumentExpression("value")] string name = "") where T : class
        {
            if (value == null)
                return value;
            throw new ArgumentException(string.Format("{0} must not be null", name), name);
        }
    }
}
