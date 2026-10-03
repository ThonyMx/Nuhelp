using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Opulos.NüHelp.CHM {
public class CHMIndexEntry : IComparable {
	public Hashtable tokens = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

	public String Name;
	public String UncleanName; // contains <b><i> tags
	public String Text { get { return (String) tokens["Text"]; } set { tokens["Text"] = value; }}
	public String SeeAlso { get { return (String) tokens["SeeAlso"]; } set { tokens["SeeAlso"] = value; }}
	public String Local { get { return (String) tokens["Local"]; } set { tokens["Local"] = value; }}
	public String Categories { get { return "" + (String) tokens["Categories"]; } set { tokens["Categories"] = value; }}
	public String Bookmark { get { return (String) tokens["Bookmark"]; } set { tokens["Bookmark"] = value; }}
	public String Keywords {
		get {
			return (String) tokens["Keywords"];
		}

		set {
			tokens["Keywords"] = value;
		}
	}

	public static String[] Split(String s, char[] delims) {
		if (String.IsNullOrEmpty(s))
			return new String[0];

		for (int i = 0; i < delims.Length; i++) {
			char d = delims[i];
			String dd = "" + d + d;
			s = s.Replace(dd, ((char) i).ToString());
		}

		String[] arr = s.Split(delims, StringSplitOptions.RemoveEmptyEntries);
		List<String> list = new List<String>();
		for (int i = 0; i < arr.Length; i++) {
			String s2 = arr[i];
			for (int j = 0; j < delims.Length; j++) {
				char d = delims[j];
				s2 = s2.Replace((char) j, d);
			}
			s2 = s2.Trim();
			if (s2.Length > 0)
				list.Add(s2);
		}

		return list.ToArray();
	}


	public String DisplayName {
		get {
			String name = (String.IsNullOrEmpty(Text) ? Name : Text);
			// could possibly add a config option at the top of the word document
			// SeeAlsoFormat = "(see also @SeeAlso)"
			if (String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(SeeAlso))
				name = "(see also " + SeeAlso + ")";

			return name;
		}
	}

	public int CompareTo(Object obj) {
		if (obj is CHMIndexEntry) {
			CHMIndexEntry ie2 = (CHMIndexEntry) obj;
			return this.DisplayName.CompareTo(ie2.DisplayName);
		}
		return this.DisplayName.CompareTo(((CHMCategory) obj).Name);
	}

	public override String ToString() {
		return DisplayName + ":" + Local + (Categories.Length > 0 ? " Categories=" + Categories : "");
	}
}
}
