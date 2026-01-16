using System;
using System.Runtime.Serialization;

namespace JWLMerge.ExcelServices.Exceptions;

[Serializable]
public class ExcelServicesException : Exception
{
    public ExcelServicesException()
    {
    }

    public ExcelServicesException(string errorMessage)
        : base(errorMessage)
    {
    }

    public ExcelServicesException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // Without this constructor, deserialization will fail
    protected ExcelServicesException(SerializationInfo info, StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
        : base(info, context)
#pragma warning restore SYSLIB0051 // Type or member is obsolete
    {
    }
}