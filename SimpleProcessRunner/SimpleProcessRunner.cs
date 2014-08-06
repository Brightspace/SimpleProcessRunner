using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleProcessRunner {

	public sealed class SimpleProcessRunner : ISimpleProcessRunner {

		private const string timeoutMsg = "Timed out waiting for process to exit";

		public ProcessResult Run(
				string workingDirectory,
				string process,
				string arguments,
				TimeSpan timeout
			) {

			int timeoutMilliseconds = Convert.ToInt32( timeout.TotalMilliseconds );

			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = process,
				Arguments = arguments,
				WorkingDirectory = workingDirectory,

				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false
			};

			int exitCode;
			string standardOutput;
			string standardError;

			Stopwatch watch = Stopwatch.StartNew();

			using( Process p = Process.Start( psi ) ) {

				try {
					Task<string> standardOutputTask = p.StandardOutput.ReadToEndAsync();
					Task<string> standardErrorTask = p.StandardError.ReadToEndAsync();

					if( !standardOutputTask.Wait( timeoutMilliseconds ) ) {
						throw new TimeoutException( timeoutMsg );
					}

					if( !standardErrorTask.Wait( timeoutMilliseconds ) ) {
						throw new TimeoutException( timeoutMsg );
					}

					standardOutput = standardOutputTask.Result;
					standardError = standardErrorTask.Result;

					// ----------------------------------------------------

					if( !p.WaitForExit( timeoutMilliseconds ) ) {
						throw new TimeoutException( timeoutMsg );
					}

					exitCode = p.ExitCode;

				} finally {

					if( !p.HasExited ) {
						p.Kill();
					}
				}
			}

			watch.Stop();

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
	}
}
