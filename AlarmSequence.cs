using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.ComponentModel;
using MethodAlarmUtils;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Media;

namespace TuccstersMethodAlarm
{
    public static class AlarmSequence
    {
        private static ProblemCompileData curProblemCompileData = null;
        private static SoundPlayer soundPlayer;
        private static System.Threading.Timer alarmTimer = null;

        [Command("alarmset", "Sets alarm to go off at a specific time the next day")]
        public static void SetAlarm(int hours, int minutes)
        {
            if (alarmTimer == null) return;

            TimeSpan alertTime = new TimeSpan(hours, minutes, 0);
            DateTime current = DateTime.Now;
            TimeSpan timeRemaining = alertTime - current.TimeOfDay;

            if (timeRemaining < TimeSpan.Zero)
                return;

            alarmTimer = new System.Threading.Timer(x => { TriggerAlarmSequence(); }, null, timeRemaining, Timeout.InfiniteTimeSpan);

            Info.Write(typeof(AlarmSequence), MessageType.Status, $"Set alarm for {hours}:{minutes}");
        }

        [Command("alarmsq", "Triggers the sequence of events for when an alarm goes off")]
        public static void TriggerAlarmSequence()
        {
            // Check if we already have an alarm sequence running
            if (curProblemCompileData != null)
            {
                Info.Write(typeof(AlarmSequence), MessageType.Warning, "Alarm sequence already in progress");
                return;
            }

            // Start alarm
            soundPlayer = new SoundPlayer();
            soundPlayer.SoundLocation = Config.UserDefined.AlarmSoundPath;
            soundPlayer.PlayLooping();

            //curProblemCompileData = ProblemManager.CheckProblemIntegrity(ProblemManager.GetRandomProblemName());
            curProblemCompileData = ProblemManager.CheckProblemIntegrity("addition");
            if (curProblemCompileData == null) return;
            Info.Write(typeof(AlarmSequence), MessageType.Status, $"Alarm triggered with problem '{curProblemCompileData.problemName}'");

            // Create a file somewhere using the template method from Solution.sourceCode
            ProblemManager.CreateAttemptFile(curProblemCompileData.problemName);

            // Open the file in ide
            EditorManager.OpenFileInIDE($@"{ProblemManager.assemblyPath}\Attempt.cs");
        }

        [Command("submit", "Checks the open attempt file to see if the method produces the correct outputs")]
        public static void SubmitAnswer()
        {
            // Check if there is an active problem
            if (curProblemCompileData == null)
            {
                Info.Write(typeof(AlarmSequence), MessageType.Warning, "No current active problem.");
                return;
            }

            // Check if correct answer was given
            CompilerResults attemptCompilerResults = ProblemManager.CompileFileIntoMemory($@"{ProblemManager.assemblyPath}\Attempt.cs", true);

            MethodParameters[] methodParameters = (MethodParameters[])curProblemCompileData.validationMethod.Invoke(null, null);
            for (int i = 0; i < methodParameters.Length; i++)
            {
                dynamic solutionReturnValue = curProblemCompileData.solutionMethod.Invoke(null, methodParameters[i].ToObjectArray());
                dynamic attemptReturnValue = attemptCompilerResults.CompiledAssembly.GetType("Attempt").GetMethods()[0].Invoke(null, methodParameters[i].ToObjectArray());
                if (solutionReturnValue != attemptReturnValue)
                    throw new ValuesNotEqualException($"Incorrect attempt. Expected value of '{solutionReturnValue}' but got '{attemptReturnValue}'.\n" +
                        $"                           Make sure to save Attempt.cs before submitting!");
            }

            curProblemCompileData = null;
            soundPlayer.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Attempt submission was correct.");
            Console.ForegroundColor = ConsoleColor.White;
        }

        [Command("alarmoff", "Turns off alarm")]
        public static void ForceAbortAlarmThread()
        {
            soundPlayer.Stop();
        }

        [Command("alarmsound", "Opens File Explorer for choosing an alarm sound")]
        public static void SetAlarmSound()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Title = "Select alarm sound";
            openFile.InitialDirectory = @"C:\Windows\Media";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                if (Path.GetExtension(openFile.FileName) != ".wav")
                    throw new FileLoadException($"Alarm sound must be of type .wav");

                Console.WriteLine($"Alarm sount set to '{Path.GetFileName(openFile.FileName)}'");
                //idePath = openFile.FileName;

                // Update the config with the new ide path
                Config.UserDefined.AlarmSoundPath = openFile.FileName;
                Config.UpdateFile();
            }
        }
    }
}
