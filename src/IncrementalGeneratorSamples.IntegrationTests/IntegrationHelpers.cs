using System.Diagnostics;

namespace IncrementalGeneratorSamples.IntegrationTests
{
    public class IntegrationHelpers
    {
        public static Process? TestOutputCompiles(string projectPath)
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
            if (exeProcess is not null)
            {
                exeProcess.WaitForExit(10000);

                Assert.Equal(0, exeProcess.ExitCode);
                Assert.Equal("", (string?)exeProcess.StandardError.ReadToEnd());
            }

            return exeProcess;
        }
        public static string? RunGeneratedProject(string projectPath, string arguments)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = $"{projectPath}{IsOsWindows(".exe", "")}";
            startInfo.Arguments = arguments;

            Process? exeProcess = Process.Start(startInfo);
            Assert.NotNull(exeProcess);
            if (exeProcess is not null)
            {
                exeProcess.WaitForExit();

                var output = exeProcess.StandardOutput.ReadToEnd();
                var error = exeProcess.StandardError.ReadToEnd();

                Assert.Equal(0, exeProcess.ExitCode);
                Assert.Equal("", error);
                return output;
            }
            return null;

            static string IsOsWindows(string windowsString, string unixString)
                => Environment.OSVersion.Platform == PlatformID.Unix
                    ? unixString
                    : windowsString;
                    }
    }
}
