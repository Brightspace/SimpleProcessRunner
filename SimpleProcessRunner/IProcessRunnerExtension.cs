using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleProcessRunner {

	public static class IProcessRunnerExtension {

		public static ProcessResult Run(
				this IProcessRunner runner,
				string process,
				string arguments,
				TimeSpan timeout
			) {

			return runner.Run(
					workingDirectory: Environment.CurrentDirectory,
					process: process,
					arguments: arguments,
					timeout: timeout
				);
		}

		public static ProcessResult Run(
				this IProcessRunner runner,
				FileInfo process,
				string arguments,
				TimeSpan timeout
			) {

			return runner.Run(
					workingDirectory: Environment.CurrentDirectory,
					process: process.FullName,
					arguments: arguments,
					timeout: timeout
				);
		}

		public static ProcessResult Run(
				this IProcessRunner runner,
				DirectoryInfo workingDirectory,
				FileInfo process,
				string arguments,
				TimeSpan timeout
			) {

			return runner.Run(
					workingDirectory: process.Directory.FullName,
					process: process.FullName,
					arguments: arguments,
					timeout: timeout
				);
		}

		public static Task<ProcessResult> RunAsync(
				this IProcessRunner runner,
				string process,
				string arguments,
				TimeSpan timeout
			) {

			return runner.RunAsync(
					workingDirectory: Environment.CurrentDirectory,
					process: process,
					arguments: arguments,
					timeout: timeout
				);
		}

		public static Task<ProcessResult> RunAsync(
				this IProcessRunner runner,
				FileInfo process,
				string arguments,
				TimeSpan timeout
			) {

			return runner.RunAsync(
					workingDirectory: Environment.CurrentDirectory,
					process: process.FullName,
					arguments: arguments,
					timeout: timeout
				);
		}

		public static Task<ProcessResult> RunAsync(
				this IProcessRunner runner,
				DirectoryInfo workingDirectory,
				FileInfo process,
				string arguments,
				TimeSpan timeout
			) {

			return runner.RunAsync(
					workingDirectory: process.Directory.FullName,
					process: process.FullName,
					arguments: arguments,
					timeout: timeout
				);
		}
	}
}
