using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Security.Principal;


namespace UIFS
{
    public class SQL
    {
        // -- Global Variables
        public string Query;
        public SqlDataReader sdr, sdr2;
        public SqlCommand cmd;
        public SqlConnection Data;
        public SQLQueries SQLQuery;

        public SQL(string ConnectionString)
        {
            // CONSTRUCTOR LOGIC
            Data = new SqlConnection(ConnectionString);
            SQLQuery = new SQLQueries();
        }


        public void OpenDatabase()
        {
            if (Data.State == ConnectionState.Closed)
            {
                Data.Open();
            }
        }

        public void CloseDatabase()
        {
            Data.Close();
        }

        public SqlCommand Command(SqlConnection DataSource)
        {
            return new SqlCommand(Query, DataSource);
        }

        public class SQLQueries
        {
            // 
            public string FormLIST = "SELECT id, currentversion, name, description, active, created, createdby, lastmodified, lastmodifiedby FROM [UIFS.Form] ORDER BY created DESC";

            // Form Operations
            public string Form_Load = "SELECT currentversion, name, description, created FROM [UIFS.Form] WHERE id={0}";
            public string Form_LoadControlList = "SELECT id, type, ordernum, version FROM [UIFS.FormControls] AS FC1 WHERE formid={0} AND version = (SELECT MAX(version) FROM [UIFS.FormControls] WHERE formid={0} AND id=FC1.id) AND active=1";
            public string Form_LoadControlList_byversion = "SELECT id, type, ordernum, version FROM [UIFS.FormControls] AS FC1 WHERE formid={0} AND id IN (SELECT [controlid] FROM [UIFS.FormVersionHistory] WHERE formid={0} AND formversion={1}) AND version = (SELECT [controlversion] FROM [UIFS.FormVersionHistory] WHERE formid={0} AND formversion={1} AND FC1.id=[controlid])";
            public string Form_GetNextAvailableControlID = "EXEC [UIFS.SP_Form_GetNextAvailableControlID] {0}";
            public string Form_Create = "INSERT INTO [UIFS.Form] (currentversion, name, description, createdby) VALUES({0}, '{1}', '{2}', '{3}') SELECT @@IDENTITY";
            public string Form_Update = "UPDATE [UIFS.Form] SET name='{1}', description='{2}', currentversion={3}, lastmodified=GETDATE(), lastmodifiedby='{4}' WHERE id={0}";
            public string Form_GetVersion = "SELECT currentversion FROM [UIFS.Form] WHERE id={0}";

            // Control queries
            public string Form_ControlcurrentVersion = "SELECT version FROM [UIFS.FormControls] WHERE ACTIVE=1 AND formid={0} AND id={1}";

            public string Form_LoadControl_Textbox = "SELECT name, prompt, tip, ordernum, required, textbox_lines, textbox_width, textbox_full FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";
            public string Form_LoadControl_List = "SELECT name, prompt, tip, ordernum, required, list_options, list_type FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";
            public string Form_LoadControl_Checkbox = "SELECT name, prompt, tip, ordernum, required, checkbox_type, checkbox_initialstate, checkbox_hasinput FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";
            public string Form_LoadControl_DateTime = "SELECT name, prompt, tip, ordernum, required, datetime_type FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";
            public string Form_LoadControl_Number = "SELECT name, prompt, tip, ordernum, required, number_min, number_max, number_interval, number_slider FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";
            public string Form_LoadControl_Percentage = "SELECT name, prompt, tip, ordernum, required, percentage_interval FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";
            public string Form_LoadControl_Range = "SELECT name, prompt, tip, ordernum, required, range_type, range_min, range_max FROM [UIFS.FormControls] WHERE formid={0} AND id={1} AND version={2}";

            public string Form_UpdateControl_CommonProperties = "UPDATE [UIFS.FormControls] SET name='{2}', prompt='{3}', tip='{4}', ordernum={5}, required={6} WHERE formid={0} AND id={1} AND active=1";
            public string Form_UpdateControl_Deactivate = "UPDATE [UIFS.FormControls] SET active=0 WHERE formid={0} AND id={1} AND version={2}";
            public string Form_UpdateControl_ColumnRename = "EXEC sp_rename '[UIFS.Form_{0}].{1}', '{2}', 'COLUMN'";
            public string Form_UpdateControl_ColumnAdd = "ALTER TABLE [UIFS.Form_{0}] ADD {1}";
            public string Form_UpdateControl_ColumnRename_ifexists = "if Exists(SELECT * FROM sys.columns WHERE Name = N'{1}' AND Object_ID = Object_ID(N'[UIFS.Form_{0}]')) EXEC sp_rename '[UIFS.Form_{0}].{1}', '{2}', 'COLUMN'";
            

            public string Form_AddControl_Textbox = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, textbox_lines, textbox_width, textbox_full)               VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  {9},{10},{11})";
            public string Form_AddControl_List = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, list_options, list_type)                                     VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  '{9}',{10})";
            public string Form_AddControl_Checkbox = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, checkbox_type, checkbox_initialstate, checkbox_hasinput) VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  {9},{10},{11})";
            public string Form_AddControl_DateTime = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, datetime_type)                                           VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  {9})";
            public string Form_AddControl_Number = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, number_min, number_max, number_interval, number_slider)    VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  {9},{10},{11},{12})";
            public string Form_AddControl_Percentage = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, percentage_interval)                                   VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  {9})";
            public string Form_AddControl_Range = "INSERT INTO [UIFS.FormControls] (formid, id, version, type, name, prompt, tip, ordernum, required, range_type, range_min, range_max)                           VALUES ({0},{1},{2},{3},'{4}','{5}','{6}',{7},{8},  {9},{10},{11})";

            // Version History            
            public string Form_VersionHistory_Create = "EXEC [UIFS.SP_Form_Create_VersionHistory] {0}";
            
            // Reporting
            public string Reporting_Settings_FormLink = "SELECT FormLink FROM [UIFS.Reporting]";
            public string Reporting_LoadSubjects ="SELECT name,db,db_id,db_idlist,Details,Relations FROM [UIFS.ReportingSubjects]";
            public string Reporting_LoadReportingDefinition = "SELECT title, Description FROM [UIFS.ReportingDefinitions] WHERE id={0}";
            public string Reporting_SaveReportingDefinition = "INSERT INTO [UIFS.ReportingDefinitions] (title, created, createdby, Description, query, language, columns) VALUES ('{0}', GETDATE(),'{1}','{2}','{3}','{4}','{5}') SELECT @@IDENTITY";
            public string Reporting_UpdateReportingDefinition = "UPDATE [UIFS.ReportingDefinitions] SET title='{1}', Description='{2}', query='{3}', language='{4}', columns='{5}', lastmodified=GETDATE(), lastmodifiedby='{6}' WHERE id={0}";

            public string Reporting_LoadReportList = "SELECT id,title,language FROM [UIFS.ReportingDefinitions] ORDER BY lastmodified DESC";

            // Recorded Forms
            public string Form_Recorded_GetVersion = "SELECT version FROM [UIFS.Form_{0}] WHERE id={1}";

            // Logging
            public string WriteLOG = "INSERT INTO [UIFS.LOG] (code, date, msg, username) VALUES ({0}, GETDATE(), '{1}', '{2}')";
            public string WriteLOG_AppError = "INSERT INTO [UIFS.LOG] (code, date, msg, username, exMessage, exSource, exStackTrace, CodeLocation) VALUES ({0}, GETDATE(), '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')";
        }

        public string ParseInput(string Parse)
        {
            Parse = Parse.Replace("'", "''");
            return Parse;
        }

        public int BoolValue(bool var)
        {
            if (var)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        // Logging routines are specific to UIFS of course.  As long as the user has provided the "UIFS.Log" appsetting in web.config, the errors
        // will be logged in the user's database of choice.

        public void WriteLog_Error(Exception ex, string ErrorMsg, string CodeLocation)
        { 
            string username =  WindowsIdentity.GetCurrent().Name;
            SQL ErrSQL = new SQL(ConfigurationManager.AppSettings["UIFS.Log"].ToString());
            ErrSQL.OpenDatabase();
            ErrSQL.Query = string.Format(ErrSQL.SQLQuery.WriteLOG_AppError, -1, ErrSQL.ParseInput(ErrorMsg), username, ErrSQL.ParseInput(ex.Message), ErrSQL.ParseInput(ex.Source), ErrSQL.ParseInput(ex.StackTrace), CodeLocation);
            ErrSQL.cmd = ErrSQL.Command(ErrSQL.Data);
            ErrSQL.cmd.ExecuteNonQuery();
            ErrSQL.CloseDatabase();
            ErrSQL = null;
        }
        public void WriteLog(string Msg)
        {
            string username = WindowsIdentity.GetCurrent().Name;
            SQL ErrSQL = new SQL(ConfigurationManager.AppSettings["UIFS.Log"].ToString());
            ErrSQL.OpenDatabase();
            ErrSQL.Query = string.Format(ErrSQL.SQLQuery.WriteLOG, 0, ErrSQL.ParseInput(Msg), username);
            ErrSQL.cmd = ErrSQL.Command(ErrSQL.Data);
            ErrSQL.cmd.ExecuteNonQuery();
            ErrSQL.CloseDatabase();
            ErrSQL = null;
        }

    }


}