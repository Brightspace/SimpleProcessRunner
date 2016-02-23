using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SimpleProcessRunner {

	internal class ProcessLogger {

		private readonly Process m_process;

		private readonly StringBuilder m_stdError = new StringBuilder();
		private readonly StringBuilder m_stdOutput = new StringBuilder();

		public ProcessLogger( Process p ) {
			if( p == null ) {
				throw new ArgumentNullException( "p", "Process cannot be null" );
			}
			m_process = p;
			RegisterProcess( p );
		}

		private void RegisterProcess( Process p ) {
			p.OutputDataReceived += WriteToBuffer( m_stdOutput );
			p.ErrorDataReceived += WriteToBuffer( m_stdError );
		}

		private DataReceivedEventHandler WriteToBuffer( StringBuilder sb ) {
			return ( obj, @event ) => {
				if( !String.IsNullOrEmpty( @event.Data ) ) {
					lock( sb ) {
						sb.AppendLine( @event.Data );
					}
				}
			};
		}

		public ProcessResult GetProcessResult() {
			return GetProcessResult( m_process.ExitTime - m_process.StartTime );
		}

		public ProcessResult GetProcessResult( TimeSpan duration ) {
			Log log = GetLogs();

			return new ProcessResult(
				workingDirectory: m_process.StartInfo.WorkingDirectory,
				process: m_process.StartInfo.FileName,
				arguments: m_process.StartInfo.Arguments,
				exitCode: m_process.ExitCode,
				standardOutput: log.StandardOutput,
				standardError: log.StandardError,
				duration: duration );
		}

		public ProcessTimeoutException GetTimeoutException() {
			string timeoutMsg = String.Format(
				CultureInfo.InvariantCulture,
				"Timed out waiting for process {0} ( {1} ) to exit",
				m_process.StartInfo.FileName,
				m_process.StartInfo.Arguments );

			Log log = GetLogs();

			return new ProcessTimeoutException(
				message: timeoutMsg,
				workingDirectory: m_process.StartInfo.WorkingDirectory,
				process: m_process.StartInfo.FileName,
				arguments: m_process.StartInfo.Arguments,
				standardOutput: log.StandardOutput,
				standardError: log.StandardError );

		}

		private Log GetLogs() {
			string standardOutputTxt;
			lock( m_stdOutput ) {
				standardOutputTxt = m_stdOutput.ToString();
			}

			string standardErrorTxt;
			lock( m_stdError ) {
				standardErrorTxt = m_stdError.ToString();
			}
			return new Log {
				StandardError = standardErrorTxt,
				StandardOutput = standardOutputTxt
			};
		}


		private struct Log {

			public string StandardOutput;
			public string StandardError;

		}

	}

}