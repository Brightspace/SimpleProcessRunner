using System;
using System.Diagnostics;
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
			ProcessResult result;

			Stopwatch watch = new Stopwatch();

			using( Process p = new Process() ) {

				p.StartInfo = GetStartInfo( workingDirectory, process, arguments );

				ProcessLogger logger = new ProcessLogger( p );

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
						throw logger.GetTimeoutException();
					}

					result = logger.GetProcessResult( p.ExitCode, watch.Elapsed );

				} catch( TimeoutException ) {

					p.KillChildProcesses( startTime );
					throw;

				} finally {

					p.Refresh();

					if( !p.HasExited ) {
						p.Kill();
					}
				}

				watch.Stop();
			}

			return result;
		}

		public async Task<ProcessResult> RunAsync(
			string workingDirectory,
			string process,
			string arguments,
			TimeSpan timeout
		) {

			ProcessStartInfo psi = GetStartInfo( workingDirectory, process, arguments );

			Process p = new Process {
				StartInfo = psi,
				EnableRaisingEvents = true
			};

			ProcessLogger logger = new ProcessLogger( p );

			TaskCompletionSource<ProcessResult> tcs = new TaskCompletionSource<ProcessResult>();
			p.Exited += ( sender, eventArgs ) => {
				Process p2 = (Process) sender;
				try {
					ProcessResult result = logger.GetProcessResult( p2 );
					tcs.TrySetResult( result );
				} catch( Exception ex ) {
					tcs.TrySetException( ex );
				}
			};

			p.Start();
			p.BeginErrorReadLine();
			p.BeginOutputReadLine();

			if( timeout > TimeSpan.Zero ) {
				CancellationTokenSource cts = new CancellationTokenSource( timeout );

				cts.Token.Register(
					() => { tcs.TrySetCanceled(); },
					false );
			}

			try {
				return await tcs.Task;
			} catch( TaskCanceledException ) {
				p.KillChildProcesses( p.StartTime );
				throw logger.GetTimeoutException();
			} finally {
				p.Dispose();
			}
		}

		private static ProcessStartInfo GetStartInfo(
			string workingDir,
			string filename,
			string arguments
		) {

			ProcessStartInfo psi = new ProcessStartInfo( filename, arguments );
			psi.WorkingDirectory = workingDir;

			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardError = true;

			return psi;
		}

	}

}