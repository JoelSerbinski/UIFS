using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using Newtonsoft.Json;

namespace UIFS
{
    public partial class ajax : System.Web.UI.Page
    {
    
        // AJAXhtmloutput = the string containing the html we want returned to the calling page
        public string AJAXhtmloutput, html, js;

        private UIFS.Designer CCDesigner;
        public UIFS.FormDataStruct FormData;
        private UIFS.Reporting Reporting;
        private UIFS.ReportDefinition ReportDefinition;
        private UIFS.Form UIFSForm;
        private UIFS.Form_Input Form_Input;
        private UIFS.Form_Output Form_Output;

        private SQL SQL; // we will use a global SQL object so that we can pass a reference to routines so that we only use one db connection per ajax call (huge use of resources/time when we open and close a connection)

        int id, iControl;
        string outputHTML = "", outputJS = "";

        // Our Control's vars...
        UIFS.FormControl.List newLC;


        public DayCodeList[] CL = new DayCodeList[7];
        public class DayCodeList
        {
            public string Code;
        }
        
        protected void Page_PreRender()
        {
            try
            {
                // Prevent browser from caching pages!!!
                Response.Buffer = true;
                Response.ExpiresAbsolute = DateTime.Now;
                Response.Expires = 0;
                Response.CacheControl = "no-cache";

                // Get DB from session
                try
                {
                    SQL = (UIFS.SQL)Session["SQL"]; SQL.OpenDatabase(); // we expect to already be open, but check nonetheless
                }
                catch
                { // Session Expired
                    AJAXhtmloutput = "<input type='hidden' id='SystemAlert' value=\":SystemAlert: Your Session Expired\nPlease refresh the page\n :-/ :SystemAlertEnd:\" />";
                    return;
                }

                // As long as the Session is functioning normally and passing data, we can continue
                if (Session["KeepAlive"] != null)
                {

                    // Check query code to see what action we need to perform
                    switch (Request.QueryString["cmd"])
                    {
                        // TODO: we need to add security here or there

                        case "0": //kill session
                            Session.Clear();
                            Session.Abandon();
                            return;

                        case "TEST": // TEST


                            //Form_Output = new UIFS.Form_Output();
                            //Form_Input.InputValue[] FormValues = new Form_Input.InputValue[0];
                            //outputHTML = ""; outputJS = "";
                            //SQL = new SQL(ConfigurationManager.AppSettings["SQL_Default"]);
                            //SQL.OpenDatabase();
                            //UIFSForm = new Form(ref SQL);
                            //if (UIFSForm.Load(8, 1, ref FormData))
                            //{
                            //    Form_Output.HTML(FormData, ref outputHTML, ref outputJS);
                            //    FormValues = Form_Output.LoadData(FormData, SQL, 1);
                            //    if (FormValues != null)
                            //    {
                            //        outputJS = outputJS + Form_Output.PopulateForm_js(FormData, FormValues);
                            //    }
                            //    else
                            //    {
                            //        outputHTML = "ERROR: null returned";
                            //    }
                            //    outputJS = "<script type='text/javascript'>" + outputJS + "</script>"; // comes raw javascript
                            //    AJAXhtmloutput = outputHTML + outputJS;
                            //}
                            //SQL.CloseDatabase(); // close after all calls 
                            break;



                    /*----/========================================================================================\----
                    * ---|                            COMMON Functionality SECTION                                  |----
                    * ----\========================================================================================/----
                        
                    * */

                        case "300": // Outputs a datatable for choosing UIFS Forms: ALL, one, or multiple (singular or plural)

                            string html = "", js = "";
                            html = "<div id='FormSelect'><table cellpadding='0' cellspacing='0' border='0' class='display' id='DataTables_FormSelect'>" +
                                "<thead><tr><th>ID</th><th>Name</th><th>version</th><th>Created by</th><th>Description</th></tr></thead>" +
                                "<tbody></tbody></table></div>";

                            js = "<script type='text/javascript'>" +
                                    "$('#DataTables_FormSelect').dataTable( {" +
                                    "'aoColumnDefs': [ " +
                                        "{ 'bSearchable': false, 'bVisible': false, 'aTargets': [ 0 ] }]," +		// 0 would be the id field...			
                                    "'bProcessing': false," + // *one-time* request for data
                                    "'bJQueryUI':true," + // use jquery ui theme
                                    "'sPaginationType': 'full_numbers'," + // for the paging navigation (either full or 2 arrows)
                                    "'aLengthMenu': [[50, 100, 200, -1], [50, 100, 200, 'All']],"+
                                    "'sScrollY': '250px'," + // MUST set our height to keep this thing under control!
                                    "'sScrollX':'100%',"+ // Set width to container size...add scrollbar in table
                                    "'sHeightMatch': 'none',"+ // do not let calculate row height...for faster display
                                    "'sAjaxSource': 'ajax.aspx?cmd=300.1',"+
                                    // The next line adds a row click function (allows selection multiple)
                                    //"'fnInitComplete': function () {$('tr').click(function () {if ($(this).hasClass('row_selected')) $(this).removeClass('row_selected'); else $(this).addClass('row_selected'); }); }"+
                                    "});" +
                                    "SubjectTable = $('#DataTables_FormSelect').dataTable();" +
                                    // Single row selection...(just copied code from site, it is very wasteful of resources..temp anyway)
                                    "$('#DataTables_FormSelect tbody').click(function(event) {$(SubjectTable.fnSettings().aoData).each(function (){ $(this.nTr).removeClass('row_selected'); }); $(event.target.parentNode).addClass('row_selected'); });" +
                                    "</script>";
                            AJAXhtmloutput= html+js;
        
                            break;

                        case "300.1": // outputs the datatable *data* in json format
                            string json = "{ \"aaData\": [";
                            string description = "",createdby="";                            
                            UIFSForm = new UIFS.Form(ref SQL);                            
                            // Get list of forms
                            UIFS.FormLIST[] FormsList = UIFSForm.List();
                            //. for each form, build array
                            for (int t = 0; t < FormsList.Length; t++)
                            {
                                //. needs to be filtered
                                createdby = Format4DataTablesJSON(FormsList[t].createdby);
                                description = Format4DataTablesJSON(FormsList[t].description);
                                //description = Newtonsoft.Json.JsonConvert.SerializeObject(FormsList[t].description);
                                json = json + "[\"" + FormsList[t].id + "\",\"" + FormsList[t].name + "\",\"" + FormsList[t].currentversion + "\",\""+createdby  + "\",\"" + description+"\"],";
                            }
                            json = json.Remove(json.Length - 1); //remove last comma
                            json = json + "]}";

                            AJAXhtmloutput = json;
                            break;



                        /*----/========================================================================================\----
                        * ---|                            REPORT DESIGNER SECTION                                       |----
                        * ----\========================================================================================/----
                        
                        * */

                        case "800": // Ask to load existing or start new
                            break;
                        case "801": // Load Report Display
                            //. get list
                            SQL.Query = SQL.SQLQuery.Reporting_LoadReportList;
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader();
                            html = "<table>";
                            if (SQL.sdr.HasRows)
                            {
                                while (SQL.sdr.Read())
                                {
                                    html = html + "<tr onclick=\"OpenReport('" + Convert.ToDouble(SQL.sdr[0]) + "')\" onmouseover=\"$(this).find('.lang').show();\" onmouseout=\"$(this).find('.lang').hide();\" >" +
                                        "<td><div class='title'>" + SQL.sdr.GetString(1) + "</div><div class='lang ReportDefinitionLanguage'>" + SQL.sdr.GetString(2) + "</div></td></tr>";
                                }
                            }
                            SQL.sdr.Close();
                            html = html+"</table>";
                            AJAXhtmloutput = html;
                            break;
                        case "801.1": // Load existing report
                            Reporting = new Reporting(ref SQL);
                            Reporting.Load_ReportingSubjects();
                            ReportDefinition = Reporting.Load_ReportingDefinition(Convert.ToInt64(Request.QueryString["id"]));
                            int iRS = Reporting.Find_ReportingSubject(ReportDefinition.Description.name);
                            Reporting.GUI = new Reporting.GraphicalUserInterface(ReportDefinition);
                            Reporting.GUI.Subject_Set(false,ref Reporting.FormLinks,Reporting.ReportingSubjects[iRS],ref Reporting.SQL, ref Reporting.ReportingSubjects);                            
                            Session["Reporting"] = Reporting;
                            break;
                        case "802": // Start new from Subject Chosen (currently a UIFS Form)
                            Reporting = new Reporting(ref SQL);
                            Reporting.Load_ReportingSubjects();
                            ReportDefinition = new UIFS.ReportDefinition();
                            ReportDefinition.Description = new UIFS.ReportDefinition.Subject();
                            ReportDefinition.Description.lang = "singular"; // singular, plural, ALL
                            ReportDefinition.Description.name = "Form"; // this is a UIFS Form object/subject
                            ReportDefinition.Description.selection = Request.QueryString["id"]; //Form id(s)
                            ReportingSubject RS = new ReportingSubject();
                            Reporting.GUI = new Reporting.GraphicalUserInterface(ReportDefinition); // setup GUI
                            Reporting.GUI.Subject_Set(true, ref Reporting.FormLinks, RS, ref SQL, ref Reporting.ReportingSubjects); // load data
                            Session["Reporting"] = Reporting;
                            break;

                        case "803": // Save Report Display
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            Reporting.SQL.OpenDatabase();
                            if (Reporting.BuildReport(ref Reporting.GUI.RD))
                            {
                                html = "<div class='input'>Title: <input id='ReportTitle' type='text' value='" + Reporting.GUI.RD.title + "' size='66' /></div>";
                                html = html + "<div class='ReportLang'>" + Reporting.GUI.RD.language + "</div>";
                                Session["Reporting"] = Reporting;
                            }
                            else
                            {
                                html = "<input type='hidden' value=':SystemAlert: There was an error trying to BUILD the report :( :SystemAlertEnd:' />";
                            }
                            //Reporting.SQL.CloseDatabase();
                            AJAXhtmloutput = html;
                            break;
                        case "803.1": // SAVE REPORT
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            Reporting.SQL.OpenDatabase();
                            //. update title
                            Reporting.GUI.RD.title = Request.QueryString["title"];
                            if (Reporting.Save_ReportingDefinition(ref Reporting.GUI.RD,this.User.Identity.Name))
                            {
                                AJAXhtmloutput = " :SystemAlert: Report Saved! :) :SystemAlertEnd: ";
                            }
                            else
                            {
                                AJAXhtmloutput = " :SystemAlert: There was an error trying to save the report :( :SystemAlertEnd: ";
                            }
                            Session["Reporting"] = Reporting;
                            //Reporting.SQL.CloseDatabase();
                            break;
                        case "808": // Report Subject display
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            Reporting.aReportOn_UIFSForm(Convert.ToInt32(Reporting.GUI.RD.Description.selection));
                            AJAXhtmloutput = Reporting.aReportOn; //Reporting.GUI.RD.Description.name
                            break;
                        case "809": // Report Definition display
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            AJAXhtmloutput= Reporting.GUI.ReportDefinition_Where();
                            break;
                        case "810": // Report Options display 
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            AJAXhtmloutput= Reporting.GUI.WHERE();                            
                            break;
                        case "810.1": // Report Options - single option redraw
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            Reporting.GUI.ReportConditions[Convert.ToInt32(Request.QueryString["id"])].RDdetail.lang = Request.QueryString["phrase"];
                            AJAXhtmloutput=Reporting.GUI.ReportConditions[Convert.ToInt32(Request.QueryString["id"])].Edit();
                            Session["Reporting"] = Reporting;
                            break;
                        case "810.2": // Report Option: USE - get value(s) of control (outputs js)
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            int Ctrlid = Convert.ToInt32(Request.QueryString["id"]);
                            FormControl FC = Reporting.GUI.ReportConditions[Ctrlid].Control;
                            ControlType CT = Reporting.GUI.ReportConditions[Ctrlid].Control_type;
                            FormData = new UIFS.FormDataStruct();
                            //. we have to manually add our control in to get parsed
                            Array.Resize(ref FormData.ControlList, 1);
                            FormData.ControlList[0] = new FormDataStruct.ControlListDetail();
                            FormData.ControlList[0].id = Ctrlid; FormData.ControlList[0].index = 0; FormData.ControlList[0].type = CT;
                            FormData.AddControl(CT,FC,false); 
                            Form_Input = new Form_Input();
                            AJAXhtmloutput = Form_Input.GetInput_js(FormData); // comes raw javascript
                            Session["FormData"] = FormData;
                            break;
                        case "810.3": // Report Option: USE - passed in value(s)
                            int RCid = Convert.ToInt32(Request.QueryString["id"]);
                            int iRDDd = -1;
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            Form_Input = new Form_Input();
                            UIFS.Form_Input.InputValue[] IV = Form_Input.FilterInput(Request.QueryString, FormData);
                            //.  give this ReportCondition a selection value
                            if (IV[0].value == null)
                            { // use Start/End (From/To) values
                                Reporting.GUI.ReportConditions[RCid].RDdetail.selection = IV[0].Start + ',' + IV[0].End;
                            }
                            else
                            { // default to just returning a single value
                                Reporting.GUI.ReportConditions[RCid].RDdetail.selection = IV[0].value;
                            }
                            //. add a new ReportDefinition.Description.Detail
                            ReportDefinition.Detail detail = new UIFS.ReportDefinition.Detail();
                            if (Reporting.GUI.ReportConditions[RCid].detail.name.StartsWith("[global]")) {
                                detail.name = Reporting.GUI.ReportConditions[RCid].detail.name;
                            }
                            else {detail.name = Reporting.GUI.ReportConditions[RCid].detail.db;}
                            detail.lang = Reporting.GUI.ReportConditions[RCid].RDdetail.lang;
                            detail.selection = Reporting.GUI.ReportConditions[RCid].RDdetail.selection;
                            iRDDd = Reporting.GUI.RD.Description.AddDetail(detail); // already holds needed/updated information!
                            Reporting.GUI.ReportConditions[RCid].iRDDdetail = iRDDd;
                            Session["Reporting"] = Reporting;
                            break;
                        case "810.4": // Report Option: Remove
                            int RC_remove = Convert.ToInt32(Request.QueryString["id"]);
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            Reporting.GUI.RD.Description.DelDetail(Reporting.GUI.ReportConditions[RC_remove].RDdetail.name);
                            Reporting.GUI.ReportConditions[RC_remove].iRDDdetail = -1; // not being used anymore
                            Session["Reporting"] = Reporting;
                            break;

                        case "811": // Report Field Selection (THAT SHOWS) display
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            AJAXhtmloutput= Reporting.GUI.THATSHOWS();
                            break;
                        case "811.1": // Report (Field) selection (from js: Aggregate_Add)
                            ReportDefinition.Aggregate RDA = new UIFS.ReportDefinition.Aggregate();
                            int iShow_add = Convert.ToInt32(Request.QueryString["id"]); // our index
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));

                            Reporting.GUI.ReportShowing[iShow_add].manipulation = Request.QueryString["mani"];
                            RDA.db = Reporting.GUI.ReportShowing[iShow_add].db;
                            RDA.title = Reporting.GUI.ReportShowing[iShow_add].title;
                            RDA.datatype = Reporting.GUI.ReportShowing[iShow_add].datatype;
                            RDA.manipulation = Reporting.GUI.ReportShowing[iShow_add].manipulation;
                            Reporting.GUI.ReportShowing[iShow_add].iAggr = Reporting.GUI.RD.Description.AddAggregrate(RDA); // push our new aggr.
                            Session["Reporting"] = Reporting;
                            break;

                        case "890": // Preview Report (this outputs a datatables..)
                            Reporting = (Reporting)Convert.ChangeType(Session["Reporting"], typeof(Reporting));
                            Reporting.SQL.OpenDatabase();
                            //TODO: should check to see if built or not...save to session..
                            if (!Reporting.BuildReport(ref Reporting.GUI.RD)) {
                                //Reporting.SQL.CloseDatabase();
                                AJAXhtmloutput = " :SystemAlert: There was an error trying to process the report :( :SystemAlertEnd: ";
                                return;
                            }
                            string AggrOutput = Reporting.Output_Aggregation(Reporting.GUI.RD);
                            string DataTablesOutput = Reporting.Output_DataTables(Reporting.GUI.RD);
                            js = "";
                            filterJavascript(ref AggrOutput, ref js);
                            filterJavascript(ref DataTablesOutput, ref js);
                            AJAXhtmloutput =
                                "<div id='ReportLang'>" + Reporting.GUI.RD.language + "</div>" +
                                "<div id='ReportAggr'>" + AggrOutput + "</div>" +                                
                                "<div id='ReportData'>" + DataTablesOutput + "</div>" +
                                "<script type='text/javascript'>" + js + "</script>";
                            //Reporting.SQL.CloseDatabase();
                            break;

                        /* ----/========================================================================================\----
                         * ----|                                  DESIGNER SECTION                                      |----
                         * ----\========================================================================================/----
                         * 
                         * */

                        case "900": // New Form                            
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            if (Convert.ToBoolean(Request.QueryString["confirmation"]))
                            { // Confirmation is TRUE, create new form
                                FormData = new UIFS.FormDataStruct();
                                FormData.newform = true; // not sure if we use this for anything...but it is functional
                                Session["FormData"] = FormData;
                                AJAXhtmloutput = "";
                            }
                            else { // if the confirmation variable is set to true, then we skip the check and start a new form
                                // First, run through current form and see if there are any unsaved changes
                                if (FormData.ControlList != null)
                                {
                                    for (int t = 0; t < FormData.ControlList.Length; t++)
                                    {
                                        if (FormData.ControlList[t].controlchanged || FormData.ControlList[t].added || FormData.ControlList[t].removed)
                                        { // If the current form has unsaved changes, tell the calling javascript with this msg
                                            AJAXhtmloutput = "UNSAVED CHANGES";
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            break;
                        case "901": // Load/Open Form Dialog
                            AJAXhtmloutput = Form_Open();
                            break;
                        case "901.1": // Actual Load Form data
                            UIFSForm = new UIFS.Form(ref SQL);
                            FormData = new UIFS.FormDataStruct();
                            if (!UIFSForm.Load(Convert.ToInt32(Request.QueryString["formid"]),-1, ref FormData))
                            { // failed to load
                                AJAXhtmloutput = " :SystemAlert: Failed to load form! ExMsg:" + UIFSForm.ErrorEx.Message + "\nStacktrace:" + UIFSForm.ErrorEx.StackTrace + " :SystemAlertEnd: ";
                                SQL.WriteLog_Error(UIFSForm.ErrorEx, "Failed to load form: " + Request.QueryString["formid"], "UIFS.ajax:901.1");
                                return;
                            }
                            Session["FormData"] = FormData; // Save to session
                            break;
                        case "901.2": // New Form based on...
                            //. load a form and clear out needed variables to make it a "new" form
                            UIFSForm = new UIFS.Form(ref SQL);
                            FormData = new UIFS.FormDataStruct();
                            if (!UIFSForm.Load(Convert.ToInt32(Request.QueryString["formid"]),-1, ref FormData))
                            { // failed to load
                                AJAXhtmloutput = " :SystemAlert: Failed to load form! ExMsg:" + UIFSForm.ErrorEx.Message + "\nStacktrace:" + UIFSForm.ErrorEx.StackTrace + " :SystemAlertEnd: ";
                                SQL.WriteLog_Error(UIFSForm.ErrorEx, "Failed to load form: " + Request.QueryString["formid"], "UIFS.ajax:901.2");
                                return;
                            }
                            // reset data for new form
                            FormData.name="";FormData.description="";FormData.id=0;FormData.version=0;FormData.newform=true;
                            Session["FormData"] = FormData; // Save to session
                            break;
                        case "903": // Save Dialog
                            AJAXhtmloutput = Form_Save();
                            break;
                        case "903.1": // Save Form
                            string ValidationMessage="";
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            if (FormData.ControlList == null) { AJAXhtmloutput = " :SystemAlert: No form to save, doh! :SystemAlertEnd: "; return; }
                            FormData.name = Request.QueryString["name"];
                            FormData.description = Request.QueryString["desc"];
                            UIFSForm = new UIFS.Form(ref SQL);
                            if (UIFSForm.Validate(ref FormData, ref ValidationMessage))
                            {
                                if (UIFSForm.Save(ref FormData))
                                {
                                    AJAXhtmloutput = " :SystemAlert: Form Saved! :SystemAlertEnd: ";
                                    // After form is saved, we need to reload it in order to properly get the changes
                                    // so, we will just clear out the Form here to act like a new form was started
                                    FormData = new UIFS.FormDataStruct();
                                    Session["FormData"] = FormData;
                                }
                                else
                                {
                                    AJAXhtmloutput = " :SystemAlert: Failed to save form :SystemAlertEnd: ";
                                }
                            }
                            else
                            {
                                AJAXhtmloutput = " :SystemAlert: " + ValidationMessage + " :SystemAlertEnd: ";
                            }
                            break;
                        case "904": // Form Settings dialog
                            AJAXhtmloutput = Form_Settings();
                            break;
                        case "904.1": // Form Settings SAVE
                            // TODO: should it just "save" to the session or the db too?
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            FormData.name = Request.QueryString["name"];
                            FormData.description = Request.QueryString["desc"];
                            //FormData.Layout.OutputFormat = (UIFS.Layout.Style)Convert.ToInt32(Request.QueryString["Layout_NumOfColumns"]);
                            Session["FormData"] = FormData;
                            AJAXhtmloutput = " :SystemAlert: Form Settings Saved! :SystemAlertEnd: ";
                            break;

                        case "910": // Live Preview
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.Form_Output FormPreview = new UIFS.Form_Output();
                            outputHTML=""; outputJS="";
                            FormPreview.HTML(FormData, ref outputHTML, ref outputJS);
                            // TOOLBAR: with buttons for testing/debugging
                            outputHTML = "<div id='FormPreview_HTML_Toolbar'><span class='submitvalues' onclick='FakeFormSubmit_GetValues()'>FakeSubmit</span>"+
                                "<span class='submitdata' onclick='FakeFormSubmit_SaveData()'>FakeSaveData</span>" +
                                "<span class='' onclick='FakeFormSubmit_Validate()'>FakeValidate</span>" +
                                "</div<div id='FormPreview_HTML_Toolbar_spacer' style='height:10px;'></div>" 
                                + outputHTML;
                            outputJS = "<script type='text/javascript'>" + outputJS + "</script>"; // comes raw javascript
                            AJAXhtmloutput = outputHTML + outputJS;
                            FormPreview = null; FormData=null;
                            break;
                        case "910.1": // Live Preview: HTML Toolbar: getvalues (fake submit)
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.Form_Input FI_getvalues = new Form_Input();
                            //AJAXhtmloutput = "<script type='text/javascript'>" + Form_Input.GetInput_js(FormData) + "</script>"; // comes raw javascript
                            AJAXhtmloutput = FI_getvalues.GetInput_js(FormData); // comes raw javascript
                            FI_getvalues = null; FormData=null;
                            break;
                        case "910.2": // Live Preview: HTML Toolbar: formsave (Test Form saving data)
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.Form_Input FI_save = new Form_Input();
                            UIFS.Form_Input.InputValue[] InputValues = FI_save.FilterInput(Request.QueryString, FormData);
                            long formid = -1;
                            if (FI_save.Save(FormData, InputValues, ref SQL, true, ref formid))
                            { // successful
                                AJAXhtmloutput = "<input type='hidden' id='SystemAlert' value=\" :SystemAlert: Form Data Save: Successful! :SystemAlertEnd: \" />";
                            }
                            else { AJAXhtmloutput = "<input type='hidden' id='SystemAlert' value=\" :SystemAlert: Form Data Save: Failed! :SystemAlertEnd: \" />"; }
                            FI_save = null; FormData = null;
                            break;

                        case "1000": // Designer Main Screen Display
                            switch (Request.QueryString["Option"]){
                                case "Controls":
                                    int FormCnt_Textbox = 0, FormCnt_List = 0, FormCnt_Checkbox = 0, FormCnt_DateTime = 0, FormCnt_Number = 0, FormCnt_Percentage=0,FormCnt_Range=0;
                                    FormData = (UIFS.FormDataStruct)Session["FormData"];
                                    if (FormData.ControlList != null)
                                    { // get count of each # of controls
                                        foreach (UIFS.FormDataStruct.ControlListDetail cld in FormData.ControlList)
                                        {
                                            switch (cld.type)
                                            {
                                                case ControlType.Checkbox: FormCnt_Checkbox += 1; break;
                                                case ControlType.DateTime: FormCnt_DateTime += 1; break;
                                                case ControlType.Number: FormCnt_Number += 1; break;
                                                case ControlType.Percentage: FormCnt_Percentage += 1; break;
                                                case ControlType.List: FormCnt_List += 1; break;
                                                case ControlType.Range: FormCnt_Range += 1; break;
                                                case ControlType.Textbox: FormCnt_Textbox += 1; break;
                                            }
                                        }
                                    }
                                    html =
                                        "<div id='CTRL_Textbox' class='CTRL ui-widget-content'>Text Box (<span style='color:Blue;'>" + FormCnt_Textbox + "</span>)</div>" +
                                        "<div id='CTRL_List' class='CTRL ui-widget-content'>List (<span style='color:Blue;'>" + FormCnt_List + "</span>)</div>" +
                                        "<div id='CTRL_List_HourBlock' class='subCTRL ui-widget-content'>Hour Blocks [Time]</div>" +
                                        "<div id='CTRL_Checkbox' class='CTRL ui-widget-content'>Checkbox (<span style='color:Blue;'>" + FormCnt_Checkbox + "</span>)</div>" +
                                        "<div id='CTRL_DateTime' class='CTRL ui-widget-content'>Date/Time (<span style='color:Blue;'>" + FormCnt_DateTime + "</span>)</div>" +
                                        "<div id='CTRL_Number' class='CTRL ui-widget-content'>Number (<span style='color:Blue;'>" + FormCnt_Number + "</span>)</div>" +
                                        "<div id='CTRL_Percentage' class='CTRL ui-widget-content'>Percentage (<span style='color:Blue;'>" + FormCnt_Percentage + "</span>)</div>" +
                                        "<div id='CTRL_Range' class='CTRL ui-widget-content'>Range (<span style='color:Blue;'>" + FormCnt_Range + "</span>)</div>"
                                        ;                                    
                                    AJAXhtmloutput = AJAXhtmloutput + html;
                                    FormData = null;
                                    break;
                                case "Form":
                                    Designer_FormTemplate();
                                    break;

                                case "Menu":
                                    html = "Save Form, New Form, Load Form, Full Preview";
                                    break;
                                default:
                                    break;
                            }                            
                            break;

                        case "1001": // DISPLAY Add/Create control dialog
                            CCDesigner = new UIFS.Designer();
                            switch (Request.QueryString["type"])
                            {
                                case "CTRL_Textbox":
                                    // create an empty control
                                    UIFS.FormControl.Textbox CTRL_TextBox = new UIFS.FormControl.Textbox();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_TextBox.id = -1;
                                    js = FormData.jQuery.Textbox_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.Textbox, CTRL_TextBox) + js;
                                    break;
                                case "CTRL_List":
                                    UIFS.FormControl.List CTRL_List = new UIFS.FormControl.List();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_List.id = -1;
                                    js = FormData.jQuery.List_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.List, CTRL_List) + js;
                                    break;
                                case "CTRL_Checkbox":
                                    UIFS.FormControl.Checkbox CTRL_Checkbox = new UIFS.FormControl.Checkbox();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_Checkbox.id = -1;
                                    js = FormData.jQuery.Checkbox_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.Checkbox, CTRL_Checkbox) + js;
                                    break;
                                case "CTRL_DateTime":
                                    UIFS.FormControl.DateTime CTRL_DateTime = new UIFS.FormControl.DateTime();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_DateTime.id = -1;
                                    js = FormData.jQuery.DateTime_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.DateTime, CTRL_DateTime) + js;
                                    break;
                                case "CTRL_Number":
                                    UIFS.FormControl.Number CTRL_Number = new UIFS.FormControl.Number();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_Number.id = -1;
                                    js = FormData.jQuery.Number_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.Number, CTRL_Number) + js;
                                    break;
                                case "CTRL_Percentage":
                                    UIFS.FormControl.Percentage CTRL_Percentage = new UIFS.FormControl.Percentage();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_Percentage.id = -1;
                                    js = FormData.jQuery.Percentage_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.Percentage, CTRL_Percentage) + js;
                                    break;
                                case "CTRL_Range":
                                    UIFS.FormControl.Range CTRL_Range = new UIFS.FormControl.Range();
                                    FormData = new UIFS.FormDataStruct(); // to use the jquery routines
                                    CTRL_Range.id = -1;
                                    js = FormData.jQuery.Range_AddNew(); // Remove save buttons 
                                    AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.Range, CTRL_Range) + js;
                                    break;
                            }
                            break;

                        case "1002": // Remove Control
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            int iCtrl = FormData.Find_ControlListEntry_byControlID(Convert.ToInt32(Request.QueryString["id"]));
                            // reorder to last, so that active controls are ordered properly
                            FormData.Sort_ControlList(Convert.ToInt32(Request.QueryString["id"]), FormData.ControlList.Length);
                            // check if existing control or new control
                            if (FormData.ControlList[iCtrl].added)
                            { // This control was added during this session (not saved) - remove from existence!
                                FormData.RemoveControl(Convert.ToInt32(Request.QueryString["id"]));
                            }
                            else { // mark as removed (routines on Save)
                                FormData.ControlList[iCtrl].removed = true;
                            }
                            Session["FormData"] = FormData;
                            AJAXhtmloutput = "";
                            break;
                        case "1050": // Reorder controls
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            int Controlid = Convert.ToInt32(Request.QueryString["id"].Substring(Request.QueryString["id"].IndexOf("_")+1));
                            FormData.Sort_ControlList(Controlid, Convert.ToInt32(Request.QueryString["sortindex"]));
                            Session["FormData"] = FormData;
                            break;

                        /* ---------------------------------------------------------------------------------------------------
                         * -- Control Update/Addition Functions
                         * ---------------------------------------------------------------------------------------------------
                         */

                        // 1099 = Common Properties for all controls functions
                        case "1099": // Update 
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            FormControl ControlChanges = new FormControl();
                            ControlChanges.name = Request.QueryString["name"];
                            ControlChanges.prompt = Request.QueryString["prompt"];
                            ControlChanges.tip = Request.QueryString["tip"];
                            ControlChanges.required = Convert.ToBoolean(Request.QueryString["req"]);
                            FormData.Update_ControlCommonProperties(Convert.ToInt32(Request.QueryString["id"]), ControlChanges,true);
                            Session["FormData"] = FormData;
                            break;

                        // 1100 = Textbox Control functions
                        case "1100": // Textbox Control - update properties
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // control id
                            iControl = FormData.Find_Controlindex_byID(id);
                            FormData.Textbox[iControl].lines = Convert.ToInt32(Request.QueryString["lines"]);
                            FormData.Textbox[iControl].width = Convert.ToInt32(Request.QueryString["width"]);
                            FormData.Textbox[iControl].FullText = Convert.ToBoolean(Request.QueryString["fulltext"]);
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData;
                            break;

                        case "1100.1": // Textbox Control - ADD NEW
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.FormControl.Textbox newTBC = new UIFS.FormControl.Textbox();
                            newTBC.name = Request.QueryString["-1_Name"].ToString();
                            newTBC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newTBC.tip = Request.QueryString["-1_Tip"].ToString();
                            newTBC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newTBC.lines = Convert.ToInt32(Request.QueryString["-1_Lines"]);
                            newTBC.width = Convert.ToInt32(Request.QueryString["-1_Width"]);
                            newTBC.FullText = Convert.ToBoolean(Request.QueryString["-1_FullText"]);
                            iControl = FormData.AddControl(UIFS.ControlType.Textbox, newTBC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData;
                            break;
                        case "1100.2": // Textbox Control - 

                            break;

                        // 1101 = List Control functions
                        case "1101": // List Control - Add Remove from Option List OR update properties
                            CCDesigner = new UIFS.Designer();
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // List control id
                            iControl = FormData.Find_Controlindex_byID(id); // find List control
                            int iItem, newOrderNum;
                            bool reDraw = false;
                            
                            switch (Request.QueryString["Option"])
                            {
                                case "Add":                                    
                                    string name = Request.QueryString["name"].ToString();
                                    string value = Request.QueryString["value"].ToString();
                                    FormData.List[iControl].AddItem(name, value); // add this item
                                    reDraw = true;
                                    break;
                                case "Remove":
                                    iItem = Convert.ToInt32(Request.QueryString["i"]); // index of item
                                    FormData.List[iControl].RemoveItem(iItem); // remove this item
                                    reDraw = true;
                                    break;
                                case "ReOrder":
                                    iItem = Convert.ToInt32(Request.QueryString["item"]); // index of item
                                    newOrderNum = Convert.ToInt32(Request.QueryString["newindex"]); // new index#
                                    FormData.List[iControl].ReOrderItem(iItem, newOrderNum); // reorder
                                    reDraw = true; // We MUST redraw here because the remove buttons are indexed for each option
                                    break;
                                case "update":
                                    FormData.List[iControl].type = (FormControl.List.listtype)Convert.ToByte(Request.QueryString["type"]);
                                    break;
                            }                            
                            if (reDraw) {
                                // we need to reinitialize jquery (AJAX picks up java at end of output and executes it)
                                js = FormData.jQuery.List(FormData.List[iControl].id);
                                AJAXhtmloutput = CCDesigner.ControlProperties(UIFS.ControlType.List, FormData.List[iControl]) + js;
                            }
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData; // Push back to session
                            break;

                        case "1101.1": // List Control - Add New
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            newLC = new UIFS.FormControl.List();
                            newLC.name = Request.QueryString["-1_Name"].ToString();
                            newLC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newLC.tip = Request.QueryString["-1_Tip"].ToString();
                            newLC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newLC.type = (FormControl.List.listtype)Convert.ToByte(Request.QueryString["-1_type"]);
                            iControl = FormData.AddControl(UIFS.ControlType.List, newLC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData;
                            break;

                        case "1101.2": // List Control - (Predefined) Hour Blocks
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            newLC = new UIFS.FormControl.List();
                            newLC.name = "Hour Block Time Selection";
                            newLC.prompt = "Choose the time frame.";
                            newLC.tip = "Please select the correct hour time frame";
                            newLC.type = FormControl.List.listtype.slider;
                            newLC.Items = FormControl.List.HourBlocks; // use static (Predefined) values
                            iControl = FormData.AddControl(UIFS.ControlType.List, newLC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData;
                            break;

                        // Checkbox functions
                        case "1102": // Checkbox Control - update properties
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // control id
                            iControl = FormData.Find_Controlindex_byID(id);
                            FormData.Checkbox[iControl].type = (UIFS.FormControl.Checkbox.checkboxtype)Convert.ToInt32(Request.QueryString["type"]);
                            FormData.Checkbox[iControl].initialstate = Convert.ToBoolean(Request.QueryString["initialstate"]);
                            FormData.Checkbox[iControl].hasinput = Convert.ToBoolean(Request.QueryString["hasinput"]);
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData;
                            break;
                        case "1102.1": // Add new checkbox control
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.FormControl.Checkbox newCbC = new UIFS.FormControl.Checkbox();
                            newCbC.name = Request.QueryString["-1_Name"].ToString();
                            newCbC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newCbC.tip = Request.QueryString["-1_Tip"].ToString();
                            newCbC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newCbC.type = (UIFS.FormControl.Checkbox.checkboxtype)Convert.ToInt32(Request.QueryString["-1_type"]);
                            newCbC.initialstate = Convert.ToBoolean(Request.QueryString["-1_initialstate"]);
                            newCbC.hasinput = Convert.ToBoolean(Request.QueryString["-1_hasinput"]);
                            iControl = FormData.AddControl(UIFS.ControlType.Checkbox, newCbC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData;
                            break;

                            // DateTime functions
                        case "1103": // DateTime Control - update properties
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // control id
                            iControl = FormData.Find_Controlindex_byID(id);
                            FormData.DateTime[iControl].type = (UIFS.FormControl.DateTime.datetimetype)Convert.ToInt32(Request.QueryString["type"]);
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData;
                            break;
                        case "1103.1": // Add a new DateTime Control
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.FormControl.DateTime newDTC = new UIFS.FormControl.DateTime();
                            newDTC.name = Request.QueryString["-1_Name"].ToString();
                            newDTC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newDTC.tip = Request.QueryString["-1_Tip"].ToString();
                            newDTC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newDTC.type = (UIFS.FormControl.DateTime.datetimetype)Convert.ToInt32(Request.QueryString["-1_type"]);
                            iControl = FormData.AddControl(UIFS.ControlType.DateTime, newDTC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData;
                            break;


                        // 1104... = Number Control functions
                        case "1104": // Number Control - update properties
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // control id
                            iControl = FormData.Find_Controlindex_byID(id);
                            FormData.Number[iControl].min = Convert.ToDecimal(Request.QueryString["min"]);
                            FormData.Number[iControl].max = Convert.ToDecimal(Request.QueryString["max"]);
                            FormData.Number[iControl].interval = Convert.ToDecimal(Request.QueryString["interval"]);
                            FormData.Number[iControl].slider = Convert.ToBoolean(Request.QueryString["slider"]);
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData; // save
                            break;
                        case "1104.1": // Add a new Number Control
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.FormControl.Number newNC = new UIFS.FormControl.Number();
                            newNC.name = Request.QueryString["-1_Name"].ToString();
                            newNC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newNC.tip = Request.QueryString["-1_Tip"].ToString();
                            newNC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newNC.min = Convert.ToDecimal(Request.QueryString["-1_min"]);
                            newNC.max = Convert.ToDecimal(Request.QueryString["-1_max"]);
                            newNC.interval = Convert.ToDecimal(Request.QueryString["-1_interval"]);
                            newNC.slider = Convert.ToBoolean(Request.QueryString["-1_slider"]);
                            iControl = FormData.AddControl(UIFS.ControlType.Number, newNC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData; // save
                            break;

                        // 1105... = Percentage Control functions
                        case "1105": // Percentage Control - update properties
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // control id
                            iControl = FormData.Find_Controlindex_byID(id);
                            FormData.Percentage[iControl].interval = Convert.ToInt32(Request.QueryString["interval"]);
                            if (FormData.Percentage[iControl].interval <= 0) { FormData.Percentage[iControl].interval = 1; } // CANNOT BE ZERO
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData; // save
                            break;
                        case "1105.1": // Add a new Percentage Control
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.FormControl.Percentage newPC = new UIFS.FormControl.Percentage();
                            newPC.name = Request.QueryString["-1_Name"].ToString();
                            newPC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newPC.tip = Request.QueryString["-1_Tip"].ToString();
                            newPC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newPC.interval = Convert.ToInt32(Request.QueryString["-1_interval"]);
                            if (newPC.interval <= 0) { newPC.interval = 1; } // CANNOT BE ZERO
                            iControl = FormData.AddControl(UIFS.ControlType.Percentage, newPC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData; // save
                            break;

                        // 1106... = Range Control functions
                        case "1106": // Range Control - update properties
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            id = Convert.ToInt32(Request.QueryString["id"]); // control id
                            iControl = FormData.Find_Controlindex_byID(id);
                            FormData.Range[iControl].type = (UIFS.FormControl.Range.Rangetype)Convert.ToInt32(Request.QueryString["type"]);
                            FormData.Range[iControl].min = Convert.ToDecimal(Request.QueryString["min"]);
                            FormData.Range[iControl].max = Convert.ToDecimal(Request.QueryString["max"]);
                            // Control changed, mark for update
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].controlchanged = true; 
                            FormData.ControlList[FormData.Find_ControlListEntry_byControlID(id)].newversionneeded = true;
                            Session["FormData"] = FormData; // save
                            break;
                        case "1106.1": // Add a new Range Control
                            FormData = (UIFS.FormDataStruct)Session["FormData"];
                            UIFS.FormControl.Range newRC = new UIFS.FormControl.Range();
                            newRC.name = Request.QueryString["-1_Name"].ToString();
                            newRC.prompt = Request.QueryString["-1_Prompt"].ToString();
                            newRC.tip = Request.QueryString["-1_Tip"].ToString();
                            newRC.required = Convert.ToBoolean(Request.QueryString["-1_Req"]);
                            newRC.type = (UIFS.FormControl.Range.Rangetype)Convert.ToInt32(Request.QueryString["-1_type"]);
                            newRC.min = Convert.ToDecimal(Request.QueryString["-1_min"]);
                            newRC.max = Convert.ToDecimal(Request.QueryString["-1_max"]);
                            iControl = FormData.AddControl(UIFS.ControlType.Range, newRC, true); // Add the new control
                            // New Control, mark for addition
                            FormData.ControlList[FormData.ControlList.Length - 1].added = true; // must be called before Sort_ControlList
                            FormData.Sort_ControlList(FormData.ControlList[FormData.ControlList.Length - 1].id, Convert.ToInt32(Request.QueryString["sortindex"])); // Reorder
                            Session["FormData"] = FormData; // save
                            break;

                        default: // THE querystring code did not check out, ignore
                            break;
                    }

                }

                else
                {
                    AJAXhtmloutput = "<input type='hidden' id='SystemAlert' value=\" :SystemAlert: Your Session has Expired!\n\nPlease exit the application :SystemAlertEnd: \" />";
                    //SQL.WriteLog(0, "Session Expired/No session data exists", this.User.Identity.Name);
                }

            }
            catch (Exception ex)
            {
                AJAXhtmloutput = "<input type='hidden' id='SystemAlert' value=\" :SystemAlert: An ERROR occured\r\nMessage:"+ex.Message+"\r\nStackTrace:"+ex.StackTrace+"<br/> :SystemAlertEnd: \" />";
            //    //SQL.WriteLog_AppError(0, "AJAX Routine Failed: " + SQL.ParseInput(Request.QueryString.ToString()), "PreRender", ex, this.User.Identity.Name);
            }
            finally
            {
            }
        }

        private void Designer_FormTemplate()
        {
            CCDesigner = new UIFS.Designer();
            UIFS.FormDataStruct FormData = (UIFS.FormDataStruct)Session["FormData"];
            if (FormData == null)
            { // No data yet, nothing to display
                return;
            }
            string Name="",Type="",Content=""; // Dependent on what type of control it is

            html = ""; js="";
            // Now we will walk through all the controls that exist, in order
            for (int cnt = 1; cnt <= FormData.controls; cnt++)
            {
                int iControl = FormData.Find_ControlListEntry_byOrdernum(cnt); // This finds the control with the order number of [cnt]
                if (iControl == -1) { // could not find this control, problem with order #s.
                    throw new Exception("The form has an error with the order #s of the controls, please fix before continuing.");
                    //break; // skip to the next 
                }
                // As long as the control has not been removed from the form, display!
                if (!FormData.ControlList[iControl].removed)
                {
                    switch (FormData.ControlList[iControl].type)
                    {
                        case UIFS.ControlType.Textbox:
                            Type = "Textbox";
                            Name = FormData.Textbox[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.Textbox, FormData.Textbox[FormData.ControlList[iControl].index]);
                            break;

                        case UIFS.ControlType.List:
                            Type = "List";
                            Name = FormData.List[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.List, FormData.List[FormData.ControlList[iControl].index]);
                            // Now we need to build the javascript to activate the sortable
                            js = FormData.jQuery.List(FormData.List[FormData.ControlList[iControl].index].id);
                            break;

                        case UIFS.ControlType.Checkbox:
                            Type = "Checkbox";
                            Name = FormData.Checkbox[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.Checkbox, FormData.Checkbox[FormData.ControlList[iControl].index]);
                            break;

                        case UIFS.ControlType.DateTime:
                            Type = "Date/Time";
                            Name = FormData.DateTime[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.DateTime, FormData.DateTime[FormData.ControlList[iControl].index]);
                            break;

                        case UIFS.ControlType.Number:
                            Type = "Number";
                            Name = FormData.Number[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.Number, FormData.Number[FormData.ControlList[iControl].index]);
                            break;

                        case UIFS.ControlType.Percentage:
                            Type = "Percentage";
                            Name = FormData.Percentage[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.Percentage, FormData.Percentage[FormData.ControlList[iControl].index]);
                            break;

                        case UIFS.ControlType.Range:
                            Type = "Range";
                            Name = FormData.Range[FormData.ControlList[iControl].index].name;
                            Content = CCDesigner.ControlProperties(UIFS.ControlType.Range, FormData.Range[FormData.ControlList[iControl].index]);
                            break;


                        default:
                            break;

                    }
                    // Create the "portlet"
                    html = html + "<div class='portlet'>" +
                        "<div class='portlet-header'><span class='FormCTRL_Type'>" + Type + ": </span>" +
                        "<span id='CTRL" + FormData.ControlList[iControl].id.ToString() + "_Name' class='FormCTRL_Name'>" + Name + "</span>" +
                        "<span class='ui-icon ui-icon-plusthick'></span></div>" +
                        "<div class='portlet-content' style='display:none' id='Control_" + FormData.ControlList[iControl].id.ToString() + "'>" + Content + "</div>" + // We identify this div for ajax calls
                        "</div>";
                }
            }

                
            CCDesigner = null;

            AJAXhtmloutput = AJAXhtmloutput + html + js;
        }

        // Open Form dialog
        public string Form_Open()
        {
            UIFSForm = new UIFS.Form(ref SQL);
            string Description="";
            // Get list of forms
            UIFS.FormLIST[] FormsList = UIFSForm.List();
            // list of all available forms
            html = "<table>";

            // Our hidden div to hold the selected form's id#
            html = html + "<input type='hidden' id='FormID' />";
            // First the div that contains a list of names with description for the tooltip
            html = html + "<tr><td><div id='FormsList'><table>";
            for (int t = 0; t < FormsList.Length; t++)
            { // form detail
                // build description
                if (FormsList[t].description.Length < 1024) {
                    Description = FormsList[t].description;
                }
                else {
                    Description = FormsList[t].description.Substring(0, 1024);
                }
                if (FormsList[t].active) {
                    html = html + "<tr class='FormsList_active' title='" + Description + "' onclick=\"FormsList_Click('" + FormsList[t].id + "')\"><td>" + FormsList[t].name + "</td><td class='date'>" + FormsList[t].created.ToShortDateString() + "</td></tr>";
                }
                else {
                    html = html + "<tr class='FormsList_inactive' title='" + Description + "' onclick=\"FormsList_Click('" + FormsList[t].id + "')\"><td>" + FormsList[t].name + "</td><td class='date'>" + FormsList[t].created.ToShortDateString() + "</td></tr>";
                }
            }
            html = html + "</table></div></td>";

            // Now we want the div with the form detail: when form name is clicked on, detail will change
            html = html + "<td style='vertical-align:top;' ><div id='FormsListDetail'>";
            for (int t = 0; t < FormsList.Length; t++)
            { // form detail
                // each form will have its own detail div
                html = html + "<div id='FormsList_" + FormsList[t].id + "' class='hiddendiv'><table>"+
                    "<tr><td class='id' >ID# <span style='color:Blue;'>" + FormsList[t].id + "</span></td><td class='version'>Version# <span style='color:Blue;'>" + FormsList[t].currentversion + "</span></td><td class='created'>Created: <span style='color:Blue;'>" + FormsList[t].created + "</span></td></tr>" +
                    "<tr><td colspan='2' class='lastmodifiedby'>Last Modified By: <span style='color:Blue;'>" + FormsList[t].lastmodifiedby + "</span></td><td class='lastmodified'>Last Modified: <span style='color:Blue;'>" + FormsList[t].lastmodified + "</span></td></tr>" +
                    "<tr><td class='description_header' colspan='9'>Description</td></tr>" +
                    "<tr><td class='description' colspan='9'><textarea readonly='1' cols='72' rows='12'>" + FormsList[t].description + "</textarea></td></tr>" +
                    "</table></div>";
            }
            html = html + "</div></td>";

            // end
            html = html + "</tr></table>";
            return html;
        }

        // Form Save dialog
        public string Form_Save()
        {
            UIFS.FormDataStruct FormData = (UIFS.FormDataStruct)Session["FormData"];
            string VersionMessage = "";
            // Cycle through the controls to see if a new version is required
            if (FormData.version != 0)
            {
                if (FormData.ControlList != null)
                {
                    for (int t = 0; t < FormData.ControlList.Length; t++)
                    {
                        if (FormData.ControlList[t].newversionneeded || FormData.ControlList[t].added || FormData.ControlList[t].removed)
                        { 
                            VersionMessage = "Changes made require a new version of this form: ver "+(FormData.version+1); break; }
                    }
                    // if it was not set, means no changes requiring a new version.
                    if (VersionMessage == ""){VersionMessage = "version will stay the same with Save";}
                }
            }
            else
            { // New form
                VersionMessage = "New Form! Will be version 1 after save.";
            }
            // Form name and desc required
            html = "<table>";
            html = html +
                "<tr><td>Version</td><td style='color:Blue;'>" + FormData.version + " ("+VersionMessage+")</td></tr>" +
                "<tr><td>Name</td><td><input id='Form_Name' type='text' size='60' value='" + FormData.name + "'></td></tr>" +
                "<tr><td>Description</td><td><textarea id='Form_Description' cols='80' rows='15'>" + FormData.description + "</textarea></td></tr>"
                ;

            return html;
        }

        // Form Settings dialog
        //   :: This output will be used by both the 'New Form' and 'Form Settings' options, only executed differently outside of this scope
        public string Form_Settings()
        {
            UIFS.FormDataStruct FormData = (UIFS.FormDataStruct)Session["FormData"];

            // Form name and desc required
            html = "<div id='Form_Settings'>";
            html = html + "<div id='Form_Settings_Tabs'><table>" +
                "<tr><td id='T_General' class='selected' onclick=\"Form_Settings_Click('General');\">General</td></tr>" +
                "<tr><td id='T_Layout' onclick=\"Form_Settings_Click('Layout');\">Layout</td></tr>" +
                "<tr><td id='T_UIFS' onclick=\"Form_Settings_Click('UIFS');\">UIFS?</td></tr>" +
                "</table></div>";
            // Container
            html = html + "<div id='Form_Settings_Data'>";
            // General settings
            html = html + "<div id='General'><table>"+
                "<tr><td>Version</td><td style='color:Blue;'>" + FormData.version +"</td></tr>" +
                "<tr><td>Name</td><td><input id='Form_Name' type='text' size='50' value='" + FormData.name + "'/></td></tr>" +
                "<tr><td>Description</td><td><textarea id='Form_Description' cols='50' rows='10'>" + FormData.description + "</textarea></td></tr>"+
                "</table></div>";
            // Layout settings
            html = html + "<div id='Layout' class='hiddendiv'><table>" +
                "<tr><td><span style='font-weight:bold;'>Output Format Style: </span>";
            switch (FormData.Layout.OutputFormat) {
                case UIFS.Layout.Style.DIVs:
                    html = html + "<input name='Layout_NumOfColumns_Control' type='radio' value='0' CHECKED onclick=\"Radio_Select('Layout_NumOfColumns') \" />DIVs &nbsp;&nbsp;<input name='Layout_NumOfColumns_Control' type='radio' value='1' onclick=\"Radio_Select('Layout_NumOfColumns') \" />One Column &nbsp;&nbsp;<input name='Layout_NumOfColumns_Control' type='radio' value='2' onclick=\"Radio_Select('Layout_NumOfColumns') \" />Two Columns <input type='hidden' id='Layout_NumOfColumns' /> </td></tr>";
                    break;
                case UIFS.Layout.Style.SingleColumn:
                    html = html + "<input name='Layout_NumOfColumns_Control' type='radio' value='0' onclick=\"Radio_Select('Layout_NumOfColumns') \" />DIVs &nbsp;&nbsp;<input name='Layout_NumOfColumns_Control' type='radio' value='1' CHECKED onclick=\"Radio_Select('Layout_NumOfColumns') \" />One Column &nbsp;&nbsp;<input name='Layout_NumOfColumns_Control' type='radio' value='2' onclick=\"Radio_Select('Layout_NumOfColumns') \" />Two Columns <input type='hidden' id='Layout_NumOfColumns' /> </td></tr>";
                    break;
                case UIFS.Layout.Style.DoubleColumn:
                    html = html + "<input name='Layout_NumOfColumns_Control' type='radio' value='0' onclick=\"Radio_Select('Layout_NumOfColumns') \" />DIVs &nbsp;&nbsp;<input name='Layout_NumOfColumns_Control' type='radio' value='1' onclick=\"Radio_Select('Layout_NumOfColumns') \" />One Column &nbsp;&nbsp;<input name='Layout_NumOfColumns_Control' type='radio' value='2' CHECKED onclick=\"Radio_Select('Layout_NumOfColumns') \" />Two Columns <input type='hidden' id='Layout_NumOfColumns' /> </td></tr>";
                    break;            
            }
                html=html+"</table></div>";
            // end data container
            html = html + "</div>";
            
            // end Form_Settings
            html = html + "</div>";

            return html;
        }


        public string Format4DataTablesJSON(string input)
        {
            //static byte[] CRLF = { (byte)'\r', (byte)'\n' };
            string output = "";
            //char[] testc = new char[]{'\r','\n'};
            char cr = (Char)'\x0D';
            char lf = (Char)'\x0A';
            output = input.Replace("\\", "\\\\");
            output = output.Replace("\"", "\\\"");

            // remove line feeds and CRs (replace with spaces for readability)
            output = output.Replace(cr.ToString(), " ");  // "\\n");
            output = output.Replace(lf.ToString(), " ");
            
            return output;
        }

        public void filterJavascript(ref string input, ref string java)
        {
            string html = "";
            int ijs = input.IndexOf("<script");
            if (ijs >= 0)
            { // separate out javascript
                java = java + input.Substring(ijs + 31, input.IndexOf("</script>") - ijs - 31); // just get the raw javascript without tags
                html = html + input.Substring(0, ijs);
            }
            else
            { // no js found
                html=input;
            }
            input = html;
        }


        // -----------------------------------------------------------------------------------------------
        
        protected void Page_UnLoad(object sender, EventArgs e)
        {

        }



        // -----------------------------------------------------------------------------------------------
        protected override void Render(HtmlTextWriter writer)
        {
            System.IO.MemoryStream mem = new System.IO.MemoryStream();
            System.IO.StreamWriter twr = new System.IO.StreamWriter(mem);
            System.Web.UI.HtmlTextWriter myWriter = new HtmlTextWriter(twr);
            base.Render(myWriter);
            myWriter.Flush();
            myWriter.Dispose();

            // Write our final output string 
            writer.Write(AJAXhtmloutput);
        }


    }
}
