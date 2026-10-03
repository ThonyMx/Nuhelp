using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Opulos.NüHelp {
public static class FileUtil {

	public static bool IsFileLocked(FileInfo file) {
		try {
			using (file.Open(FileMode.Open, FileAccess.Read, FileShare.None)) {}
		} catch (IOException) {
			return true;
		}
		return false;
	}


	[StructLayout(LayoutKind.Sequential)]
	struct RM_UNIQUE_PROCESS {
		public int dwProcessId;
		public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
	}

	const int RmRebootReasonNone = 0;
	const int CCH_RM_MAX_APP_NAME = 255;
	const int CCH_RM_MAX_SVC_NAME = 63;

	enum RM_APP_TYPE {
		RmUnknownApp = 0,
		RmMainWindow = 1,
		RmOtherWindow = 2,
		RmService = 3,
		RmExplorer = 4,
		RmConsole = 5,
		RmCritical = 1000
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	struct RM_PROCESS_INFO {
		public RM_UNIQUE_PROCESS Process;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
		public String strAppName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
		public String strServiceShortName;

		public RM_APP_TYPE ApplicationType;
		public uint AppStatus;
		public uint TSSessionId;
		[MarshalAs(UnmanagedType.Bool)]
		public bool bRestartable;
	}

	[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
	static extern int RmRegisterResources(uint pSessionHandle,
											UInt32 nFiles,
											string[] rgsFilenames,
											UInt32 nApplications,
											[In] RM_UNIQUE_PROCESS[] rgApplications,
											UInt32 nServices,
											string[] rgsServiceNames);

	[DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
	static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

	[DllImport("rstrtmgr.dll")]
	static extern int RmEndSession(uint pSessionHandle);

	[DllImport("rstrtmgr.dll")]
	static extern int RmGetList(uint dwSessionHandle,
								out uint pnProcInfoNeeded,
								ref uint pnProcInfo,
								[In, Out] RM_PROCESS_INFO[] rgAffectedApps,
								ref uint lpdwRebootReasons);

	/// <summary>
	/// Find out what process(es) have a lock on the specified file.
	/// </summary>
	/// <param name="path">Path of the file.</param>
	/// <returns>Processes locking the file</returns>
	/// <remarks>See also:
	/// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
	/// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs
	/// 
	/// </remarks>
	public static List<Process> GetFileLockedByProcesses(String path) {
		uint handle;
		String key = Guid.NewGuid().ToString();
		List<Process> processes = new List<Process>();

		int res = RmStartSession(out handle, 0, key);
		if (res != 0)
			throw new Exception("Could not begin restart session.  Unable to determine file locker.");

		try {
			const int ERROR_MORE_DATA = 234;
			uint pnProcInfoNeeded = 0,
					pnProcInfo = 0,
					lpdwRebootReasons = RmRebootReasonNone;

			String[] resources = new String[] { path }; // Just checking on one resource.

			res = RmRegisterResources(handle, (uint) resources.Length, resources, 0, null, 0, null);

			if (res != 0)
				throw new Exception("Could not register resource.");

			//Note: there's a race condition here -- the first call to RmGetList() returns
			//      the total number of process. However, when we call RmGetList() again to get
			//      the actual processes this number may have increased.
			res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

			if (res == ERROR_MORE_DATA) {
				// Create an array to store the process results
				RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
				pnProcInfo = pnProcInfoNeeded;

				// Get the list
				res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
				if (res == 0) {
					processes = new List<Process>((int) pnProcInfo);

					// Enumerate all of the results and add them to the 
					// list to be returned
					for (int i = 0; i < pnProcInfo; i++) {
						try {
							processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
						} catch (ArgumentException) {} // catch the error -- in case the process is no longer running
					}
				}
				else
					throw new Exception("Could not list processes locking resource.");

			}
			else if (res != 0)
				throw new Exception("Could not list processes locking resource. Failed to get size of result.");

		} finally {
			RmEndSession(handle);
		}

		return processes;
	}

	//--------------------------------------------
	// e.g. String exe = FileUtil.GetDefaultOpenWith("txt", "open"); returns C:\Windows\System32\Notepad.exe
	public static String GetDefaultOpenWith(String ext, String verb = "open") {
		if (!ext.StartsWith("."))
			ext = "." + ext;

		String exe = FileExtentionInfo(AssocStr.Executable, ext, verb); // Will only work for 'open' verb
		if (String.IsNullOrEmpty(exe))
			exe = FileExtentionInfo(AssocStr.Command, ext, verb); // required to find command of any other verb than 'open'

		if (exe == null)
			exe = "";

		// Extract only the path
		if (exe.StartsWith("\""))
			exe = exe.Split('\"')[1];
		else if (exe.StartsWith("'"))
			exe = exe.Split('\'')[1];

		String fn = Path.GetFileName(exe);
		if (String.Compare(fn, "openwith.exe", true) == 0)
			return null; // 'OpenWith.exe' is the windows 8 or higher default for unknown extensions.

		return (exe.Length == 0 ? null : exe);
	}

	[DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern uint AssocQueryString(AssocF flags, AssocStr str, String pszAssoc, String pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

	private static String FileExtentionInfo(AssocStr assocStr, String doctype, String verb) {
		uint pcchOut = 0;
		AssocQueryString(AssocF.Verify, assocStr, doctype, verb, null, ref pcchOut);

		if (pcchOut == 0) {
			return "";
		}

		StringBuilder pszOut = new StringBuilder((int) pcchOut);
		AssocQueryString(AssocF.Verify, assocStr, doctype, verb, pszOut, ref pcchOut);
		return pszOut.ToString();
	}

	[Flags]
	public enum AssocF {
		Init_NoRemapCLSID = 0x1,
		Init_ByExeName = 0x2,
		Open_ByExeName = 0x2,
		Init_DefaultToStar = 0x4,
		Init_DefaultToFolder = 0x8,
		NoUserSettings = 0x10,
		NoTruncate = 0x20,
		Verify = 0x40,
		RemapRunDll = 0x80,
		NoFixUps = 0x100,
		IgnoreBaseClass = 0x200
	}

	public enum AssocStr {
		Command = 1,
		Executable,
		FriendlyDocName,
		FriendlyAppName,
		NoOpen,
		ShellNewValue,
		DDECommand,
		DDEIfExec,
		DDEApplication,
		DDETopic
	}
}
}
