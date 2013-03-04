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
        public static void CheckNotNull(object value, string name)
        {
            CheckNotNull(value, name, string.Format("{0} must not be blank", name));
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is not null.
        /// </summary>
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
        public static void CheckNotNull(object value, string name, string message)
        {
            CheckNotBlank(name, "name", "name must not be blank");
            CheckNotBlank(message, "message", "message must not be blank");

            if (value == null)
            {
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
                throw new ArgumentException("name must not be blank", "name");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("message must not be blank", "message");
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
            CheckNotBlank(value, name, string.Format("{0} must not be blank", name));
        }

        /// <summary>
        /// Ensures that <paramref name="collection"/> contains at least one
        /// item.
        /// </summary>
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
        /// Thrown if <paramref name="collection"/> is empty, or if
        /// <paramref name="value"/> or <paramref name="name"/> are blank.
        /// </exception>
        public static void CheckAny<T>(IEnumerable<T> collection, string name, string message)
        {
            CheckNotBlank(name, "name", "name must not be blank");
            CheckNotBlank(message, "message", "message must not be blank");

            if (collection == null || !collection.Any())
            {
                throw new ArgumentException(message, name);
            }
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is true.
        /// </summary>
        /// <param name="value">
        /// The value to check, must be true.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="collection"/>
        /// is false, must not be blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> is false, or if <paramref name="name"/>
        /// or <paramref name="message"/> are blank.
        /// </exception>
        public static void CheckTrue(bool value, string name, string message)
        {
            CheckNotBlank(name, "name", "name must not be blank");
            CheckNotBlank(message, "message", "message must not be blank");

            if (!value)
            {
                throw new ArgumentException(message, name);
            }
        }

        /// <summary>
        /// Ensures that <paramref name="value"/> is false.
        /// </summary>
        /// <param name="value">
        /// The value to check, must be false.
        /// </param>
        /// <param name="name">
        /// The name of the parameter the value is taken from, must not be
        /// blank.
        /// </param>
        /// <param name="message">
        /// The message to provide to the exception if <paramref name="collection"/>
        /// is true, must not be blank.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="value"/> is true, or if <paramref name="name"/>
        /// or <paramref name="message"/> are blank.
        /// </exception>
        public static void CheckFalse(bool value, string name, string message)
        {
            CheckNotBlank(name, "name", "name must not be blank");
            CheckNotBlank(message, "message", "message must not be blank");

            if (value)
            {
                throw new ArgumentException(message, name);
            }
        }
    }
}
