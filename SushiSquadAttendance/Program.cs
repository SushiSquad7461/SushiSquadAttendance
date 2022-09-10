using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SushiSquadAttendance
{
    static class Program
    {
        public static DiscordClient Client { get; private set; } = new(new DiscordConfiguration()
        {
            Token = File.ReadAllText("token"),
            Intents = DiscordIntents.AllUnprivileged
        });

        public static SlashCommandsExtension SlashCommands { get; private set; } = Client.UseSlashCommands();

        static Program()
        {
            // Register our commands
            SlashCommands.RegisterCommands<Attendance.AttendanceSlashCommands>(906253258870374460);

            SlashCommands.SlashCommandErrored += (s, e) =>
            {
                Console.WriteLine($"Error in {e.Context.CommandName}:\n{e.Exception}");

                return Task.CompletedTask;
            };
        }

        private static void Main() 
        {
           Start().GetAwaiter().GetResult();
        }

        private static async Task Start()
        {
            await Client.ConnectAsync();

            // Wait forever
            await Task.Delay(-1);
        }
    }
}