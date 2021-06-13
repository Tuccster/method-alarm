using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TuccstersMethodAlarm
{
    public static class Config
    {
        public static UserDefinedVariables UserDefined { get; set; } 

        public static string exePath = Assembly.GetExecutingAssembly().Location;
        public static string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string configFilePath = $@"{assemblyPath}\config.json";

        private static JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

        public static void Init()
        {
            // Create a new config.json with default values if the file is missing or empty
            if (RegenerationCheck()) return;

            // Attempt to read from config.json -- use default values if deserialization fails
            UpdateInternal();
        }

        /// <summary>
        /// Attempt to read from config.json -- use default values if deserialization fails
        /// </summary>
        [Command("configupdate", "Reads from config.json, updating internal values.")]
        public static void UpdateInternal()
        {
            try
            {
                Info.Write(typeof(Config), MessageType.Status, "Deserializing config.json...");
                string json = File.ReadAllText(configFilePath);
                UserDefined = JsonSerializer.Deserialize<UserDefinedVariables>(json, options);

                if (UserDefined == null)
                    throw new NullConfigException("config.json evaluated to null");
            }
            catch (Exception e)
            {
                UseDefault();
                Info.WriteException(e, UserDefined.DebugMode);
            }
            Info.Write(typeof(Config), MessageType.Status, "Done");
        }

        /// <summary>
        /// Create a new config.json with default values if the file is missing or empty. Returns whether or not a new config.json was generated
        /// </summary>
        public static bool RegenerationCheck()
        {
            if (!File.Exists(configFilePath) || new FileInfo(configFilePath).Length == 0)
            {
                UseDefault();
                Info.Write(typeof(Config), MessageType.Warning, "config.json was missing or empty; new config file generated.");
                StreamWriter streamWriter = File.CreateText(configFilePath);
                streamWriter.Write(JsonSerializer.Serialize(UserDefined, options));
                streamWriter.Close();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serializes UserDefined into config.json
        /// </summary>
        public static void UpdateFile()
        {
            StreamWriter streamWriter = File.CreateText(configFilePath);
            streamWriter.Write(JsonSerializer.Serialize(UserDefined, options));
            streamWriter.Close();
        }

        /// <summary>
        /// Assign a new instance of UserDefinedVariables to userDefined and write message to console that default values are being used
        /// </summary>
        private static void UseDefault()
        {
            UserDefined = new UserDefinedVariables();
            Info.Write(typeof(Config), MessageType.Warning, "Problem while reading from config.json, using default values instead.");
        }

        // Object used for serializing and deserializing config data
        public class UserDefinedVariables
        {
            public string EditorPath { get; set; } = @"C:\Windows\System32\notepad.exe";
            public string AlarmSoundPath { get; set; } = @"C:\Windows\Media\Alarm01.wav";
            public int MaxMethodParameters { get; set; } = 256;
            public bool DebugMode { get; set; } = false;
        }

        [Command("config", "Opens the config file for this application.")]
        public static void OpenConfigFile() => Process.Start(configFilePath);
    }
}
