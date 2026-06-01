namespace Umbral.IdentityService.Application.Exceptions;

public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid userId)
        : base($"User '{userId}' was not found")
    {
    }
}
