using ErrorOr;

namespace ScrumPoker.Data.Models;

public partial class Errors
{
    public static class Session
    {
        public static Error SessionNotFound => Error.NotFound("Session.NotFound");
        public static Error SessionIsExpired => Error.Failure("Session.Expired");
        public static Error UserNotCreated => Error.Failure("Session.UserNotCreated");
        public static Error CannotCreateSession => Error.Failure("Session.CannotCreate");
        public static Error CannotJoinToSession => Error.Failure("Session.CannotJoin");
        public static Error CannotRemoveParticipant => Error.Failure("Session.CannotRemoveParticipant");
        public static Error UpdateFailed => Error.Failure("Session.UpdateFailed");

        public static Error BacklogNotFound =>  Error.NotFound("Backlog.NotFound");
    }
}
