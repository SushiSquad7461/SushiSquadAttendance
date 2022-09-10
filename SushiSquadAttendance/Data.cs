using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;   

namespace SushiSquadAttendance
{
    internal class Data
    {
        public static Data Instance { get; private set; }

        public ulong AttendanceMessage { get; set; }
        public ulong AttendanceMessageChannel { get; set; }

        public ulong PingChannel { get; set; }

        public List<Person> SignInData { get; set; } = new();

        static Data()
        {
            if (File.Exists("Data/Config.json"))
            {
                // Load config from file
                Instance = JsonConvert.DeserializeObject<Data>(
                    File.ReadAllText("Data/Config.json")
                )!;
            } 
            else
            {
                Instance = new Data();
                Save();
            }

        }

        public static void Save()
        {
            File.WriteAllText("Data/Config.json", JsonConvert.SerializeObject(Instance));
        }

        internal class Person
        {
            public string Name { get; set; } = "";
            public ulong Id { get; set; }
            public DateTime SignInTime { get; set; }
            public DateTime SignOutTime { get; set; }
        }
    }
}
