﻿using System;
using System.Runtime.Serialization;

namespace JWLMerge.BackupFileServices.Exceptions;

[Serializable]
public class BackupFileServicesException : Exception
{
    public BackupFileServicesException()
    {
    }

    public BackupFileServicesException(string errorMessage)
        : base(errorMessage)
    {
    }

    public BackupFileServicesException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // Without this constructor, deserialization will fail
    protected BackupFileServicesException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}