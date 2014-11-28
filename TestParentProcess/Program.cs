using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestParentProcess {

	internal static class Program {

		internal static int Main( string[] args ) {

			if( args.Length == 0 ) {

				Console.Error.WriteLine( "Process argument required" );
				return -1;
			}

			string process = args[0];
			string arguments = FormatArguments( args.Skip( 1 ) );

			using( Process p = new Process() ) {

				ProcessStartInfo psi = p.StartInfo;
				psi.FileName = process;
				psi.Arguments = arguments;

				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;
				psi.RedirectStandardOutput = true;
				psi.RedirectStandardError = true;
				psi.RedirectStandardInput = true;

				p.OutputDataReceived +=
					delegate( object sender, DataReceivedEventArgs @event ) {
						if( !String.IsNullOrEmpty( @event.Data ) ) {
							Console.Out.WriteLine( @event.Data );
						}
					};

				p.ErrorDataReceived +=
					delegate( object sender, DataReceivedEventArgs @event ) {
						if( !String.IsNullOrEmpty( @event.Data ) ) {
							Console.Error.WriteLine( @event.Data );
						}
					};

				p.Start();
				p.WaitForExit();

				return p.ExitCode;
			}
		}

		public static string FormatArguments( IEnumerable<string> args ) {

			StringBuilder str = new StringBuilder();

			foreach( string arg in args ) {

				if( str.Length > 0 ) {
					str.Append( " " );
				}

				str.Append( "\"" );
				str.Append( arg );
				str.Append( "\"" );
			}

			return str.ToString();
		}

	}
}
