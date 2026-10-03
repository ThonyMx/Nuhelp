using Opulos.NüHelp.CHM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Opulos.Core.Utils;
using System.Globalization;

namespace Opulos.NüHelp {

public class ProjectList {

	public FileInfo BatchFileSourceFile;
	public int ErrorBoxMillis = 5000;
	public int SuccessBoxMillis = 5000;
	public int NumThreads = 3;

	public List<OutputFile> OutputFiles = new List<OutputFile>();

	public class OutputFile {
		public bool ActiveFile = false;
		public FileInfo SettingsFile = null;
		public List<Entry> Entries = new List<Entry>();
		public String RawArg = "";

		public void Add(FileInfo contentFile, String topic, InsertPosition insertPosition) {
			Entry e = new Entry { ContentFile = contentFile, Topic = topic, InsertPosition = insertPosition };
			Entries.Add(e);
		}
	}

	public class Entry {
		public FileInfo ContentFile; // html/docx
		public String Topic;
		public InsertPosition InsertPosition;

		public override String ToString() {
			return ContentFile.Name;
		}
	}

	/*public void Add(FileInfo contentFile, String topic, InsertPosition insertPosition) {
		OutputFile active = null;
		foreach (var of in OutputFiles) {
			if (of.ActiveFile) {
				active = of;
				break;
			}
		}
		if (active == null) {
			active = new OutputFile { ActiveFile = true };
			OutputFiles.Add(active);
		}
		active.Add(contentFile, topic, insertPosition);
	}*/

	public static String ToCommandLine(ProjectList projectList) {
		StringBuilder sb = new StringBuilder();
		String exe = Application.ExecutablePath;
		if (exe.IndexOf(' ') >= 0)
			sb.Append("\"").Append(exe).Append("\"");
		else
			sb.Append(exe);

		String prevSettingsFile = Settings.Default.FilePath;

		foreach (OutputFile of in projectList.OutputFiles) {
			if (of.SettingsFile != null) {
				String ini = of.SettingsFile.FullName;
				if (String.Compare(prevSettingsFile, ini, true) != 0) {
					if (ini.IndexOf(' ') >= 0)
						sb.Append(" \"-Ini:").Append(ini).Append("\"");
					else
						sb.Append(" -Ini:" + ini);

					prevSettingsFile = ini;
				}
			}

			var list = of.Entries;
			String currentPath = GetCommonFolder(list);
			if (currentPath.Length > 0) {
				// trim off the last backslash because the command prompt interprets \" as a literal quote
				// which would need to be represented as \\"
				String cp = currentPath.TrimEnd('\\');
				sb.Append(" {0}-Path:{1}{0}".Format2(cp.IndexOf(' ') >= 0 ? "\"" : "", cp));
			}

			StringBuilder sb2 = new StringBuilder();
			for (int i = 0; i < list.Count; i++) {
				Entry e = list[i];
				if (i > 0)
					sb2.Append("|");

				//if (i > 0) {
				//	String newPath = GetCommonFolder(list[i].ContentFile, list[i-1].ContentFile);
				//	if (String.Compare(newPath, currentPath, true) != 0) {
				//		currentPath = newPath;
				//		if (currentPath.Length > 0)
				//			sb.Append(" {0}-Path:{1}{0}".Format2(currentPath.IndexOf(' ') >= 0 ? "\"" : "", currentPath));
				//	}
				//}

				String fn = e.ContentFile.FullName.Substring(currentPath.Length);

				if (!String.IsNullOrEmpty(e.Topic)) {
					String topic = e.Topic; //Node.Name;

					String asterisk = "*";
					if (e.InsertPosition == InsertPosition.Above)
						asterisk = "*+";
					else if (e.InsertPosition == InsertPosition.Below)
						asterisk = "*-";

					// if the topic name matches the filename then only an asterisk is required
					if (String.Compare(topic, Path.GetFileNameWithoutExtension(e.ContentFile.Name), true) == 0)
						fn += asterisk;
					else
						fn += asterisk + topic;
				}

				sb2.Append(fn);
			}
			String files = sb2.ToString();
			sb.Append(" {0}-File:{1}{0}".Format2(files.IndexOf(' ') >= 0 ? "\"" : "", files));

		}

		sb.Append(" -SuccessBox:").Append(projectList.SuccessBoxMillis);
		sb.Append(" -ErrorBox:").Append(projectList.ErrorBoxMillis);

		return sb.ToString();
	}

	private static String[] ToArray(DirectoryInfo d) {
		List<String> list = new List<String>();
		while (d != null) {
			list.Add(d.Name.Trim('\\'));
			d = d.Parent;
		}
		list.Reverse();
		return list.ToArray();
	}

	private static String GetCommonFolder(FileInfo f1, FileInfo f2) {
		String[] arr1 = ToArray(f1.Directory);
		String[] arr2 = ToArray(f2.Directory);

		int n = Math.Min(arr1.Length, arr2.Length);
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < n; i++) {
			if (String.Compare(arr1[i], arr2[i], true) != 0) {
				break;
			}
			sb.Append(arr1[i]).Append(Path.DirectorySeparatorChar);
		}
		return sb.ToString();
	}

	private static String GetCommonFolder(List<Entry> list) {
		if (list.Count <= 1)
			return "";

		String best = null;
		for (int i = 1; i < list.Count; i++) {
			String folder = GetCommonFolder(list[i].ContentFile, list[i-1].ContentFile);
			if (i == 1)
				best = folder;
			else if (folder.Length < best.Length)
				best = folder;

			if (best.Length == 0)
				break;
		}

		return best;
	}

	public static ProjectList Parse(FileInfo batFile) {
		var enc = Encoding.UTF8;
		String text = File.ReadAllText(batFile.FullName, enc);
		var projectList = Parse(text);
		projectList.BatchFileSourceFile = batFile;
		return projectList;
	}

	public static ProjectList Parse(String text) {
		int x = text.IndexOf(".exe", StringComparison.InvariantCultureIgnoreCase);
		if (x < 0)
			throw new ArgumentException("Cannot find nuhelp.exe in batch file.");

		x += 5; // first character after exe will be " or a space (so add 5)

		List<String> args = new List<String>();
		StringBuilder sb = new StringBuilder();
		bool inside = false;
		for (int i = x; i <= text.Length; i++) {
			char c = (i < text.Length ? text[i] : ' ');
			if (c == '\r' || c == '\n')
				break;
			if (c == '"') {
				if (inside) {
					String arg = sb.ToString().Trim();
					sb.Length = 0;
					if (arg.Length > 0)
						args.Add(arg);
				}

				inside = !inside;
			}
			else if (c == ' ') {
				if (inside) {
					sb.Append(' ');
				}
				else {
					String arg = sb.ToString().Trim();
					sb.Length = 0;
					if (arg.Length > 0)
						args.Add(arg);
				}
			}
			else
				sb.Append(c);
		}
		return Parse(args.ToArray());
	}

	// args[0] = "-File:File1|File2*Bananas|File3|File4*Oranges"
	public static ProjectList Parse(String[] args) {
		ProjectList projectList = new ProjectList();

		Settings current = Settings.Default;
		String path = "";

		StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
		String ini = null;
		ProjectList.OutputFile currentFile = new ProjectList.OutputFile();
		projectList.OutputFiles.Add(currentFile);

		foreach (String arg in args) {
			if (arg.StartsWith("-SuccessBox:", sc)) { // added on 2014-09-28
				int.TryParse(arg.Substring(arg.IndexOf(':') + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out projectList.SuccessBoxMillis);
			}
			else if (arg.StartsWith("-ErrorBox:", sc)) { // add
				int.TryParse(arg.Substring(arg.IndexOf(':') + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out projectList.ErrorBoxMillis);
			}
			else if (arg.StartsWith("-NumThreads:", sc)) {
				int.TryParse(arg.Substring(arg.IndexOf(':') + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out projectList.NumThreads);
			}
			else if (arg.StartsWith("-Path:", sc)) { // added on 2014-08-12
				path = arg.Substring(6).Trim();
			}
			else if (arg.StartsWith("-Ini:", sc)) {
				String n = arg.Substring(5).Trim();
				if (n.Length == 0)
					throw new ArgumentException("-Ini argument is blank.");

				if (!File.Exists(n)) {
					String t = Path.Combine(Settings.DefaultFolder, n);
					if (File.Exists(t))
						n = t;
					else {
						t = Path.Combine(path, n);
						if (File.Exists(t))
							n = t;
					}
				}
				ini = n;
			}
			else if (arg.StartsWith("-File:", StringComparison.InvariantCultureIgnoreCase)) {
				if (currentFile.Entries.Count > 0) {
					currentFile = new ProjectList.OutputFile();
					projectList.OutputFiles.Add(currentFile);
				}

				if (!String.IsNullOrEmpty(ini))
					currentFile.SettingsFile = new FileInfo(ini);

				String n = arg.Substring(6).Trim();
				if (n.Length == 0)
					throw new ArgumentException("-File argument is blank.");

				currentFile.RawArg = arg;
				String[] arr = n.Split(new [] { '|' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (String file in arr) {
					ProjectList.Entry e = new ProjectList.Entry();
					currentFile.Entries.Add(e);

					int c = file.LastIndexOf('*');
					String f = file;
					if (c >= 0) {
						InsertPosition insertPosition = InsertPosition.Nested;

						f = file.Substring(0, c); // fixed on 2014-08-12
						String invarName = file.Substring(c+1).Trim();

						if (invarName.StartsWith("+")) {
							invarName = invarName.Substring(1);
							insertPosition = InsertPosition.Above;
						}
						else if (invarName.StartsWith("-")) {
							invarName = invarName.Substring(1);
							insertPosition = InsertPosition.Below;
						}

						// if no Topic is provided, e.g. -File:Doc1.docx|Doc2.docx*
						// then the name of the file without extension, "Doc2" will
						// be used as the topic node to find.
						if (invarName.Length == 0)
							invarName = Path.GetFileNameWithoutExtension(f);

						// spaces are replaced with dashes in the filenames
						invarName = invarName.Replace(" ", "-");
						e.Topic = invarName;
						e.InsertPosition = insertPosition;
					}

					// only combine if the filename isn't an absolute path
					if (f.IndexOf(':') < 0 && !f.StartsWith("\\\\"))
						f = Path.Combine(path, f);

					e.ContentFile = new FileInfo(f);
				}
			}
			else {
				throw new ArgumentException("Unknown argument: " + arg);
			}
		}

		return projectList;
	}
}
}