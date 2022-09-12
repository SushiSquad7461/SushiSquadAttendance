using Notion.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SushiSquadAttendance.Notion
{
    internal class NotionInteractions
    {
        public static NotionClient Client { get; set; } = NotionClientFactory.Create(new ClientOptions()
        {
            AuthToken = File.ReadAllText("notion_token")
        });

        public static async Task<User?> GetUserFromTag(string tag)
        {
            PaginatedList<Page> pages = await Client.Databases.QueryAsync(
                Data.Instance.RosterDBID,
                new DatabasesQueryParameters()
                {
                    Filter = new RichTextFilter("Discord Tag", contains: tag)
                }
            );

            if (pages.Results.Count == 0)
                return null;

            Page userInfo = await Client.Pages.RetrieveAsync(pages.Results[0].Id);
            return (userInfo.Properties["NotionUser"] as PeoplePropertyValue)!.People[0];
        }

        public static async Task<Page?> GetPageFromDate(string date)
        {
            PaginatedList<Page> pages = await Client.Databases.QueryAsync(
                Data.Instance.AttendanceDBID,
                new DatabasesQueryParameters()
                {
                    Filter = new RichTextFilter("Name", contains: date)
                }
            );

            return pages.Results.Count == 0 ? null : pages.Results[0];
        }

        public static async Task<Page> CreatePage(string date)
        {
            // This is unbelievably cursed
            return await Client.Pages.CreateAsync(new PagesCreateParameters()
            {
                Parent = new DatabaseParentInput()
                {
                    DatabaseId = Data.Instance.AttendanceDBID
                },
                Icon = new EmojiObject()
                {
                    Emoji = "📝",
                    Type = "emoji"
                },
                Properties = new Dictionary<string, PropertyValue>()
                {
                    { 
                        "Name",
                        new TitlePropertyValue()
                        {
                            Title = new List<RichTextBase>()
                            {
                                new RichTextText()
                                {
                                    Text = new Text()
                                    {
                                        Content = date,
                                    },
                                    PlainText = date,
                                }
                            }
                        } 
                    }
                }
            });
        }


        // Datetimes will always be in UTC so we need to convert.
        // This line just creates a time zone object that represents PST.
        private static readonly TimeZoneInfo PST = TimeZoneInfo.FromSerializedString("Pacific Standard Time;-480;(UTC-08:00) Pacific Time (US & Canada);Pacific Standard Time;Pacific Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;4;1;0;];[0;02:00:00;10;5;0;];][01:01:2007;12:31:9999;60;[0;02:00:00;3;2;0;];[0;02:00:00;11;1;0;];];");
        public static async Task MarkPresent(string tag, DateTime date)
        {
            // Convert timestamp from UTC to PST
            DateTime convertedDate = TimeZoneInfo.ConvertTimeFromUtc(date, PST);

            // Grab user and date
            string dateString = convertedDate.ToString("m/d/y");

            User? user = await GetUserFromTag(tag);
            if (user == null)
                return;

            Page? currPage = await GetPageFromDate(dateString);
            if (currPage == null)
                currPage = await CreatePage(dateString);

            string pageId = currPage.Id;

            // Check if on time, and then which group they should be put in
            string attendeeGroup;
            if ((convertedDate.DayOfWeek is > DayOfWeek.Sunday and < DayOfWeek.Saturday && convertedDate.Hour < 15)
                || (convertedDate.DayOfWeek == DayOfWeek.Saturday && convertedDate.Hour < 10))
                attendeeGroup = "Attendees";
            else
                attendeeGroup = "Late Attendees";

            // Add the attendee to the people
            PeoplePropertyValue people = (currPage.Properties[attendeeGroup] as PeoplePropertyValue)!;
            people.People.Add(user);

            // Update page
            await Client.Pages.UpdateAsync(
                pageId,
                new PagesUpdateParameters()
                {
                    Properties = new Dictionary<string, PropertyValue>()
                    {
                            { attendeeGroup, people }
                    }
                }
            );
        }

        public static async Task LogPing(bool isLeaving, string tag)
        {
            User? user = await GetUserFromTag(tag);

            if (user == null)
                return;

            await Client.Pages.CreateAsync(new PagesCreateParameters()
            {
                Parent = new DatabaseParentInput()
                {
                    DatabaseId = Data.Instance.LogDBID
                },
                Properties = new Dictionary<string, PropertyValue>()
                {
                    {
                        "Leaving",
                        new CheckboxPropertyValue() { Checkbox = isLeaving }
                    },
                    {
                        "Person",
                        new PeoplePropertyValue() { People = new List<User> { user } }
                    }
                }
            });
        }
    }
}
