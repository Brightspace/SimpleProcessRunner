using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SimpleProcessRunner {
	internal static class ProcessExtensions {
		private const string QueryTempalte = @"
SELECT
	ProcessId,
	CreationDate

FROM Win32_Process
WHERE (
	ParentProcessId = {0}
)";

		public static void KillChildProcesses(
			this Process p,
			DateTime startTime
			) {

			if (p.Id <= 0) {
				return;
			}

			KillChildProcesses( p.Id, startTime );
		}
		private static void KillChildProcesses(
			int parentProcessId,
			DateTime startTime
			) {

			if (parentProcessId <= 0) {
				return;
			}

			ProcessRunner.ChildProcess[] childProcesses = GetChildProcesses(parentProcessId)
				.Where(child => child.StartTime >= startTime)
				.ToArray();

			foreach (ProcessRunner.ChildProcess childProcess in childProcesses) {

				KillChildProcesses(
					childProcess.ProcessId,
					childProcess.StartTime
					);

				try {
					using (Process proc = Process.GetProcessById(childProcess.ProcessId))
						proc.Kill();
				} catch {
				}
			}
		}

		private static IEnumerable<ProcessRunner.ChildProcess> GetChildProcesses(int parentProcessId) {

			string query = String.Format(
				CultureInfo.InvariantCulture,
				QueryTempalte,
				parentProcessId
				);

			using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
			using (ManagementObjectCollection moc = searcher.Get()) {

				foreach (ManagementObject mo in moc) {

					using (mo) {

						int childProcessId = Convert.ToInt32(mo["ProcessId"]);

						string creationDate = mo["CreationDate"].ToString();
						DateTime childStartTime = ManagementDateTimeConverter.ToDateTime(creationDate);

						yield return new ProcessRunner.ChildProcess(childProcessId, childStartTime);
					}
				}
			}
		}

	}
}
