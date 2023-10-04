using System;

public class UnsupportedArchiveException : Exception
{
    public UnsupportedArchiveException(string message) : base(message)
    {
    }
}