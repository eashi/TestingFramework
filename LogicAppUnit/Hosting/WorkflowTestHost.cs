﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace LogicAppUnit.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The function test host.
    /// </summary>
    internal class WorkflowTestHost : IDisposable
    {
        /// <summary>
        /// Get or sets the output data.
        /// </summary>
        public List<string> OutputData { get; private set; }

        /// <summary>
        /// Gets or sets the error data.
        /// </summary>
        public List<string> ErrorData { get; private set; }

        /// <summary>
        /// Gets or sets the Working directory.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// The Function runtime process.
        private Process Process;

        /// <c>true</c> if the Functions runtime start-up logs are to be written to the console, otherwise <c>false</c>.
        /// The start-up logs can be rather verbose so we don't always went to include this information in the test execution logs.
        private readonly bool WriteFunctionRuntineStartupLogsToConsole;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowTestHost"/> class.
        /// </summary>
        public WorkflowTestHost(
            WorkflowTestInput[] inputs = null,
            string localSettings = null, string parameters = null, string connectionDetails = null, string host = null, DirectoryInfo artifactsDirectory = null,
            bool writeFunctionRuntineStartupLogsToConsole = false)
        {
            this.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());
            this.OutputData = new List<string>();
            this.ErrorData = new List<string>();
            this.WriteFunctionRuntineStartupLogsToConsole = writeFunctionRuntineStartupLogsToConsole;

            this.StartFunctionRuntime(inputs, localSettings, parameters, connectionDetails, host, artifactsDirectory);
        }

        /// <summary>
        /// Starts the function runtime.
        /// </summary>
        protected void StartFunctionRuntime(WorkflowTestInput[] inputs, string localSettings, string parameters, string connectionDetails, string host, DirectoryInfo artifactsDirectory)
        {
            try
            {
                // Kill any remaining function host processes that might interfere with the tests
                KillFunctionHostProcesses();

                Directory.CreateDirectory(this.WorkingDirectory);

                if (inputs != null && inputs.Length > 0)
                {
                    foreach (var input in inputs)
                    {
                        if (!string.IsNullOrEmpty(input.WorkflowName))
                        {
                            Directory.CreateDirectory(Path.Combine(this.WorkingDirectory, input.WorkflowName));
                            File.WriteAllText(Path.Combine(this.WorkingDirectory, input.WorkflowName, input.WorkflowFilename), input.WorkflowDefinition);
                        }
                    }
                }

                if (artifactsDirectory != null)
                {
                    if (!artifactsDirectory.Exists)
                    {
                        throw new DirectoryNotFoundException(artifactsDirectory.FullName);
                    }

                    var artifactsWorkingDirectory = Path.Combine(this.WorkingDirectory, "Artifacts");
                    Directory.CreateDirectory(artifactsWorkingDirectory);
                    CopyDirectory(source: artifactsDirectory, destination: new DirectoryInfo(artifactsWorkingDirectory));
                }

                if (!string.IsNullOrEmpty(parameters))
                {
                    File.WriteAllText(Path.Combine(this.WorkingDirectory, "parameters.json"), parameters);
                }

                if (!string.IsNullOrEmpty(connectionDetails))
                {
                    File.WriteAllText(Path.Combine(this.WorkingDirectory, "connections.json"), connectionDetails);
                }

                if (!string.IsNullOrEmpty(localSettings))
                {
                    File.WriteAllText(Path.Combine(this.WorkingDirectory, "local.settings.json"), localSettings);
                }
                else
                {
                    throw new InvalidOperationException("The local.settings.json file is not provided or its path not found. This file is needed for the unit testing.");
                }

                if (!string.IsNullOrEmpty(host))
                {
                    File.WriteAllText(Path.Combine(this.WorkingDirectory, "host.json"), host);
                }
                else
                {
                    throw new InvalidOperationException("The host.json file is not provided or its path not found. This file is needed for the unit testing.");
                }

                this.Process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = this.WorkingDirectory,
                        FileName = GetEnvPathForFunctionTools(),
                        Arguments = "start --verbose",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                var processStarted = new TaskCompletionSource<bool>();

                this.Process.OutputDataReceived += (sender, args) =>
                {
                    var outputData = args.Data;

                    if (WriteFunctionRuntineStartupLogsToConsole || processStarted.Task.IsCompleted)
                    {
                        Console.WriteLine(outputData);
                    }

                    if (outputData != null && outputData.Contains("Host started") && !processStarted.Task.IsCompleted)
                    {
                        processStarted.SetResult(true);
                    }

                    lock (this)
                    {
                        this.OutputData.Add(args.Data);
                    }
                };

                var errorData = string.Empty;
                this.Process.ErrorDataReceived += (sender, args) =>
                {
                    errorData = args.Data;
                    Console.Write(errorData);

                    lock (this)
                    {
                        this.ErrorData.Add(args.Data);
                    }
                };

                this.Process.Start();

                this.Process.BeginOutputReadLine();
                this.Process.BeginErrorReadLine();

                var result = Task.WhenAny(processStarted.Task, Task.Delay(TimeSpan.FromMinutes(2))).Result;

                if (result != processStarted.Task)
                {
                    throw new InvalidOperationException("Runtime did not start properly. Please make sure you have the latest Azure Functions Core Tools installed and available on your PATH environment variable, and that Azurite is up and running.");
                }

                if (this.Process.HasExited)
                {
                    throw new InvalidOperationException($"Runtime did not start properly. The error is '{errorData}'. Please make sure you have the latest Azure Functions Core Tools installed and available on your PATH environment variable, and that Azurite is up and running.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                // Kill any remaining function host processes so that we can then delete the working directory
                KillFunctionHostProcesses();

                Directory.Delete(this.WorkingDirectory, recursive: true);

                throw;
            }
        }

        /// <summary>
        /// Kill all instances of the Function host process.
        /// </summary>
        private static void KillFunctionHostProcesses()
        {
            Process[] processes = Process.GetProcessesByName("func");
            foreach (var process in processes)
            {
                process.Kill(true);
            }
        }

        /// <summary>
        /// Retrieve the exact path of func executable (Azure Function core tools). 
        /// </summary>
        /// <returns>The path to the func executable.</returns>
        /// <exception cref="Exception">Thrown when the location of func executable could not be found.</exception>
        private static string GetEnvPathForFunctionTools()
        {
            string exePath;
            if (OperatingSystem.IsWindows())
            {
                var enviromentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                exePath = enviromentPath.Split(Path.PathSeparator).Select(x => Path.Combine(x, "func.exe")).Where(x => File.Exists(x)).FirstOrDefault();
            }
            else
            {
                var enviromentPath = Environment.GetEnvironmentVariable("PATH");
                exePath = enviromentPath.Split(Path.PathSeparator).Select(x => Path.Combine(x, "func")).Where(x => File.Exists(x)).FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(exePathWithExtension))
            {
                Console.WriteLine($"Path for Azure Function Core tools: {exePathWithExtension}");
                return exePathWithExtension;
            }
            else
            {
                throw new Exception("Enviroment variables do not have func executable path added.");
            }
        }

        /// <summary>
        /// Copies the directory.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        protected static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            // Copy all files
            var files = source.GetFiles();
            foreach (var file in files)
            {
                file.CopyTo(Path.Combine(destination.FullName, file.Name));
            }

            // Process subdirectories
            var dirs = source.GetDirectories();
            foreach (var dir in dirs)
            {
                // Get destination directory
                var destinationDir = Path.Combine(destination.FullName, dir.Name);

                // Call CopyDirectory() recursively
                CopyDirectory(dir, new DirectoryInfo(destinationDir));
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Kill any remaining function host processes so that we can then delete the working directory
                this.Process?.Close();
                KillFunctionHostProcesses();
            }
            finally
            {
                var i = 0;
                while (i < 5)
                {
                    try
                    {
                        Directory.Delete(this.WorkingDirectory, recursive: true);
                        break;
                    }
                    catch
                    {
                        i++;
                        Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                    }
                }
            }
        }
    }
}
