public static class Solution
{
    // The solution source code that will be used when creating a new problem
    // (C# cannot read its own source code, only assembly, that's why this is stored as a string)

    public static string sourceCode =
    @"using System;
using System.Collections.Generic;
using MethodAlarmUtils;

public static class Settings
{
    // This is the method that the attempt's output will be compared against. 
    public static int Solution(int x, int y)
    {
        return x + y;
    }

    // Returns an array of MethodParameters used to validate the attempt.
    public static MethodParameters[] GetParametersForValidation()
    {
        MethodParameters[] methodParams = new MethodParameters[100];
        Random random = new Random();

        for (int i = 0; i < methodParams.Length; i++)
        {
            int randomX = random.Next(0, 10000);
            int randomY = random.Next(0, 10000);
            methodParams[i] = new MethodParameters(randomX, randomY);
        }   

        return methodParams;
    }
}

// When the alarm sequence is triggered a new file containing the 'Attempt' class below, as well as any comments
// contained within it will be copied into a temp file, were the user will attempt to solve the problem.
// All 'using' directives will be copied from this file into the temp file.

public static class Attempt
{
    // Return the sum of x and y.
    public static int Add(int x, int y)
    {
        return 0;
    }
}";
}
