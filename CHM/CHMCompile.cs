using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using Opulos.Core.IO;
using Opulos.Core.Interop.Word;

namespace Opulos.NüHelp.CHM {
public class CHMCompile {

	// case 1: CHM file is blank - use the document title
	// case 2: CHM file is an absolute folder
	// case 3: CHM file is a relative folder
	// case 4: CHM file is an absolute file
	// case 5: CHM file is an relative file
	public static FileInfo GetCHMFile(Settings s, CHMDocument doc) {
		CHMOptions o = s.CHMOptions;

		String p = o.CompiledFile;
		// Path.IsPathRooted returns true for a single slash, but we want to
		// treat those as relative
		if (p.StartsWith(@"\") && !p.StartsWith(@"\\"))
			p = p.TrimStart('\\');
		else if (p.StartsWith("/") && !p.StartsWith("//"))
			p = p.TrimStart('/');

		if (!p.EndsWith(".chm", StringComparison.InvariantCultureIgnoreCase)) {
			String n = doc.Title;
			if (n.Length == 0)
				p = Path.Combine(p, "Help.chm");
			else
				p = Path.Combine(p, Path.GetFileNameWithoutExtension(n).Trim() + ".chm");
		}

		if (!Path.IsPathRooted(p))
			p = Path.Combine(s.OutputFolder, p);

		return new FileInfo(p);
	}

	private static String GetPath(String outputFolder, String name) {

		String p = name;
		// Path.IsPathRooted returns true for a single slash, but we want to
		// treat those as relative
		if (p.StartsWith(@"\") && !p.StartsWith(@"\\"))
			p = p.TrimStart('\\');
		else if (p.StartsWith("/") && !p.StartsWith("//"))
			p = p.TrimStart('/');

		if (!Path.IsPathRooted(p))
			p = Path.Combine(outputFolder, p);

		return p;
	}



	public static bool Compile(CHMDocument doc, Settings s, String hhcExePath, out String compilerMessages) {
		compilerMessages = "";

		if (!File.Exists(hhcExePath))
			throw new ArgumentException("Compiler exe file does not exist: " + hhcExePath);

		if (String.IsNullOrEmpty(s.OutputFolder))
			throw new ArgumentException("Output folder must be specified.");

		List<CHMNode> nodes = doc.GetAllNodes();
		//String defaultTopic = null;
		//if (String.IsNullOrEmpty(s.CHMOptions.DefaultTopic))
		//	 defaultTopic = nodes.Count > 0 ? nodes[0].Local : "";
		//else
		//	defaultTopic = Path.Combine(s.OutputFolder, s.CHMOptions.DefaultTopic);
		//FileInfo dt = new FileInfo(defaultTopic);
		//DirectoryInfo ofd = new DirectoryInfo(s.OutputFolder);
		//if (String.Compare(dt.Directory.FullName.TrimEnd(Path.DirectorySeparatorChar), ofd.FullName.TrimEnd(Path.DirectorySeparatorChar), true) != 0)
		//	throw new ArgumentException("The first page / default page must be in the same\nOutput Folder as the CHM file. The reason is because\nthe hhc.exe compiler makes all paths relative to the\noutput location of the CHM file.\n\nChange the Output Folder back to one used when\nthe first document was added.");

		s = s.Clone<Settings>(); // clone so we can change values without impacting the original object
		CHMOptions Options = s.CHMOptions;

		FileInfo cf = GetCHMFile(s, doc);
		if (!Directory.Exists(cf.Directory.FullName))
			Directory.CreateDirectory(cf.Directory.FullName);

		if (!Directory.Exists(s.OutputFolder))
			Directory.CreateDirectory(s.OutputFolder);

		if (File.Exists(cf.FullName)) {
			if (s.OverwriteExistingCHM)
				File.Delete(cf.FullName);
			else
				throw new ArgumentException("A chm file already exists in that folder: " + cf.FullName);
		}

		String outputFolder = s.OutputFolder;

		foreach (String extraFile in s.ExtraFiles) {
			FileInfo f = new FileInfo(extraFile);
			FileInfo f2 = new FileInfo(Path.Combine(outputFolder, Path.GetFileName(f.Name)));
			File.Copy(f.FullName, f2.FullName, true);
		}

		String chmName = Path.GetFileNameWithoutExtension(cf.Name);
		Options.CompiledFile = cf.FullName;

		FileInfo _hhp = new FileInfo(Path.Combine(outputFolder, chmName + ".hhp"));
		FileInfo hhc = new FileInfo(Path.Combine(outputFolder, chmName + ".hhc"));
		FileInfo hhk = new FileInfo(Path.Combine(outputFolder, chmName + ".hhk"));

		List<CHMIndexEntry> indexList = new List<CHMIndexEntry>();
		foreach (CHMNode n in nodes) {
			var ieList = n.IndexEntries;
			foreach (var ie in ieList) {
				String keywords = ie.Keywords;
				if (keywords == null)
					indexList.Add(ie);
				else {
					String[] arr = CHMIndexEntry.Split(keywords, s.KeywordsDelim.ToCharArray());
					foreach (String kw in arr) {
						CHMIndexEntry ie2 = new CHMIndexEntry { Text = kw };
						ie2.Local = ie.Local;
						indexList.Add(ie2);
					}
				}
			}
		}

		if (String.IsNullOrEmpty(Options.Title))
			Options.Title = chmName;

		// testing:
		/*indexList.Add(new CHMIndexEntry() { Local = "Filenames.html", Name = "Files" });
		indexList.Add(new CHMIndexEntry() { Local = "About.html", Name = "ATest1" });
		indexList.Add(new CHMIndexEntry() { Name = "Cash", Local = "About.html" });
		indexList.Add(new CHMIndexEntry() { SeeAlso = "Cash", Text = "Money!!!!!", Categories = ",Financing" });
		indexList.Add(new CHMIndexEntry() { SeeAlso = "Cash", Categories = ",Financing" });
		indexList.Add(new CHMIndexEntry() { Text = "Apple", Local = "apple.html", Categories = "Food=food.html|Fruit=fruit.html|Sugar=sweet.html,Healthy=heathly.html|Eating=edibles.html" });
		indexList.Add(new CHMIndexEntry() { Text = "x", Local = "x", Categories = "A=aa|B=bb,A|B,ZA" });
		indexList.Add(new CHMIndexEntry() { Text = "y", Local = "y", Categories = "A|B" });
		indexList.Add(new CHMIndexEntry() { Text = "z", Local = "z", Categories = "A|B|C" });
		indexList.Add(new CHMIndexEntry() { Text = "AA", Local = "aa1.html" });
		indexList.Add(new CHMIndexEntry() { Text = "AA", Local = "aa2.html" });*/

		if (s.CreateCSharpFile) {
			String p = "" + s.CSharpFilename;
			if (p.StartsWith(@"\") && !p.StartsWith(@"\\"))
				p = p.TrimStart('\\');
			else if (p.StartsWith("/") && !p.StartsWith("//"))
				p = p.TrimStart('/');

			if (!p.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase))
				p = Path.Combine(p, chmName.Replace(" ", "") + ".cs");
			
			if (!Path.IsPathRooted(p))
				p = Path.Combine(s.OutputFolder, p);

			FileInfo f = new FileInfo(p);
			if (!Directory.Exists(f.Directory.FullName))
				Directory.CreateDirectory(f.Directory.FullName);

			if (!Directory.Exists(f.Directory.FullName))
				Directory.CreateDirectory(f.Directory.FullName);

			String code = doc.GetCSharpConstantDefinitionsCode(s.CSharpNamespace, Path.GetFileNameWithoutExtension(f.Name), cf);
			File.WriteAllText(f.FullName, code);
		}

		if (indexList.Count > 0) {
			Encoding encHHK = Encoding.GetEncoding(s.HHKEncoding);
			XmlDocument kdoc = ToIndexHhk(indexList);
			String xml = kdoc.DocumentElement.InnerXml;
			String html = "<HTML><HEAD></HEAD><BODY>\r\n";
			html += "<OBJECT type=\"text/site properties\">\r\n";
			//html += "<param name=\"Font\" value=\"Arial Unicode MS,10,0\">\r\n";

			if (!String.IsNullOrEmpty(Options.IndexFont.FamilyName))
				html += "<param name=\"Font\" value=\"" + Options.IndexFont.ToString() + "\">\r\n";

			// these don't do anything:
			//html += "<param name=\"Window Styles\" value=\"0x800000\">";
			// <param name=""Background"" value=""0xff0000"">

			html += "</OBJECT>\r\n";
			html += xml;
			html += "\r\n</BODY></HTML>";
			File.WriteAllText(hhk.FullName, html, encHHK);
		}

		Options.ContentsFile = hhc.FullName;
		Options.IndexFile = (indexList.Count > 0 ? hhk.FullName : null);

		if (String.IsNullOrEmpty(Options.DefaultTopic))
			Options.DefaultTopic = nodes.Count > 0 ? nodes[0].Local : "";
		else
			Options.DefaultTopic = Path.Combine(outputFolder, Options.DefaultTopic);

		if (Options.Windows.Count == 0) {
			Options.DefaultWindow = "main";
			CHMWindow w = CHMWindow.CreateDefaultWindow(Options.DefaultTopic, Options.Title, Options.DefaultWindow);
			w.IndexFile = hhk.FullName;
			w.TOCFile = hhc.FullName;
			Options.Windows.Add(w);
		}

		CHMWindow defaultWindow = null;
		foreach (CHMWindow w in Options.Windows) {
			w.TOCFile = Options.ContentsFile;
			w.IndexFile = (indexList.Count > 0 ? hhk.FullName : null);
			if (w.Name == Options.DefaultWindow)
				defaultWindow = w;
		}

		if (defaultWindow == null) {
			defaultWindow = Options.Windows[0];
			if (String.IsNullOrEmpty(defaultWindow.Name)) {
				defaultWindow.Name = "main";
				Options.DefaultWindow = "main";
			}
		}

		if (String.IsNullOrEmpty(defaultWindow.HomeFile))
			defaultWindow.HomeFile = Options.DefaultTopic;

		// error log file replaces the entire log file, which isn't as useful
		// thus, we append to a log file ourselves.
		FileInfo logFile = (!String.IsNullOrEmpty(Options.ErrorLogFile) ? new FileInfo(GetPath(s.OutputFolder, Options.ErrorLogFile)) : null);
		Options.ErrorLogFile = "";

		// this must be done when the output folder is a parent folder, and the default index.html
		// file exists in a subfolder.  Otherwise when the CHM file is first opened, a
		// "This page cannot be displayed." error message will appear.
		if (!String.IsNullOrEmpty(Options.DefaultTopic))
			Options.DefaultTopic = GetRelativePath(s.OutputFolder, Options.DefaultTopic, true);

		foreach (CHMWindow w in Options.Windows) {
			if (!String.IsNullOrEmpty(w.DefaultFile))
				w.DefaultFile = GetRelativePath(s.OutputFolder, w.DefaultFile, true);

			if (!String.IsNullOrEmpty(w.HomeFile))
				w.HomeFile = GetRelativePath(s.OutputFolder, w.HomeFile, true);
		}

		// save hhp to disk
		Encoding encHHP = Encoding.GetEncoding(s.HHPEncoding);
		Options.HHPAppendText = Options.HHPAppendText.Replace("\r", "").Replace("\n", "\r\n");

		using (StreamWriter sw = new StreamWriter(_hhp.FullName, false, encHHP)) {
			sw.WriteLine(Options.ToString());
		}

		// save hhc to disk
		XmlDocument xhhc = new XmlDocument();
		xhhc.AppendChild(xhhc.CreateElement("hhc"));

		List<String> badFilePaths = new List<String>();
		ToHHCXMLElement(doc.Nodes, xhhc.DocumentElement, outputFolder, Options.TOCDefaultFolderImage, Options.TOCDefaultItemImage, s.BlankHeaders, badFilePaths);

		Encoding encHHC = Encoding.GetEncoding(s.HHCEncoding);
		using (StreamWriter sw = new StreamWriter(hhc.FullName, false, encHHC)) {
			// xhhc.DocumentElement.InnerXml output without any formatting.  I wanted to have the hhc file indented.
			// However, the hhc file is finicky.  The "<LI>" and "<OBJECT>" must appear on the same line
			// Thus, the whitespace between the "<LI>" and the "<OBJECT>" are removed.
			sw.WriteLine("<HTML><HEAD></HEAD><BODY>");

			sw.WriteLine("<OBJECT type=\"text/site properties\">");
			if (!String.IsNullOrEmpty(Options.TOCBackgroundColor))
				sw.WriteLine(String.Format("<param name=\"Background\" value=\"{0}\">", Options.TOCBackgroundColor)); // B G R

			if (!String.IsNullOrEmpty(Options.TOCForegroundColor))
				sw.WriteLine(String.Format("<param name=\"Foreground\" value=\"{0}\">", Options.TOCForegroundColor));

			//sw.WriteLine("<param name=\"Font\" value=\"@Arial Unicode MS,12,0\">");
			if (!String.IsNullOrEmpty(Options.TOCFont.FamilyName))
				sw.WriteLine("<param name=\"Font\" value=\"" + Options.TOCFont.ToString() + "\">");

			int styles = Options.TOCWindowStyles | Options.TOCTreeViewStyles;
			if (styles != 0)
				sw.WriteLine("<param name=\"Window Styles\" value=\"" + CHMWindow.hex(styles) + "\">");

			if (Options.TOCWindowExStyles != 0)
				sw.WriteLine("<param name=\"ExWindow Styles\" value=\"" + CHMWindow.hex(Options.TOCWindowExStyles) + "\">");

			sw.WriteLine("</OBJECT>");

			//String innerXml = xhhc.DocumentElement.InnerXml; // can't do this, must remove whitespace
			//sw.WriteLine(innerXml);

			String def = xhhc.ToStringFormatted();
			StringBuilder sb = new StringBuilder();
			int x = 0;
			int y = 0;
			while ((y = def.IndexOf("<LI>", x)) >= 0) {
				sb.Append(def.Substring(x, y + 4 - x));
				y += 4;
				for (int i = y; i < def.Length; i++) {
					char c = def[i];
					if (Char.IsWhiteSpace(c))
						y++;
					else
						break;
				}
				x = y;
			}
			sb.Append(def.Substring(x));
			sw.WriteLine(sb.ToString());

			sw.WriteLine("</BODY></HTML>");
		}

		AdvancedOptions ao = s.AdvancedOptions;
		if (!String.IsNullOrEmpty(ao.OutputTokensFilename) && !String.IsNullOrEmpty(ao.OutputTokens)) {
			String fn = GetPath(s.OutputFolder, ao.OutputTokensFilename);
			String text = CHMDocument.TokensToString(ao, doc.GetAllNodes());
			FileInfo f = new FileInfo(fn);
			if (!Directory.Exists(f.Directory.FullName))
				Directory.CreateDirectory(f.Directory.FullName);

			File.WriteAllText(f.FullName, text, Encoding.GetEncoding(ao.OutputEncodingCodePage));
		}

		if (!String.IsNullOrEmpty(ao.PrecompileProcess)) {
			ProcessStartInfo pre = new ProcessStartInfo(ao.PrecompileProcess, ao.PrecompileProcessArguments);
			pre.WorkingDirectory = new FileInfo(hhcExePath).Directory.FullName;
			pre.UseShellExecute = false;
			pre.CreateNoWindow = true;
			pre.WindowStyle = ProcessWindowStyle.Hidden;
			Process preproc = Process.Start(pre);
			preproc.WaitForExit();
			if (preproc.ExitCode != 0)
				throw new ArgumentException("Pre-compile process exit code is non-zero: " + preproc.ExitCode);
		}

		// It's possible for the hhc file to contain nested folders, e.g.
		// <param name="Local" value="a/Sub-(Topic)-1.html" />
		// An important caveat is that the index file must appear at the root, probably because
		// all other files are going to be based relative to this location.

		ProcessStartInfo start = new ProcessStartInfo(hhcExePath, "\"" + _hhp.FullName + "\"");
		start.WorkingDirectory = new FileInfo(hhcExePath).Directory.FullName;
		start.UseShellExecute = false;
		start.CreateNoWindow = true;
		start.RedirectStandardOutput = true;
		start.WindowStyle = ProcessWindowStyle.Hidden;
		Process proc = Process.Start(start);
		proc.PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
		compilerMessages = proc.StandardOutput.ReadToEnd();
		compilerMessages = compilerMessages.Replace("\r\r", "\r");

		if (badFilePaths.Count > 0) {
			StringBuilder sb2 = new StringBuilder();
			sb2.Append("\nError: Bad File Paths:");
			foreach (String badPath in badFilePaths)
				sb2.Append("\n" + badPath);
			compilerMessages += sb2.ToString();
		}

		if (s.DeleteTempFiles) {
			if (File.Exists(_hhp.FullName)) File.Delete(_hhp.FullName);
			if (File.Exists(hhc.FullName)) File.Delete(hhc.FullName);
			if (File.Exists(hhk.FullName)) File.Delete(hhk.FullName);
		}

		// error log file replaces the entire log file, which isn't as useful
		if (logFile != null) {
			try {
				if (!Directory.Exists(logFile.Directory.FullName))
					Directory.CreateDirectory(logFile.Directory.FullName);

				File.AppendAllText(logFile.FullName, compilerMessages + "----------" + Environment.NewLine);
			} catch {
				throw new ArgumentException("Cannot write to log file: " + logFile.FullName);
			}
		}

		return File.Exists(cf.FullName);
	}

	private static void ToHHCXMLElement(List<CHMNode> nodes, XmlElement RootElement, String dir, int DefaultFolderImage, int DefaultItemImage, BlankHeader BlankHeaders, List<String> badFilePaths) {
		if (nodes == null || nodes.Count == 0)
			return;

		XmlDocument doc = RootElement.OwnerDocument;
		XmlElement ulElement = doc.CreateElement("UL");
		RootElement.AppendChild(ulElement);
		foreach (CHMNode node in nodes) {
			if (node.Name.Length == 0 && (BlankHeaders == BlankHeader.HideInTOCLinked || BlankHeaders == BlankHeader.HideInTOCNotLinked))
				continue;

			XmlElement liElement = doc.CreateElement("LI");
			ulElement.AppendChild(liElement);

			XmlElement objElement = doc.CreateElement("OBJECT");
			liElement.AppendChild(objElement);
			objElement.SetAttribute("type", "text/sitemap");
			AddParamElement(objElement, "Name", node.Name);

			if (!String.IsNullOrEmpty(node.Local)) {
				String path = GetRelativePath(dir, node.Local);
				if (path.StartsWith(".."))
					badFilePaths.Add(path);

				AddParamElement(objElement, "Local", path);
			}

			if (!String.IsNullOrEmpty(node.ImageNumber))
				AddParamElement(objElement, "ImageNumber", node.ImageNumber);
			else {
				bool hasChild = false;
				foreach (var child in node.ChildNodes) {
					if (child.Name.Length > 0 || BlankHeaders == BlankHeader.ShowInTOC) {
						hasChild = true;
						break;
					}
				}

				AddParamElement(objElement, "ImageNumber", (hasChild ? "" + DefaultFolderImage : "" + DefaultItemImage));
			}

			if (node.ChildNodes.Count > 0)
				ToHHCXMLElement(node.ChildNodes, ulElement, dir, DefaultFolderImage, DefaultItemImage, BlankHeaders, badFilePaths);
		}
	}


	private static CHMCategory CreateCategory(String s) {
		int x = s.LastIndexOf('=');
		String n = s;
		String l = "";
		if (x >= 0) {
			n = s.Substring(0, x).Trim();
			l = s.Substring(x+1).Trim();
		}
		return new CHMCategory() { Name = n, Local = l };
	}

	public static XmlDocument ToIndexHhk(List<CHMIndexEntry> list) {
		// step 1, group the index entries into the categories
		List<Object> rootList = new List<Object>();
		//Hashtable catHT = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

		foreach (var ie in list) {
			// ie.DisplayName is null if the user does not format the {index} entry properly.
			// A warning message is displayed to the user in an earlier part of the code.
			if (ie.DisplayName == null)
				continue;

			String[] cats = ie.Categories.Split(new char[] { ',' }, StringSplitOptions.None);
			Hashtable ht = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
			foreach (String raw in cats) {
				// e.g. {Text="Income Tax" Categories="Financing Overview=AllFinancing|Personal=PersonalFinancing"}
				// The pipe is used to denote subcategories
				// This would look like:
				// Financing Overview   <- double clicking this shows the AllFinancing.html page
				//    Personal          <- double clicking this shows the PersonalFinancing.html page
				//        Income Tax    <- double clicking this shows the section of the word document where the index is defined
				String[] ss = raw.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

				if (ss.Length > 0) {
					CHMCategory c = CreateCategory(ss[0]);
					bool found = false;
					foreach (Object o in rootList) {
						if (o is CHMCategory && String.Compare(((CHMCategory) o).Name, c.Name, true) == 0) {
							c = (CHMCategory) o; // use the existing category
							found = true;
							break;
						}
					}
					if (!found)
						rootList.Add(c);

					for (int i = 1; i < ss.Length; i++) {
						CHMCategory c2 = CreateCategory(ss[i]);
						found = false;
						foreach (CHMCategory o in c.Children) {
							if (String.Compare(o.Name, c2.Name, true) == 0) {
								c2 = o;
								found = true;
							}
						}

						if (!found)
							c.Children.Add(c2);

						c = c2;
					}

					if (!c.Entries.Contains(ie))
						c.Entries.Add(ie);
				}
				else {
					rootList.Add(ie);
				}
			}
		}

		rootList.Sort();

		XmlDocument doc = new XmlDocument();
		doc.AppendChild(doc.CreateElement("hhk"));
		//XmlDocument doc = xhhk.DocumentElement.OwnerDocument;
		XmlElement root = doc.DocumentElement;

		XmlElement ul = doc.CreateElement("UL");
		root.AppendChild(ul);

		foreach (Object o in rootList) {
			if (o is CHMIndexEntry)
				ul.AppendChild(ToLI((CHMIndexEntry) o, doc));
			else
				ToLI((CHMCategory) o, ul, doc);
		}

		return doc;
	}

	private static XmlElement ToLI(CHMCategory cat, XmlElement ulParent, XmlDocument doc) {
		XmlElement li = doc.CreateElement("LI");
		XmlElement obj = doc.CreateElement("OBJECT");
		obj.SetAttribute("type", "text/sitemap");
		if (ulParent != null)
			ulParent.AppendChild(li);
		li.AppendChild(obj);

		String local = "about:blank";
		if (!String.IsNullOrEmpty(cat.Local)) {
			local = cat.Local;
			if (!local.EndsWith(".htm", StringComparison.InvariantCultureIgnoreCase) && !local.EndsWith(".html"))
				local = local + ".html";
		}

		AddParamElement(obj, "Name", cat.Name);
		AddParamElement(obj, "Local", local);

		ArrayList objList = new ArrayList();
		objList.AddRange(cat.Children);
		objList.AddRange(cat.Entries);
		objList.Sort();

		if (objList.Count > 0) {
			XmlElement ul2 = doc.CreateElement("UL");
			if (ulParent != null)
				ulParent.AppendChild(ul2);

			foreach (Object o in objList) {
				if (o is CHMIndexEntry)
					ul2.AppendChild(ToLI((CHMIndexEntry) o, doc));
				else
					ToLI((CHMCategory) o, ul2, doc);
			}
		}

		return li;
	}

	private static XmlElement ToLI(CHMIndexEntry ie, XmlDocument doc) {
		XmlElement li = doc.CreateElement("LI");
		XmlElement obj = doc.CreateElement("OBJECT");
		obj.SetAttribute("type", "text/sitemap");

		li.AppendChild(obj);

		AddParamElement(obj, "Name", ie.DisplayName);

		if (!String.IsNullOrEmpty(ie.Local))
			AddParamElement(obj, "Local", Path.GetFileName(ie.Local) + ie.Bookmark);

		if (!String.IsNullOrEmpty(ie.SeeAlso))
			AddParamElement(obj, "See Also", ie.SeeAlso);

		return li;
	}

	private static void Append(List<CHMIndexEntry> list, XmlElement ul) {
		XmlDocument doc = ul.OwnerDocument;
		foreach (var ie in list) {
			XmlElement li = ToLI(ie, doc);
			ul.AppendChild(li);
		}
	}

	private static void AddParamElement(XmlElement element, String strName, String strValue) {
		XmlElement e = element.OwnerDocument.CreateElement("param");
		e.SetAttribute("name", strName);
		e.SetAttribute("value", strValue);
		element.AppendChild(e);
	}

	//String baseFolder = @"c:\temp\a\";
	//String fn1 = @"C:\temp\b\blah.html"; // "../b/blah.html"
	//String fn2 = @"C:\temp\a\c\blah.html"; // "c/blah.html"
	//String fn3 = @"C:\newfolder\blah.html"; //"../../newfolder/blah.html"
	public static String GetRelativePath(String baseFolder, String fn, bool useBackslash = false) {
		Uri pathUri = new Uri(fn);

		if (!baseFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			baseFolder += Path.DirectorySeparatorChar;

		Uri folderUri = new Uri(baseFolder);
		String path = Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString());
		if (useBackslash)
			path = path.Replace('/', Path.DirectorySeparatorChar);
		return path;
	}


	// returns the encoding used by the Word doc (returns null if the document TextEncoding is < 0 or > 65535)
	// if outputEncoding is specified, then the file will be saved to HTML using that format
	public static Encoding SaveWordToHtml2(FileInfo docFile, FileInfo htmlFile, bool visible, Word wordApp = null) {
		if (!File.Exists(docFile.FullName))
			throw new ArgumentException("Word document file '" + docFile.FullName + "' does not exist.");

		String dir = Path.GetDirectoryName(htmlFile.Directory.FullName);
		if (!Directory.Exists(dir))
			throw new ArgumentException("Output folder '" + dir + "' does not exist.");

		bool wasNull = false;
		if (wordApp == null) {
			wasNull = true;
			wordApp = new Word();
		}

		int enc = 1252;
		try {
			wordApp.Visible = visible;
			WordDocument wd = wordApp.Documents.Open(docFile.FullName, ReadOnly:true, AddToRecentFiles:false);
			enc = (int) wd.TextEncoding;

			// no matter what, it seems like Word always saves to html using the default system encoding
			// I tried setting the document encoding, as well as passing in the encoding to the SaveAs method,
			// both had no effect.

			wd.SaveAs(htmlFile.FullName, WdSaveFormat.wdFormatFilteredHTML, AddToRecentFiles:false);
			wd.Close();

			if (wasNull)
				wordApp.Dispose();
		} finally {
			try { if (wasNull) wordApp.Dispose(); } catch {}
		}

		Encoding enc3 = null;
		// for some reason, TextEncoding returns a value out of range
		if ((int) enc >= 0 && (int) enc <= 65535)
			enc3 = Encoding.GetEncoding((int) enc);

		return enc3; // return the original encoding detected
	}
}

}
