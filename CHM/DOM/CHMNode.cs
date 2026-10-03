using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Opulos.NüHelp.CHM {

public class CHMNode {

	public List<CHMNode> ChildNodes { get; set; }
	public String Name { get; set; }
	public String ImageNumber { get; set; }
	public String Local { get; set; }
	//public bool IsTempFile { get; set; }
	public List<CHMIndexEntry> IndexEntries { get; set; }
	public int Level { get; set; }

	public Settings Settings { get; set; } // cache the header and footer
	public Encoding WriteEncoding { get; set; } // rewriting the headers requires knowing the encoding used (easier to cache it)
	public String SourceFile { get; set; } // required to support hyperlinks across multiple documents
	public List<CHMAnchor> Anchors { get; set; }

	// list of [CHM ... ] tokens that are defined in the header.  E.g. [CHM ImageNumber="10" TopicId="1000"]
	public Hashtable Tokens { get; set; }
	public FileInfo OriginalFile { get; set; } // only set for the first node of an inserted document
	public FileInfo OriginalFile2 { get; set; } // assigned to all nodes

	public CHMNode() {
		ChildNodes = new List<CHMNode>();
		IndexEntries = new List<CHMIndexEntry>();
		Anchors = new List<CHMAnchor>();
		Tokens = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	}

	public String InvariantName {
		get {
			return (String) Tokens["InvariantName"];
		}
	}

	public String InvariantNameThenName {
		get {
			String n = InvariantName;
			if (!String.IsNullOrEmpty(n))
				return n;
			return Name;
		}
	}

	public override String ToString() {
		return Name + ":" + Local;
	}
}

public class CHMAnchor {

	public String Anchor { get; set; }
	public bool InHeader { get; set; }

}


}