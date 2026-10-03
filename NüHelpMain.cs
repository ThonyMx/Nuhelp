using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Opulos.NüHelp.CHM;
using Opulos.Core.Utils;
using Opulos.Core.UI;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using Opulos.Core.Interop.Word;
using System.Threading;

namespace Opulos.NüHelp {

// possible future enhancements
// 1) save to PDF
// 2) save all split html files into a nhp (nuhelp project file)
//    somewhat defeats the purpose of maintaining the help files as word docs
// 3) open office support
// 4) project tree manipulation (either click and drag and ctrl+arrow)
// 5) new format that tracks deleted nodes, tracks the entire tree layout
//    would allow topics to be rearranged within a document. First step
//    is to load all doc/html sources. Then apply the structure layer.

static class NüHelpMain {

	[STAThread]
	static void Main(String[] args) {

		//var pl = Parse("nuhelp.exe -Path:C:\\temp\\ -Ini:Blah.ini \"-File:Test1a.docx|Test1b.docx*Header 3A\" -SuccessBox:500");
		//return;

		//ProjectList list = new ProjectList();
		//list.Entries.Add(new ProjectList.Entry { ContentFile = new FileInfo(@"C:\a a\b\c\1.doc"), Node = new CHMNode { Name = "Top ic1" } });
		//list.Entries.Add(new ProjectList.Entry { ContentFile = new FileInfo(@"C:\a a\b\e\2.doc") });
		//list.Entries.Add(new ProjectList.Entry { ContentFile = new FileInfo(@"C:\a a\g\e\3.doc") });

		//String cmd = ProjectList.ToCommandLine(list, null);
		//return;

		//args = new String[] { @"-Path:C:\temp\", "-File:Test1a.docx|Test1b.docx*Header 3A", "-SuccessBox:500" };
		//args = new String[] { @"-path:C:\temp", "-file:Test1a.docx|Header 3A.docx*" };
		//args = new String[] { @""-File:C:\temp\Doc1.docx|C:\temp\Header1.docx""};
		if (args.Length > 0) {
			int exitCode = 0;
			String errorMessage = "";
			int successBoxMillis = 5000;
			int errorBoxMillis = 5000;

			try {
				ProjectList projectList = ProjectList.Parse(args);
				successBoxMillis = projectList.SuccessBoxMillis;
				errorBoxMillis = projectList.ErrorBoxMillis;
				Execute(projectList, Settings.Default);
			} catch (Exception ex) {
				errorMessage = ex.Message;
				Console.WriteLine(ex.Message);
				exitCode = 1;
			}

			if (exitCode == 0) {
				if (successBoxMillis >= 0)
					MessageBox2.Show("Success.", Application.ProductName, successBoxMillis);
			}
			else {
				if (errorBoxMillis >= 0)
					MessageBox2.Show("Error: " + errorMessage, Application.ProductName, errorBoxMillis);
			}

			Environment.Exit(exitCode);
			return;
		}

		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new NüHelpForm());
	}

	private class GlobalProgress {
		private Object dummy = new Object();
		private Dictionary<ProjectList.Entry, int> indexDict = new Dictionary<ProjectList.Entry,int>();
		public IThreadController tcon;
		CHMNodeTopicMatcher matcher = new CHMNodeTopicMatcher();
		CHMDocument doc;
		public Settings settings;
		List<String> tempFiles;
		ManualResetEvent mre = null;
		int numDone = 0;
		int count = 0;
		Q[] queue;
		public List<ProcessList> processList = new List<ProcessList>();

		public GlobalProgress(IThreadController tcon, List<ProjectList.Entry> list, CHMDocument doc, Settings settings, List<String> tempFiles, ManualResetEvent mre) {
			this.tcon = tcon;
			this.doc = doc;
			this.settings = settings;
			this.tempFiles = tempFiles;
			this.mre = mre;
			queue = new Q[list.Count];
			for (int i = 0; i < list.Count; i++) {
				var e = list[i];
				indexDict[e] = i;
			}
		}

		public String ErrorMessage { get; set; }

		private class Q {
			public ProjectList.Entry e;
			public FileInfo htmlFile;
			public Encoding readEncoding;
			public int index;
		}

		public void Process(ProjectList.Entry e2, FileInfo htmlFile, Encoding readEncoding) {
			lock(dummy) {
				count++;
				if (tcon != null)
					tcon.SetProgress("Done loading file {0} of {1} name {2}.".Format2(count, queue.Length, e2.ContentFile.Name), count, queue.Length);

				var f = e2.ContentFile;
				if (IsDoc(f))
					tempFiles.Add(htmlFile.FullName);

				int index = indexDict[e2];
				queue[index] = new Q { e = e2, htmlFile = htmlFile, index = index, readEncoding = readEncoding };

				for (int i = numDone; i < queue.Length; i++) {
					Q q = queue[i];
					if (q == null)
						break;

					var e = q.e;

					CHMNode appendNode = null;
					if (!String.IsNullOrEmpty(e.Topic)) {
						// could be invariant name or could be header text if [CHM InvariantName="..."] tag is not used.
						String invarName = e.Topic;
						appendNode = matcher.FindNode(invarName);

						if (appendNode == null) {
							this.ErrorMessage = "The document cannot be appended because the node does not exist: " + invarName;
							if (tcon != null)
								tcon.RequestStop();
							break;
						}
					}

					CHMAppendResult result = doc.AppendHtmlFile(q.htmlFile, settings, appendNode, q.readEncoding, tempFiles, null, e.ContentFile, e.InsertPosition);
					matcher.Append(result.AllNodes);
					numDone++;
				}

				if (tcon != null && tcon.IsStopRequested || numDone == queue.Length) {
					mre.Set();
				}
			}
		}

		public void CheckAllThreadsDone() {
			lock(dummy) {
				// this is to protect against hanging if one of the threads has an error
				bool allThreadsDone = true;
				foreach (var pl in processList) {
					if (!pl.IsDone && pl.list.Count > 0) {
						allThreadsDone = false;
						break;
					}
				}

				if (allThreadsDone)
					mre.Set();
			}
		}
	}

	private class ProcessList : IDisposable {
		public GlobalProgress GlobalProgress = null;
		public List<ProjectList.Entry> list = new List<ProjectList.Entry>();
		private Word _word = null;
		private Thread t = null;
		public bool IsDone = false;

		public ProcessList(GlobalProgress gp) {
			GlobalProgress = gp;
		}

		public Word WordApp {
			get {
				if (_word == null)
					_word = new Word();
				return _word;
			}
		}

		public void Dispose() {
			if (_word != null)
				_word.Dispose();
			_word = null;
		}

		public Exception Error { get; set; }

		public void StartThread() {
			t = new Thread((o) => {
				var p = (ProcessList) o;
				try {
					var tcon = p.GlobalProgress.tcon;
					var settings = p.GlobalProgress.settings;
					foreach (var e in list) {
						if (tcon != null) {
							tcon.CheckSuspended();
							if (tcon.IsStopRequested)
								break;
						}

						Encoding readEncoding = null;
						FileInfo f = e.ContentFile;
						FileInfo htmlFile = null;
						if (IsDoc(f)) {
							htmlFile = NüHelpForm.GetNonExistingHtmlFile(f, settings.OutputFolder);
							//htmlFile = new FileInfo(Path.Combine(, Path.ChangeExtension(docFile.Name, "html")));
							readEncoding = CHMCompile.SaveWordToHtml2(f, htmlFile, settings.WordVisible, WordApp);
						}
						else {
							htmlFile = f;
						}
						p.GlobalProgress.Process(e, htmlFile, readEncoding);
					}
				} catch (Exception ex) {
					this.Error = ex;
				}
				Dispose();
				this.IsDone = true;
				p.GlobalProgress.CheckAllThreadsDone();
			});
			t.IsBackground = true;
			t.Start(this);
		}
	}

	private static bool IsDoc(FileInfo f) {
		String ext = f.Extension.TrimStart('.');
		return String.Compare(ext, "doc", true) == 0 || String.Compare(ext, "docx", true) == 0;
	}

	public static void Append(int numThreads, ProjectList.OutputFile of, CHMDocument doc, Settings settings, List<String> tempFiles, ThreadController tcon) {
		if (numThreads < 0)
			numThreads = 1;

		if (!Directory.Exists(settings.OutputFolder))
			Directory.CreateDirectory(settings.OutputFolder);

		ManualResetEvent mre = new ManualResetEvent(false);
		// in testing, threads vs time:
		// 1 = 30 seconds
		// 2 = 25 seconds
		// 3 = 20 seconds
		// 4 = 23 seconds

		GlobalProgress gp = new GlobalProgress(tcon, of.Entries, doc, settings, tempFiles, mre);
		List<ProcessList> list = new List<ProcessList>();
		for (int i = 0; i < numThreads; i++) {
			var pl = new ProcessList(gp);
			list.Add(pl);
			gp.processList.Add(pl);
		}

		int docCount = 0;
		int htmlCount = 0;
		for (int i = 0; i < of.Entries.Count; i++) {
			ProjectList.Entry e = of.Entries[i];
			FileInfo f = e.ContentFile;
			if (IsDoc(f)) {
				list[docCount % numThreads].list.Add(e);
				docCount++;
			}
			else {
				list[htmlCount % numThreads].list.Add(e);
				htmlCount++;
			}
		}

		foreach (ProcessList pl in list) {
			// empty lists occur when the maximum number of documents of
			// a specific type is less than the number of threads.
			if (pl.list.Count > 0)
				pl.StartThread();
		}

		mre.WaitOne();

		if (gp.ErrorMessage != null)
			throw new ArgumentException(gp.ErrorMessage);

		foreach (var pl in list) {
			if (pl.Error != null)
				throw pl.Error;
		}
	}

	public static void Execute(ProjectList projectList, Settings current) {
		foreach (ProjectList.OutputFile of in projectList.OutputFiles) {
			if (of.SettingsFile != null) {
				String n = of.SettingsFile.FullName;
				if (!File.Exists(n))
					throw new ArgumentException("Cannot find Ini file: " + n);

				current = Settings.FromXML<Settings>(n);
			}

			List<String> tempFiles = new List<String>();
			CHMDocument doc = new CHMDocument();

			try {
				Append(projectList.NumThreads, of, doc, current, tempFiles, null);

				// if arr.Length == 1 and LinkBlankHeaders is true, then you don't need to update the
				// [next][prev] links because by default they will be correct
				bool LinkBlankHeaders = (current.BlankHeaders == BlankHeader.ShowInTOC || current.BlankHeaders == BlankHeader.HideInTOCLinked);
				if (of.Entries.Count > 1 || !LinkBlankHeaders)
					doc.UpdateHeadersAndFooters(LinkBlankHeaders, null);

				String msg = "";
				bool fileExists = CHMCompile.Compile(doc, current, SharedSettings.Default.HHCExePath, out msg);
				Console.WriteLine(msg);
				if (!fileExists)
					throw new ArgumentException("Could not create CHM file for: " + of.RawArg + Environment.NewLine + msg);

			} finally {
				if (current.DeleteTempFiles) {
					doc.DeleteAllFiles();
					foreach (String file in tempFiles)
						File.Delete(file);

					NüHelpForm.DeleteEmptyDirectories(current.OutputFolder);
				}
			}

			// including URLs like http://www.google.ca in the Jump1/Jump2/Home buttons will cause warnings
			// just ignore them
			//else if (msg.IndexOf("Error:", StringComparison.InvariantCultureIgnoreCase) == 0)
			//	throw new Exception("CHM file has errors: " + arg + Environment.NewLine + msg);
		}
	}
}

}


	/*private static void Execute_old(String[] args) {
		int exitCode = 0;
		String errorMessage = "";
		int displayMessageSuccessMillis = 5000;
		int displayMessageErrorMillis = 10000;

		try {
			Settings current = Settings.Default;
			String path = "";

			StringComparison sc = StringComparison.InvariantCultureIgnoreCase;

			foreach (String arg in args) {
				if (arg.StartsWith("-SuccessBox:", sc)) { // added on 2014-09-28
					int.TryParse(arg.Substring(arg.IndexOf(':') + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out displayMessageSuccessMillis);
				}
				else if (arg.StartsWith("-ErrorBox:", sc)) { // add
					int.TryParse(arg.Substring(arg.IndexOf(':') + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out displayMessageErrorMillis);
				}
				else if (arg.StartsWith("-Path:", sc)) { // added on 2014-08-12
					path = arg.Substring(6).Trim();
				}
				else if (arg.StartsWith("-Ini:", sc)) {
					String n = arg.Substring(5).Trim();
					if (n.Length == 0)
						throw new Exception("-Ini argument is blank.");

					if (!File.Exists(n)) {
						String t = Path.Combine(Settings.DefaultFolder, n);
						if (!File.Exists(t))
							throw new Exception("Cannot find Ini file: " + n);

						current = Settings.FromXML<Settings>(t);
					}
					else
						current = Settings.FromXML<Settings>(n);
				}
				else if (arg.StartsWith("-File:", StringComparison.InvariantCultureIgnoreCase)) {
					String n = arg.Substring(6).Trim();
					if (n.Length == 0)
						throw new Exception("-File argument is blank.");

					String[] arr = n.Split(new [] { '|' }, StringSplitOptions.RemoveEmptyEntries);
					List<String> tempFiles = new List<String>();

					CHMDocument doc = new CHMDocument();
					String errMessage = null;

					foreach (String file in arr) {
						int c = file.LastIndexOf('*');
						CHMNode appendNode = null;
						String f = file;
						if (c >= 0) {
							f = file.Substring(0, c); // fixed on 2014-08-12
							String invarName = file.Substring(c+1).Trim();

							// if no Topic is provided, e.g. -File:Doc1.docx|Doc2.docx*
							// then the name of the file without extension, "Doc2" will
							// be used as the topic node to find.
							if (invarName.Length == 0)
								invarName = Path.GetFileNameWithoutExtension(f);

							// spaces are replaced with dashes in the filenames
							invarName = invarName.Replace(" ", "-");

							foreach (CHMNode node in doc.GetAllNodes()) {
								if (String.IsNullOrEmpty(node.Local))
									continue;

								String local = Path.GetFileNameWithoutExtension(node.Local);
								if (String.Compare(local, invarName, true) == 0) { // fixed on 2014-08-12
									appendNode = node;
									break;
								}
							}
							if (appendNode == null) {
								errMessage = "The document cannot be appended because the node does not exist: " + invarName;
								break;
							}
						}

						if (!Directory.Exists(current.OutputFolder))
							Directory.CreateDirectory(current.OutputFolder);

						FileInfo htmlFile = null;
						String ext = Path.GetExtension(arr[0]).TrimStart('.');
						Encoding readEncoding = null;

						f = Path.Combine(path, f);

						if (String.Compare(ext, "doc", true) == 0 || String.Compare(ext, "docx", true) == 0) {
							FileInfo docFile = new FileInfo(f);

							htmlFile = NüHelpForm.GetNonExistingHtmlFile(docFile, current.OutputFolder);
							//htmlFile = new FileInfo(Path.Combine(, Path.ChangeExtension(docFile.Name, "html")));
							readEncoding = CHMCompile.SaveWordToHtml2(docFile, htmlFile, current.WordVisible);
							tempFiles.Add(htmlFile.FullName);
						}
						else {
							htmlFile = new FileInfo(f);
						}

						doc.AppendHtmlFile(htmlFile, current, appendNode, readEncoding, tempFiles, null);
					}

					// if arr.Length == 1 and LinkBlankHeaders is true, then you don't need to update the
					// [next][prev] links because by default they will be correct
					bool LinkBlankHeaders = (current.BlankHeaders == BlankHeader.ShowInTOC || current.BlankHeaders == BlankHeader.HideInTOCLinked);
					if (arr.Length > 1 || !LinkBlankHeaders)
						doc.UpdateHeadersAndFooters(LinkBlankHeaders, null);

					String msg = "";
					bool fileExists = false;
					if (errMessage == null)
						fileExists = CHMCompile.Compile(doc, current, SharedSettings.Default.HHCExePath, out msg);

					Console.WriteLine(msg);
					if (current.DeleteTempFiles) {
						doc.DeleteAllFiles();
						foreach (String file in tempFiles)
							File.Delete(file);
					}

					if (errMessage != null)
						throw new Exception(errMessage);

					if (!fileExists)
						throw new Exception("Could not create CHM file for: " + arg + Environment.NewLine + msg);

					// including URLs like http://www.google.ca in the Jump1/Jump2/Home buttons will cause warnings
					// just ignore them
					//else if (msg.IndexOf("Error:", StringComparison.InvariantCultureIgnoreCase) == 0)
					//	throw new Exception("CHM file has errors: " + arg + Environment.NewLine + msg);
				}
				else {
					Console.WriteLine("Unknown argument:" + arg);
					exitCode = 1;
					break;
				}
			}
		} catch (Exception ex) {
			errorMessage = ex.Message;
			Console.WriteLine(ex.Message);
			exitCode = 1;
		}

		if (exitCode == 0) {
			if (displayMessageSuccessMillis >= 0)
				MessageBox2.Show("Success.", Application.ProductName, displayMessageSuccessMillis);
		}
		else {
			if (displayMessageErrorMillis >= 0)
				MessageBox2.Show("Error: " + errorMessage, Application.ProductName, displayMessageErrorMillis);
		}

		Environment.Exit(exitCode);
	}*/

	/*public static void Append_old(ProjectList.OutputFile of, CHMDocument doc, Settings settings, List<String> tempFiles, ThreadController tcon) {

		if (!Directory.Exists(settings.OutputFolder))
			Directory.CreateDirectory(settings.OutputFolder);

		CHMNodeTopicMatcher matcher = new CHMNodeTopicMatcher();
		Word wordApp = new Word();

		try {
			//foreach (ProjectList.Entry e in of.Entries) {
			int n = of.Entries.Count;
			for (int i = 0; i < n; i++) {
				ProjectList.Entry e = of.Entries[i];
				if (tcon != null) {
					tcon.setProgress("Loading file {0} of {1} name {2}.".Format2(i+1, n, e.ContentFile.Name), i+1, n);
					if (tcon.isStopRequested)
						break;
				}

				CHMNode appendNode = null;
				if (!String.IsNullOrEmpty(e.Topic)) {
					// could be invariant name or could be header text if [CHM InvariantName="..."] tag is not used.
					String invarName = e.Topic;
					appendNode = matcher.FindNode(invarName);

					if (appendNode == null)
						throw new ArgumentException("The document cannot be appended because the node does not exist: " + invarName);
				}

				FileInfo htmlFile = null;
				FileInfo f = e.ContentFile;
				String ext = f.Extension.TrimStart('.');
				Encoding readEncoding = null;

				if (String.Compare(ext, "doc", true) == 0 || String.Compare(ext, "docx", true) == 0) {
					htmlFile = NüHelpForm.GetNonExistingHtmlFile(f, settings.OutputFolder);
					//htmlFile = new FileInfo(Path.Combine(, Path.ChangeExtension(docFile.Name, "html")));
					readEncoding = CHMCompile.SaveWordToHtml2(f, htmlFile, settings.WordVisible, wordApp);
					tempFiles.Add(htmlFile.FullName);
				}
				else {
					htmlFile = f;
				}

				CHMAppendResult result = doc.AppendHtmlFile(htmlFile, settings, appendNode, readEncoding, tempFiles, null, f, e.InsertPosition);
				matcher.Append(result.AllNodes);
			}
		} finally {
			wordApp.Dispose();
		}
	}*/