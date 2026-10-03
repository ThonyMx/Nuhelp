using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.IO;

namespace Opulos.NüHelp {
public class SharedSettings : SettingsBase {

	public static SharedSettings Default = null;

	static SharedSettings() {
		String fn = DefaultFilePath;
		Default = Load(fn);
	}

	public static SharedSettings Load(String filename) {
		SharedSettings s = null;

		if (File.Exists(filename)) {
			try {
				// originally Settings was NuHelp.ini, but now
				// SharedSettings is NuHelp.ini.  So trying to
				// read an a file with the wrong class name
				// will throw an exception.
				s = FromXML<SharedSettings>(filename);
			} catch {}

			if (s != null && !File.Exists(s.HHCExePath))
				FindHHCExePath(s);
		}

		if (s == null) {
			s = new SharedSettings();

			// First time application is run, try to find the HHC.exe
			FindHHCExePath(s);
		}

		s.FilePath = filename;

		if (s.WindowW == 0)
			s.WindowW = 1000;
		if (s.WindowH == 0)
			s.WindowH = 600;
		if (s.RecentFiles == null)
			s.RecentFiles = new List<String>();
		if (s.MaxRecentFiles <= 0)
			s.MaxRecentFiles = 10;
		if (s.MaxRecentIniFiles <= 0)
			s.MaxRecentIniFiles = 10;
		if (s.FontSize < 6)
			s.FontSize = System.Drawing.SystemFonts.MenuFont.Size;

		return s;
	}

	public List<String> RecentFiles { get; set; }
	public int MaxRecentFiles { get; set; }

	public List<String> RecentIniFiles { get; set; }
	public int MaxRecentIniFiles { get; set; }

	public float FontSize { get; set; }
	public int WindowX { get; set; }
	public int WindowY { get; set; }
	public int WindowH { get; set; }
	public int WindowW { get; set; }
	public FormWindowState WindowState { get; set; }

	public String HHCExePath { get; set; }
	public String SourceFolder { get; set; } // folder where the user is adding docs from
	public String SourceFolderIni { get; set; }
	public String SourceFolderBatch { get; set; }
	public int NumThreads { get; set; }

	public SharedSettings() {
		WindowW = 1000;
		WindowH = 600;
		WindowState = FormWindowState.Normal;
		RecentFiles = new List<String>();
		RecentIniFiles = new List<String>();
		MaxRecentFiles = 10;
		MaxRecentIniFiles = 10;
		HHCExePath = @"C:\Program Files\HTML Help Workshop\hhc.exe";
		NumThreads = 3;
	}


	private static void FindHHCExePath(SharedSettings s) {
		String dir1 = ProgramFilesx86();
		String dir2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		String exe1 = Path.Combine(dir1, @"HTML Help Workshop\hhc.exe");
		String exe2 = Path.Combine(dir2, @"HTML Help Workshop\hhc.exe");
		if (File.Exists(exe1))
			s.HHCExePath = exe1;
		else if (File.Exists(exe2))
			s.HHCExePath = exe2;
	}

	static String ProgramFilesx86() {
		if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
			return Environment.GetEnvironmentVariable("ProgramFiles(x86)");

		return Environment.GetEnvironmentVariable("ProgramFiles");
	}
}

}
