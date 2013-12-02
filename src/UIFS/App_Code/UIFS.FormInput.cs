using System;
using System.Collections.Generic;

namespace UIFS
{
    /** --| Form_Input
     *  ----------------
     * This set of routines formats form data into desired output
     * 
     */
    public class Form_Input
    {

        public class InputValue
        {
            public int Controlid;
            // Standard controls that return only one value
            public string value;
            // For Checkbox controls
            public string input; // if checkbox has input
            // For Range controls
            public string Start;
            public string End;
        }

        /* This routine prepares and returns a javascript routine that builds 
         * a querystring to get the data (data from an existing, completed form on the page)
         * and format the data to pass into the system.
         * */
        
        string script; // will hold the script to return

        public string GetInput_js(FormDataStruct FormData) {
            UIFS.FormControl Control;            
            script = "var query = ''";
            // build a query string for each control's value 
            foreach (FormDataStruct.ControlListDetail CtrlDetail in FormData.ControlList) {
                Control = FormData.Get_Control(CtrlDetail.id);
                switch (CtrlDetail.type)
                {
                    // SECURITY WARNING: be sure to 'escape' those values that are passed in

                    // All of the following controls we can get their value directly from the Control object
                    case ControlType.Textbox:
                    case ControlType.Percentage:
                    case ControlType.Number:
                    case ControlType.DateTime:
                        script = script + "+'&c_" + Control.id + "='+escape($('#Control_" + Control.id + "').val())";
                        break;
                    case ControlType.List:
                        UIFS.FormControl.List L = (UIFS.FormControl.List)Control;
                        switch (L.type)
                        {
                            case FormControl.List.listtype.radio:
                                script = script + "+'&c_" + Control.id + "='+escape($('input[name=Control_" + Control.id + "]:checked').val())";
                                break;
                            case FormControl.List.listtype.dropdown:
                            case FormControl.List.listtype.slider:
                                script = script + "+'&c_" + Control.id + "='+escape($('#Control_" + Control.id + "').val())";
                                break;
                        }
                        break;                    
                    // This checkbox control is single (not grouped) and can have its state checked without having to filter through an array
                    // This checkbox control may also have an attached text input field
                    case ControlType.Checkbox:
                        UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control; //FormData.Get_Control(CtrlDetail.id);;
                        switch (CB.type)
                        {
                            case FormControl.Checkbox.checkboxtype.standard:
                                script = script + "+'&c_" + Control.id + "='+$('#Control_" + Control.id + "').prop('checked')";
                                break;
                            case FormControl.Checkbox.checkboxtype.OnOff:
                            case FormControl.Checkbox.checkboxtype.YesNo:
                                script = script + "+'&c_" + Control.id + "='+escape($('#Control_" + Control.id + "').val())";
                                break;
                        }
                        if (CB.hasinput)
                        {
                            script = script + "+'&c_" + Control.id + "_I='+escape($('#Control_" + Control.id + "_input').val())";
                        }
                        break;
                    
                    // Ranges have a *Start and *End set of values
                    case ControlType.Range:
                        script = script + "+'&c_" + Control.id + "_S='+escape($('#Control_" + Control.id + "_Start').val())";
                        script = script + "+'&c_" + Control.id + "_E='+escape($('#Control_" + Control.id + "_End').val())";
                        break;

                }
            }
            script = script + ";";
            return script;
        }

        public InputValue[] FilterInput(System.Collections.Specialized.NameValueCollection querystring, UIFS.FormDataStruct FormData)
        {
            int cnt = 0;
            InputValue[] IVs = new InputValue[FormData.ControlList.Length];
            UIFS.FormControl Control;

            foreach (UIFS.FormDataStruct.ControlListDetail CtrlDetail in FormData.ControlList)
            {
                Control = FormData.Get_Control(CtrlDetail.id);
                IVs[cnt] = new InputValue();
                IVs[cnt].Controlid = CtrlDetail.id; // set control id
                // now set the correct values based on control type
                switch (CtrlDetail.type)
                {
                    case ControlType.Textbox:
                    case ControlType.DateTime:
                    case ControlType.List:
                        IVs[cnt].value = querystring["c_" + Control.id.ToString()];
                        break;
                    case ControlType.Percentage:
                    case ControlType.Number:
                        IVs[cnt].value = querystring["c_" + Control.id.ToString()];
                        if (IVs[cnt].value == "") {
                            IVs[cnt].value = "0"; // DEFAULT to 0 if nothing returned
                        }
                        break;
                    // This checkbox control may also have an attached text input field
                    case ControlType.Checkbox:
                        UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control; //FormData.Get_Control(CtrlDetail.id);;
                        if (CB.hasinput)
                        {
                            IVs[cnt].value = SQLBOOL(querystring["c_" + Control.id.ToString()]);
                            IVs[cnt].input = querystring["c_" + Control.id.ToString()+"_I"];
                        }
                        else { IVs[cnt].value = SQLBOOL(querystring["c_" + Control.id.ToString()]);}
                        break;

                    // Ranges have a *Start and *End set of values
                    case ControlType.Range:
                        UIFS.FormControl.Range R = (UIFS.FormControl.Range)Control;
                        IVs[cnt].Start = querystring["c_" + Control.id.ToString() + "_S"];
                        IVs[cnt].End = querystring["c_" + Control.id.ToString() + "_E"];
                        switch (R.type) {
                                // numbers
                            case FormControl.Range.Rangetype.Currency:
                            case FormControl.Range.Rangetype.MinMax:
                                // DEFAULT to 0 if nothing returned
                                if (IVs[cnt].Start == ""){IVs[cnt].Start = "0"; }
                                if (IVs[cnt].End == ""){IVs[cnt].End = "0"; }                                
                            break;
                            case FormControl.Range.Rangetype.DateRange:
                            case FormControl.Range.Rangetype.DateTimeRange:
                            case FormControl.Range.Rangetype.TimeRange:
                                // DEFAULTs are typically set by Calendar widget..control creation..
                            break;
                        }
                        break;

                }
                cnt += 1; // increase our array counter
            }
            return IVs;
        }

        public bool Save(UIFS.FormDataStruct FormData, InputValue[] FormValues, ref UIFS.SQL SQL, bool test, ref long newFormid) {
            UIFS.FormControl Control;
            string query_insert = "INSERT INTO [UIFS.Form_" + FormData.id + "] (version,";
            string query_values = "VALUES("+FormData.version+",";

            // if bool test is set, we want to encap in a tran and roll it back.
            if (test)
            {
                query_insert = "BEGIN TRAN UIFSTest \n" + query_insert;
            }

            // begin...
            try
            {
                // We are going to build a querystring from all the values
                // : making sure to parse values

                // 1) For each control for the form: build insert statement with correct column names: id_ver
                // 2) and find the value(s) for the control from the form data and add to VALUES part of query
                foreach (UIFS.FormDataStruct.ControlListDetail CtrlDetail in FormData.ControlList)
                {
                    Control = FormData.Get_Control(CtrlDetail.id);
                    foreach (InputValue IV in FormValues)
                    {
                        if (IV.Controlid == Control.id)
                        { // Found the control data, now we can add to the query strings
                            switch (CtrlDetail.type)
                            {
                                // All of the following return a single string values
                                case ControlType.Textbox:
                                case ControlType.DateTime:
                                case ControlType.List:
                                    query_insert = query_insert + "[" + Control.id + "],";
                                    query_values = query_values + "'" + SQL.ParseInput(IV.value) + "',";
                                    break;
                                // The following return a single numeric values
                                case ControlType.Percentage:
                                case ControlType.Number:
                                    query_insert = query_insert + "[" + Control.id + "],";
                                    query_values = query_values + "" + IV.value + ",";
                                    break;
                                // Checkbox controls are always true/false with an optional input field
                                case ControlType.Checkbox:
                                    UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control;
                                    if (CB.hasinput)
                                    {
                                        query_insert = query_insert + "[" + Control.id + "], [" + Control.id + "_text],"; ;
                                        query_values = query_values +  IV.value + ",'" + SQL.ParseInput(IV.input) + "',";
                                    }
                                    else
                                    {
                                        query_insert = query_insert + "[" + Control.id + "],";
                                        query_values = query_values + "" + IV.value + ",";
                                    }
                                    break;
                                // Ranges have start/end values
                                case ControlType.Range:
                                    query_insert = query_insert + "[" + Control.id + "_Start], [" + Control.id + "_End],";
                                    query_values = query_values + "'"+SQL.ParseInput(IV.Start)+ "','" + SQL.ParseInput(IV.End) + "',";
                                    break;

                            }
                            break;
                        }

                    }
                }
                
                // dbwrite
                // NOTE: we have to trim our trailing commas from created strings
                SQL.Query = query_insert.Substring(0, query_insert.Length - 1) + ") " + query_values.Substring(0, query_values.Length - 1) + ")  SELECT @@IDENTITY";
                // if test, rollback
                if (test) {
                    SQL.Query = SQL.Query + "\n ROLLBACK TRAN UIFSTest";
                }
                SQL.cmd = SQL.Command(SQL.Data);
                newFormid = Convert.ToInt64(SQL.cmd.ExecuteScalar());
                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Failed to save form data: "+SQL.Query, "UIFS.FormInput.SaveForm");
                return false;
            }
            
        }

        // ---------------------------------------------------------------------------------------------------------------
        /* -- FormInput.EditSave --
        // ---------------------------------------------------------------------------------------------------------------
         |  1) updates the db record for the form with new field changes
         |  2) returns a text value "LOG" entry to the user application for it to do what it wants (a basic table example is: UIFS_formid, formid, [changes text])
         |  
        */
        public bool Update(UIFS.FormDataStruct FormData, long formid, InputValue[] FormValues_Old, InputValue[] FormValues_New, ref UIFS.SQL SQL, bool test, ref string Changes)
        {
            UIFS.FormControl Control;
            string query_update = "UPDATE [UIFS.Form_" + FormData.id + "]";
            string query_values = " SET ";
            string query_where = " WHERE [id]="+formid.ToString();
            int iOldCtrl, iNewCtrl, ctrl_currentversion;
            string ctrl_dbver = "";
            Changes = ""; // clear out first

            // if bool test is set, we want to encap in a tran and roll it back.
            if (test)
            {
                query_update = "BEGIN TRAN UIFSTest \n" + query_update;
            }

            // begin...
            try
            {
                // We are going to build a querystring from all the values
                // : making sure to parse values

                // For each control for the form: find the value(s) for the control from the form data and check for changes
                foreach (UIFS.FormDataStruct.ControlListDetail CtrlDetail in FormData.ControlList)
                {
                    Control = FormData.Get_Control(CtrlDetail.id);
                    iNewCtrl = -1; iOldCtrl = -1;

                    //. determine if this control is the latest version or older..
                    SQL.Query = string.Format(SQL.SQLQuery.Form_ControlcurrentVersion, FormData.id, Control.id);
                    SQL.cmd = SQL.Command(SQL.Data);
                    ctrl_currentversion = Convert.ToInt32(SQL.cmd.ExecuteScalar());
                    if (Control.version != ctrl_currentversion)
                    { // NOT CURRENT version, specific column name required
                        ctrl_dbver = "_" + Control.version.ToString();
                    }
                    else { ctrl_dbver = ""; }
                    for (int t = 0; t < FormValues_Old.Length;t++)
                    {
                        if (FormValues_Old[t].Controlid == Control.id)
                        { // Found the control data, now we can add to the query strings
                            iOldCtrl = t; break;
                        }
                    }
                    for (int t = 0; t < FormValues_New.Length;t++)
                    {
                        if (FormValues_New[t].Controlid == Control.id)
                        { // Found the control data, now we can add to the query strings
                            iNewCtrl = t; break;
                        }
                    }
                    if (iNewCtrl == -1 || iOldCtrl == -1) {
                        // error, could not match control value arrays!
                        continue;
                    }
                    switch (CtrlDetail.type) {
                        // All of the following return a single string values
                        case ControlType.Textbox:
                        case ControlType.DateTime:
                        case ControlType.List:
                            if (FormValues_Old[iOldCtrl].value != FormValues_New[iNewCtrl].value)
                            {
                                query_values = query_values + "[" + Control.id + "]='" + SQL.ParseInput(FormValues_New[iNewCtrl].value) + "',";
                                Changes = Changes + Control.name + "(" + Control.id + ") was changed from: " + FormValues_Old[iOldCtrl].value + " to: " + FormValues_New[iNewCtrl].value + "\n";
                            }
                            break;
                        // The following return a single numeric values
                        case ControlType.Percentage:
                        case ControlType.Number:
                            if (FormValues_Old[iOldCtrl].value != FormValues_New[iNewCtrl].value)
                            {
                                query_values = query_values + "[" + Control.id + "]=" + FormValues_New[iNewCtrl].value + ",";
                                Changes = Changes + Control.name + "(" + Control.id + ") was changed from: " + FormValues_Old[iOldCtrl].value + " to: " + FormValues_New[iNewCtrl].value + "\n";
                            }
                            break;
                        // Checkbox controls are always true/false with an optional input field
                        case ControlType.Checkbox:
                            UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control;
                            if (CheckboxBOOL(FormValues_Old[iOldCtrl].value) != CheckboxBOOL(FormValues_New[iNewCtrl].value)) {
                                query_values = query_values + "[" + Control.id + ctrl_dbver + "]=" + FormValues_New[iNewCtrl].value + ",";
                                Changes = Changes + Control.name + "(" + Control.id + ") was changed from: " + CheckboxBOOL(FormValues_Old[iOldCtrl].value) + " to: " + CheckboxBOOL(FormValues_New[iNewCtrl].value) + "\n";
                            }
                            if (CB.hasinput)
                            {
                                if (FormValues_Old[iOldCtrl].input != FormValues_New[iNewCtrl].input)
                                {
                                    query_values = query_values + "[" + Control.id + ctrl_dbver + "_text]='" + SQL.ParseInput(FormValues_New[iNewCtrl].input) + "',";
                                    Changes = Changes + Control.name + "(" + Control.id + ")'s input was changed from: " + FormValues_Old[iOldCtrl].input + " to: " + FormValues_New[iNewCtrl].input + "\n";
                                }
                            }                            
                            break;
                        // Ranges have start/end values
                        case ControlType.Range:
                            if (FormValues_Old[iOldCtrl].Start != FormValues_New[iNewCtrl].Start)
                            {
                                query_values = query_values + "[" + Control.id + ctrl_dbver + "_Start]='" + SQL.ParseInput(FormValues_New[iNewCtrl].Start) + "',";                                
                                Changes = Changes + Control.name + "(" + Control.id + ")'s Start was changed from: " + FormValues_Old[iOldCtrl].Start + " to: " + FormValues_New[iNewCtrl].Start + "\n";
                            }
                            if (FormValues_Old[iOldCtrl].End != FormValues_New[iNewCtrl].End)
                            {
                                query_values = query_values + "[" + Control.id + ctrl_dbver + "_End]='" + SQL.ParseInput(FormValues_New[iNewCtrl].End) + "',";
                                Changes = Changes + Control.name + "(" + Control.id + ")'s End was changed from: " + FormValues_Old[iOldCtrl].Start + " to: " + FormValues_New[iNewCtrl].Start + "\n";
                            }
                            break;
                    }
                }
                // dbwrite
                SQL.Query = query_update + query_values.Substring(0, query_values.Length - 1) + query_where;
                // if test, rollback
                if (test)
                {
                    SQL.Query = SQL.Query + "\n ROLLBACK TRAN UIFSTest";
                }                
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Failed to save form data: " + SQL.Query, "UIFS.FormInput.SaveForm");
                return false;
            }

        }

        public bool Delete(int UIFSformid, long formid, ref UIFS.SQL SQL, bool test)
        {
            try
            {
                string query_delete = "";
                // if bool test is set, we want to encap in a tran and roll it back.
                if (test)
                {
                    query_delete = "BEGIN TRAN UIFSTest \n";
                }
                //. build our query
                query_delete += "DELETE FROM [UIFS.Form_" + UIFSformid.ToString() + "] WHERE [id]=" + formid.ToString();
                // if test, rollback
                if (test)
                {
                    query_delete += "\n ROLLBACK TRAN UIFSTest";
                }
                SQL.Query = query_delete;
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Failed to delete form: " + SQL.Query, "UIFS.FormInput.Delete");
                return false;
            }
        }


        private string SQLBOOL(string boolvalue) {
            if (boolvalue.ToLower() == "true" || boolvalue == "1")
            {
                return "1";
            }
            return "0";
        }
        private string CheckboxBOOL(string boolvalue)
        {
            if (boolvalue.ToLower() == "true" || boolvalue == "1")
            {
                return "CHECKED";
            }
            return "UNCHECKED";
        }

    
    
    // end Form_Input
    }
}