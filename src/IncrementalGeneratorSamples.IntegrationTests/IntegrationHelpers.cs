using System.Diagnostics;

namespace IncrementalGeneratorSamples.IntegrationTests
{
    public class IntegrationHelpers
    {
        public static (int exitCode, string output, string err) TestOutputCompiles(string projectPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            ////startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WorkingDirectory = projectPath;
            startInfo.FileName = "dotnet";
            startInfo.Arguments = "build";
            Process? exeProcess = Process.Start(startInfo);
            Assert.NotNull(exeProcess);

            try
            {
                if (exeProcess is not null)
                {
                    var err = exeProcess.StandardError.ReadToEnd();
                    var output = exeProcess.StandardOutput.ReadToEnd();
                    var exitCode = exeProcess.ExitCode;
                    exeProcess.WaitForExit(5000);
                    return (exitCode, output, err);
                }
                throw new InvalidOperationException("Process not started correctly.");
            }
            catch
            {
                throw;
            }
        }

        public static (int exitCode, string output, string err)  RunGeneratedProject(string workingDir, string executableName, string arguments)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.WorkingDirectory = workingDir;
            startInfo.FileName = $"{executableName}{IsOsWindows(".exe", "")}";
            startInfo.Arguments = arguments;

            Process? exeProcess = Process.Start(startInfo);
            Assert.NotNull(exeProcess);
            if (exeProcess is not null)
            {
                var err = exeProcess.StandardError.ReadToEnd();
                var output = exeProcess.StandardOutput.ReadToEnd();
                var exitCode = exeProcess.ExitCode;
                exeProcess.WaitForExit(5000);
                return (exitCode, output, err);
            }
            throw new InvalidOperationException("Process not started correctly.");
        }

        public static string IsOsWindows(string windowsString, string unixString)
                => Environment.OSVersion.Platform == PlatformID.Unix
                    ? unixString
                    : windowsString;
    }
}
