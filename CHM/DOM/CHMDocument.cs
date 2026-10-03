using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using Opulos.Core.Utils;

namespace Opulos.NüHelp.CHM {

public class CHMDocument {

// A known problem with chm files is linking to external files.
// This script is used for external links to be able to work.  The script was
// based on the script found here:http://msdn.microsoft.com/en-us/library/windows/desktop/ms644690%28v=vs.85%29.aspx
// with improvements.
private const String scriptLinkFile =
@"<SCRIPT Language=""JScript"">
function parser(fn) {
var Y,link,h,x;
if (fn.indexOf("":"") >= 0) { // absolute path
  link = ""file:///"" + fn;
}
else {
  if (fn.length > 0 && fn.charAt(0) == '\\' || fn.charAt(0) == '/')
    fn = fn.substring(1);

  h = location.href;
  //alert(h);
  x = h.indexOf('mk:@MSITStore:');
  if (x >= 0) {
    //mk:@MSITStore:C:\CHMPath\Help.chm::/About.html
    Y = h.lastIndexOf(""\\"");
    link = ""file:///"" + h.substring(x+14, Y+1) + fn.replace(""/"", ""\\"");
  }
  else {
    Y = h.lastIndexOf(""/"") + 1;
    if (Y < 0) Y = h.lastIndexOf(""\\"") + 1;
    link = h.substring(0, Y) + fn;
  }
}
//alert(link);
location.href = link;
}
</SCRIPT>";

private const String scriptZoomImage =
@"
<SCRIPT type=""text/javascript"">
function zoomImage(idOpen, idClose) {
    document.getElementById(idOpen).style.display = """";
    document.getElementById(idClose).style.display = ""none"";
}
</SCRIPT>
";

	// because separate doc files can be appended to an existing node,
	// the [next] and [prev] links might need to be changed.  Thus,
	// each header and footer is marked so that after another document is
	// added, the header and footer of the previous documents are
	// updated.  The comments that appear are just to make it easier to
	// find the start and end of each header and footer as easy as possible.
	public const String HEADER_OPEN = "<span class=\"header\"><!-- header -->";
	public const String HEADER_CLOSE = "</span><!-- header -->";
	public const String FOOTER_OPEN = "<span class=\"footer\"><!-- footer -->";
	public const String FOOTER_CLOSE = "</span><!-- footer -->";

	public CHMDocument() {
		Nodes = new List<CHMNode>();
		Title = "";
	}

	public String Title { get; set; }
	public List<CHMNode> Nodes { get; set; }
	private List<Constant> constants = new List<Constant>();

	public void Clear() {
		Nodes.Clear();
		constants.Clear();
	}

	public void DeleteAllFiles() {
		List<String> files = GetAllFiles();
		foreach (String f in files) {
			if (File.Exists(f))
				File.Delete(f);
		}
	}

	public List<String> GetAllFiles() {
		List<String> list = new List<String>();
		foreach (CHMNode n in Nodes)
			GetAllFiles(list, n);
		return list;
	}

	private static void GetAllFiles(List<String> list, CHMNode n) {
		if (!String.IsNullOrEmpty(n.Local))
			list.Add(n.Local);

		foreach (CHMNode m in n.ChildNodes)
			GetAllFiles(list, m);
	}

	// looks for a charset= meta tag. If that's not found then use the System encoding.
	// Not all programs output a BOM, so the BOM may not be available.  As well, 1252
	// is detected at UTF-8 probably because there is no BOM.
	public static Encoding DetectEncoding(FileInfo htmlFile, out int StartIndex, out int EndIndex) {
		StartIndex = -1;
		EndIndex = -1;
		Encoding encoding = Encoding.Default;

		String tmp = "";
		using (FileStream fs = new FileStream(htmlFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
			using (StreamReader sr = new StreamReader(fs, true)) {
				tmp = sr.ReadToEnd();
			}
		}

		// <meta http-equiv=Content-Type content="text/html; charset=windows-1252">
		int x1 = tmp.IndexOf("charset=", StringComparison.InvariantCultureIgnoreCase);
		if (x1 >= 0) {
			StringBuilder sb = new StringBuilder();
			for (int i = x1 + 8; i < tmp.Length; i++) {
				char c = tmp[i];
				if (char.IsDigit(c) || char.IsLetter(c) || c == '-' || c == '_') {
					if (StartIndex == -1)
						StartIndex = i;
					sb.Append(c);
				}
				else if (sb.Length > 0) {
					EndIndex = i;
					break;
				}
			}

			String charset = sb.ToString();
			encoding = Encoding.GetEncoding(charset);
		}
		return encoding;
	}

	// looks for the meta tag charset= and replaces the contents of the attribute value with the
	// WebName of the input charset.
	public static String ReplaceCharset(String html, Encoding charset, int StartIndex, int EndIndex) {
		if (charset == null || StartIndex < 0 || EndIndex < 0)
			return html;

		String html2 = html.Substring(0, StartIndex) + charset.WebName + html.Substring(EndIndex);
		return html2;
	}

	// http://en.wikipedia.org/wiki/Quotation_mark_glyphs
	public static String CleanHTML(String html) {
		StringBuilder sb = new StringBuilder(html);
		for (int i = 0; i < sb.Length; i++) {
			char c = sb[i];
			if (c == '\u2018' || c == '\u2019' || c == '\u201A' || c == '\u201B' || c == '\uFF07') // single curly left / right quote
				sb[i] = '\'';
			else if (c == '\u201C' || c == '\u201D' || c == '\u201E' || c == '\u201E' || c == '\u301D' || c == '\u301E' || c == '\u301F' || c == '\uFF02') // double curly quotes
				sb[i] = '"';
		}

		StringBuilder sb2 = new StringBuilder();
		try {
			// <span title="...">...</span> tags are used in the word document for tooltips
			// using the comment feature doesn't appear right
			String s = sb.ToString();
			int x = 0;
			int y = 0;
			while ((y = s.IndexOf("&lt;span", x)) >= 0) {
				sb2.Append(s.Substring(x, (y - x)));
				sb2.Append("<span");
				int z = s.IndexOf("&lt;/span&gt;", y);
				int y2 = s.LastIndexOf("&gt;", z);
				String span = s.Substring(y + 8, (y2 - (y + 8)));
				span = span.Replace("&quot;", "\"", StringComparison.InvariantCultureIgnoreCase);
				sb2.Append(span);
				sb2.Append(">");
				sb2.Append(s.Substring(y2 + 4, z - (y2 + 4)));
				sb2.Append("</span>");
				x = z + 13;
			}
			sb2.Append(s.Substring(x));
		} catch {
			throw new ArgumentException("Span tags are not formatted correctly.");
		}

		return sb2.ToString();
	}

	private static void TestRemoveDivClassWordSectionTags() {
		// String test = "...<div class=WordSection1>111<div>222</div>333</div>444<div class=WordSection2>555</div>666<div></div><div class=WordSection3>777<div>888</div>999</div>...";
		// test = "<div class=WordSection1><div>bbbb</div><div>aaaa</div></div><div class=WordSection1><div>bbbb</div><div><div>aaaa</div></div>";
		// String result = CHMDocument.RemoveDivClassWordSectionTags(test, new List<String>());
	}

	// When word produces an html document, it surrounds each section in div tags.  The parsing processing expects that the h-tags
	// are not going to be nested within higher level tags.  For example, this would not be good:
	//
	// <div class=WordSection1>
	// <h1>...</h1>
	// blah blah blah
	// </div>
	// <div class=WordSection2>
	// <h1>...</h1>
	// more blah
	// </div>
	//
	// If you assumed that there were no higher level tags, then you might end up with two pages that look like:
	//
	// Page 1: (contains a close </div> tag and then an open <div class=WordSection2> tag)
	// <h1>...</h1>
	// blah blah blah
	// </div>
	// <div class=WordSection2>
	//
	// Page 2: (contains a close </div> tag with no open div tag)
	// <h1>...</h1>
	// more blah
	// </div>
	//
	// So the goal of this method is to extract the inner html so this kind of bad HTML formatting doesn't happen.
	// The final result would look like:
	//
	// <h1>...</h1>
	// blah blah blah
	// <h1>...</h1>
	// more blah
	public static String RemoveDivClassWordSectionTags(String s, List<String> warningMessages) {
		StringBuilder sb = new StringBuilder();
		StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
		int x = 0;
		int y = 0;
		bool warnFlag = false;

		while ((y = s.IndexOf("<div class=WordSection", x, sc)) >= 0) {
			// find the closing tag
			int start = s.IndexOf('>', y) + 1; // start of wanted text
			int div = 1;
			int p = y + 1;
			while (div != 0) {
				int x1 = s.IndexOf("<div", p, sc);
				int x2 = s.IndexOf("</div>", p, sc);
				if (x2 >= 0) {
					if (x1 >= 0 && x1 < x2) {
						div++;
						p = x1 + 1;
					}
					else { // x2 < x1 or x1 doesn't exist
						div--;
						p = x2 + 1;
					}
				}
				else {
					break;
				}
			}

			if (div == 0) {
				int len = p - start;
				String t = s.Substring(start, len - 1);
				sb.Append(t);
			}
			else {
				if (!warnFlag) {
					warnFlag = true;
					if (warningMessages != null)
						warningMessages.Add("Cannot find closing div tag for <div class=WordSection");
				}
			}

			x = y + 1;
		}

		if (sb.Length == 0) {
			// not all html files will have those div tags, so if none are detected then return s.
			//warningMessages.Add("No <div class=WordSection> tags were detected.");
			return s;
		}

		return sb.ToString();
	}


	///<param name="htmlFile">The file that contains the html text to parse.</param>
	///<param name="outputFolder">The folder where all the individual html files will be saved to.</param>
	///<param name="readEncoding">The encoding of the htmlFile.  If null, the readEncoding is auto-detected.</param>
	///<param name="s.writeEncoding">The Settings object contains the write encoding. If -1, then the readEncoding value is used.</param>
	///<param name="tempFiles">A list to add linked image files that were copied to a folder relative to the output folder.</param>
	///<param name="parent">The node to place the new html pages at.</param>
	public CHMAppendResult AppendHtmlFile(FileInfo htmlFile, Settings s, CHMNode parent, Encoding readEncoding, List<String> tempFiles, List<String> warningMessages, FileInfo sourceFile, InsertPosition insertPosition) {
		if (FileUtil.IsFileLocked(htmlFile))
			System.Threading.Thread.Sleep(s.HtmlFileLockedTimeoutMillis);

		if (String.IsNullOrEmpty(Title))
			Title = Path.GetFileNameWithoutExtension(htmlFile.Name).Replace("_raw", "");

		String outputFolder = s.OutputFolder;

		Encoding writeEncoding = null;
		if (s.HtmlFilesWriteEncoding >= 0)
			writeEncoding = Encoding.GetEncoding(s.HtmlFilesWriteEncoding);

		if (readEncoding == null && s.HtmlFilesReadEncoding >= 0)
			readEncoding = Encoding.GetEncoding(Settings.Default.HtmlFilesReadEncoding);

		if (String.IsNullOrEmpty(outputFolder))
			outputFolder = htmlFile.Directory.FullName;

		if (!Directory.Exists(outputFolder))
			Directory.CreateDirectory(outputFolder);

		int CharSetStartIndex = -1;
		int CharSetEndIndex = -1;
		Encoding detectedEncoding = DetectEncoding(htmlFile, out CharSetStartIndex, out CharSetEndIndex);

		if (readEncoding == null)
			readEncoding = detectedEncoding;

		if (writeEncoding == null)
			writeEncoding = readEncoding;

		String strHtml = File.ReadAllText(htmlFile.FullName, readEncoding);
		strHtml = ReplaceCharset(strHtml, writeEncoding, CharSetStartIndex, CharSetEndIndex);
		strHtml = CleanHTML(strHtml);

		StringComparison sc = StringComparison.OrdinalIgnoreCase;

		int headIndex = strHtml.IndexOf("<head>", sc);
		int headIndexClose = strHtml.IndexOf("</head>", headIndex + 1, sc);
		String strHead = (headIndex >= 0 && headIndexClose >= 0 ? strHtml.Substring(headIndex, (headIndexClose - headIndex) + 7) : "");
		if (s.ReplaceFontSizes)
			strHead = ReplaceFontSizes(strHead);

		int bodyIndex = strHtml.IndexOf("<body", headIndexClose + 1, sc);
		int bodyIndex2 = strHtml.IndexOf(">", bodyIndex + 1);
		int bodyIndexClose = strHtml.IndexOf("</body>", bodyIndex2 + 1, sc);
		String strBody = (bodyIndex >= 0 && bodyIndexClose >= 0 ? strHtml.Substring(bodyIndex2 + 1, (bodyIndexClose - (bodyIndex2 + 1))).Trim() : "");

		/*int divIndex = strBody.IndexOf("<div", sc);
		if (divIndex >= 0) {
			int divIndex2 = strBody.IndexOf(">", divIndex + 1);
			int divIndexClose = strBody.LastIndexOf("</div>", sc);
			if (divIndexClose >= 0)
				strBody = strBody.Substring(divIndex2 + 1, (divIndexClose - (divIndex2 + 1))).Trim();
		}*/

		strBody = RemoveDivClassWordSectionTags(strBody, warningMessages);

		// everything from the beginning of the file to the first h-tag is ignored.  This allows for storing a TOC at the top of the word document.

		Hashtable invarHT = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

		//Hashtable anchorHT = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		//Hashtable anchorHT2 = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

		String htag = null;
		int index = 0;

		List<String> titleList = new List<String>();
		List<String> contentList = new List<String>();
		List<String> filenameList = new List<String>();

		Hashtable fnHT = new Hashtable(); // ht[fn] = CHMNode

		// stores the nodes at the document's root level
		List<CHMNode> rootNodes = new List<CHMNode>();
		// stores all nodes created in order to find the immediate parent when adding a new node
		List<CHMNode> nodes = new List<CHMNode>();
		CHMAppendResult appendResult = new CHMAppendResult();
		appendResult.RootNodes = rootNodes;
		appendResult.AllNodes = nodes;

		// characters not allowed in filenames.  Spaces also not allowed for chm.
		// \u2026 is the ellipsis (...) it causes punycode check to fail
		// \u00a0 aka 160 is a non-breaking whitespace
		// any missed whitespace are also caught down below
		// CHM help does not like MS long dash either
		// # cannot be used because it is used as a #bookmark location
		char[] specialChars = new char[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|', ' ', '\u2026', '\u00a0', '\t', '\r', '\n', '–', '#' };

		// Note: Word stops at <h6>.  For h7 and higher it uses <p class=MsoHeading7 ...
		// No plans on supporting this.
		List<String> hList = new List<String>();
		List<String> hcList = new List<String>();
		for (int i = 1; i <= 9; i++) {
			String h = "h" + i;
			hList.Add("<" + h);
			hcList.Add("</" + h + ">");
			if (String.Compare(h, s.MaxNestedHeader, true) == 0)
				break;
		}

		String[] harr = hList.ToArray(); //{ "<h1", "<h2", "<h3", "<h4", "<h5", "<h6", "<h7", "<h8", "<h9" };
		String[] hcarr = hcList.ToArray(); //{ "</h1>", "</h2>", "</h3>", "</h4>", "</h5>", "</h6>", "</h7>", "</h8>", "</h9>" };

		String LastNonBlankHeader = "";
		bool isFirstNode = true;

		while ((index = HTag(strBody, index, sc, harr, out htag)) >= 0) {
			int index2 = strBody.IndexOf(">", index + 1);

			String hctag = HCTag(htag, harr, hcarr);
			int hIndexClose = strBody.IndexOf(hctag, index2 + 1, sc);
			int level = Level(htag);

			int titleIndex = index;
			int titleLength = hIndexClose + hctag.Length - index;
			int contentIndex = hIndexClose + hctag.Length;

			// some styles wrap the h-tag in a div, e.g. <div ...><h1 .. ></h1></div>
			// so we need to get the substring starting at the opening div tag
			bool openDivNotFound = false;
			int divOpenIndex = strBody.IndexOf("<div", index, sc);
			int divCloseIndex = strBody.IndexOf("</div>", index, sc);
			if (divCloseIndex >= 0 && (divOpenIndex < 0 || divOpenIndex > divCloseIndex)) {
				int index5 = strBody.LastIndexOf("<div", divCloseIndex, sc);
				if (index5 >= 0) {
					titleIndex = index5;
					titleLength = (divCloseIndex - index5) + 6;
					contentIndex = titleIndex + titleLength;
				}
				else {
					openDivNotFound = true;
				}
			}

			//String htitle = strBody.Substring(index2 + 1, hIndexClose - (index2 + 1));
			String htitle = strBody.Substring(titleIndex, titleLength);

			String innerName = InnerName(htitle);
			if (openDivNotFound && warningMessages != null)
				warningMessages.Add("Open div tag not found for section: " + innerName);

			// replace all header sizes with h1, otherwise some topics have tiny fonts for the title
			if (s.OverrideHTags) {
				String hot = "<" + s.HTagSize; // h open tag
				String hct = "</" + s.HTagSize + ">"; // h close tag
				htitle = htitle.Replace(htag, hot).Replace(hctag, hct);
			}

			List<String> anchors = ExtractAnchorNames(htitle);

			bool cleanCHM = false;
			Hashtable ht = GetCHMTokens(ref htitle, out cleanCHM);
			if (!cleanCHM && warningMessages != null)
				warningMessages.Add("Missing quotation mark or square bracket on [CHM tag: " + innerName);

			String invarName = (String) ht["InvariantName"];
			if (!String.IsNullOrEmpty(invarName)) {
				// replace MS long dash with regular dash.  Treat this as special case to prevent
				// hard to understand warning message.
				invarName = invarName.Replace('–', '-');
				if (invarHT.ContainsKey(invarName))
					throw new ArgumentException("InvariantName: " + invarName + " is used more than once.");
				invarHT[invarName] = invarName;
			}

			if (innerName.Length == 0) {
				if (s.ShowBlankHeadersWarning && warningMessages != null)
					warningMessages.Add("Blank header detected just after topic: " + LastNonBlankHeader);
			}
			else
				LastNonBlankHeader = innerName;

			CHMNode n = new CHMNode() { Level = level, Name = innerName, Settings = s, WriteEncoding = writeEncoding, SourceFile = htmlFile.FullName };
			nodes.Add(n);
			n.ImageNumber = (String) ht["ImageNumber"];
			n.OriginalFile2 = sourceFile;
			if (isFirstNode) {
				isFirstNode = false;
				n.OriginalFile = sourceFile; // only set for the first node
			}

			// when a new node is added, it is added to the most recent node that has a level less than its own
			bool found = false;
			for (int i = nodes.Count - 1; i >= 0; i--) {
				CHMNode n2 = nodes[i];
				if (n.Level > n2.Level) {
					n2.ChildNodes.Add(n);
					found = true;
					break;
				}
			}

			if (!found)
				rootNodes.Add(n);

			//sb.AppendLine(htag + "\t" + level + "\t" + htitle + "\t" + innerName + "\t" + invarname);

			// the content of this node is everything from the header to the start of the next title, which
			// might be an <h tag or a <div tag.
			int index3 = HTag(strBody, hIndexClose + hctag.Length, sc, harr, out htag);
			int contentLength = index3 - (hIndexClose + hctag.Length);

			if (index3 == -1) {
				contentLength = strBody.Length - contentIndex;
				index3 = strBody.Length;
			}

			// For headers wrapped in div tags, we want to make sure the start of the div tag
			// of the next header is ignored.  We do this by looking for an open div tag with
			// no closing div tag.
			int divOpenIndex2 = strBody.LastIndexOf("<div", (contentIndex + contentLength), sc);
			if (divOpenIndex2 >= 0) {
				int divCloseIndex2 = strBody.IndexOf("</div>", divOpenIndex2, sc);
				if (divCloseIndex2 < 0 || divCloseIndex2 > contentIndex + contentLength) {
					int newLength = divOpenIndex2 - contentIndex;
					if (newLength >= 0)
						contentLength = newLength;
					else if (warningMessages != null)
						warningMessages.Add("There is a mismatch of <div> tags around: " + innerName);
				}
			}

			String content = strBody.Substring(contentIndex, contentLength).Trim();
			content = ReplaceImgTokens(content, outputFolder, htmlFile.FullName, tempFiles, warningMessages);

			String baseName = (String.IsNullOrEmpty(invarName) ? innerName : invarName);
			//baseName = baseName.UnescapeHtml(); // MS Word escapes some letters to their HTML encoded equivalent.

			foreach (char c in specialChars)
				baseName = baseName.Replace(c, '-');

			// just in case to catch any whitespace not caught
			List<char> otherWhitespace = new List<char>();
			foreach (char c in baseName) {
				if (Char.IsWhiteSpace(c) && !otherWhitespace.Contains(c))
					otherWhitespace.Add(c);
			}

			foreach (char c in otherWhitespace)
				baseName = baseName.Replace(c, '-');

			baseName = baseName.ConvertToAscii(false);
			baseName = baseName.Clean("-", '-');
			try {
				baseName = baseName.ConvertToPunyCode();
			} catch (Exception ex) {
				if (warningMessages != null)
					warningMessages.Add("Header text did not pass punycode check: " + baseName + "\nYou may wish to remove any special characters from the header text or use the [CHM InvariantName=] tag.\n" + ex.Message);
			}

			if (baseName.Length == 0)
				baseName = "File";

			if (baseName.EndsWith(".html", StringComparison.InvariantCultureIgnoreCase) || baseName.EndsWith(".htm", StringComparison.InvariantCultureIgnoreCase))
				baseName = Path.GetFileNameWithoutExtension(baseName);

			if (baseName.Length > 160) { // total length cannot exceed 260, so giving 100 for the path
				if (warningMessages != null)
					warningMessages.Add("Extremely long header, {0} characters, starting with: {1}".Format2(baseName.Length, baseName.Substring(0, 10)));

				baseName = baseName.Substring(0, 160);
			}

			bool invalidInvarName = false;
			if (!String.IsNullOrEmpty(invarName) && String.Compare(invarName, baseName) != 0) {
				invalidInvarName = true;
				if (warningMessages != null)
					warningMessages.Add("Bad invariant name '{0}' replaced with '{1}'. Invariant names should not contain spaces or reserved filename characters.".Format2(invarName, baseName));
			}

			String fn = null;
			int k = 1;

			while (true) {
				fn = GetFilename(baseName, k++);
				String path = Path.Combine(outputFolder, fn);
				if (!File.Exists(path) && !fnHT.ContainsKey(path))
					break;
			}

			if (!String.IsNullOrEmpty(invarName) && k > 2 && !invalidInvarName) {
				if (warningMessages != null) {
					bool dupHeader = false;
					foreach (var n2 in GetAllNodes()) {
						if (n2 == n)
							continue;
						if (String.Compare(invarName, Path.GetFileNameWithoutExtension(n2.Local), true) == 0) {
							dupHeader = true;
							break;
						}
					}
					String msg = null;
					if (dupHeader)
						msg = "Invariant name '{0}' cannot be used because another header in the project has the same name.";
					else
						msg = "Invariant name '{0}' cannot be used because a file with that name already exists. Consider deleting temp html files or using a different invariant name.";

					warningMessages.Add(msg.Format2(invarName));
				}
			}

			String fn_no_path = fn;
			fn = Path.Combine(outputFolder, fn);
			n.Local = fn;
			fnHT[fn] = n;

			//----
			ht["Level"] = n.Level;
			ht["Title"] = innerName;
			ht["Local"] = fn_no_path;
			ht["WriteEncoding"] = writeEncoding.CodePage;
			ht["SourceFile"] = n.SourceFile;
			n.Tokens = ht;
			//----

			//---------------------------------------
			//foreach (String a in anchors)
			//	anchorHT[a] = fn;

			List<String> anchors2 = ExtractAnchorNames(content);
			//foreach (String a in anchors2)
			//	anchorHT2[a] = fn;

			foreach (String a in anchors)
				n.Anchors.Add(new CHMAnchor { Anchor = a, InHeader = true });

			foreach (String a in anchors2)
				n.Anchors.Add(new CHMAnchor { Anchor = a, InHeader = false });

			foreach (CHMAnchor a in n.Anchors) {
				// don't need to ignore them since they are above the first h-tag
				// which means the bookmark is ignored anyway.
				//if (a.Anchor.StartsWith("_toc", sc)) // ignore auto-generated anchors
				//	continue;
				add(fn, (a.InHeader ? null : a.Anchor), constants);
			}

			//----------------------------------------

			filenameList.Add(fn);
			titleList.Add(htitle);
			contentList.Add(content);
			index = index3;
		} // end while

		//-----------------------------------------------------------
		// now we need to merge the new nodes with the existing nodes
		// and also adjust the levels
		if (parent == null) {
			// levels do not need adjusting
			if (insertPosition == InsertPosition.Above) {
				var arr = Nodes.ToArray();
				Nodes.Clear();
				Nodes.AddRange(rootNodes);
				Nodes.AddRange(arr);
			}
			else { // nested or below
				Nodes.AddRange(rootNodes);
			}
		}
		else {
			if (insertPosition == InsertPosition.Nested) {
				foreach (var n in nodes)
					n.Level += parent.Level;

				parent.ChildNodes.AddRange(rootNodes);
			}
			else {
				// find the node at the next level up of the selected node and insert the new nodes above or below the selected node's index
				bool b = false;
				CHMNode target = null;
				var allNodes = GetAllNodes();
				for (int i = allNodes.Count - 1; i >= 0; i--) {
					if (allNodes[i] == parent) {
						b = true;
					}
					else if (b) {
						if (allNodes[i].Level < parent.Level) {
							target = allNodes[i];
							break;
						}
					}
				}

				int levelStart = 0;
				List<CHMNode> children = null;
				if (target == null) {
					children = Nodes;
				}
				else {
					children = target.ChildNodes;
					levelStart = target.Level;
				}

				foreach (var n in nodes)
					n.Level += levelStart;

				int x = (insertPosition == InsertPosition.Above ? 0 : 1);

				for (int i = 0; i < children.Count; i++) {
					var nn = children[i];
					if (nn == parent) {
						children.InsertRange(i+x, rootNodes);
						break;
					}
				}
			}
		}

		//-----------------------------------------------------------

		bool indexingOn = true; // this has to be kept outside of the forloop!

		Hashtable nodeHT = CreateNodeHashtable(this);

		// some href may refer to names lower down in the document, so only after
		// all the anchor names are gathered, we can substitute the href= values
		//<a name="..."
		//<a href="..."
		// substitute and a href= references and write everything to disk
		for (int i = 0; i < filenameList.Count; i++) {
			String fn = filenameList[i];
			String title = titleList[i];
			String content = title + Environment.NewLine + contentList[i];
			constants.Add(CSharpConstant(Path.GetFileName(fn), null));

			bool notUsed = false;
			content = UpdateHyperlinks(nodeHT, content, constants, null, out notUsed);

			CHMNode n = (CHMNode) fnHT[fn];
			n.IndexEntries.AddRange(GetIndexEntries(fn, sc, s, ref content, ref indexingOn, warningMessages));

			using (StreamWriter sw = new StreamWriter(fn, false, writeEncoding)) {
				String next = (i < filenameList.Count - 1 ? filenameList[i+1] : "#");
				String prev = (i > 0 ? filenameList[i-1] : "#");

				sw.WriteLine("<html>");
				
				int x = strHead.IndexOf("</head>", sc);
				sw.WriteLine(strHead.Substring(0, x));

				if (content.IndexOf("parser(", sc) >= 0)
					sw.WriteLine(scriptLinkFile);

				sw.WriteLine(scriptZoomImage);

				sw.WriteLine("</head>");

				sw.WriteLine(s.HtmlBodyOpenTag.Replace("</body>", "", sc)); // replace </body> just in case the user accidentally includes it

				String header = "";
				String footer = "";
				if (s.UseMiddleHeaderFooterAlways) {
					header = s.HtmlHeader;
					footer = s.HtmlFooter;
				}
				else {
					if (filenameList.Count == 1) {
						header = s.HtmlHeaderFirstPage;
						footer = s.HtmlFooterLastPage;
					}
					else {
						if (i == 0) {
							header = s.HtmlHeaderFirstPage;
							footer = s.HtmlFooterFirstPage;
						}
						else if (i == filenameList.Count - 1) {
							header = s.HtmlHeaderLastPage;
							footer = s.HtmlFooterLastPage;
						}
						else {
							header = s.HtmlHeader;
							footer = s.HtmlFooter;
						}
					}
				}

				sw.WriteLine(HEADER_OPEN);
				if (!String.IsNullOrEmpty(header)) {
					header = header.Replace("[next]", next);
					header = header.Replace("[prev]", prev);
					sw.WriteLine(header);
				}
				sw.WriteLine(HEADER_CLOSE);

				if (s.ReplaceFontSizes)
					content = ReplaceFontSizes(content);

				sw.WriteLine(content);

				sw.WriteLine(FOOTER_OPEN);
				if (!String.IsNullOrEmpty(footer)) {
					footer = footer.Replace("[next]", next);
					footer = footer.Replace("[prev]", prev);
					sw.WriteLine(footer);
				}
				sw.WriteLine(FOOTER_CLOSE);

				sw.WriteLine("</body>");
				sw.WriteLine("</html>");
			}
		}
		return appendResult;
	}

	public ProjectList.OutputFile ToProjectOutputFile() {
		var of = new ProjectList.OutputFile() { ActiveFile = true };

		CHMNodeTopicMatcher matcher = new CHMNodeTopicMatcher();
		matcher.Append(GetAllNodes());

		Stack s = new Stack();
		for (int i = Nodes.Count - 1; i >= 0; i--) {
			s.Push(Nodes[i]);
		}

		Hashtable htParent = new Hashtable();
		while (s.Count > 0) {
			CHMNode n = (CHMNode) s.Pop();
			for (int i = n.ChildNodes.Count - 1; i >= 0; i--) {
				s.Push(n.ChildNodes[i]);
				htParent[n.ChildNodes[i]] = n;
			}
			if (n.OriginalFile == null)
				continue;

			CHMNode parent = (CHMNode) htParent[n];
			ProjectList.Entry e = null;
			FileInfo anchorFile = null;
			if (parent == null) {
				// optimize the size of the command line by not including
				// a topic name if not required.
				// a topic name is required if a root level node is inserted
				// between two other nodes that share the same SourceFile.
				Hashtable htBefore = new Hashtable();
				bool isSandwiched = false;
				bool isBefore = true;
				CHMNode sibling = null;
				for (int i = 0; i < Nodes.Count; i++) {
					CHMNode n2 = Nodes[i];
					if (n2 == n)
						isBefore = false;
					else if (isBefore)
						sibling = n2;

					if (n2.SourceFile != n.SourceFile) {
						if (isBefore) {
							htBefore[n2.SourceFile] = n2;
						}
						else {
							if (htBefore.ContainsKey(n2.SourceFile)) {
								isSandwiched = true;
								break;
							}
						}
					}
				}

				e = new ProjectList.Entry { ContentFile = n.OriginalFile };
				if (isSandwiched) {
					e.InsertPosition = InsertPosition.Below;
					e.Topic = sibling.InvariantNameThenName;
					anchorFile = sibling.OriginalFile2;
				}
			}
			else {
				bool isFirstChild = (parent.ChildNodes[0] == n);
				if (isFirstChild) {
					e = new ProjectList.Entry { ContentFile = n.OriginalFile, Topic = parent.InvariantNameThenName, InsertPosition = InsertPosition.Nested };
					anchorFile = parent.OriginalFile2;
				}
				else {
					var sibling = parent.ChildNodes[parent.ChildNodes.IndexOf(n)-1]; // the node immediately above/before at the same level
					e = new ProjectList.Entry { ContentFile = n.OriginalFile, Topic = sibling.InvariantNameThenName, InsertPosition = InsertPosition.Below };
					anchorFile = sibling.OriginalFile2;
				}
			}

			if (!String.IsNullOrEmpty(e.Topic) && matcher.IsAmbiguous(e.Topic)) {
				var ambiNodes = matcher.GetAmbiousNodes(e.Topic);
				String alt = " All documents which contain the same header text: " + String.Join(", ", ambiNodes.Select(x => x.OriginalFile2.Name).ToArray());
				throw new ArgumentException("{0} is inserted relative to topic '{1}' in {2}, but that topic is not unique. To fix this error, edit the document and change the header text or use a [CHM InvariantName=\"...\"] tag.".Format2(e.ContentFile.Name, e.Topic, anchorFile.Name) + alt);
			}

			of.Entries.Add(e);
		}

		return of;
	}

	private static Hashtable CreateNodeHashtable(CHMDocument doc) {
		Hashtable nodeHT = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		var nodes = doc.GetAllNodes();
		nodeHT[""] = nodes; // represents case when no filename is specified, thus all nodes are searched
		foreach (CHMNode n in nodes) {
			String key = Path.GetFileNameWithoutExtension(n.SourceFile);
			List<CHMNode> list = (List<CHMNode>) nodeHT[key];
			if (list == null) {
				list = new List<CHMNode>();
				nodeHT[key] = list;
			}
			list.Add(n);
		}

		return nodeHT;
	}

	// nodeHT[SourceFileNoExt] = List<CHMNode>
	private static String UpdateHyperlinks(Hashtable nodeHT, String content, List<Constant> constants, List<String> unresolvedLinks, out bool changed) {
		changed = false;
		StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
		int hx = 0;
		int hy = 0;
		StringBuilder sb2 = new StringBuilder();

		while ((hy = content.IndexOf("href", hx, sc)) >= 0) {
			int q1 = content.IndexOf("\"", hy + 1);
			int q2 = content.IndexOf("\"", q1 + 1);
			if (q1 < 0 || q2 < 0)
				break;

			String href = content.Substring(q1 + 1, q2 - (q1 + 1));

			if (href.IndexOf("file://", StringComparison.InvariantCultureIgnoreCase) >= 0) {
				//file:///\\NüHelp.chm
				//file:///\\output\NüHelp.chm -> output\NuHelp.chm -> output/NuHelp.chm
				String path = href.Replace("file://", "").TrimStart('/', '\\').Replace("\\", "/");
				String s2 = content.Substring(hx, (q1 + 1) - hx);
				int y = s2.LastIndexOf("href", sc);
				s2 = s2.Substring(0, y);
				sb2.Append(s2);
				sb2.Append(String.Format(" onclick=\"parser('{0}')\" style=\"text-decoration:underline;color:green;cursor:hand", path));
				changed = true;
			}
			else {
				CHMNode sn = null; // source node
				String bookmark = null; // e.g. href="../File.docx#blah"  then bookmark = "blah"

				// determine if a filename is available
				//    If so, then only consider bookmarks (<a name="#..."></a>) in that file
				//    If not, then search all nodes.

				bool addWarning = false;

				// only those href entries that start with # or . need to be fixed,
				// e.g.
				// #somebookmark
				// ../../../SomeFile.docx#bookmark
				//
				// Other href entries that have already been fixed will start with C:\...
				// or some start with http://, mailto:, javascript:  those do not need to be fixed.
				if (href.StartsWith("#") || href.StartsWith(".")) {
					int px = href.IndexOf('#');
					String href2 = (px >= 0 ? href.Substring(0, px) : href);
					String fn = "";
					bookmark = (px >= 0 ? href.Substring(px+1) : "");

					addWarning = true; // only add warning if we try to fix but cannot find the fix

					if (href2.EndsWith(".docx", sc) || href2.EndsWith(".doc", sc)) {
						// this case represents a document that links to another document
						// for example, when multiple documents are appended individually to the same project tree
						//
						// So at this point you have to look through the CHMNodes of the document to find the ones
						// added from file.docx reference, and then within each CHMNode, look at the bookmarks to
						// find the exact CHMNode to link to.  If no bookmark matches then do nothing.  If no
						// bookmark is defined then pick the first node.
						//
						// E.g.
						// href="../../../../../../../temp/Test1a.docx"
						// href="../../../../../../../temp/Test1b.docx#bookmark"

						int si = href2.LastIndexOfAny(new [] { '\\', '/' }); // slash index
						fn = (si >= 0 ? href2.Substring(si) : href2);
						fn = Path.GetFileNameWithoutExtension(fn);
						fn = Uri.UnescapeDataString(fn) + "_raw";
					}

					List<CHMNode> nodes = (List<CHMNode>) nodeHT[fn] ?? new List<CHMNode>();

					//if (fn.Length == 0)
					//	nodes.Insert(0, contentNode); // always search content node first if no filename is specified
					bool isHeaderAnchor = false;

					foreach (CHMNode n in nodes) {
						if (bookmark.Length > 0) {
							// first pass, look for an exact match
							foreach (var a in n.Anchors) {
								String a2 = a.Anchor.TrimStart('#');
								if (String.Compare(bookmark, a2, true) == 0) {
									sn = n;
									isHeaderAnchor = a.InHeader;
									break;
								}
							}
						}
						else {
							sn = n;
							break;
						}
					}

					if (sn == null) {
						// second pass, look for a prefix match
						// As an additional check, we look for cases where
						// Word incorrectly outputs an internal bookmark as an external file hyperlink.
						// The href will look like:
						// ../../../../../Downloads/Featur#_Features_[CHM_InvariantName=
						// sometimes the name is cut off.  E.g.
						// anchor = _Features_[CHM_InvariantName=\"Featur
						// name = _Features_[CHM_InvariantName=
						foreach (CHMNode n in nodes) {
							isHeaderAnchor = true; // reset flag
							foreach (var a in n.Anchors) {
								String a2 = a.Anchor.TrimStart('#');
								if (a2.StartsWith(bookmark, sc)) {
									sn = n;
									isHeaderAnchor = a.InHeader;
									break;
								}
							}
						}
					}

					// If a link refers to an anchor in the header, then it's
					// nicer to link to the page without the bookmark.  If the
					// bookmark is used then initially the page scrolls down and
					// the [Previous] link will be hidden.
					if (isHeaderAnchor)
						bookmark = "";
				}

				if (sn != null) {
					changed = true;
					String fno = sn.Local; //Path.GetFileName(sn.Local);

					sb2.Append(content.Substring(hx, (q1 + 1) - hx));
					sb2.Append(fno);
					if (bookmark.Length > 0) // not a link to a header, must include a # bookmark
						sb2.Append("#").Append(bookmark);
				}
				else {
					if (addWarning && nodeHT.Count > 0 && unresolvedLinks != null)
						unresolvedLinks.Add(href);
					sb2.Append(content.Substring(hx, q2 - hx));
				}
			}

			hx = q2;
		}

		sb2.Append(content.Substring(hx));
		return sb2.ToString();
	}

	// Prevent adding duplicate constants to the C# file.
	private static void add(String fn, String bookmark, List<Constant> list) {
		Constant c = CSharpConstant(fn, bookmark);

		foreach (Constant c2 in list) {
			if (String.Compare(c.name, c2.name, true) == 0 && String.Compare(c.url, c2.url, true) == 0)
				return;
		}
		list.Add(c);
	}

	private static String ToString(List<Token> list) {
		StringBuilder sb = new StringBuilder();
		sb.Append("<img");
		foreach (var t in list)
			sb.Append(" ").Append(t.ToString());
		sb.Append(">");
		return sb.ToString();
	}

	private static String GetFirstFolderName(String fn) {
		DirectoryInfo d = new DirectoryInfo(fn);
		while (d != null) {
			if (d.Parent != null && d.Parent.Parent == null)
				break;
			d = d.Parent;
		}
		return d.Name;
	}

	private static FileInfo FindTruePath(String fn, String outputFolder, List<String> warningMessages) {
		String fnOrig = fn;
		String fnUnescaped = Uri.UnescapeDataString(fn); //folderUri.MakeRelativeUri(pathUri).ToString());
		fn = fnUnescaped;

		//--------------------------------------------
		// if the fn is a relative path then it will start with folder/img.png or ./folder/img.png or img.png
		// we try to see if the file exists
		if (!fn.StartsWith("..") && !fn.StartsWith("/")) {
			String fn2 = fn;
			if (fn2.StartsWith("./")) // unlikely but possible it would start with "./"
				fn2 = fn2.Substring(2);

			String direct = Path.Combine(outputFolder, fn2);
			if (File.Exists(direct))
				return new FileInfo(direct);
		}

		//----------------------------------------------
		// Next test is to try to find the file relative to the outputFolder:
		fn = fn.TrimStart('/'); // just in case

		int ups = 0;
		while (fn.StartsWith("../")) {
			fn = fn.Substring(3);
			ups++;
		}

		if (ups > 0) {
			// give preference to the file equal to half, but check all files.
			int half = ups / 2; // word outputs twice the number of ../ required
			DirectoryInfo od = new DirectoryInfo(outputFolder);
			FileInfo altFile = null;
			int x = 0;
			while (od != null) {
				String exact = Path.Combine(od.FullName, fn);
				if (File.Exists(exact)) {
					if (x == half)
						return new FileInfo(exact);
					if (altFile == null)
						altFile = new FileInfo(exact);
				}

				od = od.Parent;
				x++;
			}

			if (altFile != null)
				return altFile;
		}
		//----------------------------------------------
		// 

		Hashtable ht = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		foreach (Environment.SpecialFolder sf in Enum.GetValues(typeof(Environment.SpecialFolder))) {
			String sp = Environment.GetFolderPath(sf);
			if (!String.IsNullOrEmpty(sp)) {
				String lfn = new DirectoryInfo(sp).Name;
				ht[lfn] = sp;
			}
		}

		fn = "/" + fn.TrimStart('/');
		FileInfo f = new FileInfo(fn);
		DirectoryInfo d = f.Directory;

		String p0 = null;
		String p1 = null;
		Stack s = new Stack();
		if (d == null) {
			String[] arr = fn.SplitAndTrim('/', '\\');
			for (int i = arr.Length - 1; i >= 1; i--)
				s.Push(arr[i]);

			if (arr.Length > 0) {
				p0 = arr[0];
				if (arr.Length > 1)
					p1 = arr[1];
			}
			else {
				p0 = "";
				if (warningMessages != null)
					warningMessages.Add("Cannot find root directory for img src: " + fnOrig);
			}
		}
		else {
			DirectoryInfo root = d.Root;
			if (root != null)
				p0 = d.Root.FullName;
			else {
				p0 = "";
				if (warningMessages != null)
					warningMessages.Add("Cannot find root directory for img src: " + fnOrig);
			}

			s.Push(f.Name);
			while (d != null) {
				s.Push(d.Name);
				if (d.Parent != null && d.Parent.Parent == null)
					break;
				d = d.Parent;
			}

			p1 = (String) ht[d.Name];
		}

		String p2 = outputFolder;
		bool first = true;
		while (s.Count > 0) {
			String v = (String) s.Pop();
			p0 = Path.Combine(p0, v);
			p2 = Path.Combine(p2, v);
			if (p1 != null) {
				if (!first) // ignore first otherwise path will be \Desktop\Desktop\
					p1 = Path.Combine(p1, v);
			}
			first = false;
		}

		if (p1 != null && File.Exists(p1))
			return new FileInfo(p1);

		if (File.Exists(p0))
			return new FileInfo(p0);

		if (File.Exists(p2))
			return new FileInfo(p2);

		try {
			var drives = DriveInfo.GetDrives();
			String[] arr = fn.Split('/');
			String fn3 = arr[arr.Length - 1];
			FileInfo file = null;
			for (int i = arr.Length - 2; i >= 0; i--) {
				fn3 = arr[i] + Path.DirectorySeparatorChar + fn3;
				foreach (var drive in drives) {
					String fn4 = Path.Combine(drive.ToString(), fn3);
					if (File.Exists(fn4))
						file = new FileInfo(fn4);
				}
			}
			// return the file with the longest path possible
			if (file != null)
				return file;
		} catch {}

		if (warningMessages != null)
			warningMessages.Add("Cannot find image file: " + fnUnescaped);

		return null;
	}

	static int imgId = 0;
	public static String ReplaceImgTokens(String content, String outputFolder, String htmlFilename, List<String> tempFiles, List<String> warningMessages) {
		String of = new DirectoryInfo(outputFolder).FullName; // for consistent back slashes (incase outputFolder has forward slashes)
		if (!outputFolder.EndsWith("" + Path.DirectorySeparatorChar))
			of = of + Path.DirectorySeparatorChar;

		String imageFolder = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(htmlFilename) + "_files");
		if (!Directory.Exists(imageFolder))
			Directory.CreateDirectory(imageFolder);

		StringBuilder sb = new StringBuilder();
		int x = 0;
		int y = 0;
		while ((x = content.IndexOf("<img", x, StringComparison.InvariantCultureIgnoreCase)) >= 0) {
			sb.Append(content.Substring(y, (x - y)));
			y = x;
			int endIndex = content.IndexOf(">", x);
			if (endIndex < 0)
				break;

			List<Token> tokens = GetTokens(content, x + 5, endIndex);
			Token id = null;
			Token style = null;
			foreach (var t in tokens) {
				if (String.Compare(t.Name, "id", true) == 0)
					id = t;
				else if (String.Compare(t.Name, "style", true) == 0)
					style = t;
				else if (String.Compare(t.Name, "src", true) == 0) {

					FileInfo src = FindTruePath(t.Value, outputFolder, warningMessages);

					if (src != null && File.Exists(src.FullName)) {
						bool b = src.FullName.StartsWith(of, StringComparison.InvariantCultureIgnoreCase);

						// image is not relative to the output path
						// thus it must be copied to a relative path so that
						// the chm can link it.
						if (!b) {
							String n = Path.GetFileNameWithoutExtension(src.Name);
							String ext = Path.GetExtension(src.Name);
							int k = 0;
							FileInfo dst = null;
							do {
								String nn = (k == 0 ? src.Name : n + k + ext);
								dst = new FileInfo(Path.Combine(imageFolder, nn));
								k++;
							} while (File.Exists(dst.FullName));
						
							tempFiles.Add(dst.FullName);
							File.Copy(src.FullName, dst.FullName);
							t.Value = dst.Directory.Name + "/" + dst.Name;
						}
						else {
							// the image file is located in the temporary output folder
							// which was probably put there by the Word to Html conversion
							tempFiles.Add(src.FullName);
						}
					}
				}
			}

			if (id == null) {
				id = new Token() { Name = "id", Value = "img" + imgId++, HasValue = true, QuotChar = "\"" };
				tokens.Add(id);
			}

			if (style == null) {
				style = new Token() { Name = "style", Value = "border:0", HasValue = true, QuotChar = "\"" };
				tokens.Add(style);
			}

			String id1 = id.Value;
			String id2 = id1 + "zoom";

			String img1 = ToString(tokens);
			id.Value = id2;
			style.Value += ";display:none";
			for (int i = tokens.Count - 1; i >= 0; i--) {
				var t = tokens[i];
				if (String.Compare(t.Name, "width", true) == 0 || String.Compare(t.Name, "height", true) == 0)
					tokens.RemoveAt(i);
			}

			String img2 = ToString(tokens);

			sb.AppendLine();
			sb.AppendLine(String.Format("<a href=\"javascript:zoomImage('{0}', '{1}')\">{2}</a>", id2, id1, img1));
			sb.AppendLine(String.Format("<a href=\"javascript:zoomImage('{0}', '{1}')\">{2}</a>", id1, id2, img2));

			//sb.Append("<img");
			//foreach (var t in tokens) {
			//	sb.Append(" ").Append(t.ToString());
			//}
			//sb.Append(">");
			x = endIndex + 1;
			y = x;
		}

		sb.Append(content.Substring(y));
		return sb.ToString();
	}

	private static List<Token> GetTokens(String content, int startIndex, int endIndex) {
		List<Token> tokens = new List<Token>();
		GetTokens(content, startIndex, endIndex, tokens);
		Token alt = null;
		foreach (var t in tokens) {
			if (String.Compare(t.Name, "alt", true) == 0) {
				alt = t;
				break;
			}
		}

		if (alt != null) {
			String[] arr = alt.Value.Trim('"').Split('\n');
			StringBuilder sb = new StringBuilder();
			List<Token> altTokens = new List<Token>();
			for (int i = 0; i < arr.Length; i++) {
				if (arr[i].IndexOf('=') >= 0) {
					GetTokens(arr[i], 0, arr[i].Length, altTokens);
				}
				else {
					if (sb.Length > 0)
						sb.Append("\n");
					sb.Append(arr[i]);
				}
			}

			alt.Value = sb.ToString();

			// tokens might contain duplicates (because of alt)
			// this code removes the duplicates and preserves the original order
			// since the 'alt' tokens are processed last, they take precedence and
			// overwrite earlier tokens
			Hashtable ht = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
			foreach (var t in tokens)
				ht[t.Name] = t;

			// if the user specifies a width, then remove the existing height value
			// if the user specifies a height, then remove the existing width value
			foreach (var t in altTokens) {
				if (String.Compare(t.Name, "width", true) == 0)
					ht.Remove("height");
				else if (String.Compare(t.Name, "height", true) == 0)
					ht.Remove("width");
			}

			if (alt.Value.Length == 0)
				ht.Remove(alt.Name);

			tokens.AddRange(altTokens);
			foreach (var t in altTokens)
				ht[t.Name] = t;

			List<Token> tokens2 = new List<Token>();
			foreach (var t in tokens) {
				Token t2 = (Token) ht[t.Name];
				if (t2 != null) {
					tokens2.Add(t2);
					ht.Remove(t.Name);
				}
			}
			tokens = tokens2;
		}

		return tokens;
	}

	private class Token {
		public String Name = "";
		public String Value = "";
		public String QuotChar = ""; // not all tokens have quot chars, e.g. width=123
		public bool HasValue = true;

		public override String ToString() {
			if (!HasValue)
				return Name;

			return Name + "=" + QuotChar + Value + QuotChar;
		}
	}


	private static void GetTokens(String content, int startIndex, int endIndex, List<Token> tokens) {

		bool inside = false;
		bool pendingValue = false;
		StringBuilder sbName = new StringBuilder();
		StringBuilder sbValue = new StringBuilder();
		String quoteChar = "";
		bool hasWS = false;

		for (int i = startIndex; i < endIndex; i++) {
			char c = content[i];
			if (c == '"' || c == '\'') {
				quoteChar = "" + c;
				inside = !inside;
				//ht[sbName.ToString()] = sbValue.ToString();
				if (!inside) {
					tokens.Add(new Token() { Name = sbName.ToString(), Value = sbValue.ToString(), HasValue = pendingValue, QuotChar = quoteChar });
					quoteChar = "";
					sbName.Length = 0;
					sbValue.Length = 0;
					pendingValue = false;
					hasWS = false;
				}
			}
			else {
				if (inside) {
					sbValue.Append(c);
				}
				else if (pendingValue) {
					if (Char.IsWhiteSpace(c)) {
						if (sbValue.Length > 0) {
							tokens.Add(new Token() { Name = sbName.ToString(), Value = sbValue.ToString(), HasValue = pendingValue, QuotChar = quoteChar });
							pendingValue = false;
							sbName.Length = 0;
							sbValue.Length = 0;
							hasWS = false;
						}
					}
					else
						sbValue.Append(c);
				}
				else {
					if (c == '=') {
						pendingValue = true;
						quoteChar = "";
						hasWS = false;
					}
					else {
						if (Char.IsWhiteSpace(c)) {
							if (sbName.Length > 0)
								hasWS = true;
						}
						else {
							// e.g. <tag a b c />
							if (hasWS) {
								tokens.Add(new Token() { Name = sbName.ToString(), Value = sbValue.ToString(), HasValue = pendingValue, QuotChar = quoteChar });
								pendingValue = false;
								sbName.Length = 0;
								sbValue.Length = 0;
								hasWS = false;
							}

							sbName.Append(c);
						}
					}
				}
			}
		}

		if (sbName.Length > 0) {
			//ht[sbName.ToString()] = sbValue.ToString();
			tokens.Add(new Token() { Name = sbName.ToString(), Value = sbValue.ToString(), HasValue = pendingValue, QuotChar = quoteChar });
		}
	}

	private static bool isAnchors(String s) {
		StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
		return String.Compare(s, "Anchors", true) == 0 || s.StartsWith("Anchors.", sc);
	}

	private static bool isIndexEntries(String s) {
		StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
		return String.Compare(s, "IndexEntries", true) == 0 || s.StartsWith("IndexEntries.", sc);
	}

	public static String TokensToString(AdvancedOptions options, IEnumerable<CHMNode> nodes) {
		char splitChar = options.TokenSplitChar.Length > 0 ? options.TokenSplitChar[0] : ',';

		String[] tokens = options.OutputTokens.SplitAndTrim(splitChar);
		char quot = options.OutputQuoteChar.Length > 0 ? options.OutputQuoteChar[0] : '"';
		char literalChar = options.TokenLiteralChar.Length > 0 ? options.TokenLiteralChar[0] : '"';
		String delim = options.OutputDelimChar.Replace("<tab>", "\t").Replace("<space>", " ").Replace("<none>", "");
		String newline = options.OutputNewline.Replace(@"\n", "\n").Replace(@"\r", "\r").Replace(@"\t", "\t");
		String tlc = "" + options.TokenLiteralChar;
		QuoteMode qm = options.QuoteMode;

		bool hasAnchors = tokens.Where(s => isAnchors(s)).FirstOrDefault() != null;
		bool hasIndexEntries = tokens.Where(s => isIndexEntries(s)).FirstOrDefault() != null;

		if (tokens.Length == 0)
			return String.Empty;

		String q1 = "" + quot;
		String q2 = q1 + q1;

		StringBuilder sb = new StringBuilder();
		if (options.OutputHeaderRow) {
			for (int i = 0; i < tokens.Length; i++) {
				if (i > 0)
					sb.Append(delim);

				sb.Append(tokens[i]);
			}
			sb.Append(newline);
		}

		foreach (var n in nodes) {

			int jn = 1;
			if (hasAnchors && n.Anchors.Count > jn)
				jn = n.Anchors.Count;
			if (hasIndexEntries && n.IndexEntries.Count > jn)
				jn = n.IndexEntries.Count;

			for (int j = 0; j < jn; j++) {
				bool hasContent = false;
				StringBuilder sb2 = new StringBuilder();
				for (int i = 0; i < tokens.Length; i++) {
					if (i > 0)
						sb2.Append(delim);

					String t = tokens[i];
					bool isLiteral = t.StartsWith(tlc) && t.EndsWith(tlc);

					Object o = null;
					if (isLiteral)
						o = t.Trim(literalChar);
					else {
						if (String.Compare(t, "Anchors", true) == 0)
							o = (j < n.Anchors.Count ? n.Anchors[j].Anchor : null);
						else if (isIndexEntries(t)) {
							if (j < n.IndexEntries.Count) {
								String prop = (t + ".").Split('.')[1];
								if (prop.Length == 0 || String.Compare(prop, "Name", true) == 0)
									o = n.IndexEntries[j].Name;
								else if (String.Compare(prop, "UncleanName", true) == 0)
									o = n.IndexEntries[j].UncleanName;
								else if (String.Compare(prop, "DisplayName", true) == 0)
									o = n.IndexEntries[j].DisplayName;
								else
									o = n.IndexEntries[j].tokens[prop];
							}
						}
						else
							o = n.Tokens[t];
					}

					String val = (o == null ? "" : o.ToString());
					if (val.Length > 0)
						hasContent = true;

					bool needsQuotes = false;
					if (qm == QuoteMode.Always)
						needsQuotes = true;
					else if ((qm == QuoteMode.Normal || !isLiteral && qm == QuoteMode.NormalNeverLiterals) && (val.IndexOf(quot) >= 0 || val.Contains(newline) || delim.Length > 0 && val.Contains(delim)))
						needsQuotes = true;

					if (needsQuotes)
						val = q1 + val.Replace(q1, q2) + q1;

					sb2.Append(val);
				}

				if (hasContent) {
					sb.Append(sb2);
					sb.Append(newline);
				}
			}
		}

		return sb.ToString();
	}

	public String GetCSharpConstantDefinitionsCode(String @namespace, String className, FileInfo chmFile) {
		String pn = "", pu = "";
		constants.Sort();

		StringBuilder sb3 = new StringBuilder();
		sb3.AppendLine("using System;");
		sb3.AppendLine("using System.Windows.Forms;");
		sb3.AppendLine();
		sb3.AppendLine("namespace " + @namespace + " {");
		sb3.AppendLine("public static class " + className + " {");

		sb3.AppendLine();
		sb3.AppendLine("\tpublic static readonly String CHMFile = \"" + chmFile.Name + "\";"); // don't use FullName, just Name

		foreach (Constant c in constants) {
			if (String.Compare(c.name, pn, true) == 0) { // prevent duplicate entries
				if (String.Compare(c.url, pu, true) != 0) {
					throw new Exception("URL mismatch: " + c.name + " " + pu + " " + c.url);
				}
			}
			else
				sb3.AppendLine(c.ToString());

			pn = c.name; // previous name
			pu = c.url;  // previous url
		}

		sb3.AppendLine();
		sb3.AppendLine("\tpublic static void ShowHelp(Form f, String constant) {");
		sb3.AppendLine("\t\tHelp.ShowHelp(f, CHMFile, HelpNavigator.Topic, constant);");
		sb3.AppendLine("\t}");

		sb3.AppendLine("}");
		sb3.AppendLine("}");
		return sb3.ToString();
	}

	public void UpdateHeadersAndFooters(bool LinkBlankHeaders, List<String> warningMessages) {
		Hashtable htParent = new Hashtable(); // ht[CHMNode] = CHMNode ... except for special case,
		htParent[""] = Nodes; // special case to store root level nodes

		Hashtable htNode = CreateNodeHashtable(this); // ht[filename] = List<CHMNode>

		List<String> unresolvedLinks = (warningMessages == null ? null : new List<String>());

		UpdateHeadersAndFooters(htNode, this, Nodes, null, htParent, new Hashtable(), LinkBlankHeaders, warningMessages, unresolvedLinks);

		if (unresolvedLinks != null && unresolvedLinks.Count > 0) {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("The following hyperlinks cannot be resolved, but may resolve when another document is added:");
			foreach (String ul in unresolvedLinks)
				sb.Append("\n").Append(ul);

			warningMessages.Add(sb.ToString());
		}

		if (warningMessages != null) {
			// check anchors for duplicates
			Hashtable ht = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
			foreach (CHMNode n in this.GetAllNodes()) {
				foreach (var a in n.Anchors) {
					String b = a.Anchor.TrimStart('#');
					List<CHMNode> nodes = (List<CHMNode>) ht[b];
					if (nodes == null) {
						nodes = new List<CHMNode>();
						ht[b] = nodes;
					}
					nodes.Add(n);
				}
			}

			StringBuilder sb = new StringBuilder();
			foreach (DictionaryEntry de in ht) {
				String anchor = (String) de.Key;
				List<CHMNode> nodes = (List<CHMNode>) de.Value;
				if (nodes.Count > 1) {
					sb.AppendLine("Duplicate anchor '{0}' found in these topics:".Format2(anchor));
					foreach (var n in nodes)
						sb.AppendLine(n.Name + " : " + Path.GetFileName(n.SourceFile));
				}
			}

			if (sb.Length > 0)
				warningMessages.Add(sb.ToString());
		}
	}

	private static void UpdateHeadersAndFooters(Hashtable nodeHT, CHMDocument doc, List<CHMNode> list, CHMNode parent, Hashtable htParent, Hashtable htPrev, bool LinkBlankHeaders, List<String> warningMessages, List<String> unresolvedLinks) {
		for (int i = 0; i < list.Count; i++) {
			CHMNode n = list[i];

			htParent[n] = parent;

			CHMNode nextNode = GetNextNode(n, htParent, LinkBlankHeaders);

			if (nextNode != null)
				htPrev[nextNode] = n; // the next node's previous node is n

			CHMNode prevNode = (CHMNode) htPrev[n];

			UpdateHeadersAndFooters(nodeHT, doc, n.ChildNodes, n, htParent, htPrev, LinkBlankHeaders, warningMessages, unresolvedLinks);

			Settings s = n.Settings;
			String header = "";
			String footer = "";
			if (s != null) {
				if (s.UseMiddleHeaderFooterAlways) {
					header = s.HtmlHeader;
					footer = s.HtmlFooter;
				}
				else {
					if (nextNode == null && prevNode == null) {
						header = s.HtmlHeaderFirstPage;
						footer = s.HtmlFooterLastPage;
					}
					else {
						if (prevNode == null) {
							header = s.HtmlHeaderFirstPage;
							footer = s.HtmlFooterFirstPage;
						}
						else if (nextNode == null) {
							header = s.HtmlHeaderLastPage;
							footer = s.HtmlFooterLastPage;
						}
						else {
							header = s.HtmlHeader;
							footer = s.HtmlFooter;
						}
					}
				}
			}

			String text = File.ReadAllText(n.Local, n.WriteEncoding);
			bool linksChanged = false;
			text = UpdateHyperlinks(nodeHT, text, doc.constants, unresolvedLinks, out linksChanged);

			int h1 = text.IndexOf(HEADER_OPEN);
			int h2 = text.IndexOf(HEADER_CLOSE);

			int f1 = text.IndexOf(FOOTER_OPEN);
			int f2 = text.IndexOf(FOOTER_CLOSE);

			String next = (nextNode == null ? "#" : nextNode.Local);
			String prev = (prevNode == null ? "#" : prevNode.Local);
			header = header.Replace("[next]", next, StringComparison.InvariantCultureIgnoreCase);
			header = header.Replace("[prev]", prev, StringComparison.InvariantCultureIgnoreCase);
			footer = footer.Replace("[next]", next, StringComparison.InvariantCultureIgnoreCase);
			footer = footer.Replace("[prev]", prev, StringComparison.InvariantCultureIgnoreCase);

			StringBuilder sb = new StringBuilder();
			int x = 0;
			if (h1 >= 0 && h2 >= 0) {
				sb.Append(text.Substring(0, h1 + HEADER_OPEN.Length)).Append(Environment.NewLine);
				sb.Append(header).Append(Environment.NewLine);
				x = h2;
			}

			if (f1 >= 0 && f2 >= 0) {
				sb.Append(text.Substring(x, (f1 + FOOTER_OPEN.Length) - x)).Append(Environment.NewLine);
				sb.Append(footer).Append(Environment.NewLine);
				x = f2;
			}

			// if x is still zero that means h1/h2 and f1/f2 weren't found, in which case there
			// is no reason to save the file because nothing was changed
			if (x > 0) {
				sb.Append(text.Substring(x));
				String newText = sb.ToString();
				if (linksChanged || !newText.Equals(text))
					File.WriteAllText(n.Local, newText, n.WriteEncoding);
			}
		}
	}

	private static CHMNode GetNextNode(CHMNode n, Hashtable htParent, bool LinkBlankHeaders) {
		if (!LinkBlankHeaders) {
			if (n.Name.Length == 0)
				return null;

			CHMNode n2 = n;
			while (n2 != null) {
				if (n2.Name.Length == 0)
					return null;

				n2 = (CHMNode) htParent[n2];
			}
		}

		for (int i = 0; i < n.ChildNodes.Count; i++) {
			if (n.ChildNodes[i].Name.Length > 0 || LinkBlankHeaders)
				return n.ChildNodes[i];
		}

		while (n != null) {
			CHMNode p = (CHMNode) htParent[n];
			List<CHMNode> children = null;
			if (p == null)
				children = (List<CHMNode>) htParent[""]; // special case: root level nodes
			else
				children = p.ChildNodes;

			// determine of the index of n within the parent
			int i = 0;
			for (; i < children.Count; i++) {
				if (n == children[i])
					break;
			}

			CHMNode next = null;
			// if LinkBlankHeaders, then next is the i+1 child if not at the end of the array
			// otherwise find the next child that has a non-blank Name
			i++; // i must be incremented, otherwise same node is returned
			for (; i < children.Count; i++) {
				if (LinkBlankHeaders || children[i].Name.Length > 0) {
					next = children[i];
					break;
				}
			}

			// next == null means at the end of the array or all siblings are blank
			// thus, go up to the parent.
			if (next == null)
				n = p;
			else
				return next; // otherwise we found the next link
		}
		return null;
	}

	private class Constant : IComparable {
		public String name;
		public String url;
		public override String ToString() {
 			return "\tpublic static readonly String " + name + " = \"" + url + "\";";
		}

		public int CompareTo(object obj) {
			int x = String.Compare(name, ((Constant) obj).name, true);
			if (x != 0) return x;
			return String.Compare(url, ((Constant) obj).url, true);
		}
	}

	// the name is cleaned to acount for names like: Category - Topic -> Category_Topic
	// only digits, letters and underscore is allowed
	private static String CleanVariableName(String name) {
		StringBuilder sb = new StringBuilder();
		foreach (char c in name) {
			if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')
				sb.Append(c);
			else
				sb.Append('_');
		}
		name = sb.ToString().Clean("_", '_');
		return name;
	}

	private static Constant CSharpConstant(String fn, String href) {
		fn = Path.GetFileName(fn); // remove path information
		String var = CleanVariableName(Path.GetFileNameWithoutExtension(fn));
		if (String.IsNullOrEmpty(href))
			return new Constant() { name = var, url = fn };

		String x = "_" + CleanVariableName(href).TrimStart('_');
		return new Constant { name = var + x, url = fn + "#" + href.TrimStart('#') };
	}

	// finds all <a name="#name" tags and retuns a list of the #name.
	private static List<String> ExtractAnchorNames(String s) {
		List<String> list = new List<String>();
		int x = 0;
		// could be <a href= or <a name=
		while ((x = s.IndexOf("<a", x, StringComparison.InvariantCultureIgnoreCase)) >= 0) {
			int y = s.IndexOf("name=", x, StringComparison.InvariantCultureIgnoreCase);
			if (y == -1)
				break;

			// make sure all characters between <a and name are whitespace
			bool allWhitespace = true;
			for (int i = x + 2; i < y; i++) {
				char c = s[i];
				if (!Char.IsWhiteSpace(c)) {
					allWhitespace = false;
					break;
				}
			}

			if (allWhitespace) {
				//int z = s.IndexOf("\"", y + 7, StringComparison.InvariantCultureIgnoreCase);
				int z = s.IndexOf(">", y + 7, StringComparison.InvariantCultureIgnoreCase);
				String name = s.Substring(y + 5, z - (y + 5));
				name = name.Trim('"');
				if (name.Length > 0)
					list.Add(name);

				x = z;
			}
			else {
				x = x + 1; 
			}
		}

		return list;
	}

	private static List<CHMIndexEntry> GetIndexEntries(String filename, StringComparison sc, Settings settings, ref String content, ref bool indexingOn, List<String> warningMessages) {
		var list = new List<CHMIndexEntry>();

		content = content.Replace("IndexingOffIndexingOff", "~IOFFx2!", sc);
		content = content.Replace("IndexingOnIndexingOn", "~IONx2!", sc);

		StringBuilder sb = new StringBuilder();
		int x = 0;
		while (true) {
			int i1 = content.IndexOf('{', x);
			int i2 = content.IndexOf("IndexingOff", x, sc);
			int i3 = content.IndexOf("IndexingOn", x, sc);

			int k1 = (i1 < 0 ? int.MaxValue : i1);
			int k2 = (i2 < 0 ? int.MaxValue : i2);
			int k3 = (i3 < 0 ? int.MaxValue : i3);

			if (k1 < k2 && k1 < k3) {
				int i1b = content.IndexOf('}', i1+1);
				if (i1b > 0) {
					// extract index entry
					String txt = content.Substring(i1, (i1b - i1) + 1);
					CHMIndexEntry ie = ToIndexEntry(txt);
					String[] keywords = CHMIndexEntry.Split(ie.Keywords, settings.KeywordsDelim.ToCharArray());
					if (indexingOn || keywords.Length > 0) {
						if (ie.DisplayName == null && warningMessages != null && keywords.Length == 0)
							warningMessages.Add("Index text not formatted correctly: " + txt);
						sb.Append(content.Substring(x, (k1 - x)));
						sb.Append(ie.UncleanName);
						ie.Local = filename;
						list.Add(ie);
					}
					else {
						sb.Append(content.Substring(x, (i1b - x) + 1));
					}
					x = i1b+1;
				}
				else { // no more close } brackets
					break;
				}
			}
			else if (k2 < k1 && k2 < k3) { // IndexingOff found next
				indexingOn = false;
				sb.Append(content.Substring(x, k2 - x));
				x = k2 + 11;
			}
			else if (k3 < k1 && k3 < k2) { // IndexingOn found next
				indexingOn = true;
				sb.Append(content.Substring(x, k3 - x));
				x = k3 + 10;
			}
			else {
				break;
			}
		}

		sb.Append(content.Substring(x));
		content = sb.ToString();
		content = content.Replace("~IOFFx2!", "IndexingOff", sc);
		content = content.Replace("~IONx2!", "IndexingOn", sc);

		return list;
	}

	public static CHMIndexEntry ToIndexEntry(String s) {
		var ie = new CHMIndexEntry();
		s = s.Replace("&quot;", "\"");

		s = s.Trim('{', '}').Trim();
		// use a clean name to detect '=' because s might be equal to: <span style='color:red'>a<b>bo</b></span><b>u</b>t}
		// in which case an '=' is detected because it's inside a span tag.
		String t = CleanIndexName(s);

		//String s = "{  <span style='color:red'>a<b>b<u>o</u></b></span><b><u>u</u></b><u>t  Te</u>xt  =   \"bl<u>ah\" B</u>B  }";
		// In the above example, there is a lot of formatting going on inside an index tag.
		// name = about
		// unclean name = <span style'color:red'>a<b>b<u>o</u></b></span><b><u>u</u></b><u>t</u><u></u>
		// sbTags = </u><u></u>
		// tokens:
		//		Text = "blah"
		//		BB = ""
		//
		// As you can see, the formatting tags, <u>, </u>, <b>, etc, are pulled out of the name and values and then appended to
		// the end of the Unclean name.  If this was not done then the last <u> in abou<u>t would not be closed and the rest
		// of the page would appear underlined.
		StringBuilder sbTags = new StringBuilder();
		if (t.IndexOf('=') < 0) {
			ie.Name = s.Trim().Replace("\r\n", " ").Replace("\n", " ").Clean();
		}
		else {
			StringBuilder sbName = new StringBuilder();
			StringBuilder sbValue = new StringBuilder();
			int angle = 0;
			Hashtable ht = ie.tokens;
			bool insideQuote = false;
			for (int i = 0; i <= s.Length; i++) {
				char c = (i < s.Length ? s[i] : '"');
				if (Char.IsWhiteSpace(c))
					c = ' ';

				if (c == '<') angle++;
				else if (c == '>') angle--;

				if (c == '"') {
					insideQuote = !insideQuote;
					if (!insideQuote || i == s.Length) {
						String n = sbName.ToString();
						String v = sbValue.ToString();
						if (ht.Count == 0) {
							int y = n.Trim().LastIndexOf(' ');
							if (y >= 0) {
								ie.Name = n.Substring(0, y).Trim();
								n = n.Substring(y+1);
							}
						}

						sbTags.Append(Tags(n));
						sbTags.Append(Tags(v));

						ht[CleanIndexName(n.Trim())] = CleanIndexName(v.Clean()).UnescapeHtml();
						sbName.Length = 0;
						sbValue.Length = 0;
					}
				}
				else {
					if (insideQuote)
						sbValue.Append(c);
					else if (c != '=' || angle != 0)
						sbName.Append(c);
				}
			}
		}

		// ie.Name is null when just the Text attribute is used, e.g. {Text="Index Entry"}
		if (ie.Name != null) {
			ie.UncleanName = ie.Name + sbTags;
			ie.Name = CleanIndexName(ie.Name);
		}

		return ie;
	}

	// removes anything surrounded by two angle brackets, e.g. <b><i>Text -> Text
	// as well, the clean name unescapes any html so it appears properly in the index
	private static String CleanIndexName(String name) {
		StringBuilder sb = new StringBuilder();

		int angle = 0;
		for (int i = 0; i < name.Length; i++) {
			char c = name[i];
			if (c == '<')
				angle++;
			else if (c == '>')
				angle--;
			else {
				if (angle == 0)
					sb.Append(c);
			}
		}
		return sb.ToString().Clean().UnescapeHtml();
	}

	// only get the text inside angle brackets
	private static String Tags(String name) {
		StringBuilder sb = new StringBuilder();

		int angle = 0;
		for (int i = 0; i < name.Length; i++) {
			char c = name[i];
			if (c == '<')
				angle++;
			else if (c == '>')
				angle--;

			if (c == '<' || c == '>' || angle != 0)
				sb.Append(c);
		}
		return sb.ToString();
	}

	private static String GetFilename(String x, int k) {
		if (k <= 1) return x + ".html";
		return x + "-" + k + ".html";
	}

	private static Hashtable GetCHMTokens(ref String htext, out bool clean) {
		StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
		clean = true;
		Hashtable ht = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		int x = htext.LastIndexOf("[chm", sc);
		if (x < 0) return ht;
		// it happened that [CHM\r\n thus, indexof("[chm ") return -1.  
		if (x + 4 >= htext.Length || !Char.IsWhiteSpace(htext[x+4]))
			return ht;

		clean = false;
		bool insideQuote = false;
		StringBuilder sbName = new StringBuilder();
		StringBuilder sbValue = new StringBuilder();

		// Header1 &quot;blah&quot;[chm InvariantName="[text]&quot;]
		// Header1 "blah"[chm InvariantName="[text]"]
		char xx = '\0';
		String htext2 = htext.Replace("&quot;", xx.ToString(), sc);
		int y = htext2.LastIndexOf("[chm", sc);
		int sqIndex = -1;

		for (int i = y + 5; i < htext2.Length; i++) {
			char c = htext2[i];
			if (c == '"' || c == xx) {
				insideQuote = !insideQuote;
				if (!insideQuote) {
					String n = sbName.ToString().Trim();
					if (n.Length > 0)
						ht[n] = sbValue.ToString().Trim();

					sbName.Length = 0;
					sbValue.Length = 0;
				}
			}
			else {
				if (insideQuote)
					sbValue.Append(c);
				else {
					if (c == ']') {
						sqIndex = i;
						break;
					}
					else if (!Char.IsWhiteSpace(c) && c != '=')
						sbName.Append(c);
				}
			}
		}

		if (sqIndex >= 0) {
			clean = true;
			htext2 = htext2.Substring(0, y).Trim() + htext2.Substring(sqIndex + 1);
			htext = htext2.Replace(xx.ToString(), "&quot;", sc);
		}

		return ht;
	}

	private static String InnerName(String html) {
		int angle = 0;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < html.Length; i++) {
			char c = html[i];
			if (c == '<') angle++;
			else if (c == '>') angle--;
			else if (angle == 0) {
				sb.Append(c);
			}
		}

		// MS-Word saves Chinese text as ampersand values, e.g. &#27721;&#35821;&#27721
		// So we convert them back to true chars so they show up properly in the treeview
		String s = sb.ToString().UnescapeHtml();

		s = s.Replace("\r\n", " ").Replace("\n", " ").Trim();
		int x = s.IndexOf("[chm", StringComparison.InvariantCultureIgnoreCase);
		if (x >= 0)
			s = s.Substring(0, x).Trim();

		s = s.Clean().Trim();
		//s = s.Replace("&nbsp;", " ", StringComparison.InvariantCultureIgnoreCase).Clean().Trim();
		//s = s.Replace("&quot;", "\"", StringComparison.InvariantCultureIgnoreCase);
		//s = s.Replace("&amp;", "&", StringComparison.InvariantCultureIgnoreCase);
		s = s.Replace("–", "-"); // replace the extra long dash with a regular dash (otherwise it shows up funny in the TOC)

		return s;
	}

	private static int HTag(String s, int startIndex, StringComparison sc, String[] harr, out String htag) {
		htag = null;
		int minIndex = int.MaxValue;
		foreach (String h in harr) {
			int index = s.IndexOf(h, startIndex, sc);
			if (index != -1 && index < minIndex) {
				minIndex = index;
				htag = h;
			}
		}

		return (minIndex == int.MaxValue ? -1 : minIndex);
	}

	private static int Level(String htag) {
		return int.Parse(htag.Substring(2, htag.Length - 2));
	}

	private static String HCTag(String htag, String[] harr, String[] hcarr) {
		for (int i = 0; i < harr.Length; i++) {
			if (String.Compare(harr[i], htag, true) == 0)
				return hcarr[i];
		}
		return null;
	}

	public List<CHMNode> GetAllNodes() {
		List<CHMNode> list = new List<CHMNode>();
		GetNodes(list, Nodes);
		return list;
	}

	private static void GetNodes(List<CHMNode> list, List<CHMNode> list2) {
		foreach (CHMNode node in list2) {
			list.Add(node);
			GetNodes(list, node.ChildNodes);
		}
	}

	private static String ReplaceFontSizes(String s) {
		int x = 0;
		int y = 0;
		StringBuilder sb = new StringBuilder();
		while ((y = s.IndexOf("font-size", x, StringComparison.InvariantCultureIgnoreCase)) >= 0) {

			int x1 = s.IndexOf(";", y);
			int x2 = s.IndexOf("\"", y);
			int x3 = s.IndexOf(":", y);
			if (x1 == -1 && x2 == -1) break;

			int x4 = 0;
			if (x1 == -1) x4 = x2;
			else if (x2 == -1) x4 = x1;
			else x4 = Math.Min(x1, x2);

			if (x3 == -1 || x3 > x4) break;

			//xx-small, x-small, small, medium, large, x-large, xx-large
			String f = s.Substring(x3 + 1, x4 - (x3 + 1));
			f = f.Replace("pt", "").Replace("px", "");
			float fs = 0;
			if (float.TryParse(f, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out fs)) {
				sb.Append(s.Substring(x, x3 + 1 - x));
				if (fs <= 5)
					sb.Append("xx-small");
				else if (fs <= 6)
					sb.Append("x-small");
				else if (fs <= 10)
					sb.Append("small");
				else if (fs <= 12)
					sb.Append("medium");
				else if (fs <= 14)
					sb.Append("large");
				else if (fs <= 16)
					sb.Append("x-large");
				else
					sb.Append("xx-large");

				x = x4;
			}
			else {
				sb.Append(s.Substring(x, y + 1 - x));
				x = y + 1;
			}
		}
		sb.Append(s.Substring(x));
		return sb.ToString();
	}
}

public class CHMAppendResult {
	public List<CHMNode> RootNodes { get; set; }
	public List<CHMNode> AllNodes { get; set; }
}

}