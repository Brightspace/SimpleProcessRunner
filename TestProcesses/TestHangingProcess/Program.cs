using System;
using System.Threading;

namespace TestHangingProcess {

	internal static class Program {

		internal static int Main( string[] args ) {

			if( args.Length == 0 ) {

				Console.Error.WriteLine( "Sleep time required" );
				return -1;
			}

			int sleep = Int32.Parse( args[0] );
			Thread.Sleep( sleep );
			return 0;
		}
	}
}
