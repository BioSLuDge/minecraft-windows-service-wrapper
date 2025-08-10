using System.Diagnostics;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IProcessRunner
    {
        ProcessResult Run(ProcessStartInfo startInfo);
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}
