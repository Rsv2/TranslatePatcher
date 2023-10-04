using System;

public class InvalidArchiveException : Exception
{
    public InvalidArchiveException(string message) : base(message)
    {
    }
}
