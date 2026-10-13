namespace AI_LMS.Application.Exceptions;

public sealed class LmsException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
