using System;
using System.IO;

namespace SimpleProcessRunner {

	public interface ISimpleProcessRunner {

		/// <summary>
		/// Runs the process.
		/// </summary>
		/// <param name="workingDirectory">The working directory.</param>
		/// <param name="process">The process name or file path.</param>
		/// <param name="arguments">The process arguments.</param>
		/// <param name="timeout">The execution timeout.</param>
		/// <returns>Returns the process output and exit code.</returns>
		ProcessResult Run(
				string workingDirectory,
				string process,
				string arguments,
				TimeSpan timeout
			);
	}
}
