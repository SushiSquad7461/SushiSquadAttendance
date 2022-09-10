using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.Linq;
using System.Threading.Tasks;

namespace SushiSquadAttendance.Attendance
{
    internal static class AttendanceMessages
    {
        public static DiscordMessageBuilder GetUpdatedAttendanceMessage()
        {
            // Get the description the embed should have
            string description = "";
            foreach (Data.Person person in Data.Instance.SignInData)
                description += $"`{person.Name}` - {Formatter.Timestamp(person.SignInTime, TimestampFormat.ShortDateTime)}\n";

            DiscordMessageBuilder builder = new DiscordMessageBuilder()
                // Add main embed
                .WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Welcome to Sushi Squad!")
                    .WithDescription(
                        "Please sign in using the `/signin` command.\n" +
                        "Once you leave please use the `/signout` command."
                    )
                    .AddField(
                        "Doors",
                        "`Inner door` - The door on the inside of the building.\n" +
                        "`Door 4` - The outside door on the same side as the meeting room (NE).\n" +
                        "`Door 3` - The outside door on the opposite side as the meeting room (NW). Avoid this door."
                    )
                    .AddField(
                        "Signed In Members:",
                        Data.Instance.SignInData.Count == 0 ? "No one :(" : description
                    )
                );
            return builder;
        }

        public static Task UpdateAttendanceMessage(DiscordGuild guild)
        {
            return UpdateAttendanceMessage(guild.GetChannel(Data.Instance.AttendanceMessageChannel));
        }

        public static async Task UpdateAttendanceMessage(DiscordChannel channel)
        {
            try
            {
                DiscordChannel oldChannel = channel.Guild.GetChannel(Data.Instance.AttendanceMessageChannel);
                DiscordMessage message = await (oldChannel ?? channel).GetMessageAsync(Data.Instance.AttendanceMessage);
                await message.DeleteAsync();
            }
            // Ignore if it can't find the message to delete
            catch (NotFoundException) { }

            DiscordMessage newMessage = await channel.SendMessageAsync(GetUpdatedAttendanceMessage());
            
            // Set new message id in data
            Data.Instance.AttendanceMessageChannel = channel.Id;
            Data.Instance.AttendanceMessage = newMessage.Id;
            Data.Save();
        }

        public static async Task SendPings(Doors door, Data.Person person, DiscordGuild guild)
        {
            DiscordChannel channel = guild.GetChannel(Data.Instance.PingChannel);

            if (Data.Instance.SignInData.Count != 0)
            {
                // Loop through each person, get their id and format it into a ping,
                // then combine into a string
                string pings = string.Concat(Data.Instance.SignInData.SelectMany((person) => $"<@{person.Id}> "));

                await channel.SendMessageAsync($"`{person.Name}` needs to be let in at {DoorToString(door)}.\n\n{pings}");
            }
            else
            {
                await channel.SendMessageAsync($"`{person.Name}` has just opened GIX!");
            }
        }

        public static string DoorToString(Doors door)
        {
            // Basically a squished switch statement
            return door switch
            {
                Doors.InnerDoor => "the inner door",
                Doors.Door4 => "door 4",
                Doors.Door3 => "door 3",
                _ => "the shadow realm"
            };
        }
    }
}
