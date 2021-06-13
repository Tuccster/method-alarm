using MethodAlarmUtils;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace TuccstersMethodAlarm
{
    class ProblemManager
    {
        public static string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // This directory should be checked when the program starts
        // Perhaps a new class could be used to check integrety of files and folders
        public static string problemsDirPath = $@"{assemblyPath}\Problems";

        [Command("oal", "Opens File Explorer to the path of the this program's assembly")]
        public static void OpenAssemblyLocation() => Process.Start("explorer.exe", assemblyPath);

        [Command("clearproblemsfolder", "Completly wipes the 'Problems' folder of its contents")]
        public static void ClearProblemsFolder()
        {
            Console.WriteLine($"Clear all files and folders in '{problemsDirPath}'?");
            string confirmInput = "CONFIRM";
            Console.WriteLine($"Type '{confirmInput}' to proceed");
            if (Console.ReadLine() != confirmInput) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(problemsDirPath);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
                directory.Delete(true);

            Console.WriteLine($"All find cleared from '{problemsDirPath}'");
        }

        [Command("new", "Creates a new problem C# file with template methods for creating a custom problem")]
        public static void BuildNew(string name)
        {
            string newBuildPath = $@"{assemblyPath}\Problems\{name}.cs";

            if (File.Exists(newBuildPath))
                throw new CreateDuplicateFileException($"Problem with name '{name}' already exists.");

            FileStream fileStream = File.Create(newBuildPath);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(Solution.sourceCode);
            streamWriter.Close();
            Console.WriteLine($"New problem '{name}' created successfully.");

            if (File.Exists($@"{problemsDirPath}\Solutions.csproj")) 
                return;

            Console.WriteLine("No Solutions.csproj file found, generating project file... ");
            string command = $@"/C dotnet new console -n ""Solutions"" -o {problemsDirPath}";
            Process cmdProcess = Process.Start("cmd.exe", command);
            cmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmdProcess.StartInfo.UseShellExecute = false;
            cmdProcess.EnableRaisingEvents = true;
            cmdProcess.Exited += new EventHandler(OnNewCsprojCreated);
        }

        // Works in conjunction with BuildNew
        public static void OnNewCsprojCreated(object sender, EventArgs e)
        {
            // 'dotnet new console' generates a starting file which we don't need
            string generatedEntryFilePath = $@"{problemsDirPath}\Program.cs";
            if (File.Exists(generatedEntryFilePath))
                File.Delete(generatedEntryFilePath);

            // Edit the csproj file to include a reference to MethodAlarmUtils.dll file
            // (The .NET CLI doesn't include a command to add local assembly references)
            string csprojPath = $@"{problemsDirPath}\Solutions.csproj";
            if (!File.Exists(csprojPath))
                return;

            XDocument doc = XDocument.Load(csprojPath);
            XElement root = doc.Root;
            root.Add(
                new XElement("Reference", 
                new XAttribute("Include", "MethodAlarmUtils"), 
                new XElement("HintPath", @"..\MethodAlarmUtils.dll")));
            doc.Save(csprojPath);

            // Causes undesired behaviour with the CommandParser ReadLine() formatting
            //Console.WriteLine("\nSuccessfully generated new .csproj file.");
        }

        [Command("problems", "Lists all problems")]
        public static void ListProblems()
        {
            string[] dirs = Directory.GetFiles(problemsDirPath, "*.cs");
            Console.WriteLine($"Problems found: {dirs.Length}");
            for (int i = 0; i < dirs.Length; i++)
                Console.WriteLine($"    {(i == dirs.Length - 1 ? "└───" : "├───")}{Path.GetFileNameWithoutExtension(dirs[i])}");
        }

        [Command("edit", "Opens a problem in editor")]
        public static void EditProblem(string problemName)
        {
            EditorManager.OpenFileInIDE(GetProblemPathFromName(problemName));
            Console.WriteLine("File opened.");
        }

        [Command("check", "Checks a problem to see if it is setup properly")]
        public static ProblemCompileData CheckProblemIntegrity(string name)
        {
            List<string> errors = new List<string>();
            Console.WriteLine($"Checking problem '{name}':");

            // Compile problem file
            CompilerResults compilerResults;
            try
            {
                compilerResults = CompileFileIntoMemory(GetProblemPathFromName(name), true);
            }
            catch (Exception e)
            {
                Console.Write($"[{e.GetType().Name}]: ");
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }

            // Check Settings class
            MethodInfo solutionMethod = null;
            MethodInfo validationMethod = null;

            Type entry = compilerResults.CompiledAssembly.GetType("Settings");
            if (entry == null)
                errors.Add($"Problem '{name}.cs' must contain 'Settings' class.");
            else
            {
                // Check for Solution method and make sure its setup properly
                solutionMethod = entry.GetMethod("Solution");
                if (solutionMethod == null)
                    errors.Add("Class 'Settings' must contain a 'Solution' method.");
                else if (!CheckMethod(solutionMethod, typeof(AnyExceptVoid), null, true))
                    errors.Add("'Settings' class not set up correctly.");

                // Check for GetParametersForValidation method and make sure its setup properly
                validationMethod = entry.GetMethod("GetParametersForValidation");
                if (validationMethod == null)
                    errors.Add("Class 'Attempt' must contain a 'GetParametersForValidation' method.");
                else
                {
                    if (!CheckMethod(validationMethod, typeof(MethodParameters[]), new Type[0], true))
                        errors.Add("'GetParametersForValidation' method not set up correctly.");
                    else
                        CheckMethodParameters(ref errors, ref solutionMethod, ref validationMethod);
                }
            }

            // Check Attempt class
            Type attempt = compilerResults.CompiledAssembly.GetType("Attempt");
            if (attempt == null)
                errors.Add($"Problem '{name}.cs' must contain 'Attempt' class.");
            else
            {
                MethodInfo[] methods = attempt.GetMethods();
                if (methods.Length != 5) // methods array also includes four methods from Object
                    errors.Add($"Class 'Attempt' must only contain one method.");
                else if(solutionMethod != null)
                {
                    // Check that the attempt method and solution method have matching return types
                    MethodInfo attemptMethod = methods[0];
                    if (attemptMethod.ReturnType != solutionMethod.ReturnType)
                        errors.Add($"Attempt method '{attemptMethod.Name}' must have same return type as 'Solution'.");

                    // Check that the attempt method and solution method have matching parameter types
                    ParameterInfo[] solutionParameters = solutionMethod.GetParameters();
                    ParameterInfo[] attemptParameters = attemptMethod.GetParameters();
                    for (int i = 0; i < solutionParameters.Length; i++)
                        if (solutionParameters[i].ParameterType != attemptParameters[i].ParameterType)
                        {
                            errors.Add($"Attempt method '{attemptMethod.Name}' must have same parameter types as 'Solution'.");
                            break;
                        }                            
                }
            }

            // Write the errors present to console
            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < errors.Count; i++)
                Console.WriteLine($"[ProblemIntegrityError|{name}]: {errors[i]}");
            Console.ForegroundColor = ConsoleColor.White;

            if (errors.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Problem '{name}' is set up correctly.");
                Console.ForegroundColor = ConsoleColor.White;

                return new ProblemCompileData(name, solutionMethod, validationMethod);
            }

            return null;
        }

        [Command("checkall", "Checks all problems to determain if they are setup properly")]
        public static bool CheckProblemIntegrityAll()
        {
            string[] dirs = Directory.GetFiles(problemsDirPath, "*.cs");
            int problemsWithErrors = 0;
            for (int i = 0; i < dirs.Length; i++)
            {
                string problemName = Path.GetFileNameWithoutExtension(dirs[i]);
                problemsWithErrors += Convert.ToInt32(!(CheckProblemIntegrity(problemName) == null));

                // Add spaces between each check for improved readability
                Console.Write($"{(i == dirs.Length - 1 ? string.Empty : "\n")}");
            }

            return problemsWithErrors == 0;
        }

        // Checks whether or not the method is setup up correctly, based on given criteria
        // Passing parameters as 'null' will skip checking them, passing 'new Type[0]' will ensure there are none
        public static bool CheckMethod(MethodInfo method, Type returnType, Type[] parameters, bool writeErrors)
        {
            if (method == null) return false;
            List<string> errors = new List<string>();

            // Check method return type
            if (returnType == typeof(AnyExceptVoid) && method.ReturnType == typeof(void))
                errors.Add($"'{method}' cannot return void.");
            else if (returnType != typeof(AnyExceptVoid) && method.ReturnType != returnType)
                errors.Add($"'{method}' must return {returnType}.");

            // If specified, check the parameters
            if (parameters != null)
            {
                ParameterInfo[] parameterInfos = method.GetParameters();
                if (parameters.Length != parameterInfos.Length)
                    errors.Add($"Method '{method}' does have correct parameter count.");

                // Check parameter types
                if (parameterInfos.Length > 0)
                {
                    Type[] parameterTypes = new Type[parameterInfos.Length];
                    for (int i = 0; i < parameterTypes.Length; i++)
                        parameterTypes[i] = parameterInfos[i].ParameterType;
                    if (!parameterTypes.SequenceEqual(parameters))
                        errors.Add($"Method '{method}' does not use the correct parameters.");
                }
            }

            if (writeErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                for (int i = 0; i < errors.Count; i++)
                    Console.WriteLine($"[CheckMethodError|{method.Name}]: {errors[i]}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            return errors.Count == 0;
        }

        // This method seems redundant because its only called once from CheckProblemIntegrity, but I assure you
        // it serves an important purpose: to NOT use a goto statement (and to improve readability, but who cares about that)
        private static void CheckMethodParameters(ref List<string> errors, ref MethodInfo solutionMethod, ref MethodInfo validationMethod)
        {
            // This limit variable should be put in a config of sorts.
            int maxValidationParams = 256;

            ParameterInfo[] solutionParameters = solutionMethod.GetParameters();
            MethodParameters[] vParams = (MethodParameters[])validationMethod.Invoke(null, null);

            if (maxValidationParams != 0 && vParams.Length > maxValidationParams)
            {
                errors.Add($"Method 'GetParametersForValidation' can only return a maximum of {maxValidationParams} MethodParameters (returned {vParams.Length}).");
                return;
            }

            for (int i = 0; i < vParams.Length; i++)
            {
                if (vParams[i].Parameters.Length != solutionParameters.Length)
                {
                    errors.Add($"Not all MethodParameters from the 'GetParametersForValidation' method contain {solutionParameters.Length} parameters.");
                    break;
                }

                for (int j = 0; j < solutionParameters.Length; j++)
                {
                    if (vParams[i].Parameters[j].GetType() != solutionParameters[j].ParameterType)
                    {
                        errors.Add($"Not all MethodParameters from the 'GetParametersForValidation' match type of 'Solution' method parameters.");
                        return;
                    }
                }
            }
        }

        // Pulls nessesary code from a problem file and generates an attempt file from it
        public static bool CreateAttemptFile(string problemName, bool printOutput = false)
        {
            string filePath = GetProblemPathFromName(problemName);
            if (filePath == string.Empty)
                throw new FileNotFoundException($"Could not find problem with name '{problemName}'");

            // Make sure given file will compile
            Console.WriteLine($"Compiling and checking '{problemName}.cs'...");
            CompilerResults compilerResults = CompileFileIntoMemory(filePath, true);

            // Check Attempt class to make sure it's present and  setup correct
            Type attemptClass = compilerResults.CompiledAssembly.GetType("Attempt");
            if (attemptClass == null)
                throw new MissingClassException($"'{problemName}.cs' must include 'Attempt' class");
            if (!attemptClass.IsPublic || !attemptClass.IsAbstract || !attemptClass.IsSealed)
                throw new IncorrectMethodSignature($"File '{problemName}.cs', 'Attempt' class must be marked as public and static.");

            string sourceCode = File.ReadAllText(filePath);
            StringBuilder tempFile = new StringBuilder();

            // Get using directives
            string[] semicolonSplit = sourceCode.Replace("\n", "").Replace("\r", "").Split(';');
            for (int i = 0; i < semicolonSplit.Length; i++)
                if (semicolonSplit[i].Trim().Length >= 5 && semicolonSplit[i].Trim().Substring(0, 5) == "using")
                    tempFile.Append($"{semicolonSplit[i]};\n");
            tempFile.Append("\n");

            // Get Attempt class
            string attemptMethodSignature = "public static class Attempt";
            int attemptClassIndex = sourceCode.IndexOf(attemptMethodSignature);
            if (attemptClassIndex == -1)
            {
                Console.WriteLine($"'{attemptMethodSignature} {{ }}' not found");
                return false;
            }

            // Get code from Attempt class body
            int bracketValue = -1;
            for (int i = attemptClassIndex; i < sourceCode.Length; i++)
            {
                if (sourceCode[i] == '{') 
                    bracketValue += bracketValue == -1 ? 2 : 1;
                else if (sourceCode[i] == '}') 
                    bracketValue--;
                tempFile.Append(sourceCode[i]);

                if (bracketValue == 0) break;
            }

            StreamWriter streamWriter = File.CreateText($@"{assemblyPath}\Attempt.cs");
            streamWriter.Write(tempFile);
            streamWriter.Close();

            Console.WriteLine("Attempt.cs file created...");
            if (printOutput)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("-Attempt.cs-------");
                Console.WriteLine(tempFile.ToString());
                Console.WriteLine("------------------");
                Console.ForegroundColor = ConsoleColor.White;
            }

            return true;
        }

        private class AnyExceptVoid { } // This solution is both the best and worst idea I have ever had

        public static CompilerResults CompileFileIntoMemory(string path, bool writeErrors)
        {
            // Check path
            if (!File.Exists(path))
                throw new FileNotFoundException($"The file '{Path.GetFileName(path)}' does not exist in '{Path.GetDirectoryName(path)}'.");
            if (Path.GetExtension(path) != ".cs")
                throw new UnexpectedFileTypeException("File must be of type *.cs");

            // Specify .dll's to include in the compile
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters compilerParams = new CompilerParameters
            {
                ReferencedAssemblies =
                    {
                        "mscorlib.dll",
                        "System.dll",
                        "System.Linq.dll",
                        "System.Collections.dll",
                        "System.Core.dll",
                        "MethodAlarmUtils.dll"
                    },
                GenerateInMemory = true,
                GenerateExecutable = false
            };

            // Compile file into memory
            CompilerResults results = provider.CompileAssemblyFromSource(compilerParams, File.ReadAllText(path));

            // If file was compiled with errors, throw exception. Write errors if specified
            if (results.Errors.Count > 0)
            {
                if (writeErrors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Problem{(results.Errors.Count == 0 ? "" : "s")} found while compiling {Path.GetFileName(path)}:");
                    foreach (CompilerError error in results.Errors)
                        Console.WriteLine($"[CompilerError|{Path.GetFileName(path)}|ln:{error.Line}|ch:{error.Column}]: {error.ErrorText}.");
                }

                throw new CompiledWithErrorsException($"File '{path}' compiles will errors.");
            }

            return results;
        }

        public static string GetRandomProblemName()
        {
            string[] dirs = Directory.GetDirectories(problemsDirPath);
            Random random = new Random();
            return Path.GetDirectoryName(dirs[random.Next(0, dirs.Length)]);
        }

        public static string GetProblemPathFromName(string problemName)
        {
            string[] dirs = Directory.GetFiles(problemsDirPath, "*.cs");
            if (dirs.Length == 0)
                throw new FileNotFoundException($"No problems found in '{problemsDirPath}'");

            for (int i = 0; i < dirs.Length; i++)
                if (problemName == Path.GetFileNameWithoutExtension(dirs[i]))
                    return dirs[i];
            throw new FileNotFoundException($"No problem found with name '{problemName}'. Use 'problems' to show a list of available problems.");
        }
    }
}
