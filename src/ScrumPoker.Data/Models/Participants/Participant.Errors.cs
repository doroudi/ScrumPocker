using System;
using ErrorOr;

namespace ScrumPoker.Data.Models;

public partial class Errors
{
    public static class Participant
    {
        public static Error ParticipantExists => Error.Conflict("Participant.Exists");
        public static Error CannotCreate => Error.Failure("Participant.CannotCreate");
    }
}
