using System;
using System.Collections.Generic;
using System.Web;

namespace UIFS
{

    public partial class DesignerDisplay : System.Web.UI.Page
    {
        public UIFS.FormDataStruct FormData = new UIFS.FormDataStruct();

        protected void Page_Load(object sender, EventArgs e)
        {
            // Need to set to initialize and keep session active for ajax calls, other windows, etc.
            Session["KeepAlive"] = "HI.YA!";

            if (Session["FormData"] == null)
            { // if no form exists in session, start new one.
                Session["FormData"] = new UIFS.FormDataStruct();
            }

            
        }

    }
}
