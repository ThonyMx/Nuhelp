using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Opulos.NüHelp.CHM {
public class CHMCategory : IComparable {
	public String Name;
	public String Local;
	public List<CHMIndexEntry> Entries = new List<CHMIndexEntry>();
	public List<CHMCategory> Children = new List<CHMCategory>();

	public override bool Equals(object obj) {
		return Name.Equals(((CHMCategory) obj).Name);
	}

	public override int GetHashCode() {
		return Name.GetHashCode();
	}

	public int CompareTo(object obj) {
		if (obj is CHMCategory)
			return Name.CompareTo(((CHMCategory) obj).Name);
		return Name.CompareTo(((CHMIndexEntry) obj).DisplayName);
	}

	public override String ToString() {
		return Name + (!String.IsNullOrEmpty(Local) ? ":" + Local : "");
	}
}
}
