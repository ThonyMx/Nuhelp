using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Opulos.NüHelp.CHM {
public class CHMFont {

	public String FamilyName { get; set; }
	public int Size { get; set; }
	public byte CharSet { get; set; }

	public override String ToString() {
		return FamilyName + "," + Size + "," + CharSet;
	}
}
}
