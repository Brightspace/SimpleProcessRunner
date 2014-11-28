﻿using System;
using System.IO;
using System.Reflection;
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

		[Test]
		public void Timeout() {

			Assert.Throws<TimeoutException>(
				() => {
					m_runner.Run( "git.exe", "clone http://test.org", TimeSpan.FromSeconds( 1 ) );
				}
			);
		}

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

			Assert.Throws<TimeoutException>(
				() => {
					m_runner.Run(
						parentProcess,
						args,
						TimeSpan.FromSeconds( 1 )
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

			Assert.Throws<TimeoutException>(
				() => {
					m_runner.Run(
						parentProcess,
						args,
						TimeSpan.FromSeconds( 10 )
					);
				}
			);
		}

		private string GetTestProcess( string relativePath ) {

			Assembly aseembly = this.GetType().Assembly;
			FileInfo assemblyFile = new FileInfo( aseembly.Location );

			string path = Path.Combine( assemblyFile.DirectoryName, relativePath );
			return path;
		}
	}
}