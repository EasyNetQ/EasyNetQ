using System;

namespace EasyNetQ;

/// <summary>
/// Collection of precondition methods for qualifying method arguments.
/// </summary>
internal static class Preconditions
{
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
            throw new ArgumentException($"{name} must not be blank", name);
        }
    }

    public static void CheckShortString(string value, string name)
    {
        if (value.Length > 255)
        {
            throw new ArgumentException($"Argument '{name}' must be less than or equal to 255 characters.");
        }
    }
}
