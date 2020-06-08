using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleApp64
{
    public class PinInscountHelper
    {
        public string PinExePath { get; private set; }
        public string PinDirectory => Path.GetDirectoryName(PinExePath);

        public string Inscount32Path { get; private set; }
        public string Inscount64Path { get; private set; }
        public PinInscountHelper(string PinExePath, string Inscount32Path, string Inscount64Path)
        {
            if (!File.Exists(PinExePath))
                throw new FileNotFoundException("Can't find pin executable!");

            this.PinExePath = PinExePath;
            if (!string.IsNullOrWhiteSpace(Inscount32Path))
            {
                if (File.Exists(Inscount32Path))
                    this.Inscount32Path = Inscount32Path;
                else if (File.Exists(Path.Combine(PinDirectory, Inscount32Path.TrimStart('/', '\\'))))
                    this.Inscount32Path = Path.Combine(PinDirectory, Inscount32Path.TrimStart('/', '\\'));
                else
                    throw new FileNotFoundException("Can't find 32 bit version of Inscount library!");
            }

            if (!string.IsNullOrWhiteSpace(Inscount64Path))
            {
                if (File.Exists(Inscount64Path))
                    this.Inscount64Path = Inscount64Path;
                else if (File.Exists(Path.Combine(PinDirectory, Inscount64Path.TrimStart('/', '\\'))))
                    this.Inscount64Path = Path.Combine(PinDirectory, Inscount64Path.TrimStart('/', '\\'));
                else
                    throw new FileNotFoundException("Can't find 64 bit version of Inscount library!");
            }
        }

        private long _RunCounter = 0;
        private ulong _RunPin(string TargetExe, string RunArguments, string Input, bool ThreadSafe, string InscountPath)
        {
            string WorkDir = PinDirectory;

            while (ThreadSafe)
            {
                WorkDir = Path.Combine(PinDirectory, Interlocked.Increment(ref _RunCounter).ToString());
                if (!Directory.Exists(WorkDir))
                {
                    Directory.CreateDirectory(WorkDir);
                    break;
                }
            }

            ProcessStartInfo pinfo = new ProcessStartInfo();
            pinfo.WorkingDirectory = WorkDir;
            pinfo.FileName = PinExePath;
            pinfo.Arguments = string.Format("-t \"{0}\" -- \"{1}\" \"{2}\"", InscountPath, TargetExe, RunArguments);

            pinfo.UseShellExecute = false;
            pinfo.RedirectStandardInput = true;
            pinfo.RedirectStandardOutput = true;

            using (Process p = Process.Start(pinfo))
            {
                if (!string.IsNullOrEmpty(Input))
                    p.StandardInput.WriteLine(Input);

                p.WaitForExit();
            }
            ulong inscount = Convert.ToUInt64(File.ReadAllText(Path.Combine(WorkDir, "inscount.out")).Trim().Substring(6));


            while (ThreadSafe)
            {
                Thread.Sleep(100);
                try
                {
                    Directory.Delete(WorkDir, true);
                    break;
                }
                catch
                {
                    continue;
                }
            }

            return inscount;
        }

        public ulong Run32(string TargetExe, string RunArguments, string Input, bool ThreadSafe)
        {
            if (Inscount32Path == null)
                throw new InvalidOperationException("Path to 32 bit version of inscount not setted!");
            return _RunPin(TargetExe, RunArguments, Input, ThreadSafe, Inscount32Path);
        }
        public ulong Run64(string TargetExe, string RunArguments, string Input, bool ThreadSafe)
        {
            if (Inscount64Path == null)
                throw new InvalidOperationException("Path to 64 bit version of inscount not setted!");
            return _RunPin(TargetExe, RunArguments, Input, ThreadSafe, Inscount64Path);
        }
    }
}
