using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleProcessRunner {
	internal class ProcessLogger {

		private struct Log {

			public string StandardOutput;
			public string StandardError;

		}

		private readonly StringBuilder m_stdError = new StringBuilder();
		private readonly StringBuilder m_stdOutput = new StringBuilder();
		private readonly string m_workingDirectory;
		private readonly string m_fileName;
		private readonly string m_arguments;

		public ProcessLogger( string workingDirectory, string filename, string arguments ) {
			m_workingDirectory = workingDirectory;
			m_fileName = filename;
			m_arguments = arguments;
		}

		public void RegisterProcess( Process p ) {
			p.OutputDataReceived += WriteToBuffer( m_stdOutput );
			p.ErrorDataReceived += WriteToBuffer( m_stdError );
		}

		private DataReceivedEventHandler WriteToBuffer(StringBuilder sb) {
			return (obj, @event) => {
				if (!String.IsNullOrEmpty(@event.Data)) {
					lock (sb) {
						sb.AppendLine(@event.Data);
					}
				}
			};
		}

		public ProcessResult GetProcessResult( Process p ) {
			return GetProcessResult( p.ExitCode, p.ExitTime - p.StartTime );
		}

		public ProcessResult GetProcessResult(int exitCode, TimeSpan duration) {
			Log log = GetLogs();

			return new ProcessResult(
				workingDirectory: m_workingDirectory,
				process: m_fileName,
				arguments: m_arguments,
				exitCode: exitCode,
				standardOutput: log.StandardOutput,
				standardError: log.StandardError,
				duration: duration);
		}

		public ProcessTimeoutException GetTimeoutException() {
			string timeoutMsg = String.Format(
				CultureInfo.InvariantCulture,
				"Timed out waiting for process {0} ( {1} ) to exit",
				m_fileName,
				m_arguments );

			Log log = GetLogs();

			return new ProcessTimeoutException(
					message: timeoutMsg,
					workingDirectory: m_workingDirectory,
					process: m_fileName,
					arguments: m_arguments,
					standardOutput: log.StandardOutput,
					standardError: log.StandardError );

		}

		private Log GetLogs() {
			string standardOutputTxt;
			lock (m_stdOutput) {
				standardOutputTxt = m_stdOutput.ToString();
			}

			string standardErrorTxt;
			lock (m_stdError) {
				standardErrorTxt = m_stdError.ToString();
			}
			return new Log {
				StandardError = standardErrorTxt,
				StandardOutput = standardOutputTxt
			};
		}

	


	}
}
