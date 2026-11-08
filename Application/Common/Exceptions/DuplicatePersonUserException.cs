namespace Application.Common.Exceptions;

public class DuplicatePersonUserException : Exception
{
    public DuplicatePersonUserException()
        : base("This person is already associated with a user account.")
    {
    }

    public DuplicatePersonUserException(string message)
        : base(message)
    {
    }

    public DuplicatePersonUserException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
