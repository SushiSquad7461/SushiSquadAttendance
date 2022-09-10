using DSharpPlus.SlashCommands;

namespace SushiSquadAttendance.Attendance
{
    internal enum Doors
    {
        [ChoiceName("Inner Door")]
        InnerDoor,

        [ChoiceName("Door 4")]
        Door4,

        [ChoiceName("Door 3 (Avoid)")]
        Door3
    }
}
