using System;
using System.Diagnostics;

namespace LightGL.DynamicLibrary
{
    public class ProcessUtils
    {
        public static ProcessResult ExecuteCommand(string command, string arguments, string workingDirectory = ".")
        {
            var proc = new Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = command;
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.ErrorDialog = false;
            proc.StartInfo.WorkingDirectory = workingDirectory;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            var outputString = proc.StandardOutput.ReadToEnd();
            var errorString = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            var exitCode = proc.ExitCode;

            return new ProcessResult()
            {
                OutputString = outputString,
                ErrorString = errorString,
                ExitCode = exitCode,
            };
            //proc.WaitForExit();
        }

        public static ProcessResult RunProgramInBackgroundAsRoot(string applicationPath, string applicationArguments)
        {
            // Create a new process object
            var processObj = new Process();

            processObj.StartInfo = new ProcessStartInfo()
            {
                // StartInfo contains the startup information of the new process
                FileName = applicationPath,
                Arguments = applicationArguments,

                UseShellExecute = true,
                Verb = "runas",

                // These two optional flags ensure that no DOS window appears
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                //RedirectStandardOutput = true,
                RedirectStandardOutput = false,
            };

            var OutputString = "";
            var ErrorString = "";
            Exception? exception = null;
            // Wait that the process exits
            try
            {
                // Start the process
                processObj.Start();

                //OutputString = ProcessObj.StandardOutput.ReadToEnd();
                //ErrorString = ProcessObj.StandardError.ReadToEnd();
                processObj.WaitForExit();
            }
            catch (Exception ex2)
            {
                exception = ex2;
                Console.WriteLine(exception);
            }

#pragma warning disable CS8601
            return new ProcessResult()
            {
                OutputString = OutputString,
                ErrorString = ErrorString,
                Exception = exception,
                ExitCode = processObj.ExitCode,
            };
#pragma warning restore CS8601
        }
    }

    public class ProcessResult
    {
        public string OutputString;
        public string ErrorString;
        public int ExitCode;
        public Exception Exception;
        public bool Success => Exception == null && ExitCode == 0;
    }
}
