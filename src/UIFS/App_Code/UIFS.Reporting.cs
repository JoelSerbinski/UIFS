using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UIFS
{
    public class Reporting
    {
        public UIFS.SQL SQL; // SQL (a database) is essential and used everywhere in this module

        public FormLink FormLinks; // part of setup, holds settings for the way this module inter-operates; pop'd at instantiation
        public ReportingSubject[] ReportingSubjects = new ReportingSubject[0]; // populated with call to Load_ReportingSubjects()
        public GraphicalUserInterface GUI; // user interface output 
        public string aReportOn; // Do we require this?

        public Reporting(ref SQL SQL)
        {
            this.SQL = SQL;
            
            // Load information needed to operate (settings)
            try
            {
                SQL.Query = SQL.SQLQuery.Reporting_Settings_FormLink;
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                // "FormLinks" is how the application lets UIFS know what Subjects/Details are "tied" to a UIFS.Form
                this.FormLinks = (FormLink)JsonConvert.DeserializeObject(SQL.sdr.GetString(0), typeof(FormLink));
                SQL.sdr.Close();
            }
            catch (Exception ex)
            { // ERROR: failed to load needed data for reporting engine
                SQL.WriteLog_Error(ex, "Failed to load needed data for reporting engine", "UIFS.Reporting()");
            }

        }


        public string TEST()
        {
            return "";
        }

        public int Find_ReportingSubject(string name)
        {
            for (int a = 0; a < ReportingSubjects.Length; a++)
            {
               if (ReportingSubjects[a].name == name) { return a; }
            }
            return -1;
        }


        /* /-------[   function: BuildReport                           ]-------\
           | Currently, this will build Query and Language strings
           | These could be saved at the time the report is created since they will not change
           | Then used to get data, etc.
           \-------------------------------------------------------------------/
         */
        // So, a report is based on a "Subject" that has its specific "Details" (we have our special "Form" subject and direct form info from the using app)
        // Optionally, we can link in other "Subjects" with their specific "Details" (those subjects that ARE related/linked)
        public bool BuildReport(ref UIFS.ReportDefinition ReportDefinition)
        {
            string Query = "",Q_FieldSelection="SELECT ", Q_Subject="", Q_Details=" WHERE ", Q_Joins="";
            string columnnames = "";
            string Language = "";
            string[] selections;
            string Temp_Query="",Temp_Language="";
            int iFormLinkdetail, iReportingSubject; // index holders
            ReportingSubject.Detail RSDetail = new ReportingSubject.Detail(); // used to pass data for building queries
            string FormDbName = ""; // SINGULAR forms will set this.  Used in Aggregate query building

            try
            {
                // --] LOAD Needed Data [--
                Load_ReportingSubjects();


                Q_Subject = " FROM [" + FormLinks.TableName + "]"; // Our Subject clause always starts from the main table

                //. Add required id fields
                Q_FieldSelection = Q_FieldSelection + "[" + FormLinks.TableName + "].[UIFS_formid],[" + FormLinks.TableName + "].[formid],";
                columnnames = columnnames + "_UIFS_formid,_formid,";
                //. Need to add in all linked form fields, but they will (can) be hidden from the output (used for easy linking)
                foreach (FormLink.Detail FLdetail in FormLinks.Details)
                {
                    Q_FieldSelection += "["+ FormLinks.TableName+"].["+ FLdetail.field+ "],";
                    columnnames += FLdetail.name.Replace(",", "") + ","; // make sure no commas in field name
                }

                //. Start with the Main Subject
                switch (ReportDefinition.Description.lang)
                {
                    case "ALL": // all forms

                        break;
                    case "singular": // single form
                        //. load the form details
                        UIFS.Form Form = new Form(ref SQL);
                        UIFS.FormDataStruct FormData = new FormDataStruct(); // hold our data

                        // TODO: diff form version loading here...
                        if (!Form.Load(Convert.ToInt32(ReportDefinition.Description.selection),-1, ref FormData))
                        { // failed to load, end
                            SQL.WriteLog_Error(Form.ErrorEx, "failed to load specified formdata", "UIFS.Reporting.BuildReport()");
                            return false;
                        }
                        FormDbName = "[UIFS.Form_" + FormData.id.ToString() + "]";                        
                        //. Construct a field selection clause based on all fields in this form
                        // TODO: if report is on a different version other than the current form version...need to specify that.
                        //       Can get the form version by querying the chosen form....

                        //. build list of fields to extract, default is all
                        /* NOTES:
                         * - not sure if we want to use the user selected fields, or just have everything in the data output
                         */
                        // WARNING: make sure to remove comma's from fieldnames as we use this as our list separator.
                        // we do this with: .Replace(",", "");
                        string fieldid = "", fieldname="";
                        foreach (FormDataStruct.ControlListDetail formctrl in FormData.ControlList)
                        {
                            switch (formctrl.type)
                            {
                                case ControlType.Range: // TWO FIELDS/VALUES/COLUMNS
                                    fieldid = FormDbName + ".[" + formctrl.id.ToString() + "_Start]," + FormDbName + ".[" + formctrl.id.ToString() + "_End],";
                                    fieldname = FormData.Get_Control(formctrl.id).name.Replace(",", "") + "[FROM]," + FormData.Get_Control(formctrl.id).name.Replace(",", "")+"[TO]";
                                    break;
                                case ControlType.Checkbox: // checkbox can have an INPUT field...
                                    fieldid = FormDbName + ".[" + formctrl.id.ToString() + "],";
                                    fieldname = FormData.Get_Control(formctrl.id).name.Replace(",", "");
                                    break;
                                case ControlType.Textbox:
                                case ControlType.Number:
                                case ControlType.List:
                                case ControlType.DateTime:
                                case ControlType.Percentage:
                                    fieldid = FormDbName + ".[" + formctrl.id.ToString() + "],";
                                    fieldname = FormData.Get_Control(formctrl.id).name.Replace(",", "");
                                    break;
                            }
                            Q_FieldSelection += fieldid;
                            columnnames += fieldname + ","; 
                        }
                        Q_FieldSelection = Q_FieldSelection.Remove(Q_FieldSelection.Length - 1); // take out last comma

                        //. link form table by formid from formlinks main table (standard practice)
                        Q_Joins = Q_Joins + " INNER JOIN " + FormDbName + " ON " + FormDbName + ".[id] = [" + FormLinks.TableName+"].[formid] AND ";
                        Language = "A report that shows for this '" + ReportDefinition.Description.name + "': <span class='detail'>" + FormData.name + "</span><br/>";

                        if (ReportDefinition.Description.Details != null)
                        {
                            //. walk through the non-[global] details that are form specific
                            foreach (ReportDefinition.Detail detail in ReportDefinition.Description.Details)
                            {
                                if (detail.name.StartsWith("[global]")) { continue; } // skip globals (dealt with later)
                                // GET the assigned name/id for this Detail
                                // NOTE: since we are working with [Form] Subject data, this is actually an identifier(int)
                                UIFS.FormControl detail_ctrl = FormData.Get_Control(Convert.ToInt32(detail.name)); // so, get Form->Control name

                                //. specific query building for UIFS.FormControl
                                FormControl_formatqueryandlanguage(FormData.ControlList[FormData.Find_ControlListEntry_byControlID(Convert.ToInt32(detail.name))].type, detail_ctrl, detail, ref Temp_Query, ref Temp_Language);
                                //. from returned vars, build query and language 
                                Q_Joins = Q_Joins + FormDbName + "." + Temp_Query; // select using [form].[detail]
                                Language = Language + Temp_Language;
                            }
                        }
                        if (Q_Joins.EndsWith("ON ")) { Q_Joins = Q_Joins.Remove(Q_Joins.Length - 3); } // this would occur if no detailed selection was made..."no detail filter"
                        else { if (Q_Joins.EndsWith("AND ")) { Q_Joins = Q_Joins.Remove(Q_Joins.Length - 5); } }

                        Q_Details = Q_Details + " [" + FormLinks.TableName + "].UIFS_formid=" + FormData.id.ToString() + " AND ";
                        // the query detail string always adds an "AND " after each addition

                        break;
                    case "plural": // multiple forms
                        Q_Details = Q_Details + " [UIFS_formid] IS IN (";
                        selections = ReportDefinition.Description.selection.Split(new char[] { ',' });
                        foreach (string specificdetail in selections)
                        {
                            Q_Details = Q_Details + specificdetail + ",";
                        }
                        Q_Details = Q_Details.Remove(Q_Details.Length - 1); // remove last comma
                        Q_Details = Q_Details + ") AND ";
                        break;
                }

                // On Forms we have these [global] fields that the user/application has added that are linked to a specific form
                // these are contained in the same user table that has the main form linking data

                //. Now deal with [global] details
                if (ReportDefinition.Description.Details != null)
                {
                    foreach (ReportDefinition.Detail detail in ReportDefinition.Description.Details)
                    {
                        if (detail.name.StartsWith("[global]"))
                        {
                            //. Get 
                            iFormLinkdetail = FormLinks.Find_detail(detail.name.Remove(0, 8)); // index of our global detail info
                            switch (FormLinks.Details[iFormLinkdetail].type)
                            {
                                case "Subject": // need to join data from another collection
                                    //. find Subject info
                                    iReportingSubject = Find_ReportingSubject(FormLinks.Details[iFormLinkdetail].name);
                                    //. restrict our query to a selection of this subject
                                    Q_Joins = Q_Joins + " INNER JOIN [" + ReportingSubjects[iReportingSubject].db + "]" +
                                        " ON [" + ReportingSubjects[iReportingSubject].db + "].[" + ReportingSubjects[iReportingSubject].db_id + "]=[" + FormLinks.TableName + "]." + FormLinks.Details[iFormLinkdetail].field;
                                    RSDetail.db = "[" + FormLinks.TableName + "].[" + FormLinks.Details[iFormLinkdetail].field + "]";
                                    RSDetail.lang = detail.lang;
                                    RSDetail.name = ReportingSubjects[iReportingSubject].name;
                                    RSDetail.type = "id"; // currently fixed
                                    Q_Details = Q_Details + Query_FormatDetail(RSDetail, detail.selection, ref Temp_Language);
                                    Language = Language + Temp_Language;
                                    break;
                                case "Detail": // this specific field data exists in the user 'FormLink' table
                                    RSDetail.db = "[" + FormLinks.TableName + "].[" + FormLinks.Details[iFormLinkdetail].field + "]";
                                    RSDetail.lang = detail.lang;
                                    RSDetail.name = detail.name.Remove(0, 8);
                                    RSDetail.type = FormLinks.Details[iFormLinkdetail].datatype;
                                    Q_Details = Q_Details + Query_FormatDetail(RSDetail, detail.selection, ref Temp_Language);
                                    Language = Language + Temp_Language;
                                    break;
                            }
                        }
                    }
                }

                //. Details cleanup
                if (Q_Details == "WHERE ") { Q_Details = ""; }
                else
                { // remove last " AND " from query (if exists)
                    if (Q_Details.EndsWith("AND ")) { Q_Details = Q_Details.Remove(Q_Details.Length - 5); }
                }

                //. put the query together
                Query = Q_FieldSelection + Q_Subject + Q_Joins + Q_Details;

                //. Assign/Push
                ReportDefinition.query = Query;
                ReportDefinition.language = Language;
                ReportDefinition.columns = columnnames.Remove(columnnames.Length-1); // Q_FieldSelection.Substring(7);

                // --[ AGGREGATION ]--
                //
                //. build a query for each type of aggregation the user wants
                string AggrTable = "", AggrQuery = "",AggrGroup="";
                ReportDefinition.Aggregate RDA;
                if (ReportDefinition.Description.Aggregates != null)
                {
                    for (int t = 0; t < ReportDefinition.Description.Aggregates.Length; t++)
                    {
                        RDA = ReportDefinition.Description.Aggregates[t];
                        //. find our field
                        AggrTable = ""; AggrQuery = ""; AggrGroup = "";
                        if (RDA.title.StartsWith("[global]"))
                        {
                            AggrTable = FormLinks.TableName;
                        }
                        else
                        {
                            AggrTable = FormDbName;
                        }

                        switch (RDA.manipulation)
                        {
                            case "CNT":
                                switch (RDA.datatype)
                                {
                                    case "bit": // boolean sum(iF(flushot,0,1)),sum(flushot)
                                        AggrQuery = "SELECT COUNT(CASE " + AggrTable + ".[" + RDA.db + "] WHEN 0 THEN 1 ELSE null END), COUNT(CASE " + AggrTable + ".[" + RDA.db + "] WHEN 1 THEN 1 ELSE null END) ";
                                        break;
                                    default:
                                        AggrQuery = "SELECT COUNT(" + AggrTable + ".[" + RDA.db + "]) ";
                                        break;
                                }
                                break;
                            case "SUM":
                                AggrQuery = "SELECT SUM(" + AggrTable + ".[" + RDA.db + "]) ";
                                break;
                            case "AVG":
                                AggrQuery = "SELECT AVG(" + AggrTable + ".[" + RDA.db + "]) ";
                                break;
                            case "SUM_DTRANGE":
                            case "SUM_DRANGE":
                            case "SUM_TRANGE":
                                AggrQuery = "SELECT DATEADD(ms, SUM(DATEDIFF(ms,[" + RDA.db + "_Start],[" + RDA.db + "_End])),0)";
                                //AggrQuery = "SELECT SUM([" + RDA.db + "_End]-[" + RDA.db + "_Start])";
                                break;
                            case "AVG_DTRANGE":
                            case "AVG_DRANGE":
                            case "AVG_TRANGE":
                                AggrQuery = "SELECT DATEADD(ms, AVG(DATEDIFF(ms,[" + RDA.db + "_Start],[" + RDA.db + "_End])),0)";
                                break;
                        }
                        if (AggrQuery != "")
                        {
                            AggrQuery = AggrQuery + Q_Subject + Q_Joins + Q_Details + AggrGroup; // rest of query is same...except for grouping
                            RDA.query = AggrQuery;
                        }
                    }
                }

                // END BuildReport
                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "failed", "UIFS.Reporting.BuildReport()");
                return false;
            }

        }


        public bool Load_ReportingSubjects()
        {
            try
            {
                SQL.Query = SQL.SQLQuery.Reporting_LoadSubjects;
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader();
                while (SQL.sdr.Read())
                {
                    Array.Resize(ref ReportingSubjects, ReportingSubjects.Length+1);
                    this.ReportingSubjects[ReportingSubjects.Length - 1] = new ReportingSubject();
                    this.ReportingSubjects[ReportingSubjects.Length - 1].name = SQL.sdr.GetString(0);
                    this.ReportingSubjects[ReportingSubjects.Length - 1].db = SQL.sdr.GetString(1);
                    this.ReportingSubjects[ReportingSubjects.Length - 1].db_id = SQL.sdr.GetString(2);
                    this.ReportingSubjects[ReportingSubjects.Length - 1].db_idlist = SQL.sdr.GetString(3);
                    this.ReportingSubjects[ReportingSubjects.Length - 1].Details = (ReportingSubject.Detail[])JsonConvert.DeserializeObject(SQL.sdr.GetString(4), typeof(ReportingSubject.Detail[]));
                }
                SQL.sdr.Close();
                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "load failed", "UIFS.Reporting.Load_ReportingSubjects()");
                return false;
            }

        }

        public ReportDefinition Load_ReportingDefinition(long id)
        {
            ReportDefinition RD = new ReportDefinition();
            try
            {
                SQL.Query = string.Format(SQL.SQLQuery.Reporting_LoadReportingDefinition,id);
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader();SQL.sdr.Read();
                if (SQL.sdr.HasRows) {
                    RD.id = id;
                    RD.title = SQL.sdr.GetString(0);
                    RD.Description = (ReportDefinition.Subject)JsonConvert.DeserializeObject(SQL.sdr.GetString(1),typeof(ReportDefinition.Subject));
                }
                else { RD=null; // no data
                }
                SQL.sdr.Close();                
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "load failed", "UIFS.Reporting.Load_ReportingDefinition()");
                return null;
            }
            return RD;

        }
        public bool Save_ReportingDefinition(ref ReportDefinition RD, string user)
        {
            try
            {
                if (RD.id == 0) // New Report
                {
                    SQL.Query = string.Format(SQL.SQLQuery.Reporting_SaveReportingDefinition, SQL.ParseInput(RD.title), SQL.ParseInput(user), JsonConvert.SerializeObject(RD.Description), SQL.ParseInput(RD.query), SQL.ParseInput(RD.language), SQL.ParseInput(RD.columns));
                    SQL.cmd = SQL.Command(SQL.Data);
                    RD.id = Convert.ToInt32(SQL.cmd.ExecuteScalar());
                }
                else
                { // Update Report
                    SQL.Query = string.Format(SQL.SQLQuery.Reporting_UpdateReportingDefinition, RD.id, SQL.ParseInput(RD.title), JsonConvert.SerializeObject(RD.Description), SQL.ParseInput(RD.query), SQL.ParseInput(RD.language), SQL.ParseInput(RD.columns), SQL.ParseInput(user));
                    SQL.cmd = SQL.Command(SQL.Data);
                    SQL.cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Save/Update failed on id:"+RD.id+" :(", "UIFS.Reporting.Save_ReportingDefinition()");
                return false;
            }
            return true;
        }


        /* /-------[   class: FormControl_formatqueryandlanguage       ]-------\
           | based on UIFS.ControlType, we will have different ways to build
           | a query.  This being the standardized routine
           \-------------------------------------------------------------------/
         */
        public void FormControl_formatqueryandlanguage(UIFS.ControlType ctrltype, UIFS.FormControl Control, ReportDefinition.Detail detail, ref string Query, ref string Language)
        {
            string[] selections;
            ReportingSubject.Detail RSDetail = new ReportingSubject.Detail(); // used to pass data for building queries

            switch (ctrltype)
            {
                // All of the following return a single string values
                case ControlType.Textbox:
                case ControlType.DateTime:
                case ControlType.List:
                    RSDetail.db="[" + Control.id.ToString() + "]";
                    RSDetail.lang=detail.lang;
                    RSDetail.name = Control.name;
                    RSDetail.type="text";
                    Query = Query_FormatDetail(RSDetail, detail.selection, ref Language);
                    break;
                // The following return a single numeric values
                case ControlType.Percentage:
                case ControlType.Number:
                    RSDetail.db="[" + Control.id.ToString() + "]";
                    RSDetail.lang=detail.lang;
                    RSDetail.name = Control.name;
                    RSDetail.type="number";
                    Query = Query_FormatDetail(RSDetail, detail.selection, ref Language);
                    break;
                // Checkbox controls are always true/false with an optional input field
                case ControlType.Checkbox:
                    RSDetail.db="[" + Control.id.ToString() + "]";
                    RSDetail.lang=detail.lang;
                    RSDetail.name = Control.name;
                    RSDetail.type="bit";
                    Query = Query_FormatDetail(RSDetail, detail.selection, ref Language);
                    break;
                // Ranges have start/end values
                
                //TODO: this type
                case ControlType.Range:
                    switch (Language)
                    {
                        case "IS BETWEEN":
                            selections = detail.selection.Split(new char[] { ',' });
                            //Query = "[" + Control.id + "_Start]>=" + selections[0] + " AND [" + Control.id + "_End]<='" + selections[1] + "'";
                            break;
                    }
                    break;


            }
        }

        public string Query_FormatDetail(ReportingSubject.Detail detail, string selection, ref string language)
        {
            string q = ""; // pad a space?
            string[] selections;
            string selectionlang;
            DataType datatype = new DataType();
            switch (detail.lang)
            {
                case "equals":
                case "IS":
                    selectionlang = selection;
                    if (detail.type == "bit")
                    {
                        if (selection == "1") { selectionlang = "True"; } else { selectionlang = "False"; }
                    }                    
                    if (datatype.UseQuotes(detail.type))
                    {// use quotes
                        q = detail.db + "='" + selection + "' AND ";
                        language = " Where '" + detail.name + "' is: '" + selectionlang + "' <br/>";
                    }
                    else
                    { // no quotes
                        q = detail.db + "=" + selection + " AND ";
                        language = " Where '" + detail.name + "' is: " + selectionlang + " <br/>";
                    }
                    break;
                case "IS ONE OF":
                case "IS IN":
                    // convert the comma delimited list of values into an array and parse
                    selections = selection.Split(new char[] { ',' });
                    q = detail.db + " IN (";
                    foreach (string specificdetail in selections)
                    {
                        if (datatype.UseQuotes(detail.type))
                        {// use quotes
                            q = q +"'" +specificdetail + "',";
                        }
                        else
                        { // no quotes
                            q = q + specificdetail + ",";
                        }                        
                    }
                    q = q.Remove(q.Length - 1); // remove last comma
                    q = q + ") AND ";
                    language = " Where '" + detail.name + "' is one of: '" + selection + "' <br/>";
                    break;
                case "IS BETWEEN":
                    selections = selection.Split(new char[] { ',' });
                    if (datatype.UseQuotes(detail.type))
                    {// use quotes
                        q = detail.db + ">='" + selections[0] + "' AND " + detail.db + "<='" + selections[1]+"'";
                    }
                    else
                    { // no quotes
                        q = detail.db + ">=" + selections[0] + " AND " + detail.db + "<=" + selections[1];
                    }                    
                    language = " Where '" + detail.name + "' is between: '" + selections[0] + " and " + selections[1] + "' <br/>";
                    break;
                case "IS BEFORE":
                    if (datatype.UseQuotes(detail.type))
                    {// use quotes
                        q = detail.db + "<'" + selection + "' AND ";
                        language = " Where '" + detail.name + "' is before: '" + selection + "' <br/>";
                    }
                    else
                    { // no quotes
                        q = detail.db + "<" + selection + " AND ";
                        language = " Where '" + detail.name + "' is after: " + selection + " <br/>";
                    }
                    break;
                case "IS AFTER":
                    if (datatype.UseQuotes(detail.type))
                    {// use quotes
                        q = detail.db + ">'" + selection + "' AND ";
                        language = " Where '" + detail.name + "' is before: '" + selection + "' <br/>";
                    }
                    else
                    { // no quotes
                        q = detail.db + ">" + selection + " AND ";
                        language = " Where '" + detail.name + "' is after: " + selection + " <br/>";
                    }
                    break;
                
            }
            return q;
        }


        // -------------------------------------------------------------------
        // --[ Begin Report Data Outputs


        /* /-------[   function: Output_DataTables       ]-------\
           | Creates HTML and Javascript for the "DataTables" jquery plugin
           | * we can specify column settings: width, 
           | 
           \-------------------------------------------------------------------/
        */

        public string Output_DataTables(ReportDefinition RD)
        {
            string html ="",js="",data="";

            //. build table
            html = "<table cellpadding='0' cellspacing='0' border='0' class='display' id='Reporting_DataTables'>" +
                "<thead><tr>";
            string[] columns = RD.columns.Split(new char[] { ',' });
            for (int t = 0; t < columns.Length; t++)
            {
                html = html + "<th>" + columns[t] + "</th>";
            }
            html = html + "</tr></thead>"+
                "<tbody></tbody></table>";

            //. get data!
            SQL.Query=RD.query;
            SQL.cmd=SQL.Command(SQL.Data);
            SQL.sdr = SQL.cmd.ExecuteReader();
            if (SQL.sdr.HasRows)
            {
                while (SQL.sdr.Read())
                {
                    data = data + "[";
                    for (int t = 0; t < columns.Length; t++)
                    {
                        if (!SQL.sdr.IsDBNull(t))
                        {
                            data = data + JsonConvert.SerializeObject(SQL.sdr[t].ToString()) + ",";
                        }
                        else { data += "\"\","; }
                    }
                    data = data.Remove(data.Length - 1, 1) + "],";
                }
                data = data.Remove(data.Length - 1, 1);
            }
            SQL.sdr.Close();

            js = "<script type='text/javascript'>" +
                "$('#Reporting_DataTables').dataTable( {" +
                "'aoColumnDefs': [ { 'bSearchable': false, 'bVisible': false, 'aTargets': [0,1] }]," +		// 0 would be the id field...			                
                "'bJQueryUI':true," + // use jquery ui theme
                "'sPaginationType': 'full_numbers'," + // for the paging navigation (either full or 2 arrows)
                "'aLengthMenu': [[25, 50, 100, 200, -1], [25, 50, 100, 200, 'All']]," +
                "'sScrollY':'250px'," + // MUST set our height to keep this thing under control!
                "'sScrollX':'100%'," + // Set width to container size...add scrollbar in table
                "'sHeightMatch': 'none'," + // do not let calculate row height...for faster display
                //"'sAjaxSource': 'ajax.aspx?cmd=300.1'," +
                        // The next line adds a row click function (allows selection multiple)
                "'fnInitComplete': function () {$('tr').click(function () {if ($(this).hasClass('row_selected')) $(this).removeClass('row_selected'); else $(this).addClass('row_selected'); }); }," +
                "'aaData':["+data+"]"+
                "});" +
                "SubjectTable = $('#DataTables_FormSelect').dataTable();" +
                "</script>";
            return html+js;
        }

/* /-------[   function: Output_Aggregation       ]-------\
   | Can output HTML with javascript at the end.
   | We use the plugin: jqplot
   | 
   \-------------------------------------------------------------------/
*/
        public string Output_Aggregation(ReportDefinition RD)
        {
            string html = "",js="";
            AggregationDisplay ADisplay = new AggregationDisplay();
            if (RD.Description.Aggregates != null)
            {
                html = "<table cellpadding='0' cellspacing='0' border='0' class='display'>" +
                    "<thead></thead>";
                for (int t = 0; t < RD.Description.Aggregates.Length; t++)
                {
                    ReportDefinition.Aggregate RDA = RD.Description.Aggregates[t];
                    DateTime DT;
                    SQL.Query = RDA.query;
                    SQL.cmd = SQL.Command(SQL.Data);
                    //. query may return different data depending on the datatype
                    try
                    {
                        switch (RDA.manipulation)
                        {
                            case "CNT":
                                switch (RDA.datatype)
                                {
                                    case "bit":
                                        // we expect false (0) first
                                        SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                                        if (SQL.sdr.HasRows)
                                        {
                                            ADisplay.CNT_Bool_False = SQL.sdr.GetInt32(0);
                                            ADisplay.CNT_Bool_True = SQL.sdr.GetInt32(1);
                                        }
                                        // TODO: depending on Yes/No, On/Off...
                                        //html = html + "<tr><td>HOW MANY: <span class='title'>" + RDA.title + "</span></td><td>YES=" + ADisplay.CNT_Bool_True + "; NO=" + ADisplay.CNT_Bool_False + "</td></tr>";
                                        html = html + "<tr><td colspan='2'>"+
                                            "<div id='piechart_" + t.ToString() + "' style='width:300px; height:180px; margin-left:auto;margin-right:auto; '></div>" +
                                            "</td></tr>";
                                        //. build jqplot data and script
                                        js = js + "jqdata = [['YES ("+ADisplay.CNT_Bool_True+")',"+ADisplay.CNT_Bool_True+"],['NO ("+ADisplay.CNT_Bool_False+")',"+ADisplay.CNT_Bool_False+"]];";
                                        js = js + "$.jqplot('piechart_"+t.ToString()+"', [jqdata], {seriesDefaults: {renderer:$.jqplot.PieRenderer,"+
                                                "rendererOptions:{padding:10,sliceMargin:4, startAngle:-90, showDataLabels:true}},"+
                                                "legend: {show:true},"+
                                                "title: {text:'HOW MANY: " + RDA.title + "',textColor:'#FFDEAD'}" +
                                                " });";
                                        SQL.sdr.Close();
                                        break;
                                    default:
                                        ADisplay.AggrValue = Convert.ToInt32(SQL.cmd.ExecuteScalar());
                                        html = html + "<tr><td>A COUNT of: " + RDA.title + "</td><td>" + ADisplay.AggrValue + "</td></tr>";
                                        break;
                                }
                                break;
                            case "SUM":
                                ADisplay.AggrValue = Convert.ToInt32(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>A SUM of: " + RDA.title + "</td><td>" + ADisplay.AggrValue + "</td></tr>";
                                break;
                            case "AVG":
                                ADisplay.AggrValue = Convert.ToInt32(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>AVERAGE of: " + RDA.title + "</td><td>" + ADisplay.AggrValue + "</td></tr>";
                                break;

                                
                            // -- DATETIME RANGES
                            // : Time Ranges should only display Hours, Minutes (Days are invalid here)
                            // : Date & Time Ranges can display Days, Hours, Minutes...
                            case "SUM_DTRANGE":
                                DT = Convert.ToDateTime(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>SUM of: " + RDA.title + "</td><td>Days: " + DT.Day +", Hours: "+DT.Hour+", Minutes: "+DT.Minute+ "</td></tr>";
                                break;
                            case "SUM_DRANGE":
                                DT = Convert.ToDateTime(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>SUM of: " + RDA.title + "</td><td>Days: " + DT.Day + "</td></tr>";
                                break;
                            case "SUM_TRANGE":
                                DT = Convert.ToDateTime(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>SUM of: " + RDA.title + "</td><td>Hours: " + DT.Hour + ", Minutes: " + DT.Minute + "</td></tr>";
                                break;
                            case "AVG_DTRANGE":
                                DT = Convert.ToDateTime(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>AVERAGE of: " + RDA.title + "</td><td>Days: " + DT.Day + ", Hours: " + DT.Hour + ", Minutes: " + DT.Minute + "</td></tr>";
                                break;
                            case "AVG_DRANGE":
                                DT = Convert.ToDateTime(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>AVERAGE of: " + RDA.title + "</td><td>Days: " + DT.Day + "</td></tr>";
                                break;
                            case "AVG_TRANGE":
                                DT = Convert.ToDateTime(SQL.cmd.ExecuteScalar());
                                html = html + "<tr><td>AVERAGE of: " + RDA.title + "</td><td>Hours: " + DT.Hour + ", Minutes: " + DT.Minute + "</td></tr>";
                                break;

                        }
                    }
                    catch (Exception ex)
                    {
                        SQL.WriteLog_Error(ex, "Could not read data with: " + SQL.Query, "UIFS.Reporting.Output_Aggregation()");
                        if (!SQL.sdr.IsClosed) { SQL.sdr.Close(); }
                        html = html + "<tr><td>ERROR:" + ex.Message + "<br/>" + ex.StackTrace + "</td></tr>";
                    }

                }
                html = html + "</table>";
            }

            return html +"<script type='text/javascript'>" + js + "</script>";
        }
        public class AggregationDisplay
        {
            public int AggrValue;
            public int CNT_Bool_True;
            public int CNT_Bool_False;
        }

        public class DataType
        {
            public bool UseQuotes(string datatype)
            {
                switch (datatype)
                {
                    // no quotes
                    case "number":
                    case "bit":
                    case "id":
                        return false;
                }
                // default is to use quotes
                return true;

            }
            public string Phrases(string datatype)
            {
                switch (datatype)
                {
                    case "bit":
                        return "IS";
                    case "number":
                        return "IS,IS BETWEEN,IS BEFORE,IS AFTER";
                    case "text":
                        return "IS,CONTAINS";
                    case "datetime":
                    case "date":
                    case "time":
                        return "IS,IS BETWEEN,IS BEFORE,IS AFTER";
                    case "id": // This datatype is like a "foreign link"
                        return "IS,IS ONE OF";
                    case "percentage":
                        return "IS,IS BETWEEN";
                }
                return "";
            }
            // Returns the type of control that should be used to get this type of input
            public string FormEntryType(string datatype, string language) {

                //TODO: may need to run a "filter" on language if we do not import by Language Phrase defaults
                switch (language)
                {
                    case "CONTAINS": // only text
                        return "text";

                    // single value entry
                    case "IS":                    
                        switch (datatype)
                        {
                            case "number":
                                return "number";
                            case "text":
                                return "text";
                            case "datetime":
                                return "datetime"; // We need all three: datetime, date, time.  datatype is always datetime
                            case "date":
                                return "date";
                            case "time":
                                return "time";
                            case "bit":
                                return "checkbox";
                            case "id":
                                return "id";
                            case "percentage":
                                return "percentage";
                        }
                        break;
                    // double value entry (RANGES)
                    case "IS BETWEEN":
                        switch (datatype)
                        {
                            case "date":
                                return "range_date";
                            case "time":
                                return "range_time";
                            case "datetime":
                                return "range_datetime";
                            case "number":
                                return "range_number";
                            case "percentage":
                                return "range_percentage";
                        }
                        break;
                    case "IS AFTER":
                    case "IS BEFORE":
                        switch (datatype)
                        {
                            case "number":
                                return "number";
                            case "datetime":
                                return "datetime"; // We need all three: datetime, date, time.  datatype is always datetime
                            case "date":
                                return "date";
                            case "time":
                                return "time";
                            case "percentage":
                                return "percentage";
                        }
                        break;
                    // multiple value entry (LISTS)
                    case "IS ONE OF":
                        switch (datatype)
                        {
                            case "number":
                                return "list_number";
                            case "id":
                                return "list_id";
                            case "percentage":
                                return "list_percentage";
                        }
                        break;
                }
                return ""; //oops!
            }
            public string convertUIFS(string UIFSdatatype)
            {
                switch (UIFSdatatype)
                {
                    case "checkbox":
                        return "bit";
                    case "time":
                        return "time";
                    case "date":
                        return "date";
                    case "datetime":
                        return "datetime";
                    case "number":
                        return "number";
                    case "percentage":
                        return "percentage";
                    case "range":
                        return "";
                    case "list":
                    case "textbox":
                        return "text";
                    case "TimeRange":
                        return "time";
                    case "Currency":
                    case "MinMax":
                        return "number";

                }
                return "";
            }
        }

        /* /---------------[     class: Reporting.GUI          ]---------------\
         * | Functions for creating the GUI
         * | Needs to be used as follows:
         * |  1. If a new report, after creating GUI, and using Subject_Choose(), call Subject_Set()
         * |  2. If passing in an existing RD: After creating GUI, call Subject_Set()
         * \-------------------------------------------------------------------/
         */
        public class GraphicalUserInterface {

            public ReportDefinition RD; // We modify this as we go along
            public ReportCondition[] ReportConditions; // Holds all the current possible 
            public ReportShow[] ReportShowing; // a list of all possible data field manipulations for report output
            
            //. Need to "build the GUI" by creating all the possible ReportConditions
            //  then, we can save to Mem and access the dynamic controls, individually, without reloading unnecessary data
            //  this happens in Subject_Set();

            public GraphicalUserInterface(ReportDefinition RD)
            {
                this.RD = RD; // save our ReportDefinition
            }
            // Returns html for setting up a new report
            public string Subject_Choose()
            {
                //. Display a list of forms to choose from

                string html="";
                html = html + "<title, subject";
                return html;
            }

            /* /---[ Subject_Set       ]--------------------------------------------------------------------\
             * | Loads all the ReportConditions based on Subject (does not matter if a new report or existing one)
             * | 
             * \-------------------------------------------------------------------/
             * isnew    =   false if loading/editing an existing RD  (otherwise if a new Subject is chosen, TRUE to rebuild)
             * 
             */
            public bool Subject_Set(bool isnew, ref FormLink Formlinks, ReportingSubject RS, ref SQL SQL, ref ReportingSubject[] ReportingSubjects)
            {
                bool exists = false;
                ReportingSubject.Detail RSdetail;
                ReportDefinition.Detail existingRDdetail = new ReportDefinition.Detail();
                FormControl UIFSFormControl;
                if (isnew || this.ReportConditions == null)
                {
                    this.ReportConditions = new ReportCondition[0]; // starting over fresh...should this be possible?
                }

                //. setup [globals]
                foreach (FormLink.Detail FLdetail in Formlinks.Details) {
                    int iRDDd=-1; // holds the index of the RD.Desc.detail for linking the ReportCondition to the actual RD.Description...
                    switch (FLdetail.type)
                    {
                        // -[ Global Subject ]- 
                        // this behaves as a "pointer"/collection of id(s) to reference a Subject or set of Subjects to filter by...
                        // this is a UIFS standard, but for UX simplification (This "subject" has a corresponding id field that is unique to each form created)
                        case "Subject":
                            if (!isnew) { // check to see if exists in current RD
                                for (int t=0;t<RD.Description.Details.Length;t++) {
                                    if ("[global]"+FLdetail.name == RD.Description.Details[t].name)
                                    {
                                        existingRDdetail = RD.Description.Details[t]; iRDDd = t;
                                        exists = true; break;
                                    }
                                }
                            }
                            RSdetail = new ReportingSubject.Detail();
                            RSdetail.db = FLdetail.field;
                            RSdetail.name = "[global]" + FLdetail.name;
                            RSdetail.type = "id"; // this basically tells the application that this is a linked *Subject
                            Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                            ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSdetail, ReportConditions.Length - 1);
                            if (exists)
                            {
                                ReportConditions[ReportConditions.Length - 1].RDdetail = existingRDdetail;
                                ReportConditions[ReportConditions.Length - 1].iRDDdetail = iRDDd;
                                exists = false; // reset
                            }
                            //. Load subject selection data
                            int pscount=0; int iRS;
                            //. Find reporting subject 
                            for (iRS = 0; iRS < ReportingSubjects.Length; iRS++)
                            {
                                if (ReportingSubjects[iRS].name == FLdetail.name) { break; }
                            }
                            SQL.Query = ReportingSubjects[iRS].BuildQuery_IDList(); // dynamic build...
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader();
                            while (SQL.sdr.Read())
                            {
                                Array.Resize(ref ReportConditions[ReportConditions.Length - 1].PossibleValues, pscount+1);
                                ReportConditions[ReportConditions.Length - 1].PossibleValues[pscount] = new FormControl.List.Item();
                                ReportConditions[ReportConditions.Length - 1].PossibleValues[pscount].value = SQL.sdr[0].ToString();
                                ReportConditions[ReportConditions.Length - 1].PossibleValues[pscount].name = SQL.sdr[1].ToString();
                                pscount += 1;
                            }
                            SQL.sdr.Close();
                            // Report Show possibility
                            ReportShow_Add(RSdetail.db, RSdetail.name, RSdetail.type);
                            break;
                        case "Detail":
                            if (!isnew)
                            { // check to see if exists in current RD
                                for (int t = 0; t < RD.Description.Details.Length; t++)
                                {
                                    if ("[global]" + FLdetail.name == RD.Description.Details[t].name)
                                    {
                                        existingRDdetail = RD.Description.Details[t]; iRDDd = t;
                                        exists = true; break;
                                    }
                                }
                            }
                            RSdetail = new ReportingSubject.Detail();
                            RSdetail.db = FLdetail.field;
                            RSdetail.name = "[global]" + FLdetail.name;
                            RSdetail.type = FLdetail.datatype;
                            Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                            ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSdetail, ReportConditions.Length - 1);
                            if (exists)
                            {
                                ReportConditions[ReportConditions.Length - 1].RDdetail = existingRDdetail;
                                ReportConditions[ReportConditions.Length - 1].iRDDdetail = iRDDd;
                                exists = false; // reset
                            }
                            // Report Show possibility
                            ReportShow_Add(RSdetail.db, RSdetail.name, RSdetail.type);
                            break;
                    }

                } // end globals
                
                // NOTE: Right now, reporting is based off of having a UIFS.Form selection as the MAIN SUBJECT
                //    This means we are not setup to handle any generic *Subject as the main...here and in BuildReport()

                // ONLY when a SINGLE *Form* is chosen can we use its details
                if (RD.Description.lang == "singular")
                {
                    exists = false;
                    //. Now we walk through our chosen "Subject"'s Details
                    // this is a UIFS.Form
                    if (RD.Description.name == "Form")
                    { 
                        //. need to get data from UIFS.Form for advanced functionality
                        UIFS.Form UIFSForm = new Form(ref SQL);
                        UIFS.FormDataStruct UIFSFormData = new FormDataStruct();
                        DataType UIFSConvertDT = new DataType();
                        // TODO: diff form ver
                        if (!UIFSForm.Load(Convert.ToInt32(RD.Description.selection),-1, ref UIFSFormData))
                        {
                            SQL.WriteLog_Error(UIFSForm.ErrorEx, "Error loading form:" + RD.Description.selection, "UIFS.Reporting.GraphicalUserInterface.Subject_Set()");
                            return false;
                        }
                        //. walk through all form controls...
                        foreach (FormDataStruct.ControlListDetail CLD in UIFSFormData.ControlList)
                        {
                            int iRDDd = -1;
                            UIFSFormControl = UIFSFormData.Get_Control(CLD.id);
                            switch (CLD.type) {
                                case ControlType.Range: // Range Controls have 2 values
                                    FormControl.Range Ctrl_Range = (FormControl.Range)UIFSFormControl;
                                    //. create Start option
                                    RSdetail = new ReportingSubject.Detail();
                                    RSdetail.db = CLD.id.ToString()+"_Start";
                                    RSdetail.name = UIFSFormControl.name+" START";
                                    RSdetail.type = UIFSConvertDT.convertUIFS(Ctrl_Range.type.ToString()); 
                                    if (!isnew) { // check to see if exists in current RD
                                        for (int t = 0; t < RD.Description.Details.Length; t++) {
                                            if (RSdetail.db == RD.Description.Details[t].name){
                                                existingRDdetail = RD.Description.Details[t]; iRDDd = t;
                                                exists = true; break;
                                            }
                                        }
                                    }
                                    Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                                    ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSdetail, ReportConditions.Length - 1);
                                    if (exists)
                                    {
                                        ReportConditions[ReportConditions.Length - 1].RDdetail = existingRDdetail;
                                        ReportConditions[ReportConditions.Length - 1].iRDDdetail = iRDDd;
                                        exists = false; // reset
                                    }
                                    //. create End option
                                    RSdetail = new ReportingSubject.Detail();
                                    RSdetail.db = CLD.id.ToString()+"_End";
                                    RSdetail.name = UIFSFormControl.name +" END";
                                    RSdetail.type = UIFSConvertDT.convertUIFS(Ctrl_Range.type.ToString()); 
                                    if (!isnew) { // check to see if exists in current RD
                                        for (int t = 0; t < RD.Description.Details.Length; t++) {
                                            if (RSdetail.db == RD.Description.Details[t].name){
                                                existingRDdetail = RD.Description.Details[t]; iRDDd = t;
                                                exists = true; break;
                                            }
                                        }
                                    }
                                    Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                                    ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSdetail, ReportConditions.Length - 1);
                                    if (exists)
                                    {
                                        ReportConditions[ReportConditions.Length - 1].RDdetail = existingRDdetail;
                                        ReportConditions[ReportConditions.Length - 1].iRDDdetail = iRDDd;
                                        exists = false; // reset
                                    }
                                    // Report Show possibility
                                    switch (Ctrl_Range.type)
                                    {
                                        case FormControl.Range.Rangetype.DateRange:
                                        case FormControl.Range.Rangetype.TimeRange:
                                        case FormControl.Range.Rangetype.DateTimeRange:
                                            ReportShow_Add(CLD.id.ToString(), UIFSFormControl.name, "Range_DateTime");
                                            break;
                                        case FormControl.Range.Rangetype.MinMax:
                                        case FormControl.Range.Rangetype.Currency:
                                            ReportShow_Add(CLD.id.ToString(),UIFSFormControl.name,"Range_Number");//UIFSFormControl.name
                                            break;
                                    }
                                    break;
                                case ControlType.DateTime:
                                    FormControl.DateTime Ctrl_DateTime = (FormControl.DateTime)UIFSFormControl;
                                    RSdetail = new ReportingSubject.Detail();
                                    RSdetail.db = CLD.id.ToString();
                                    RSdetail.name = UIFSFormControl.name;
                                    RSdetail.type = UIFSConvertDT.convertUIFS(Ctrl_DateTime.type.ToString().ToLower()); // UIFS.ControlType..
                                    if (!isnew) { // check to see if exists in current RD
                                        for (int t = 0; t < RD.Description.Details.Length; t++) {
                                            if (RSdetail.db == RD.Description.Details[t].name){
                                                existingRDdetail = RD.Description.Details[t]; iRDDd = t;
                                                exists = true; break;
                                            }
                                        }
                                    }
                                    Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                                    ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSdetail, ReportConditions.Length - 1);
                                    ReportConditions[ReportConditions.Length - 1].UIFSFormControl = true;
                                    ReportConditions[ReportConditions.Length - 1].UIFSControl = UIFSFormControl;
                                    ReportConditions[ReportConditions.Length - 1].UIFSFormControl_type = CLD.type;
                                    if (exists)
                                    {
                                        ReportConditions[ReportConditions.Length - 1].RDdetail = existingRDdetail;
                                        ReportConditions[ReportConditions.Length - 1].iRDDdetail = iRDDd;
                                        exists = false; // reset
                                    }
                                    // Report Show possibility
                                    ReportShow_Add(RSdetail.db, RSdetail.name, RSdetail.type);
                                    break;
                                default:                                    
                                    RSdetail = new ReportingSubject.Detail();
                                    RSdetail.db = CLD.id.ToString();
                                    RSdetail.name = UIFSFormControl.name;
                                    RSdetail.type = UIFSConvertDT.convertUIFS(CLD.type.ToString().ToLower()); // UIFS.ControlType..
                                    if (!isnew) { // check to see if exists in current RD
                                        for (int t = 0; t < RD.Description.Details.Length; t++) {
                                            if (RSdetail.db == RD.Description.Details[t].name){
                                                existingRDdetail = RD.Description.Details[t]; iRDDd = t;
                                                exists = true; break;
                                            }
                                        }
                                    }
                                    Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                                    ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSdetail, ReportConditions.Length - 1);
                                    ReportConditions[ReportConditions.Length - 1].UIFSFormControl = true;
                                    ReportConditions[ReportConditions.Length - 1].UIFSControl = UIFSFormControl;
                                    ReportConditions[ReportConditions.Length - 1].UIFSFormControl_type = CLD.type;
                                    if (exists)
                                    {
                                        ReportConditions[ReportConditions.Length - 1].RDdetail = existingRDdetail;
                                        ReportConditions[ReportConditions.Length - 1].iRDDdetail = iRDDd;
                                        exists = false; // reset
                                    }
                                    // Report Show possibility
                                    ReportShow_Add(RSdetail.db,RSdetail.name,RSdetail.type);

                                    break;
                            }
                        }
                    }

                    // NOT IMPLEMENTED
                    else
                    { // generic Subject, use the Reporting DB
                        foreach (ReportingSubject.Detail RSd in RS.Details)
                        {
                            Array.Resize(ref ReportConditions, ReportConditions.Length + 1);
                            ReportConditions[ReportConditions.Length - 1] = new ReportCondition(RSd, ReportConditions.Length - 1);
                            if (!isnew)
                            { // check to see if exists in current RD
                                foreach (ReportDefinition.Detail RDdetail in RD.Description.Details)
                                {
                                    if (RSd.name == RDdetail.name)
                                    { // if an RD is loaded, this gives us the selection details
                                        ReportConditions[ReportConditions.Length - 1].RDdetail = RDdetail;
                                        break;
                                    }
                                }
                            }
                        }

                    }
                }

                // END of Subject_Set()
                return true;
            }

            // Returns a list of WHERE "options" [ReportConditions]
            // enapsulated in divs with a naming convention for ajax redisplay
            public string WHERE()
            {
                string html = "", js="", output="";
                int ijs=-1;
                // need to call ReportCondition.Edit, then split out the javascript
                //. walk through all ReportConditions
                foreach (ReportCondition RC in this.ReportConditions)
                {
                    if (RC.iRDDdetail == -1)
                    { // not used in reportdefinition already
                        output = RC.Edit();
                        ijs = output.IndexOf("<script");
                        // Sometimes javascript is returned, sometimes not. (always at end)
                        if (ijs >= 0)
                        { // separate out javascript
                            js = js + output.Substring(ijs + 31, output.IndexOf("</script>") - ijs - 31); // just get the raw javascript without tags
                            html = html + "<div id='ROpt_" + RC.id + "' class='ROpt'>" + output.Substring(0, ijs) + "</div>";
                        }
                        else
                        {
                            html = html + "<div id='ROpt_" + RC.id + "' class='ROpt'>" + output + "</div>";
                        }
                    }
                }
                return html+ "<script type='text/javascript'>" + js + "</script>";
            }

            // Returns the list of [ReportConditions] that are defined for this report
            public string ReportDefinition_Where()
            {
                string html = "", js = "", output = "";
                int ijava = -1;
                if (RD.Description.Details != null)
                {
                    for (int t = 0; t < ReportConditions.Length; t++)
                    {
                        if (ReportConditions[t].iRDDdetail != -1) {
                            output = ReportConditions[t].Display();
                            ijava = output.IndexOf("<script");
                            // Sometimes javascript is returned, sometimes not. (always at end)
                            if (ijava >= 0)
                            { // separate out javascript
                                js = js + output.Substring(ijava + 31, output.IndexOf("</script>") - ijava - 31); // just get the raw javascript without tags
                                html = html + "<div id='RDOpt_" + ReportConditions[t].id + "' class='RDOption'>" + output.Substring(0, ijava) + "</div>";
                            }
                            else
                            {
                                html = html + "<div id='RDOpt_" + ReportConditions[t].id + "' class='RDOption'>" + output + "</div>";
                            }
                        }
                    }
                }
                return html + "<script type='text/javascript'>" + js + "</script>";
            }

            // Returns the list of [ReportConditions] for selection, manipulation
            public string THATSHOWS()
            {
                string html = "", js = "", options="";

                html = html + "<div class='RShow_Header'><table><tr><td class='mani_buttons'><button class='rs_reset' onclick='RS_RESET()'>RESET</button></td>"
                    + "<td class='field_buttons'><button class='rs_all' onclick='RS_ALL()'>ALL fields</button></td></tr></table></div>";
                js = js + "$('.rs_reset').button({icons:{primary:'ui-icon-arrowrefresh-1-n'}});$('.rs_all').button({icons:{primary:'ui-icon-check'}});";
                
                //. walk through all "fields"
                for (int t=0;t<ReportShowing.Length;t++) 
                {
                    options = "";
                    switch (ReportShowing[t].datatype)
                    {
                        case "bit":
                            options = options + "<option value='CNT'>HOW MANY (a count)</option>";
                            break;
                        case "number":
                            //options = options + "<option value='CNT'>HOW MANY (a count)</option>";
                            options = options + "<option value='SUM'>SUM of values</option>";
                            options = options + "<option value='AVG'>AVERAGE of values</option>";
                            break;
                        case "text":
                            break;
                        case "datetime":
                        case "date":
                        case "time":
                            break;
                        case "id":
                            options = options + "<option value='CNT'>HOW MANY (a count)</option>";
                            options = options + "<option value='UNIQUE'>HOW MANY UNIQUE (distinct)</option>";
                            break;
                        case "percentage":
                            options = options + "<option value='SUM'>SUM of percentages</option>";
                            options = options + "<option value='AVG'>AVERAGE Percentage</option>";
                            break;
                        
                        // TODO: add different DT range types
                        // SUM_TRANGE, etc.
                        case "Range_DateTime": // we get to give the user an option of calculating time diff!
                            options = options + "<option value='SUM_DTRANGE'>SUM of Time</option>";
                            options = options + "<option value='AVG_DTRANGE'>AVERAGE Time</option>";

                            break;
                        
                        //"<option value='CNT'>HOW MANY (a count)</option>" +
                        //"<option value='SUM'>SUM of values</option>" +

                    }
                    if (options != "")
                    {
                        html =html+ "<div class='RShow'>" +
                        "<table><tr><td class='rowpad'></td>" +
                        "<td class='manipulation'><select id='RShow_"+t.ToString()+"'>"+
                        options + "</select></td>" +
                        "<td><input type='checkbox' id='RShow_" + t.ToString() + "' onclick=\"Aggregate_Add('"+t.ToString()+"')\" /></td><td class='name'>" + ReportShowing[t].title + "</td>" +
                        "</div>";
                    }
                }

                return html + "<script type='text/javascript'>" + js + "</script>";
            }


            
            // we are creating a class to encapsulate and separate out the parts for creating individual output of a [ReportingSubject.Detail]
            // PASS IN:
            // detail.name  =   as said
            // detail.type  =   datatype
            // detail.lang  =   ?string comma delimited ARRAY of all possible user-controlled PHRASES (i.e. not the defaults)
            // detail.db    =   ?


            // UIFSFormControl  =   if this is TRUE, UIFS.Form actions will be applied to this control?
            // Control      =   Holds the needed data for creating the html

            public class ReportCondition
            {
                // OPTIONS
                public bool _output = true; // true if included in output

                public ReportingSubject.Detail detail;
                public int id; // holds our unique identifier (actually is just an array index)
                public bool UIFSFormControl = false; // default is false = this is not a UIFSFormControl
                public UIFS.ControlType UIFSFormControl_type; // set on creation if this condition comes from a UIFS FormControl
                public UIFS.FormControl UIFSControl; // set on creation, hold origional UIFS.FormControl details
                public UIFS.FormControl Control; // contains the details we need to create a control to get input...
                public UIFS.ControlType Control_type; // is set when Edit() runs to hold the type of Control created dynamically for validation/submission routines
                public ReportDefinition.Detail RDdetail; // hold the chosen detail
                public int iRDDdetail = -1; // index of ReportDefinition detail
                public FormControl.List.Item[] PossibleValues; // IF the condition is such that it can/will display a list of values to choose from...

                public ReportCondition(ReportingSubject.Detail detail, int id) {
                    this.detail = detail; this.id = id;
                }
                
                // For Editing:
                // RDdetail     =   need to know what is selected (if anything yet)

                public string Edit() {
                    string prompt="",html="", js="", phraseselect="";
                    string[] phrases;
                    DataType datatype = new DataType();
                    UIFS.Form_Output FormOut = new Form_Output();
                                        
                    FormControl.Checkbox Ctrl_Checkbox;
                    FormControl.DateTime Ctrl_DateTime;
                    FormControl.Number Ctrl_Number;                    
                    FormControl.Percentage Ctrl_Percentage;
                    FormControl.List Ctrl_List;
                    FormControl.Range Ctrl_Range;
                    FormControl.Textbox Ctrl_Textbox;

                    // Load Phrases for this datatype, if phrase selection data does not exist ... set to default
                    phrases = datatype.Phrases(detail.type).Split(new char[] { ',' });
                    if (RDdetail == null)
                    {
                        RDdetail = new ReportDefinition.Detail();
                        RDdetail.lang = phrases[0]; // first language phrase
                    }                    
                    prompt = "<span class=\"name\">" + detail.name + "</span> ";
                    // Language selection
                    //: currently based on datatype
                    prompt = prompt + "<select id=\""+this.id.ToString()+"_phrase\" onchange=\"Option_Redraw('"+this.id.ToString()+"'); \">";                    
                    foreach (string phrase in phrases)
                    {
                        if (RDdetail.lang == phrase) { phraseselect = " selected=\"1\" "; } else { phraseselect = ""; }
                        prompt = prompt + "<option value=\"" + phrase + "\" " + phraseselect + ">" + phrase + "</option>";
                    }
                    prompt = prompt + "</select>";
                    

                    // --[  Builds a dynamic UIFS.FormControl we use to get input needed to build form  ]
                    // entry type...based on language, then datatype
                    string CtrlEntryType = datatype.FormEntryType(detail.type, RDdetail.lang);
                    switch (CtrlEntryType)
                    {
                        // possibly have two different methods 
                        // 1: for getting data specific to UIFS.Form (this way we can mirror our control properties)
                        // 2: for generic Subject-Detail

                        case "id":
                        case "list_id":
                            // GLOBAL identifiers are user defined formlinks to id lists of this Subject type...
                            //. Use a list control
                            if (detail.name.StartsWith("[global]")) {
                                Ctrl_List = new FormControl.List();
                                Ctrl_List.id = this.id;
                                Ctrl_List.prompt = prompt;
                                Ctrl_List.tip = "Please choose your Subject";
                                Ctrl_List.type = FormControl.List.listtype.dropdown;
                                Ctrl_List.Items = PossibleValues;
                                FormOut.HTML_FormControl(ControlType.List, Ctrl_List, ref html, ref js);
                                Control = Ctrl_List; Control_type = ControlType.List;
                            }                            
                            break;
                        case "checkbox":
                            if (UIFSFormControl) { 
                                Ctrl_Checkbox = (FormControl.Checkbox)UIFSControl;
                                Ctrl_Checkbox.hasinput = false; // we do not want this 
                            }
                            else { 
                                Ctrl_Checkbox = new FormControl.Checkbox(); }
                            Ctrl_Checkbox.id = this.id;
                            Ctrl_Checkbox.prompt = prompt; //TEST: we want to use this as part of our Control div if possible
                            Ctrl_Checkbox.tip = "Choose one or the other";
                            FormOut.HTML_FormControl(ControlType.Checkbox, Ctrl_Checkbox, ref html, ref js); // builds html for control
                            Control = Ctrl_Checkbox; Control_type = ControlType.Checkbox;
                            break;
                        case "number":
                            if (UIFSFormControl){Ctrl_Number = (FormControl.Number)UIFSControl; }
                            else { Ctrl_Number = new FormControl.Number(); }
                            Ctrl_Number.id = this.id; Ctrl_Number.prompt = prompt; 
                            Ctrl_Number.tip = "Please choose a number between: " + Ctrl_Number.min.ToString() + " AND " + Ctrl_Number.max.ToString();
                            FormOut.HTML_FormControl(ControlType.Number, Ctrl_Number, ref html, ref js);
                            Control = Ctrl_Number; Control_type = ControlType.Number;
                            break;
                        case "datetime":
                        case "date":
                        case "time":
                            if (UIFSFormControl) {Ctrl_DateTime = (FormControl.DateTime)UIFSControl; }
                            else { Ctrl_DateTime = new FormControl.DateTime();}
                            Ctrl_DateTime.id = this.id;
                            Ctrl_DateTime.prompt = prompt;
                            Ctrl_DateTime.tip = "Please select a date/time";                
                            switch (CtrlEntryType)
                            {
                                case "datetime":
                                    Ctrl_DateTime.type = FormControl.DateTime.datetimetype.datetime;
                                    break;
                                case "date":
                                    Ctrl_DateTime.type = FormControl.DateTime.datetimetype.date;
                                    break;
                                case "time":
                                    Ctrl_DateTime.type = FormControl.DateTime.datetimetype.time;
                                    break;
                            }                            
                            FormOut.HTML_FormControl(ControlType.DateTime, Ctrl_DateTime, ref html, ref js);
                            Control = Ctrl_DateTime; Control_type = ControlType.DateTime;
                            break;
                        case "text":
                            if (UIFSFormControl)
                            { // if 
                                switch (UIFSFormControl_type)
                                {
                                    case ControlType.List: // List Controls are basically text field values; which is what the input value is
                                        Ctrl_List = (FormControl.List)UIFSControl;
                                        Ctrl_List.id = this.id;
                                        Ctrl_List.prompt = prompt;
                                        FormOut.HTML_FormControl(ControlType.List, Ctrl_List, ref html, ref js);
                                        Control = Ctrl_List; Control_type = ControlType.List;
                                        break;
                                    case ControlType.Textbox:
                                        Ctrl_Textbox = (FormControl.Textbox)UIFSControl;
                                        Ctrl_Textbox.id = this.id;
                                        Ctrl_Textbox.prompt = prompt;
                                        FormOut.HTML_FormControl(ControlType.Textbox, Ctrl_Textbox, ref html, ref js);
                                        Control = Ctrl_Textbox; Control_type = ControlType.Textbox;
                                        break;
                                }
                            }
                            else
                            { // default
                                Ctrl_Textbox = new FormControl.Textbox();
                                Ctrl_Textbox.id = this.id;
                                Ctrl_Textbox.prompt = prompt;
                                Ctrl_Textbox.tip = "value to look for...";
                                FormOut.HTML_FormControl(ControlType.Textbox, Ctrl_Textbox, ref html, ref js);
                                Control = Ctrl_Textbox; Control_type = ControlType.Textbox;
                            }
                            break;
                        //NOTE: should this be allowed to be a generic?
                        case "percentage":
                            if (UIFSFormControl) { Ctrl_Percentage = (FormControl.Percentage)UIFSControl; }
                            else { Ctrl_Percentage = new FormControl.Percentage(); }
                            Ctrl_Percentage.id = this.id;
                            Ctrl_Percentage.prompt = prompt;
                            Ctrl_Percentage.interval = 1; // allow to select all values
                            Ctrl_Percentage.tip = "select a percentage value";
                            FormOut.HTML_FormControl(ControlType.Percentage, Ctrl_Percentage, ref html, ref js);
                            Control = Ctrl_Percentage; Control_type = ControlType.Percentage;
                            break;
                        case "range_number":
                        case "range_percentage":
                            Ctrl_Range = new FormControl.Range();
                            if (CtrlEntryType == "range_percentage")
                            {
                                Ctrl_Range.min = 0; Ctrl_Range.max = 100;
                                Ctrl_Range.tip = "Please choose your percentage range";
                            }
                            else
                            {
                                if (UIFSFormControl)
                                {
                                    if (this.UIFSFormControl_type == ControlType.Number)
                                    {
                                        Ctrl_Number = (FormControl.Number)UIFSControl;
                                        Ctrl_Range.min = Ctrl_Number.min; Ctrl_Range.max = Ctrl_Number.max; // get values from UIFS control properties
                                        Ctrl_Range.tip = Ctrl_Number.tip;
                                    }
                                }
                                else
                                {
                                    Ctrl_Range.min = 0; Ctrl_Range.max = 1000; // default
                                    Ctrl_Range.tip = "Please choose your range";
                                }
                            }
                            Ctrl_Range.id = this.id; Ctrl_Range.prompt = prompt; 
                            Ctrl_Range.type = FormControl.Range.Rangetype.MinMax;                                    
                            FormOut.HTML_FormControl(ControlType.Range, Ctrl_Range, ref html, ref js);
                            Control = Ctrl_Range; Control_type = ControlType.Range;
                            break;
                        case "range_time":
                        case "range_date":
                        case "range_datetime":
                            Ctrl_Range = new FormControl.Range();
                            if (UIFSFormControl) {
                                if (this.UIFSFormControl_type == ControlType.DateTime)
                                {
                                    Ctrl_DateTime = (FormControl.DateTime)UIFSControl;
                                    Ctrl_Range.tip = Ctrl_DateTime.tip;
                                    switch (Ctrl_DateTime.type)
                                    {
                                        case FormControl.DateTime.datetimetype.time:
                                            Ctrl_Range.type = FormControl.Range.Rangetype.TimeRange;
                                            break;
                                        case FormControl.DateTime.datetimetype.date:
                                            Ctrl_Range.type = FormControl.Range.Rangetype.DateRange;
                                            break;
                                        case FormControl.DateTime.datetimetype.datetime:
                                            Ctrl_Range.type = FormControl.Range.Rangetype.DateTimeRange;
                                            break;
                                    }
                                }
                            }
                            else{ // non-UIFS control
                                Ctrl_Range.tip = "Please choose your range";
                                switch (CtrlEntryType)
                                {
                                    case "range_time":
                                        Ctrl_Range.type = FormControl.Range.Rangetype.TimeRange;
                                        break;
                                    case "range_date":
                                        Ctrl_Range.type = FormControl.Range.Rangetype.DateRange;
                                        break;
                                    case "range_datetime":
                                        Ctrl_Range.type = FormControl.Range.Rangetype.DateTimeRange;
                                        break;
                                }
                            }
                            Ctrl_Range.id = this.id; Ctrl_Range.prompt = prompt;
                            FormOut.HTML_FormControl(ControlType.Range, Ctrl_Range, ref html, ref js);
                            Control = Ctrl_Range; Control_type = ControlType.Range;
                            break;
                        case "list_number":
                            break;
                        default:
                            return "";
                    }
                    Control.id = this.id; // mirror for simplification in ajax routines

                    //TODO: temp to show what exists if it does not find its way through
                    if (html == "") { html = "<div id='"+detail.name+"'>"+prompt+"</div>"; }
                    // USE button                    
                    html = "<table class='selection' onMouseover=\"ToggleButton(this,1);\" onMouseout=\"ToggleButton(this,0);\" ><tr><td class='input'>" + html + "</td>" +
                        "<td class='buttons'><div class='button' onMousedown=\"ToggleButton(this.parentNode,2);\" onMouseup=\"ToggleButton(this.parentNode,3);\" onclick=\"Option_Use(" + this.id + ",'" + Control_type.ToString() + "');\">USE</div></td>" +
                        "</tr></table>";

                    // return combined (if js exists)
                    if (js == "") { return html; }
                    else { return html + "<script type='text/javascript'>" + js + "</script>"; }
                }

                public string Display()
                {
                    string html="",js="";
                    string name = "", lang = "", selection = "", buttons="";
                    string[] selections;
                    switch (detail.type) {
                        case "bit":
                            //. is UIFS?
                            if (UIFSFormControl)
                            { 
                                FormControl.Checkbox Ctrl_Checkbox = (FormControl.Checkbox)UIFSControl;
                                switch (Ctrl_Checkbox.type) {
                                    case FormControl.Checkbox.checkboxtype.standard:
                                        if (RDdetail.selection == "1") { selection = "CHECKED"; } else { selection = "NOT CHECKED"; }
                                        break;
                                    case FormControl.Checkbox.checkboxtype.OnOff:
                                        if (RDdetail.selection == "1") { selection = "ON"; } else { selection = "OFF"; }
                                        break;
                                    case FormControl.Checkbox.checkboxtype.YesNo:
                                        if (RDdetail.selection == "1") { selection = "YES"; } else { selection = "NO"; }
                                        break;
                                }
                                name = detail.name; // Actual control name...the reportdefinition contains the control id for the identifier
                                lang = RDdetail.lang;
                            }
                            else { // non UIFS
                                name = detail.name; lang = RDdetail.lang; selection = RDdetail.selection;  
                            }
                            break;
                        // Can output these values as they are
                        case "number":                        
                        case "datetime":
                        case "percentage":
                            name = detail.name; lang = RDdetail.lang; selection = RDdetail.selection;
                            switch (lang)
                            {
                                case "IS":
                                    selection = RDdetail.selection;
                                    break;
                                case "IS BETWEEN":
                                    selections = RDdetail.selection.Split(new char[] { ',' });
                                    if (detail.type == "percentage") { selections[0] = selections[0] + "%"; selections[1] = selections[1] + "%"; }
                                    selection = selections[0] + " <span class='lang'>AND</span> " + selections[1];
                                    break;
                            }
                            break;
                        case "id": // This datatype is like a "foreign link"
                            name = detail.name;lang = RDdetail.lang;   
                            //. Need to retrieve a list...
                            selections = RDdetail.selection.Split(new char[] { ',' });
                            for (int t = 0; t < selections.Length; t++){                            
                                foreach (FormControl.List.Item LI in PossibleValues) {                                
                                    if (LI.value == selections[t])
                                    {
                                        selection = selection + LI.name+"; ";
                                        break;
                                    }
                                }
                            }
                            selection = selection.Substring(0, selection.Length - 2); // remove last semicolon and space
                            break;
                        case "text":
                            switch (lang)
                            {
                                case "CONTAINS":
                                    break;
                            }
                            name = detail.name;;lang = RDdetail.lang;selection = RDdetail.selection;
                            break;
                    }
                    // build button
                    //buttons = "<button>Remove</button>";
                    buttons = "<div class='button' onMousedown=\"ToggleButton(this.parentNode,2);\" onMouseup=\"ToggleButton(this.parentNode,3);\" onclick=\"Option_Remove(" + this.id + ");\">Remove</div>";

                    html = html + "<table onMouseover=\"ToggleButton(this,1);\" onMouseout=\"ToggleButton(this,0);\" >" +
                        "<tr><td class='name'>" + name + "</td> <td class='lang'>" + lang + "</td> <td class='selection'>" + selection+"</td><td class='but'>"+buttons+"</td> </tr></table>";
                    js = js + "$('#ReportDefinition td.but button').button();";

                    // return combined (if js exists)
                    if (js == "") { return html; }
                    else { return html + "<script type='text/javascript'>" + js + "</script>"; }
                }
                

            }
            public class ReportShow {
                public int iAggr; // if set, we display
                public string db; // our field identifier(s)...comma delimited
                public string title; // either expanded field name or...
                public string datatype; // tells us what we can do with this data (if we do not have this, we do not know how to manipulate the report graphical display)
                public string manipulation; // CNT, SUM, AVG
                public Visualize Visualization;

                    public enum Visualize : byte {
                        Number = 0,
                        PIE = 1,
                        DATETIME = 2
                    }
            }
            public void ReportShow_Add(string db, string title, string datatype) {
                if (ReportShowing == null) {
                    Array.Resize(ref ReportShowing,1);
                } else {
                Array.Resize(ref ReportShowing,ReportShowing.Length+1);
                }
                ReportShowing[ReportShowing.Length-1] = new ReportShow();
                ReportShowing[ReportShowing.Length-1].db = db;
                ReportShowing[ReportShowing.Length-1].title = title;
                ReportShowing[ReportShowing.Length-1].datatype = datatype;                
            }
        }

        public void aReportOn_UIFSForm(int formid)
        {
            UIFS.Form Form = new Form(ref SQL);
            UIFS.FormDataStruct FormData = new FormDataStruct();
            SQL.OpenDatabase();
            // TODO: diff form ver?
            if (!Form.Load(formid,-1, ref FormData))
            { // failed to load, end
                SQL.WriteLog_Error(Form.ErrorEx, "failed to load specified formdata:"+formid.ToString(), "UIFS.Reporting.aReportOn_UIFSForm()");
            }
            SQL.CloseDatabase();
            aReportOn = "Form: "+FormData.name;            
        }

    }
    // UIFS.Reporting main class ends here
    /// <summary>
    /// UIFS.Reporting: interface for reporting functions (needs ref to SQL)
    /// </summary>




    /* /---------------[     class: ReportDefinition       ]---------------\
     * | Designed to be a compilation of Subjects and their Details with
     * | "sub" subjects for linking related information.
     * \-------------------------------------------------------------------/
     * 
    *
  * --| These are our common attributes that assist us in interpreting between the user and the application
     * 
  * ) "name" : Subject/Detail   = our object to pull data on
  * ) "lang" : Language         = how we (and the user) are selecting this subject/detail with describing language:  "THIS", "THESE", "WHERE", "FROM"
  * ) "selection" : Selection   = either equals "*" for ALL this Subject, or a specific list selection of this subject
  * 
  * 
  * */
    public class ReportDefinition
    {
        public long id=0;
        public string title; // What the user has titled the report
        public Subject Description; // This contains all the data necessary for storing/re-creating: the "Language" piece; the "DataQuery" piece; 
        public string query; // pre-compiled data query
        public string language; // pre-compiled language: the complete "language" of the report OR "human-readable translation"
        public string columns; // a list of columns that will be in the report output..display


        public class Subject
        {
            public string name; // name of the subject we are working with (identifier)
            public string lang; // selection language: tells us how/which subjects were chosen
            public string selection; // exactly which Subject(s) are chosen (list of identifiers)
            public Detail[] Details; // All the details with specific filtering selections
            public Aggregate[] Aggregates; // our aggregated data for the base report output 
            public Subject[] SecondarySubject; // 

            public int AddDetail(Detail detail)
            {
                if (Details == null) Array.Resize(ref Details, 1); else Array.Resize(ref Details, Details.Length + 1);
                Details[Details.Length - 1] = new Detail();
                Details[Details.Length - 1] = detail;
                return Details.Length - 1;
            }
            public void DelDetail(string detail_name)
            {
                // if it is the ONLY control on the form
                if (Details.Length == 1)
                {
                    Details = null;
                } // otherwise, we walk through array
                else
                {
                    // we can leave the array the same until we reach the control to be removed
                    int t, a;
                    for (t = 0; t < Details.Length; t++)
                    {
                        if (Details[t].name == detail_name)
                        {
                            break;
                        }
                    }
                    // check if at the end of array..otherwise, shift remaining array
                    if (t == Details.Length - 1)
                    {
                        Array.Resize(ref Details, t); // remove by resizing without last element
                    }
                    else
                    {
                        // for the remainder, just copy the next over the previous
                        for (a = t; a < Details.Length - 1; a++)
                        {
                            Details[a] = Details[a + 1];
                        }
                        // now resize array 1 size smaller
                        Array.Resize(ref Details, a);
                    }
                }
            }
            public int AddAggregrate(Aggregate aggregate)
            {
                if (Aggregates == null) Array.Resize(ref Aggregates, 1); else Array.Resize(ref Aggregates, Aggregates.Length + 1);
                Aggregates[Aggregates.Length - 1] = new Aggregate();
                Aggregates[Aggregates.Length - 1] = aggregate;
                return Aggregates.Length - 1;
            }
            public void DelAggregate(int iAggr)
            {
                // if it is the ONLY one
                if (Aggregates.Length == 1)
                {
                    Aggregates = null;
                } // otherwise, we walk through array
                else
                {
                    int a;
                    // check if at the end of array..otherwise, shift remaining array
                    if (iAggr == Aggregates.Length - 1)
                    {
                        Array.Resize(ref Aggregates, iAggr); // remove by resizing without last element
                    }
                    else
                    {
                        // for the remainder, just copy the next over the previous
                        for (a = iAggr; a < Aggregates.Length - 1; a++)
                        {
                            Aggregates[a] = Aggregates[a + 1];
                        }
                        // now resize array 1 size smaller
                        Array.Resize(ref Aggregates, a);
                    }
                }
            }
        }
        // This holds all the specific data field selections...
        public class Detail
        {
            public string name; // name is our identifier...
            public string lang; // language for data query...
            public string selection; // our chosen filter(s)
        }
        // [CLASS: Aggregate]
        // Desc: A list of all possible "outputs" from data fields (UIFS.Control - adds Ranges (2 values/2 fields), etc.)
        public class Aggregate
        {
            public string db; // our identifier(s)...comma delimited
            public string title; // either expanded field name or...
            public string datatype; // tells us what we can do with this data (if we do not have this, we do not know how to manipulate the report graphical display)
            public string manipulation; // CNT, SUM, AVG
            public string query; // pre-compiled for fast delivery
        }
    }

    public class FormLink
    {
        public string TableName;
        public Detail[] Details;

        public class Detail
        {
            public string name;
            public string field;
            public string type;
            public string datatype;
        }

        public int Find_detail(string name)
        {
            for (int a = 0; a < Details.Length;a++ )
            {
                if (Details[a].name == name) { return a; }
            }
            return -1;
        }
    }

    public class ReportingSubject
    {
        public string name;
        public string db;
        public string db_id;
        public string db_idlist;
        public Detail[] Details;        

        public class Detail
        {
            public string name;
            public string db;
            public string type;
            public string lang;
        }

        // Builds a query that will return a list of this Subject's *ids* and their equivalent interpreted readable format for user selection
        // :: Dynamically configured from the Subject's properties
        // NOTE: This exists in the "live" ReportingSubject instance because you may want to expand to include queries for different db systems...
        public string BuildQuery_IDList()
        {
            string Query;
            Query = "SELECT [" + db_id + "]," + db_idlist + " FROM [" + db + "]";
            return Query;
        }
    }

}