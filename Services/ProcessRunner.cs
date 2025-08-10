using System;
using System.Diagnostics;

namespace minecraft_windows_service_wrapper.Services
{
    public class ProcessRunner : IProcessRunner
    {
        public ProcessResult Run(ProcessStartInfo startInfo)
        {
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start process");
            process.WaitForExit();

            var output = startInfo.RedirectStandardOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
            var error = startInfo.RedirectStandardError ? process.StandardError.ReadToEnd() : string.Empty;

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = output,
                StandardError = error
            };
        }
    }
}
