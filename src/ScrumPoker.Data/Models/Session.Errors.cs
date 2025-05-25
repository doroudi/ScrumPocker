using ErrorOr;

namespace ScrumPoker.Data.Models;

public static class Errors
{
    public static class Session
    {
        public static Error SessionNotFound => Error.NotFound("Session.NotFound");
        public static Error SessionIsExpired => Error.Failure("Session.Expired");
        public static Error UserNotCreated => Error.Failure("Session.UserNotCreated");
    }
}
