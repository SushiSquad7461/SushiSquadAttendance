using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CA1822 // Mark members as static
namespace SushiSquadAttendance.Attendance
{
    internal class AttendanceSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("ResendAttendance", "Resends the main attendance message.")]
        [SlashRequireBotPermissions(Permissions.SendMessages)]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        [SlashRequireGuild]
        public async Task ResendAttendance(
            InteractionContext ctx,
            [Option("Channel", "The channel to send the message to.")] DiscordChannel channel)
        {
            // Mark bot as in a "thinking" state
            await ctx.DeferAsync();

            // Update attendance message (and save new data)
            await AttendanceMessages.UpdateAttendanceMessage(channel);

            // Notify user of success
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Message successfully resent!")
            );
        }

        [SlashCommand("SetPingChannel", "Sets the channel to send pings into.")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        [SlashRequireGuild]
        public async Task SetPingChannel(
            InteractionContext ctx,
            [Option("Channel", "The channel to send pings to.")] DiscordChannel channel)
        {
            Data.Instance.PingChannel = channel.Id;
            Data.Save();

            await ctx.CreateResponseAsync("Channel successfully set!");
        }

        [SlashCommand("SignIn", "Signs a person in.")]
        [SlashRequireGuild]
        public async Task SignIn(
            InteractionContext ctx, 
            [Option("Door", "The door you're entering from.")] Doors door)
        {
            // Check if user is in right channel
            if (ctx.Channel.Id != Data.Instance.PingChannel)
            {
                await ctx.CreateResponseAsync($"Please go to <#{Data.Instance.PingChannel}> for this command.", true);
                return;
            }

            // Check whether this person is signed in or not
            if (Data.Instance.SignInData.Any(person => person.Id == ctx.User.Id))
            {
                await ctx.CreateResponseAsync("You're already signed in.", true);
                return;
            }

            await ctx.DeferAsync(true);

            // Get user's nickname
            DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);

            // Add a new person to the sign in data
            Data.Person person = new()
            {
                Id = ctx.User.Id,
                Name = member.DisplayName,
                SignInTime = DateTime.UtcNow
            };

            // Send pings
            await AttendanceMessages.SendPings(door, person, ctx.Guild);

            // Update our data
            Data.Instance.SignInData.Add(person);
            Data.Save();

            // Update the attendance message
            await AttendanceMessages.UpdateAttendanceMessage(ctx.Guild);

            // Tell the user they're signed in
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Successfully signed in. Please wait while someone comes to open the door for you!")
            );
        }

        [SlashCommand("SignOut", "Signs a person out.")]
        [SlashRequireGuild]
        public async Task HandleSignOut(InteractionContext ctx)
        {
            // Check if user is in right channel
            if (ctx.Channel.Id != Data.Instance.PingChannel)
            {
                await ctx.CreateResponseAsync($"Please go to <#{Data.Instance.PingChannel}> for this command.", true);
                return;
            }

            // Check whether this person is signed in or not
            int personIndex = Data.Instance.SignInData.FindIndex(person => person.Id == ctx.User.Id);
            if (personIndex == -1)
            {
                await ctx.CreateResponseAsync("You're not signed in.", true);
                return;
            }

            await ctx.DeferAsync(true);

            Data.Instance.SignInData.RemoveAt(personIndex);
            Data.Save();

            await AttendanceMessages.UpdateAttendanceMessage(ctx.Guild);

            // Tell the user they've signed out
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Successfully signed out. Thanks for coming!")
            );
        }
    }
}
