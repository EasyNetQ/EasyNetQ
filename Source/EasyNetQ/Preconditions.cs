using System;
using System.Collections.Generic;
using System.Linq;

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
        public static void CheckNotBlank(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name must not be blank", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(string.Format("{0} must not be blank", name), name);
            }
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
        public static void CheckAny<T>(IEnumerable<T> collection, string name, string message)
        {
            if (collection == null || !collection.Any())
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }
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
        public static void CheckTrue(bool value, string name, string message)
        {
            if (!value)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }
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
        public static void CheckFalse(bool value, string name, string message)
        {
            if (value)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }
        }

        public static void CheckShortString(string value, string name)
        {
            CheckNotNull(value, name);
            if (value.Length > 255)
            {
                throw new ArgumentException(string.Format("Argument '{0}' must be less than or equal to 255 characters.", name));
            }
        }

        public static void CheckTypeMatches(Type expectedType, object value, string name, string message)
        {
            bool assignable = expectedType.IsAssignableFrom(value.GetType());
            if (!assignable)
            {
                CheckNotBlank(name, nameof(name), "name must not be blank");
                CheckNotBlank(message, nameof(message), "message must not be blank");

                throw new ArgumentException(message, name);
            }
        }

        public static void CheckLess(TimeSpan value, TimeSpan maxValue, string name)
        {
            if (value < maxValue)
                return;
            throw new ArgumentOutOfRangeException(name, string.Format("Arguments {0} must be less than maxValue", name));
        }

        public static void CheckNull<T>(T value, string name) where T : class
        {
            if (value == null)
                return;
            throw new ArgumentException(string.Format("{0} must not be null", name), name);
        }
    }
}
