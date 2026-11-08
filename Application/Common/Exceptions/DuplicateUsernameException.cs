namespace Application.Common.Exceptions;

public class DuplicateUsernameException : Exception
{
    public DuplicateUsernameException()
        : base("Username is already in use.")
    {
    }

    public DuplicateUsernameException(string message)
        : base(message)
    {
    }

    public DuplicateUsernameException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
