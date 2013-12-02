using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Configuration;

namespace UIFS
{
    public class Global : System.Web.HttpApplication
    {

        void Application_Start(object sender, EventArgs e)
        {

        }

        void Application_End(object sender, EventArgs e)
        {

        }

        void Application_Error(object sender, EventArgs e)
        {

        }

        void Session_Start(object sender, EventArgs e)
        {
            UIFS.SQL SQL = new UIFS.SQL(ConfigurationManager.AppSettings["SQL_Default"]);
            Session["SQL"] = SQL;
        }

        void Session_End(object sender, EventArgs e)
        {

        }

    }
}
