using System;
using System.Collections.Generic;
using System.Web;

namespace UIFS
{
    /* ***___ Form Reports ___***
     *
     * DESC: Provides a way to automatically generate reports based on forms
     * 
     * TODO: I want this to eventually build *ReportDefinitions* that can then be manipulated on their own, but some specific func. does not exist in UIFS.Reporting, YET!
     * 
    */  
    public class Form_Reports
    {
        /* ***___ DateAxis ___***
         * 
         * DESC: Plots values along a date axis
         * 
         * --[ PARAMS ]--
         * type: the type of DateAxis graph....
         * GroupingInterval: set to the # of days you want the results grouped into (simple way to dynamically group results by any # of days with one variable)
         * data: required set of identifiers 
         * 
         */
        public class DateAxis
        {
            public DateAxis_type type=DateAxis_type.undefined;
            public int GroupingInterval = 1; // # of days to group data results into
            public string SQLQuery; 
            public string FieldList; // built by BuildReport for calling routine

            private UIFS.FormDataStruct FormData; // Holds all the form information we will need
            private string db_TableName;
            private string db_FieldName_UIFSFormid;
            private string db_FieldName_formid;
            private string db_FieldName_date;
            private string db_query_ConditionalJoins;
            private DateTime StartDate;
            private DateTime EndDate;
            // Optionals
            private string db_timerangefield;
            private string db_boolfieldlist;
            private string Aggregate;

            // --[ Constructor ]--
            public DateAxis(ref FormDataStruct FormData, string dbTableName, string dbFieldName_UIFSFormid, string dbFieldName_formid, string dbFieldName_date, string dbConditionalJoins
                , DateTime StartDate
                , DateTime EndDate
                // Optional params here (Optional depending on what type of report you are calling)
                ,string db_timerangefield=""
                ,string db_boolfieldlist=""
                ,string Aggregate="SUM" // SUM or AVG
                )
            {
                this.FormData = FormData;
                this.db_TableName = dbTableName;
                this.db_FieldName_UIFSFormid = dbFieldName_UIFSFormid;
                this.db_FieldName_formid = dbFieldName_formid;
                this.db_FieldName_date = dbFieldName_date;
                this.db_query_ConditionalJoins = dbConditionalJoins;
                this.StartDate=StartDate; this.EndDate=EndDate;
                // Optionals
                this.db_timerangefield = db_timerangefield;
                this.db_boolfieldlist = db_boolfieldlist; // TODO: eventually we either want to expand this to a field/desiredvalue array (in order to incorporate other field types), or ...
                this.Aggregate = Aggregate;
            }

            public void BuildReport()
            {
                string Q_Interval = "DECLARE @IntervalMinutes INT SET @IntervalMinutes="+(60 * 24 * GroupingInterval)+ " ";
                string Q_Fields = "";
                string Q_FROM = " FROM [" + db_TableName + "]";
                string Q_WHERE = " WHERE [" + db_TableName +"].[" + db_FieldName_UIFSFormid + "]="+FormData.id.ToString()+ // specific form...
                    " AND [" + db_TableName + "].[" + db_FieldName_date + "] >= '"+StartDate.ToShortDateString()+"' AND [" + db_TableName + "].[" + db_FieldName_date + "] <= '"+EndDate.ToShortDateString()+"' "  // specific date range
                    ;
                    //" WHERE [" + db_formid_FieldName + "] IN ()";
                string Q_JOINS = db_query_ConditionalJoins;
                string Q_GROUPING = " GROUP BY dbo.GroupByMinutes([" + db_TableName + "].[" + db_FieldName_date + "],@IntervalMinutes)"+
                    " ORDER BY dbo.GroupByMinutes([" + db_TableName + "].[" + db_FieldName_date + "],@IntervalMinutes)";
                string FieldList="";

                // This is optional and required for different types                
                string[] boolfieldlist = this.db_boolfieldlist.Split(new char[] { ',' });

                // take 
                switch (this.type)
                {
                    /* ------[ boolean ]------
                    * :: simply creates an aggregate report of a count of bool fields = true
                    * PARAMS Needed: 
                    *  - A set of dates to pull data from specific forms..
                    * PARAMS Optional: 
                    *  - the Aggregate
                    *  - A list of the bool fields you want calculated
                    */
                    case DateAxis_type.boolean: // ControlTypes: Checkbox
                        Q_Fields = "";
                        bool AddControl;
                        for (int t = 0; t < this.FormData.ControlList.Length; t++)
                        {
                            AddControl = false;
                            if (this.db_boolfieldlist != "")
                            {
                                // Only add control if it is in the list!
                                for (int a = 0; a < boolfieldlist.Length; a++)
                                {
                                    if (FormData.ControlList[t].id.ToString() == boolfieldlist[a]) { AddControl = true; }
                                }
                            }
                            else
                            {
                                if (FormData.ControlList[t].type == ControlType.Checkbox) {AddControl = true;}
                            }
                            if (AddControl)
                            { // We get a sum of when the bit is true
                                Q_Fields = Q_Fields + Aggregate + "(CASE WHEN [" + FormData.Checkbox[FormData.ControlList[t].index].id + "]=1 THEN 1 ELSE 0 END),";
                                FieldList = FieldList + FormData.Checkbox[FormData.ControlList[t].index].name + ",";
                            }
                        }
                        Q_JOINS = " INNER JOIN [UIFS.Form_" + FormData.id.ToString() + "] ON [UIFS.Form_" + FormData.id.ToString() + "].id=[" + db_TableName +"].[" + db_FieldName_formid + "]";
                        break;
                    /* ------[ timerange ]------
                     * When a DateTime-Range field is the "basis":
                     * :: we want to compare against other field values and return a SUM or AVG of the TIME span recorded with that record
                     * PARAMS Needed: 
                     *  - The TimeRange control field: A distinctly selected set of date/time db fields for the range (the basis)
                     *  - A set of dates to pull data from specific forms..
                     *  - A list of the bool fields you want calculated
                    */
                    case DateAxis_type.timerange:
                        for (int t = 0; t < this.FormData.ControlList.Length; t++)
                        {
                            // Check our list for this control
                            for (int a = 0; a < boolfieldlist.Length; a++)
                            {
                                // So we are here collecting TimeSpent info for each boolean type that is "true"
                                if (FormData.ControlList[t].id.ToString() == boolfieldlist[a])
                                { // We get a sum of when the bit is true
                                    Q_Fields = Q_Fields + Aggregate+"(dbo.CalcTimeDiff(Q" + t.ToString() + ".[" + db_timerangefield + "_Start]," +
                                    "Q" + t.ToString() + ".[" + db_timerangefield + "_End])),";
                                    FieldList = FieldList + FormData.Checkbox[FormData.ControlList[t].index].name + ",";
                                    // also add the left outer join table with field condition
                                    Q_JOINS += " LEFT OUTER JOIN [UIFS.Form_" + FormData.id.ToString() + "] AS [Q" + t.ToString() + "] ON [Q" + t.ToString() + "].id=[" + db_TableName + "].[" + db_FieldName_formid + "]" +
                                        " AND [Q" + t.ToString() + "].[" + FormData.Checkbox[FormData.ControlList[t].index].id + "]=1";
                                    break;
                                }
                            }
                        }
                        break;                    
                }
                Q_Fields = " SELECT "+ Q_Fields + "dbo.GroupByMinutes([" + db_TableName + "].[" + db_FieldName_date + "],@IntervalMinutes)";
                FieldList = FieldList.Remove(FieldList.Length - 1);

                this.SQLQuery = Q_Interval + Q_Fields + Q_FROM + Q_JOINS + Q_WHERE+ Q_GROUPING;
                this.FieldList = FieldList;
            }
        }
        public enum DateAxis_type : int
        {
            undefined = 0,
            boolean = 1,
            timerange = 2,
        }
        public class DateAxis_data
        {
            public long formid;
            public DateTime date;
        }

    }
}