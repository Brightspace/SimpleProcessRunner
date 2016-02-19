using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleProcessRunner;

namespace SimpleProcessRunnerTests {

	[TestFixture]
	[Category( "Integration" )]
	public sealed class ProcessRunnerTests {

		private const int SixtySeconds = 60000;

		private IProcessRunner m_runner;

		[TestFixtureSetUp]
		public void TestFixtureSetUp() {
			m_runner = new SimpleProcessRunner.ProcessRunner();
		}
		#region synchronousTests
		[Test]
		public void StandardOutput() {

			ProcessResult result = m_runner.Run(
					@"C:\Windows\System32\cmd.exe",
					"/C echo hello world",
					TimeSpan.FromSeconds( 10 )
				);

			Assert.AreEqual( 0, result.ExitCode );
			Assert.AreEqual( "hello world", result.StandardOutput.Trim() );
			Assert.IsEmpty( result.StandardError );
		}

		[Test]
		public void StandardError() {

			ProcessResult result = m_runner.Run(
					@"C:\Windows\System32\cmd.exe",
					@"/C dir Boom:\ReallyNotFound",
					TimeSpan.FromSeconds( 10 )
				);

			Assert.AreEqual( 1, result.ExitCode );
			Assert.IsEmpty( result.StandardOutput );
			Assert.AreEqual( "The filename, directory name, or volume label syntax is incorrect.", result.StandardError.Trim() );
		}

		[Test]
		public void Timeout_WithChildProcess() {

			string parentProcess = GetTestProcess( "TestParentProcess.exe" );
			string hangingProcess = GetTestProcess( "TestHangingProcess.exe" );

			string args = ProcessArgumentsFormatter.Format(
					hangingProcess,
					SixtySeconds
				);

			Assert.Throws<ProcessTimeoutException>(
				() => {
					m_runner.Run(
						parentProcess,
						args,
						TimeSpan.FromSeconds( 2 )
					);
				}
			);
		}

		[Test]
		public void Timeout_WithNestedChildProcess() {

			string parentProcess = GetTestProcess( "TestParentProcess.exe" );
			string hangingProcess = GetTestProcess( "TestHangingProcess.exe" );

			string args = ProcessArgumentsFormatter.Format(
					parentProcess,
					hangingProcess,
					SixtySeconds
				);

			Assert.Throws<ProcessTimeoutException>(
				() => {
					m_runner.Run(
						parentProcess,
						args,
						TimeSpan.FromSeconds( 2 )
					);
				}
			);
		}

		[Test]
		public void Timeout_WithDeepNestedChildProcess() {

			string parentProcess = GetTestProcess( "TestParentProcess.exe" );
			string hangingProcess = GetTestProcess( "TestHangingProcess.exe" );

			string args = ProcessArgumentsFormatter.Format(
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					hangingProcess,
					SixtySeconds
				);

			Assert.Throws<ProcessTimeoutException>(
				() => {
					m_runner.Run(
						parentProcess,
						args,
						TimeSpan.FromSeconds( 2 )
					);
				}
			);
		}

		[Test]
		[Explicit( "Should only be run manually" )]
		public void ContinuousTimeoutTest() {

			for( int i = 0; i < 1000; i++ ) {

				Timeout_WithNestedChildProcess();
				Thread.Sleep( 100 );
			}
		}
#endregion
		#region AsyncTests
		[Test]
		public async Task AsyncStandardOutput() {

			ProcessResult result = await m_runner.RunAsync(
					@"C:\Windows\System32\cmd.exe",
					"/C echo hello world",
					TimeSpan.FromSeconds(10)
				);

			Assert.AreEqual(0, result.ExitCode);
			Assert.AreEqual("hello world", result.StandardOutput.Trim());
			Assert.IsEmpty(result.StandardError);
		}

		[Test]
		public async Task AsyncStandardError() {

			ProcessResult result = await m_runner.RunAsync(
					@"C:\Windows\System32\cmd.exe",
					@"/C dir Boom:\ReallyNotFound",
					TimeSpan.FromSeconds(10)
				);

			Assert.AreEqual(1, result.ExitCode);
			Assert.IsEmpty(result.StandardOutput);
			Assert.AreEqual("The filename, directory name, or volume label syntax is incorrect.", result.StandardError.Trim());
		}

		[Test]
		[ExpectedException(typeof(ProcessTimeoutException))]
		public async Task AsyncTimeout_WithChildProcess() {

			string parentProcess = GetTestProcess("TestParentProcess.exe");
			string hangingProcess = GetTestProcess("TestHangingProcess.exe");

			string args = ProcessArgumentsFormatter.Format(
					hangingProcess,
					SixtySeconds
				);

			ProcessResult result = await m_runner.RunAsync(
						parentProcess,
						args,
						TimeSpan.FromSeconds(2)
					);
		}

		[Test]
		[ExpectedException(typeof(ProcessTimeoutException))]
		public async Task AsyncTimeout_WithNestedChildProcess() {

			string parentProcess = GetTestProcess("TestParentProcess.exe");
			string hangingProcess = GetTestProcess("TestHangingProcess.exe");

			string args = ProcessArgumentsFormatter.Format(
					parentProcess,
					hangingProcess,
					SixtySeconds
				);

			ProcessResult result = await m_runner.RunAsync(
						parentProcess,
						args,
						TimeSpan.FromSeconds(2)
					);
		}

		[Test]
		[ExpectedException(typeof(ProcessTimeoutException))]
		public async Task AsyncTimeout_WithDeepNestedChildProcess() {

			string parentProcess = GetTestProcess("TestParentProcess.exe");
			string hangingProcess = GetTestProcess("TestHangingProcess.exe");

			string args = ProcessArgumentsFormatter.Format(
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					parentProcess,
					hangingProcess,
					SixtySeconds
				);

			ProcessResult result = await m_runner.RunAsync(
						parentProcess,
						args,
						TimeSpan.FromSeconds(2)
					);
		}
#endregion

		private string GetTestProcess( string relativePath ) {
			Assembly aseembly = this.GetType().Assembly;
			FileInfo assemblyFile = new FileInfo( aseembly.Location );

			string path = Path.Combine( assemblyFile.DirectoryName, relativePath );
			return path;
		}
	}
}
