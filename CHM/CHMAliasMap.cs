using System;
using System.Collections.Generic;
using System.Text;

namespace Opulos.NüHelp.CHM {
///<summary>
///The Map and Alias sections must be set if you want to create a Context ID to URL mapping. Used when the
///application opens help using the HH_HELP_CONTEXT help command. ie. For opening help using an HelpContext / integer ID.
///E.g. HtmlHelp(0, "c:\app\help\Help.chm", HH_HELP_CONTEXT, 1001);
///The [MAP] section defines the IDs.
///The [ALIAS] section defines the mapping between ID and help topic.
///</summary>
public class CHMAliasMap {

	///<summary>E.g. IDH_HomePage</summary>
	public String Alias;

	///<summary>E.g. 1000</summary>
	public int ID;

	///<summary>E.g. default.html</summary>
	public String URL;

	///<summary>Optional</summary>
	public String Comment;
}
}
