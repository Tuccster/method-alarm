using System.Reflection;

namespace TuccstersMethodAlarm
{
    // A ProblemCompileData object is the return type of ProblemManager.CheckMethodIntegrity().
    // This way, when the alarm chooses a problem, it can check it and also get the data needed
    // to compare the results of attempt and the solution without needing to compile again.

    public class ProblemCompileData
    {
        public string problemName;
        public MethodInfo solutionMethod;
        public MethodInfo validationMethod;

        public ProblemCompileData(string name, MethodInfo solution, MethodInfo validation)
        {
            problemName = name;
            solutionMethod = solution;
            validationMethod = validation;
        }
    }
}
