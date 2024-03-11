﻿namespace FoundationaLLM.Authorization.Exceptions
{
    /// <summary>
    /// Represents an error generated by FoundationaLLM.Authoriztion resource provider.
    /// </summary>
    public class AuthorizationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class with a default message.
        /// </summary>
        public AuthorizationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class with its message set to <paramref name="message"/>.
        /// </summary>
        /// <param name="message">A string that describes the error.</param>
        public AuthorizationException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class with its message set to <paramref name="message"/>.
        /// </summary>
        /// <param name="message">A string that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public AuthorizationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
