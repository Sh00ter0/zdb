namespace Infrastructure.Exceptions
{
    public class UserVisibleException : Exception
    {
        public UserVisibleException(string message) : base(message) { }
    }
}
