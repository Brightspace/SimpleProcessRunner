using System;

namespace SimpleProcessRunner {

	public partial class ProcessRunner {

		internal sealed class ChildProcess {

			public readonly int ProcessId;
			public readonly DateTime StartTime;

			public ChildProcess(
					int processId,
					DateTime startTime
				) {

				this.ProcessId = processId;
				this.StartTime = startTime;
			}
		}
	}
}
