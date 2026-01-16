using System;
using System.Runtime.Serialization;

namespace JWLMergeCLI.Exceptions;

[Serializable]
public class JwlMergeCmdLineException : Exception
{
    public JwlMergeCmdLineException()
    {
    }

    public JwlMergeCmdLineException(string message)
        : base(message)
    {
    }

    public JwlMergeCmdLineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // Without this constructor, deserialization will fail
    protected JwlMergeCmdLineException(SerializationInfo info, StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        : base(info, context)
#pragma warning restore SYSLIB0051 // Type or member is obsolete
    {
    }
}