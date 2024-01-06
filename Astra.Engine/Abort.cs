using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Astra.Engine;

internal static class Abort
{
    [DllImport("libc")]
    private static extern void abort();
    
    [DllImport("kernel32.dll")]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    
    private static void TerminateCurrentProcess()
    {
        var currentProcess = Process.GetCurrentProcess();
        var processHandle = currentProcess.Handle;

        TerminateProcess(processHandle, 6);
    }

    private static bool IsUnix => (int)Environment.OSVersion.Platform is 4 or 6 or 128;

    public static bool CanBeAborted
    {
        get
        {
            try
            {
                if (IsUnix) 
                    abort();
                else TerminateCurrentProcess();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}