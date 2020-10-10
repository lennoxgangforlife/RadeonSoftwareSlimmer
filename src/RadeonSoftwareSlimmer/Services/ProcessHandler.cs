﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RadeonSoftwareSlimmer.ViewModels;

namespace RadeonSoftwareSlimmer.Services
{
    public class ProcessHandler
    {
        private readonly FileInfo _file;
        private readonly string _fileNameWithoutExtension;

        public ProcessHandler(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new NullReferenceException($"{fileName} is null or empty");

            _file = new FileInfo(fileName);
            _fileNameWithoutExtension = _file.Name.Substring(0, _file.Name.Length - _file.Extension.Length);
        }


        public int RunProcess()
        {
            return RunProcess(string.Empty);
        }

        public int RunProcess(string arguments)
        {
            if (!_file.Exists)
            {
                StaticViewModel.AddDebugMessage($"{_file.FullName} does not exist or user does not have access");
                return -1;
            }

            using (Process process = new Process())
            {
                process.StartInfo.FileName = _file.FullName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                StaticViewModel.AddDebugMessage($"Running {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                process.Start();
                process.WaitForExit();
                StaticViewModel.AddDebugMessage($"Process finished with ExitCode: {process.ExitCode}");
                StaticViewModel.AddDebugMessage($"StandardOutput: {process.StandardOutput.ReadToEnd()}");
                StaticViewModel.AddDebugMessage($"StandardError: {process.StandardError.ReadToEnd()}");

                return process.ExitCode;
            }
        }

        public bool IsProcessRunning()
        {
            if (!_file.Exists)
                return false;

            if (Process.GetProcessesByName(_fileNameWithoutExtension).Length > 0)
                return true;
            else
                return false;
        }

        public void WaitForProcessToEnd(int maxWaitSeconds)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (IsProcessRunning() == true)
            {
                if (sw.ElapsedMilliseconds >= maxWaitSeconds * 1000)
                {
                    try
                    {
                        foreach (Process process in Process.GetProcessesByName(_fileNameWithoutExtension))
                        {
                            process.Kill();
                        }
                    }
                    catch (Win32Exception) { }
                }

                Thread.Sleep(1000);
            }
        }
    }
}