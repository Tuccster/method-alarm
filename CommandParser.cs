using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TuccstersMethodAlarm
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Trigger;
        public string Summary;

        public CommandAttribute(string trigger, string summary = "")
        {
            Trigger = trigger;
            Summary = summary;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CreatableAttribute : Attribute
    {
        public string Name;

        public CreatableAttribute(string name)
        {
            Name = name;
        }
    }

    public class CommandData
    {
        public string rawInput;
        public MethodInfo method;
        public object[] parameters;

        public CommandData(string _rawInput, MethodInfo _method, object[] _parameters)
        {
            rawInput = _rawInput;
            method = _method;
            parameters = _parameters;
        }
    }

    public class CommandParser
    {
        protected static List<string> disabledCommands = new List<string>();
        private static Assembly assembly;
        private static MethodInfo[] commandMethods;
        private static CommandAttribute[] commandAttributes;

        public static Stack<CommandData> historyStack = new Stack<CommandData>();

        public static void Init()
        {
            assembly = Assembly.GetExecutingAssembly();
            commandMethods = assembly.GetTypes()
                                     .SelectMany(t => t.GetMethods())
                                     .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                                     .ToArray();

            commandAttributes = new CommandAttribute[commandMethods.Length];
            for (int i = 0; i < commandMethods.Length; i++)
                commandAttributes[i] = (CommandAttribute)Attribute.GetCustomAttribute(commandMethods[i], typeof(CommandAttribute));
        }

        // Returns CommandData based on input; returns null if parse fails
        public static CommandData Parse(string input)
        {
            // TODO : Support methods using params //

            // First time parsing, initialize class data
            if (commandMethods == null) Init();

            // TODO: This is a terrible implementation of this feature; fix it!
            // Use last entry in historyStack
            if (input == "!!")
            {
                if (historyStack.Count > 0) Execute(historyStack.Peek());
                else Console.WriteLine("[Error]: No history");
                return null;
            }

            // Tidy up input string and extract trigger and params from input
            // input = input.ToLower();
            input = input.Trim();
            if (input == string.Empty) return null;

            string inputTrigger = input.Split(' ')[0];
            string[] inputParams = new string[0];
            if (input.Length > inputTrigger.Length)
            {
                inputParams = input.Remove(0, inputTrigger.Length + 1).Split(' ');
                /*
                List<string> quoteCombine = new List<string>();
                for (int i = 0; i < inputParams.Length; i++)
                {
                    if (inputParams[i][0] == '"')
                    {

                    }
                    else if (inputParams[i][inputParams[i].Length - 1] == '"')
                    {

                    }
                }
                */
            }

            // Find method tagged with the same trigger as our input trigger
            int matchingIndex = -1;
            for (int i = 0; i < commandAttributes.Length; i++)
                if (commandAttributes[i].Trigger == inputTrigger)
                    matchingIndex = i;
            if (matchingIndex == -1)
                throw new CommandNotFoundException($"No command found with trigger '{inputTrigger}'. Use 'help' for a list of commands");

            ParameterInfo[] methodParams = commandMethods[matchingIndex].GetParameters();

            // method takes no parameters, call it and exit
            if (methodParams.Length == 0)
            {
                try
                {
                    commandMethods[matchingIndex].Invoke(null, null);
                }
                catch
                {
                    throw;
                    //Console.WriteLine($"[Error]: Problem while calling method.");
                    //Console.WriteLine($"\"{e.Message}\"");
                    //Console.WriteLine($"Use 'help {commandAttributes[matchingIndex].Trigger}' for usage");
                }
                return null;
            }

            // Check if the correct amount of parameters was given
            byte requiredParams = 0;
            foreach (ParameterInfo parameter in methodParams)
                if (!parameter.IsOptional) requiredParams++;
            if (inputParams.Length < requiredParams || inputParams.Length > methodParams.Length)
                throw new TargetParameterCountException($"Use 'help {commandAttributes[matchingIndex].Trigger}' for usage");

            // Convert input parameter strings into the types needed to be passed to the method
            object[] convParams = new object[methodParams.Length];
            for (int j = 0; j < methodParams.Length; j++)
            {
                // Use default value if the parameter is optional and we don't have an input parameter for it
                if (methodParams[j].IsOptional && inputParams.Length < methodParams.Length)
                {
                    convParams[j] = methodParams[j].DefaultValue;
                    continue;
                }

                // Populate convParams
                Type paramType = methodParams[j].ParameterType;
                try
                {
                    switch (Type.GetTypeCode(paramType))
                    {
                        case TypeCode.String: convParams[j] = inputParams[j]; break;
                        case TypeCode.Single: convParams[j] = float.Parse(inputParams[j]); break;
                        case TypeCode.Int32: convParams[j] = int.Parse(inputParams[j]); break;
                        case TypeCode.Boolean: convParams[j] = bool.Parse(inputParams[j]); break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error parsing argument {j + 1} ('{inputParams[j]}')");
                    Console.WriteLine($"\"{e.Message}\"");
                    Console.WriteLine($"Use 'help {commandAttributes[matchingIndex].Trigger}' for usage");
                }
            }

            return new CommandData(input, commandMethods[matchingIndex], convParams);
        }

        // Execute using CommandData object
        public static void Execute(CommandData commandData)
        {
            if (commandData == null) return;
            try
            {
                commandData.method.Invoke(null, commandData.parameters);
            }
            catch
            {
                throw;
            }
            historyStack.Push(commandData);
        }

        // Shorthand way to parse and execute the input in one method call
        public static void Auto(string input)
        {
            Execute(Parse(input));
        }

        // Extract text contained between quotes as strings
        public static string[] GetQuotedStrings(string s)
        {
            List<string> quotesFound = new List<string>();
            bool started = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '"')
                {
                    started = !started;
                    if (started) quotesFound.Add(string.Empty);
                }
                else if (started) quotesFound[quotesFound.Count - 1] += s[i];
            }
            return quotesFound.ToArray();
        }

        // Convert MethodInfo object to a formatted string -> i.e. "ExampleMethod(int x, string s)"
        public static string MethodInfoStringFormat(MethodInfo methodInfo)
        {
            string formatted = $"{methodInfo.Name}(";
            ParameterInfo[] parameters = methodInfo.GetParameters();
            for (int j = 0; j < parameters.Length; j++)
            {
                string parameter = $"{GetSimpleTypeName(parameters[j].ParameterType)} ";
                parameter += parameters[j].Name;
                parameter += parameters[j].IsOptional ? $" = {parameters[j].DefaultValue}" : string.Empty;
                formatted += parameter;
                if (j < parameters.Length - 1)
                    formatted += ", ";
            }
            return formatted + ")";
        }

        // Allows for the disabling and enabling of a single command.
        public static void SetCommandUsable(string command, bool usable)
        {
            SetCommandsUsable(new string[] { command }, usable);
        }

        // Allows for the disabling and enabling of multiple commands.
        public static void SetCommandsUsable(string[] commands, bool usable)
        {
            for (int i = 0; i < commands.Length; i++)
            {
                if (usable) disabledCommands.Remove(commands[i]);
                else disabledCommands.Add(commands[i]);
            }
        }

        // Get shorthand name of type like what is typically used in C#.
        public static string GetSimpleTypeName(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String: return "string";
                case TypeCode.Single: return "float";
                case TypeCode.Int32: return "int";
                case TypeCode.Boolean: return "bool";
                default: return type.ToString();
            }
        }

        // TODO:
        // Should make abstract as to allow for inheritance; custom implementation of any default command.
        public static class DefaultCommands
        {
            public static void DisableAll()
            {
                SetCommandsUsable(new[] { "clear", "help", "history" }, false);
            }

            [Command("clear")]
            public static void ClearCommand()
            {
                Console.Clear();
            }

            [Command("help")]
            public static void HelpCommand(string specific = null)
            {
                // TODO: Include the displaying of params //

                for (int i = 0; i < commandMethods.Length; i++)
                {
                    // Skip if we are looking for help on a specific command and this isn't it
                    if (specific != null && commandAttributes[i].Trigger != specific) continue;

                    // Write trigger string and seperator if necessary
                    if (i > 0 && specific == null) Console.WriteLine("--------");
                    Console.WriteLine($"Trigger : '{commandAttributes[i].Trigger}'");

                    // Write format of method
                    Console.Write($"Method  : {commandMethods[i].Name}(");
                    ParameterInfo[] parameters = commandMethods[i].GetParameters();
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        string parameter = $"{GetSimpleTypeName(parameters[j].ParameterType)} ";
                        parameter += parameters[j].Name;
                        parameter += parameters[j].IsOptional ? $" = {parameters[j].DefaultValue}" : string.Empty;
                        Console.Write($"{parameter}");
                        if (j < parameters.Length - 1)
                            Console.Write(", ");
                    }
                    Console.WriteLine(")");

                    if (commandAttributes[i].Summary != string.Empty)
                        Console.WriteLine($"Summary : {commandAttributes[i].Summary}");
                }

                // Write collective command information
                Console.WriteLine("========");
                Console.WriteLine($"Command count : {commandMethods.Length}");
            }

            [Command("history")]
            public static void HistoryCommand()
            {
                Console.WriteLine(historyStack.Count);
                int index = 0;
                foreach (CommandData commandData in historyStack)
                {
                    Console.WriteLine($"rawInput              : {commandData.rawInput}");
                    Console.WriteLine($"converted method call : {MethodInfoStringFormat(commandData.method)}");
                    if (index < historyStack.Count - 1)
                        Console.WriteLine("--------");
                    index++;
                }
            }
        }
    }
}
