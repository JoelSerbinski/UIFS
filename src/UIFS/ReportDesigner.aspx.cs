using System;
using System.Collections.Generic;
using System.Web;

namespace UIFS
{
    public partial class ReportDesigner : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Need to set to initialize and keep session active for ajax calls, other windows, etc.
            Session["KeepAlive"] = "HI.YA!";

        }
    }
}