using System;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Text;

namespace SimpleProcessRunner {

	public sealed class ProcessRunner : IProcessRunner {

		public static IProcessRunner Default = new ProcessRunner();

		public ProcessResult Run(
				string workingDirectory,
				string process,
				string arguments,
				TimeSpan timeout
			) {

			int timeoutMilliseconds = Convert.ToInt32( timeout.TotalMilliseconds );

			int exitCode;
			StringBuilder standardOutput = new StringBuilder();
			StringBuilder standardError = new StringBuilder();
			Stopwatch watch = new Stopwatch();

			using( Process p = new Process() ) {

				ProcessStartInfo psi = p.StartInfo;
				psi.FileName = process;
				psi.Arguments = arguments;
				psi.WorkingDirectory = workingDirectory;

				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;
				psi.RedirectStandardOutput = true;
				psi.RedirectStandardError = true;

				p.OutputDataReceived +=
					delegate( object sender, DataReceivedEventArgs @event ) {
						if( !String.IsNullOrEmpty( @event.Data ) ) {
							standardOutput.AppendLine( @event.Data );
						}
					};

				p.ErrorDataReceived +=
					delegate( object sender, DataReceivedEventArgs @event ) {
						if( !String.IsNullOrEmpty( @event.Data ) ) {
							standardError.AppendLine( @event.Data );
						}
					};

				watch.Start();

				p.Start();
				int processId = p.Id;

				try {
					p.BeginOutputReadLine();
					p.BeginErrorReadLine();

					if( p.WaitForExit( timeoutMilliseconds ) ) {

						// Call the blocking WaitForExit to ensure that the asynchronous output and error
						// streams have been received
						// http://msdn.microsoft.com/en-us/library/fb4aw7b8%28v=vs.110%29.aspx
						p.WaitForExit();

					} else {

						string timeoutMsg = String.Format(
								CultureInfo.InvariantCulture,
								"Timed out waiting for process {0} ( {1} ) to exit",
								process,
								arguments
							);

						throw new TimeoutException( timeoutMsg );
					}

					exitCode = p.ExitCode;

				} finally {

					KillChildProcesses( processId );

					if( !p.HasExited ) {
						p.Kill();
					}
				}

				watch.Stop();
			}

			ProcessResult result = new ProcessResult(
					workingDirectory: workingDirectory,
					process: process,
					arguments: arguments,
					exitCode: exitCode,
					standardOutput: standardOutput.ToString(),
					standardError: standardError.ToString(),
					duration: watch.Elapsed
				);

			return result;
		}

		private const string QueryTempalte = @"
SELECT ProcessId
FROM Win32_Process
WHERE (
	ParentProcessId = {0}
)";

		private void KillChildProcesses( int prcoessId ) {

			string query = String.Format(
					CultureInfo.InvariantCulture,
					QueryTempalte,
					prcoessId
				);

			ManagementObjectSearcher searcher = new ManagementObjectSearcher( query );

			ManagementObjectCollection moc = searcher.Get();
			foreach( ManagementObject mo in moc ) {

				int childProcessId = Convert.ToInt32( mo["ProcessId"] );
				KillChildProcesses( childProcessId );

				try {
					Process proc = Process.GetProcessById( childProcessId );
					proc.Kill();
				} catch {
				}
			}
		}
	}
}
