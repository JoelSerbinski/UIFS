using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace UIFS
{


    public class Form
    {
        public Exception ErrorEx;
        public UIFS.SQL SQL;
        public string userlogin;

        // INITIALIZER
        public Form(ref SQL SQL)
        {
            this.SQL = SQL; SQL.OpenDatabase();
        }

        // Returns a list of existing forms
        public FormLIST[] List()
        {
            FormLIST[] Forms = new FormLIST[0];
            int FormCNT = 0;

            try
            {
                SQL.Query = SQL.SQLQuery.FormLIST;
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader();
                while (SQL.sdr.Read())
                {
                    Array.Resize(ref Forms, FormCNT + 1);
                    Forms[FormCNT] = new FormLIST();
                    Forms[FormCNT].id = SQL.sdr.GetInt32(0);
                    Forms[FormCNT].currentversion = SQL.sdr.GetInt16(1);
                    Forms[FormCNT].name = SQL.sdr.GetString(2);
                    Forms[FormCNT].description = SQL.sdr.GetString(3);
                    Forms[FormCNT].active = SQL.sdr.GetBoolean(4);
                    Forms[FormCNT].created = SQL.sdr.GetDateTime(5);
                    Forms[FormCNT].createdby = SQL.sdr.GetString(6);
                    if (!SQL.sdr.IsDBNull(7)) { Forms[FormCNT].lastmodified = SQL.sdr.GetDateTime(7); }
                    if (!SQL.sdr.IsDBNull(8)) { Forms[FormCNT].lastmodifiedby = SQL.sdr.GetString(8); }
                    FormCNT += 1;
                }
                SQL.sdr.Close();
            }

            catch (Exception ex)
            { // Failed to load list
                ErrorEx = ex;
                SQL.WriteLog_Error(ex, "Failed loading a list of forms:" + SQL.Query, "UIFS.Form.List()");
            }
            return Forms;
        }


        // -- This routine will load the form data from the db and put it in the referenced FormData
        // formversion :: set to -1 to get LATEST version
        public bool Load(int formid, int formversion, ref FormDataStruct FormData)
        {
            try
            {
                FormData = null; // Clear out just in case
                FormData = new FormDataStruct();
                int iControl = 0;
                bool reOrderControls = false; // set to true to perform a complete reordering of control order#s

                //: 1) Load main form data
                SQL.Query = string.Format(SQL.SQLQuery.Form_Load, formid);
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader();
                SQL.sdr.Read();
                FormData.id = formid;
                FormData.version = SQL.sdr.GetInt16(0);
                FormData.name = SQL.sdr.GetString(1);
                FormData.description = SQL.sdr.GetString(2);
                FormData.created = SQL.sdr.GetDateTime(3);
                SQL.sdr.Close();

                //: 2) Load controls list
                if (formversion != -1)
                { // retrieves the controls for a specific version of the form
                    // required: reordering!
                    reOrderControls = true;
                    SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControlList_byversion, formid, formversion);
                }
                else { // retrieves the latest version of this form
                    SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControlList, formid);
                }
                // This query actually only returns the common control data which we will use to create the control list.
                //  With the list we will then walk through all control types with specific control type queries to load the control data.
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader();
                while (SQL.sdr.Read())
                {
                    FormData.controls += 1;
                    Array.Resize(ref FormData.ControlList, FormData.controls);
                    FormData.ControlList[FormData.controls - 1] = new FormDataStruct.ControlListDetail();
                    FormData.ControlList[FormData.controls - 1].id = SQL.sdr.GetInt16(0);
                    FormData.ControlList[FormData.controls - 1].type = (ControlType)SQL.sdr.GetInt32(1);
                    FormData.ControlList[FormData.controls - 1].ordernum = SQL.sdr.GetInt16(2);
                    FormData.ControlList[FormData.controls - 1].version = SQL.sdr.GetInt16(3);
                }
                SQL.sdr.Close();

                //: 3) Load Controls
                for (int i = 0; i < FormData.ControlList.Length; i++)
                {
                    switch (FormData.ControlList[i].type)
                    {
                        case ControlType.Textbox:
                            FormControl.Textbox newTextBox = new FormControl.Textbox();
                            newTextBox.id = FormData.ControlList[i].id; // copy id
                            newTextBox.version = FormData.ControlList[i].version;
                            // Load rest of data from db
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_Textbox, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newTextBox.name = SQL.sdr.GetString(0);
                            newTextBox.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newTextBox.tip = SQL.sdr.GetString(2); }
                            newTextBox.ordernum = SQL.sdr.GetInt16(3);
                            newTextBox.required = SQL.sdr.GetBoolean(4);
                            newTextBox.lines = SQL.sdr.GetInt32(5);
                            newTextBox.width = SQL.sdr.GetInt32(6);
                            newTextBox.FullText = SQL.sdr.GetBoolean(7);
                            SQL.sdr.Close();
                            // Add new control to control array and get index
                            iControl = FormData.AddControl(ControlType.Textbox, newTextBox, false);
                            FormData.ControlList[i].index = iControl; // record index                            
                            break;
                        case ControlType.List:
                            string ListOptions = "";
                            string[] ListOption;
                            int iItem; // index of current item...
                            FormControl.List newList = new FormControl.List();

                            newList.id = FormData.ControlList[i].id; // copy id
                            newList.version = FormData.ControlList[i].version;
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_List, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newList.name = SQL.sdr.GetString(0);
                            newList.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newList.tip = SQL.sdr.GetString(2); }
                            newList.ordernum = SQL.sdr.GetInt16(3);
                            newList.required = SQL.sdr.GetBoolean(4);
                            ListOptions = SQL.sdr.GetString(5);
                            newList.type = (FormControl.List.listtype)SQL.sdr.GetByte(6);
                            SQL.sdr.Close();
                            // Load Option names/values
                            ListOption = ListOptions.Split(new char[] { ',' });
                            Array.Resize(ref newList.Items, ListOption.Length);
                            for (int t = 0; t < ListOption.Length; t++)
                            {
                                newList.Items[t] = new FormControl.List.Item();
                                iItem = ListOption[t].IndexOf(":");
                                newList.Items[t].name = ListOption[t].Substring(0, iItem);
                                newList.Items[t].value = ListOption[t].Substring(iItem + 1);
                            }
                            iControl = FormData.AddControl(ControlType.List, newList, false);
                            FormData.ControlList[i].index = iControl; // Set index value in control list for faster search/display
                            break;
                        case ControlType.Checkbox:
                            FormControl.Checkbox newCheckbox = new FormControl.Checkbox();
                            newCheckbox.id = FormData.ControlList[i].id; // copy id
                            newCheckbox.version = FormData.ControlList[i].version;
                            // Load rest of data from db
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_Checkbox, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newCheckbox.name = SQL.sdr.GetString(0);
                            newCheckbox.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newCheckbox.tip = SQL.sdr.GetString(2); }
                            newCheckbox.ordernum = SQL.sdr.GetInt16(3);
                            newCheckbox.required = SQL.sdr.GetBoolean(4);
                            newCheckbox.type = (FormControl.Checkbox.checkboxtype)SQL.sdr.GetByte(5);
                            newCheckbox.initialstate = SQL.sdr.GetBoolean(6);
                            newCheckbox.hasinput = SQL.sdr.GetBoolean(7);
                            SQL.sdr.Close();
                            // Add new control to control array and get index
                            iControl = FormData.AddControl(ControlType.Checkbox, newCheckbox, false);
                            FormData.ControlList[i].index = iControl; // record index     
                            break;
                        case ControlType.DateTime:
                            FormControl.DateTime newDateTime = new FormControl.DateTime();
                            newDateTime.id = FormData.ControlList[i].id; // copy id
                            newDateTime.version = FormData.ControlList[i].version;
                            // Load rest of data from db
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_DateTime, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newDateTime.name = SQL.sdr.GetString(0);
                            newDateTime.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newDateTime.tip = SQL.sdr.GetString(2); }
                            newDateTime.ordernum = SQL.sdr.GetInt16(3);
                            newDateTime.required = SQL.sdr.GetBoolean(4);
                            newDateTime.type = (FormControl.DateTime.datetimetype)SQL.sdr.GetByte(5);
                            SQL.sdr.Close();
                            // Add new control to control array and get index
                            iControl = FormData.AddControl(ControlType.DateTime, newDateTime, false);
                            FormData.ControlList[i].index = iControl; // record index     
                            break;
                        case ControlType.Number:
                            FormControl.Number newNumber = new FormControl.Number();
                            newNumber.id = FormData.ControlList[i].id; // copy id
                            newNumber.version = FormData.ControlList[i].version;
                            // Load rest of data from db
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_Number, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newNumber.name = SQL.sdr.GetString(0);
                            newNumber.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newNumber.tip = SQL.sdr.GetString(2); }
                            newNumber.ordernum = SQL.sdr.GetInt16(3);
                            newNumber.required = SQL.sdr.GetBoolean(4);
                            newNumber.min = (decimal)SQL.sdr[5];
                            newNumber.max = (decimal)SQL.sdr[6];
                            newNumber.interval = (decimal)SQL.sdr[7];
                            newNumber.slider = SQL.sdr.GetBoolean(8);
                            SQL.sdr.Close();
                            // Add new control to control array and get index
                            iControl = FormData.AddControl(ControlType.Number, newNumber, false);
                            FormData.ControlList[i].index = iControl; // record index     
                            break;
                        case ControlType.Percentage:
                            FormControl.Percentage newPercentage = new FormControl.Percentage();
                            newPercentage.id = FormData.ControlList[i].id; // copy id
                            newPercentage.version = FormData.ControlList[i].version;
                            // Load rest of data from db
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_Percentage, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newPercentage.name = SQL.sdr.GetString(0);
                            newPercentage.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newPercentage.tip = SQL.sdr.GetString(2); }
                            newPercentage.ordernum = SQL.sdr.GetInt16(3);
                            newPercentage.required = SQL.sdr.GetBoolean(4);
                            newPercentage.interval = SQL.sdr.GetInt32(5);
                            SQL.sdr.Close();
                            // Add new control to control array and get index
                            iControl = FormData.AddControl(ControlType.Percentage, newPercentage, false);
                            FormData.ControlList[i].index = iControl; // record index     
                            break;
                        case ControlType.Range:
                            FormControl.Range newRange = new FormControl.Range();
                            newRange.id = FormData.ControlList[i].id; // copy id
                            newRange.version = FormData.ControlList[i].version;
                            // Load rest of data from db
                            SQL.Query = string.Format(SQL.SQLQuery.Form_LoadControl_Range, formid, FormData.ControlList[i].id, FormData.ControlList[i].version);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.sdr = SQL.cmd.ExecuteReader(); SQL.sdr.Read();
                            newRange.name = SQL.sdr.GetString(0);
                            newRange.prompt = SQL.sdr.GetString(1);
                            if (!SQL.sdr.IsDBNull(2)) { newRange.tip = SQL.sdr.GetString(2); }
                            newRange.ordernum = SQL.sdr.GetInt16(3);
                            newRange.required = SQL.sdr.GetBoolean(4);
                            newRange.type = (FormControl.Range.Rangetype)SQL.sdr.GetByte(5);
                            newRange.min = (decimal)SQL.sdr[6];
                            newRange.max = (decimal)SQL.sdr[7];
                            SQL.sdr.Close();
                            // Add new control to control array and get index
                            iControl = FormData.AddControl(ControlType.Range, newRange, false);
                            FormData.ControlList[i].index = iControl; // record index     
                            break;
                    }
                }

                //: 4) Get next available control id
                SQL.Query = string.Format(SQL.SQLQuery.Form_GetNextAvailableControlID, formid);
                SQL.cmd = SQL.Command(SQL.Data);
                FormData.nextcontrolid = Convert.ToInt32(SQL.cmd.ExecuteScalar());

                //: 5) reorder Control list if needed
                if (reOrderControls)
                {
                    FormData.ReOrder_ControlList();
                }
                //: ?) what is next?
                
                FormData.newform = false; // This is not a new form.

            }
            catch (Exception ex)
            { // Failed to load form
                ErrorEx = ex;
                return false;
            }
            return true;
        }


        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.Save --
        // ---------------------------------------------------------------------------------------------------------------
        /* -- This routine is run after form controls have been changed via the designer and the user chooses the 'Save' option
         * -- It can be an initial save or an update
         * 
         * -- Designed to build a Query, test it in a controlled transaction first, then committ
         */

        public bool Save(ref FormDataStruct FormData)
        {
            FormControl Control = new FormControl();
            bool increaseVersion = false;
            string DBSaveQuery = "";

            // 1) Check to see if form has been created: if zero, this is a new form
            if (FormData.id != 0)
            { // form exists
                // 1a) Cycle through all controls and see which ones have changed
                for (int t = 0; t < FormData.ControlList.Length; t++)
                {
                    // 1a.1) Update changed controls (not new/added controls)
                    if (FormData.ControlList[t].controlchanged && !FormData.ControlList[t].added && !FormData.ControlList[t].removed)
                    {
                        // DB_UpdateControl will perform the functions needed
                        Control = FormData.Get_Control(FormData.ControlList[t].id);
                        // Add Control update routine to query
                        DBSaveQuery = DBSaveQuery + DB_UpdateControl(FormData.ControlList[t].type, Control, FormData.ControlList[t].newversionneeded, FormData.id) + "\n";
                        if (FormData.ControlList[t].newversionneeded) { increaseVersion = true; }
                    }
                }
                // 2a) Cycle through all controls...find additions...find deletions
                for (int t = 0; t < FormData.ControlList.Length; t++)
                {
                    Control = FormData.Get_Control(FormData.ControlList[t].id);
                    // 2a.1) New Controls!
                    if (FormData.ControlList[t].added)
                    {
                        increaseVersion = true;
                        // Query to add FormControl data
                        DBSaveQuery = DBSaveQuery + DB_FormControl_AddQuery(Control, FormData.ControlList[t].type, FormData.id) + "\n";
                        // Query to update form table by adding a new column for this control
                        DBSaveQuery = DBSaveQuery + string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnAdd, FormData.id, DB_FormControl_ColumnCreation(FormData.ControlList[t].type, Control)) + "\n";
                    }
                    else
                    {
                        // 2a.2) Removed Controls
                        if (FormData.ControlList[t].removed)
                        {
                            increaseVersion = true;
                            // Build query and execute
                            DBSaveQuery = DBSaveQuery + string.Format(SQL.SQLQuery.Form_UpdateControl_Deactivate,
                                FormData.id, FormData.ControlList[t].id, Control.version)+"\n";
                        }
                    }
                }

                // TEST SAVE
                try
                {
                    //BEGIN TRAN UIFS_Update \n BEGIN TRY " + Query_NewControlVersion + "\n" + Query_OldControlDeactivate + "\n" + Query_DataTableUpdate + "\n COMMIT TRAN UIFS_Update \n END TRY \n BEGIN CATCH\nROLLBACK TRAN UIFS_Update\nDECLARE @ErrMsg NVARCHAR(4000);DECLARE @ErrSev INT;DECLARE @ErrState INT; SELECT @ErrMsg = ERROR_MESSAGE(),@ErrSev = ERROR_SEVERITY(),@ErrState = ERROR_STATE(); RAISERROR (@ErrMsg,@ErrSev,@ErrState); \n END CATCH ";

                    SQL.Query = "BEGIN TRAN UIFS_Update \n BEGIN TRY " +DBSaveQuery+
                        "\n COMMIT TRAN UIFS_Update \n END TRY \n BEGIN CATCH\nROLLBACK TRAN UIFS_Update\nDECLARE @ErrMsg NVARCHAR(4000);DECLARE @ErrSev INT;DECLARE @ErrState INT; SELECT @ErrMsg = ERROR_MESSAGE(),@ErrSev = ERROR_SEVERITY(),@ErrState = ERROR_STATE(); RAISERROR (@ErrMsg,@ErrSev,@ErrState); \n END CATCH ";
                    SQL.cmd = SQL.Command(SQL.Data);
                    SQL.cmd.ExecuteNonQuery();
                    //-- SUCCESS! --
                    try
                    {
                        // if a version change is required:
                        if (increaseVersion) { FormData.version += 1; }
                        // Update the main form details: name, version, lastmodifiedby                    
                        SQL.Query = string.Format(SQL.SQLQuery.Form_Update, FormData.id, SQL.ParseInput(FormData.name), SQL.ParseInput(FormData.description), FormData.version, WindowsIdentity.GetCurrent().Name);
                        SQL.cmd = SQL.Command(SQL.Data);
                        SQL.cmd.ExecuteNonQuery();
                        // This runs last, if version changed
                        if (increaseVersion)
                        {
                            // Create version history..(This is created immediately after form changes to preserve a record of form and form control's states)
                            SQL.Query = string.Format(SQL.SQLQuery.Form_VersionHistory_Create, FormData.id);
                            SQL.cmd = SQL.Command(SQL.Data);
                            SQL.cmd.ExecuteNonQuery();
                        }
                        SQL.WriteLog("Saved changes to form: "+FormData.id.ToString());
                    }
                    catch (Exception ex)
                    {
                        SQL.WriteLog_Error(ex, "Failed to update version and/or version history...", "UIFS.Form.Save()");
                    }
                }
                catch (Exception ex) { // -- SAVE FAILED :( boo                    
                    SQL.WriteLog_Error(ex,"SAVING FORM FAILED: "+DBSaveQuery,"UIFS.Form.Save()");
                    return false;
                }

            }
            else
            { // 1b.1) New Form, create db entries for the form and its controls, create a table for the data
                try
                {
                    if (!DB_CreateForm(ref FormData)) { return false; }
                    if (!DB_CreateDataTable(FormData)) { return false; }

                    // Create version history..
                    SQL.Query = string.Format(SQL.SQLQuery.Form_VersionHistory_Create, FormData.id);
                    SQL.cmd = SQL.Command(SQL.Data);
                    SQL.cmd.ExecuteNonQuery();
                    SQL.WriteLog("Created new form!");
                }
                catch (Exception ex)
                {
                    SQL.WriteLog_Error(ex,"Failed to create a new form! ","UIFS.Form.Save()");
                }
            }

            // update any other components, layout,etc.

            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.Validate --
        // ---------------------------------------------------------------------------------------------------------------
            checks the form controls for any possible issues (use before saving)
        */
        public bool Validate(ref FormDataStruct FormData, ref string Message)
        {            
            for (int t = 0; t < FormData.ControlList.Length; t++)
            {
                switch (FormData.ControlList[t].type)
                {
                    case ControlType.List:
                        UIFS.FormControl.List LControl;
                        LControl=(UIFS.FormControl.List)FormData.Get_Control(FormData.ControlList[t].id);
                        if (LControl.Items.Length == 0) { Message = "You need to add some items to this *List* Control: "+LControl.name; return false; }
                        break;
                }
            }
            return true;
        }




        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.DB_CreateForm --
        // ---------------------------------------------------------------------------------------------------------------
            Creates the new form adding appropriate db entries
        */
        public bool DB_CreateForm(ref FormDataStruct FormData)
        {
            FormControl Control = new FormControl();

            try
            {
                // Create new Form entry
                SQL.Query = string.Format(SQL.SQLQuery.Form_Create, 1, SQL.ParseInput(FormData.name), SQL.ParseInput(FormData.description), WindowsIdentity.GetCurrent().Name);
                SQL.cmd = SQL.Command(SQL.Data);
                // Assign the new form its id
                FormData.id = Convert.ToInt32(SQL.cmd.ExecuteScalar());

                // create all the controls
                for (int t = 0; t < FormData.ControlList.Length; t++)
                {
                    switch (FormData.ControlList[t].type)
                    {
                        case ControlType.Textbox:
                            Control = FormData.Textbox[FormData.ControlList[t].index];
                            break;
                        case ControlType.List:
                            Control = FormData.List[FormData.ControlList[t].index];
                            break;
                        case ControlType.Checkbox:
                            Control = FormData.Checkbox[FormData.ControlList[t].index];
                            break;
                        case ControlType.DateTime:
                            Control = FormData.DateTime[FormData.ControlList[t].index];
                            break;
                        case ControlType.Number:
                            Control = FormData.Number[FormData.ControlList[t].index];
                            break;
                        case ControlType.Percentage:
                            Control = FormData.Percentage[FormData.ControlList[t].index];
                            break;
                        case ControlType.Range:
                            Control = FormData.Range[FormData.ControlList[t].index];
                            break;
                    }
                    // Get query and execute
                    SQL.Query = DB_FormControl_AddQuery(Control, FormData.ControlList[t].type, FormData.id);
                    SQL.cmd = SQL.Command(SQL.Data);
                    SQL.cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Could not create a new form: " + SQL.Query, "UIFS.Form.DB_CreateForm");
                return false;
            }
        }



        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.DB_CreateDataTable --
        // ---------------------------------------------------------------------------------------------------------------
            This routine requires that the FormData is loaded from the db pointing to an existing form and its controls.
         * */

        public bool DB_CreateDataTable(FormDataStruct FormData)
        {
            string CreateTableQuery = "", TableColumns = "";

            /* Columns ARE created/named by their id#.  
            The current version of the form will always have its controls represented by a column name of the control id#.
            Controls may need more than one column to store their data.  These will be named as so: [id#]_[controldataname]

            VERSIONING:  Any controls that get changed and force a new version will:
             1) Rename the current column(s) with the [id#]_[version#]
             2) Create a new column with the id#

            NOTES:
            This way no data is ever lost and can be accessed by version# for reporting, etc.  This method of storing dynamic
            form data is the most efficient since it uses a single table per form.  It is not very complicated compared to
            some other possible methods.

            REASONS:
                - Columns are always nullable because of expiring versions.  This means all data verification needs to happen
                outside of the db scope.
             
            */

            try
            {

                // Now we iterate through the controls to create the columns
                // Now we will walk through all the controls that exist, in order


                for (int i = 0; i < FormData.ControlList.Length; i++)
                {
                    // TEMPLATE: TableColumns=TableColumns+", ["+column_name+"] "+column_type+" NULL";

                    // Get the query language for the creation of each column, building the string...
                    TableColumns = TableColumns + ", " + DB_FormControl_ColumnCreation(FormData.ControlList[i].type, FormData.Get_Control(FormData.ControlList[i].id)) + "\n";
                }

                CreateTableQuery = "CREATE TABLE dbo.[UIFS.Form_" + FormData.id + "]( [id] [bigint] IDENTITY(1,1) NOT NULL \n"
                 + ", [created] [datetime] NOT NULL CONSTRAINT [DF_Form_" + FormData.id + "created]  DEFAULT (getdate()) \n"
                 + ", [version] [smallint] NOT NULL \n"
                 + TableColumns
                 + ",CONSTRAINT [PK_Form_" + FormData.id + "Entry] PRIMARY KEY CLUSTERED ([id] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY] ) ON [PRIMARY]"
                 ;


                // DB work
                SQL.Query = CreateTableQuery; // Create the new table for the form
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Could not create the Form's data table", "UIFS.Form.DB_CreateDataTable");
                return false;
            }
        }


        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.DB_FormControl_ColumnCreation --
        // ---------------------------------------------------------------------------------------------------------------
        /* -- This encapsulates the sql column creation language so that it can be accessed by individual control
         */
        public string DB_FormControl_ColumnCreation(ControlType type, FormControl Control)
        {
            string ColumnCreation = "";
            string ControlID = Control.id.ToString(); // This is what we use to name/identify the columns.
            // DEVEL NOTE: if you decide to go by control names in the future OR offer it as an option, this is now setup for that

            switch (type)
            {
                case ControlType.Textbox:
                    // The textbox control only needs one column for its data
                    FormControl.Textbox TB = (FormControl.Textbox)Control;
                    if (TB.FullText)
                    { // MAX for fulltext
                        ColumnCreation = "[" + ControlID + "] VARCHAR(MAX) NULL";
                    }
                    else
                    { // 255 length
                        ColumnCreation = "[" + ControlID + "] VARCHAR(255) NULL";
                    }
                    break;
                case ControlType.List:
                    // The list stores the option chosen, single value
                    ColumnCreation = "[" + ControlID + "] VARCHAR(255) NULL";
                    break;
                case ControlType.Checkbox:
                    // CHECKBOX: Can have an optional text input (which will be 256 chars)
                    FormControl.Checkbox CB = (FormControl.Checkbox)Control;
                    if (CB.hasinput)
                    { // two values
                        ColumnCreation = "[" + ControlID + "] BIT NULL, [" + ControlID + "_text] VARCHAR(255) NULL";
                    }
                    else
                    { // single value
                        ColumnCreation = "[" + ControlID + "] BIT NULL";
                    }
                    break;
                case ControlType.DateTime:
                    // yah self splanitory yo
                    ColumnCreation = "[" + ControlID + "] DATETIME NULL";
                    break;
                case ControlType.Number:
                    // NUMBER: Set to 19 places and 4 decimals (stores as 9 bytes)
                    ColumnCreation = "[" + ControlID + "] NUMERIC(19, 4) NULL";
                    break;
                case ControlType.Percentage:
                    // PERCENTAGE: this can be a tinyint(256) since it is 0 - 100
                    ColumnCreation = "[" + ControlID + "] TINYINT NULL";
                    break;
                case ControlType.Range:
                    // RANGE: Depends on the type of range!
                    FormControl.Range RG = (FormControl.Range)Control;
                    switch (RG.type)
                    {
                        case FormControl.Range.Rangetype.TimeRange:
                        case FormControl.Range.Rangetype.DateRange:
                        case FormControl.Range.Rangetype.DateTimeRange:
                            // : stores as 2 datetimes
                            ColumnCreation = "[" + ControlID + "_Start] DATETIME NULL, [" + ControlID + "_End] DATETIME NULL";
                            break;
                        case FormControl.Range.Rangetype.Currency:
                        case FormControl.Range.Rangetype.MinMax:
                            // CURRENCY & MINMAX: These both will have a mininum number and a maximum numbered range.  We use our default numeric storage (9 bytes)
                            ColumnCreation = "[" + ControlID + "_Start] NUMERIC(19, 4) NULL, [" + ControlID + "_End] NUMERIC(19, 4) NULL";
                            break;
                    }
                    break;

                default:
                    break;
            }

            return ColumnCreation;

        }

        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.DB_UpdateControl --
        // ---------------------------------------------------------------------------------------------------------------
        /*  Any controls that get "added" (insert cmds) can have their query created via this function
         */
        public string DB_FormControl_AddQuery(FormControl Control, ControlType type, int Formid)
        {
            string Query_NewControl = "";

            // Find the control type and build the query
            switch (type)
            {
                case ControlType.Textbox: // (formid, id, version, type, name, prompt, tip, textbox_lines, textbox_width)
                    FormControl.Textbox Control_TB = (FormControl.Textbox)Control;
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_Textbox, Formid, Control_TB.id, Control_TB.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), Control_TB.lines, Control_TB.width,SQL.BoolValue(Control_TB.FullText));
                    break;
                case ControlType.List: // (formid, id, version, type, name, prompt, tip, list_options, list_type) 
                    FormControl.List Control_L = (FormControl.List)Control;
                    string ListOptions = "";
                    for (int t = 0; t < Control_L.Items.Length; t++)
                    {
                        ListOptions = ListOptions + Control_L.Items[t].name + ":" + Control_L.Items[t].value + ",";
                    }
                    ListOptions = ListOptions.Substring(0, ListOptions.Length - 1);
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_List, Formid, Control_L.id, Control_L.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), ListOptions, (int)Control_L.type);
                    break;
                case ControlType.Checkbox: // (formid, id, version, type, name, prompt, tip, checkbox_type, checkbox_initialstate, checkbox_hasinput)
                    FormControl.Checkbox Control_CB = (FormControl.Checkbox)Control;
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_Checkbox, Formid, Control_CB.id, Control_CB.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), (int)Control_CB.type, Convert.ToInt32(Control_CB.initialstate), Convert.ToInt32(Control_CB.hasinput));
                    break;
                case ControlType.DateTime: // (formid, id, version, type, name, prompt, tip, datetime_type)
                    FormControl.DateTime Control_DT = (FormControl.DateTime)Control;
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_DateTime, Formid, Control_DT.id, Control_DT.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), (int)Control_DT.type);
                    break;
                case ControlType.Number: // (formid, id, version, type, name, prompt, tip, number_min, number_max, number_interval, number_slider)
                    FormControl.Number Control_N = (FormControl.Number)Control;
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_Number, Formid, Control_N.id, Control_N.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), Control_N.min, Control_N.max, Control_N.interval, Convert.ToInt32(Control_N.slider));
                    break;
                case ControlType.Percentage: // (formid, id, version, type, name, prompt, tip, percentage_interval)
                    FormControl.Percentage Control_P = (FormControl.Percentage)Control;
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_Percentage, Formid, Control_P.id, Control_P.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), Control_P.interval);
                    break;
                case ControlType.Range: // (formid, id, version, type, name, prompt, tip, range_type, range_min, range_max) 
                    FormControl.Range Control_Range = (FormControl.Range)Control;
                    Query_NewControl = string.Format(SQL.SQLQuery.Form_AddControl_Range, Formid, Control_Range.id, Control_Range.version, (int)type, SQL.ParseInput(Control.name), SQL.ParseInput(Control.prompt), SQL.ParseInput(Control.tip), Control.ordernum, SQL.BoolValue(Control.required), (int)Control_Range.type, Control_Range.min, Control_Range.max);
                    break;
            }
            return Query_NewControl;
        }


        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.DB_FormControl_UpdateColumnQuery --
        // ---------------------------------------------------------------------------------------------------------------
        /*  Updates a control's column(s) in the Form's data table
         */
        public string DB_FormControl_UpdateColumnQuery(FormControl Control, ControlType type, int Formid)
        {
            string UpdateColumnQuery = "";
            /*  -- STEPS -- 
             *  1) rename old column(s)
             *  2) create new column(s)
             */

            //:: the different control types have different # of columns
            // Step #1
            switch (type)
            {
                case ControlType.Textbox: // single value
                case ControlType.List: // single value                
                case ControlType.DateTime: // single value
                case ControlType.Number: // single value
                case ControlType.Percentage: // single value
                    UpdateColumnQuery = string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnRename, Formid, Control.id, Control.id + "_" + (Control.version - 1)) + "\n";
                    break;
                case ControlType.Checkbox: // can have a text entry box
                    FormControl.Checkbox CB = (FormControl.Checkbox)Control;
                    if (CB.hasinput)
                    { // two values
                        UpdateColumnQuery = string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnRename, Formid, Control.id, Control.id + "_" + (Control.version - 1)) + "\n";
                        // we use a different query struct here because if the control did not have text input this column will not exist and not need to be renamed..
                        UpdateColumnQuery = UpdateColumnQuery + string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnRename_ifexists, Formid, Control.id + "_text", Control.id +"_"+ (Control.version - 1) + "_text") + "\n";
                    }
                    else
                    { // single value
                        UpdateColumnQuery = string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnRename, Formid, Control.id, Control.id + "_" + (Control.version - 1)) + "\n";
                    }
                    break;
                case ControlType.Range: // Ranges have two values...
                    UpdateColumnQuery = string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnRename, Formid,Control.id + "_Start", Control.id + "_" + (Control.version - 1) + "_Start", Control.id) + "\n";
                    UpdateColumnQuery = UpdateColumnQuery + string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnRename, Formid, Control.id + "_End", Control.id + "_" + (Control.version - 1) + "_End") + "\n";
                    break;
            }

            // Step #2 (this step does not require an individual control breakout because it uses the columncreation routine that returns ALL needed column query language
            UpdateColumnQuery = UpdateColumnQuery + string.Format(SQL.SQLQuery.Form_UpdateControl_ColumnAdd, Formid, DB_FormControl_ColumnCreation(type, Control)) + "\n";

            return UpdateColumnQuery;
        }

        // ---------------------------------------------------------------------------------------------------------------
        /* -- Form.DB_UpdateControl --
        // ---------------------------------------------------------------------------------------------------------------
        /*  Updates a single control with the new properties, makes changes to necessary tables
         */
        public string DB_UpdateControl(ControlType type, FormControl NewControl, bool newversion, int Formid)
        {
            string TheQuery = "";
            // The ONLY row updating that will be done is for COMMON properties, everything else requires a new version = new row

            if (!newversion)
            { // just update the common properties
                TheQuery = string.Format(SQL.SQLQuery.Form_UpdateControl_CommonProperties, Formid, NewControl.id, SQL.ParseInput(NewControl.name), SQL.ParseInput(NewControl.prompt), SQL.ParseInput(NewControl.tip), NewControl.ordernum, SQL.BoolValue(NewControl.required));
            }
            else
            { // updating to new version!
                string Query_NewControlVersion = "", Query_DataTableUpdate = "", Query_OldControlDeactivate = "";

                // Increase version # 
                NewControl.version += 1;

                /* New Version Steps
                 *  1) new row: UIFS.FormControls
                 *  1a) mark old version (row) as inactive
                 *  2) update/modify: the form's table column(s)
                 *  
                 * NOTE: really, we just create a new row with the new version # in the [UIFS.FormControls] table
                 *       then, rename the Control's column(s) in the form's data table and create new ones
                 */

                // Update a single control with the new properties, makes changes to necessary tables                    
                // Step #1
                Query_NewControlVersion = DB_FormControl_AddQuery(NewControl, type, Formid);
                // Step #1a
                Query_OldControlDeactivate = string.Format(SQL.SQLQuery.Form_UpdateControl_Deactivate, Formid, NewControl.id, NewControl.version - 1);
                // Step #2
                Query_DataTableUpdate = DB_FormControl_UpdateColumnQuery(NewControl, type, Formid);

                TheQuery = Query_NewControlVersion + "\n" + Query_OldControlDeactivate + "\n" + Query_DataTableUpdate + "\n";
            }
            return TheQuery;
        }

        // Returns the version # of the recorded Form from this UIFS.Form's data  (or -1 if err)
        public int GetVersion(int UIFS_Formid, long Formid)
        {
            try
            {
                SQL.Query = string.Format(SQL.SQLQuery.Form_Recorded_GetVersion, UIFS_Formid, Formid);
                SQL.cmd = SQL.Command(SQL.Data);
                return Convert.ToInt32(SQL.cmd.ExecuteScalar());
            }
            catch
            {
                return -1;
            }
        }

    }
}