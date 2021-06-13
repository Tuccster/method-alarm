using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuccstersMethodAlarm
{
    public class CommandNotFoundException : Exception { public CommandNotFoundException(string message) : base(message) { } }

    public class IncorrectMethodSignature : Exception { public IncorrectMethodSignature(string message) : base(message) { } }

    public class MissingClassException : Exception { public MissingClassException(string message) : base(message) { } }

    public class CreateDuplicateFileException : Exception { public CreateDuplicateFileException(string message) : base(message) { } }

    public class UnexpectedFileTypeException : Exception { public UnexpectedFileTypeException(string message) : base(message) { } }

    public class CompiledWithErrorsException : Exception { public CompiledWithErrorsException(string message) : base(message) { } }

    public class ValuesNotEqualException : Exception { public ValuesNotEqualException(string message) : base(message) { } }

    public class NullConfigException : Exception { public NullConfigException(string message) : base(message) { } }

    public class EditorNotSetException : Exception { public EditorNotSetException(string message) : base(message) { } }

    public class FailedWindowDockException : Exception { public FailedWindowDockException(string message) : base(message) { } }
}
