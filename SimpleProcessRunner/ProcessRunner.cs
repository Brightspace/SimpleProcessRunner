using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleProcessRunner {

	public sealed partial class ProcessRunner : IProcessRunner {

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

				p.OutputDataReceived += WriteToBuffer( standardOutput );
				p.ErrorDataReceived += WriteToBuffer( standardError );

				watch.Start();

				p.Start();

				int processId = p.Id;
				DateTime startTime = p.StartTime;

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

						string standardOutputTxt;
						lock( standardOutput ) {
							standardOutputTxt = standardOutput.ToString();
						}

						string standardErrorTxt;
						lock( standardError ) {
							standardErrorTxt = standardError.ToString();
						}

						throw new ProcessTimeoutException(
								message: timeoutMsg,
								workingDirectory: workingDirectory,
								process: process,
								arguments: arguments,
								standardOutput: standardOutputTxt,
								standardError: standardErrorTxt
							);
					}

					exitCode = p.ExitCode;

				} catch( TimeoutException ) {

					KillChildProcesses( processId, startTime );
					throw;

				} finally {

					p.Refresh();

					if( !p.HasExited ) {
						p.Kill();
					}
				}

				watch.Stop();
			}

			{
				string standardOutputTxt;
				lock( standardOutput ) {
					standardOutputTxt = standardOutput.ToString();
				}

				string standardErrorTxt;
				lock( standardError ) {
					standardErrorTxt = standardError.ToString();
				}

				ProcessResult result = new ProcessResult(
						workingDirectory: workingDirectory,
						process: process,
						arguments: arguments,
						exitCode: exitCode,
						standardOutput: standardOutputTxt,
						standardError: standardErrorTxt,
						duration: watch.Elapsed
					);

				return result;
			}
		}

		public async Task<ProcessResult> RunAsync(
			string workingDirectory,
			string process,
			string arguments,
			TimeSpan timeout
		) {
			var completionSource = new TaskCompletionSource<int>();

			StringBuilder standardOutput = new StringBuilder();
			StringBuilder standardError = new StringBuilder();

			ProcessStartInfo psi = new ProcessStartInfo( process, arguments ) {
				WorkingDirectory = workingDirectory,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			Process p = new Process {
				StartInfo = psi,
				EnableRaisingEvents = true
			};

			p.OutputDataReceived += WriteToBuffer(standardOutput);
			p.ErrorDataReceived += WriteToBuffer(standardError);

			TaskCompletionSource<ProcessResult> tcs = new TaskCompletionSource<ProcessResult>();
			p.Exited += (sender, eventArgs) => {
				Process p2 = (Process)sender;
				try {
					string standardOutputTxt;
					lock (standardOutput) {
						standardOutputTxt = standardOutput.ToString();
					}

					string standardErrorTxt;
					lock (standardError) {
						standardErrorTxt = standardError.ToString();
					}

					ProcessResult result = new ProcessResult(
						workingDirectory: workingDirectory,
						process: process,
						arguments: arguments,
						exitCode: p2.ExitCode,
						standardOutput: standardOutputTxt,
						standardError: standardErrorTxt,
						duration: (p2.ExitTime - p2.StartTime) );

					tcs.TrySetResult(result);
				} catch (Exception ex) {
					tcs.TrySetException(ex);
				}
			};

			p.Start();
			p.BeginErrorReadLine();
			p.BeginOutputReadLine();
			
			if( timeout > TimeSpan.Zero ) {
				CancellationTokenSource cts = new CancellationTokenSource( timeout );

				cts.Token.Register(
					() => {
						tcs.TrySetCanceled();	
					},
					false );
			}

			try {
				return await tcs.Task;
			} catch( TaskCanceledException ) {
				KillChildProcesses( p.Id, p.StartTime );
				
				string timeoutMsg = String.Format(
					CultureInfo.InvariantCulture,
					"Timed out waiting for process {0} ( {1} ) to exit",
					process,
					arguments
					);
				string standardOutputTxt;
				lock( standardOutput ) {
					standardOutputTxt = standardOutput.ToString();
				}

				string standardErrorTxt;
				lock( standardError ) {
					standardErrorTxt = standardError.ToString();
				}
				
				throw new ProcessTimeoutException(
					message: timeoutMsg,
					workingDirectory: workingDirectory,
					process: process,
					arguments: arguments,
					standardOutput: standardOutputTxt,
					standardError: standardErrorTxt
					);
			} finally {
				p.Dispose();
			}
		}

		private DataReceivedEventHandler WriteToBuffer( StringBuilder sb ) {
			return ( obj, @event ) => {
				if( !String.IsNullOrEmpty( @event.Data ) ) {
					lock(sb) {
						sb.AppendLine( @event.Data );
					}
				}
			};
		}

		private void KillChildProcesses(
				int parentProcessId,
				DateTime startTime
			) {

			if( parentProcessId <= 0 ) {
				return;
			}

			ChildProcess[] childProcesses = GetChildProcesses( parentProcessId )
				.Where( child => child.StartTime >= startTime )
				.ToArray();

			foreach( ChildProcess childProcess in childProcesses ) {

				KillChildProcesses(
						childProcess.ProcessId,
						childProcess.StartTime
					);

				try {
					using( Process proc = Process.GetProcessById( childProcess.ProcessId ) ) {
						proc.Kill();
					}
				} catch {
				}
			}
		}

		private const string QueryTempalte = @"
SELECT
	ProcessId,
	CreationDate

FROM Win32_Process
WHERE (
	ParentProcessId = {0}
)";

		private IEnumerable<ChildProcess> GetChildProcesses( int parentProcessId ) {

			string query = String.Format(
					CultureInfo.InvariantCulture,
					QueryTempalte,
					parentProcessId
				);

			using( ManagementObjectSearcher searcher = new ManagementObjectSearcher( query ) )
			using( ManagementObjectCollection moc = searcher.Get() ) {

				foreach( ManagementObject mo in moc ) {

					using( mo ) {

						int childProcessId = Convert.ToInt32( mo["ProcessId"] );

						string creationDate = mo["CreationDate"].ToString();
						DateTime childStartTime = ManagementDateTimeConverter.ToDateTime( creationDate );

						yield return new ChildProcess( childProcessId, childStartTime );
					}
				}
			}
		}
	}
}
