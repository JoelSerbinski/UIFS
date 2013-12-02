using System;
using System.Collections.Generic;

namespace UIFS
{
    /** --| Form_Output
     *  ----------------
     * This set of routines formats form data into desired output
     * 
     */
    public class Form_Output
    {


        /** --| HTML
         * ----------
         * = This is the default system designed HTML output.
         * It uses standards for naming all the elements so that the elements can be manipulated via javascript (after the fact) if desired.
         * A js function UIFS_ValidateForm() is dynamically created based on the form fields
         * | Each control is placed inside its own div:
         * 
         */
        /* Format into HTML for user input/display */
        
        public void HTML(FormDataStruct FormData, ref string HTML, ref string Javascript)
        {
            HTML = ""; Javascript = ""; // MAKE SURE our return vars are clear as we are going to work with them directly
            string jsUIFS_ValidateForm = "this.UIFS_ValidateForm = function() {var valid=true;";
            string jsForm_CtrlMonitoring = ""; // for applying events to the controls so we can track which ones have been "touched"
            int currentColumn = 1;
            bool CtrlRequired = true;

            // Setup HTML
            HTML = HTML + "<div id='ControlColumn_1'>"; // start with column #1

            // Walk through all controls in display order
            for (int cnt = 1; cnt <= FormData.controls; cnt++)
            {
                int iControl = FormData.Find_ControlListEntry_byOrdernum(cnt); // This finds the control with the order number of [cnt]
                if (FormData.ControlList[iControl].removed) { continue; } // skip deleted controls (only live form editing)
                
                // All controls are in order starting with column 1 then 2 then 3...
                // so we can just walk through them all
                switch (FormData.Layout.OutputFormat)
                {
                    case Layout.Style.DIVs:
                        // NO EXTRA layout design
                        break;
                    case Layout.Style.SingleColumn:
                        if (cnt > 1) { HTML = HTML + "</td></tr>"; } // Close out previous
                        HTML = HTML + "<tr><td>";
                        break;
                    case Layout.Style.DoubleColumn:
                        if (cnt > 1) { HTML = HTML + "</td></tr>"; } // Close out previous
                        if (FormData.ControlList[iControl].Layout.column > currentColumn)
                        { // start a new column!
                            HTML = HTML + "</div><div id='ControlColumn_" + FormData.ControlList[iControl].Layout.column+"'>"; }
                        else
                        { // continue with elements
                            HTML = HTML + "<tr><td>";
                        }
                        currentColumn = FormData.ControlList[iControl].Layout.column;
                        break;
                }
                

                //. Create Control div
                switch (FormData.ControlList[iControl].type) {                    
                    case ControlType.Checkbox:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.Checkbox[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.Checkbox[FormData.ControlList[iControl].index].required;
                        break;
                    case ControlType.DateTime:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.DateTime[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.DateTime[FormData.ControlList[iControl].index].required;
                        break;
                    case ControlType.Number:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.Number[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.Number[FormData.ControlList[iControl].index].required;
                        break;
                    case ControlType.Percentage:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.Percentage[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.Percentage[FormData.ControlList[iControl].index].required;
                        break;
                    case ControlType.List:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.List[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.List[FormData.ControlList[iControl].index].required;
                        break;
                    case ControlType.Range:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.Range[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.Range[FormData.ControlList[iControl].index].required;
                        break;
                    case UIFS.ControlType.Textbox:
                        HTML_FormControl(FormData.ControlList[iControl].type, FormData.Textbox[FormData.ControlList[iControl].index], ref HTML, ref Javascript);
                        CtrlRequired = FormData.Textbox[FormData.ControlList[iControl].index].required;
                        break;                        
                }
                //. Add this control to the validate function
                jsUIFS_ValidateForm = jsUIFS_ValidateForm + "if (! UIFS_ValidateControl('Control_" + FormData.ControlList[iControl].id + "','" + FormData.ControlList[iControl].type.ToString() + "'," + CtrlRequired.ToString().ToLower() + ") ) { valid=false; }\n";
            }

            // End div
            HTML = HTML + "</div>";
            jsUIFS_ValidateForm = jsUIFS_ValidateForm + 
                "if (!valid){alert('You have incomplete items, please review the items in red'); return false;}"+
                "return true; };\n";
            Javascript = Javascript + jsUIFS_ValidateForm + jsForm_CtrlMonitoring; // add to our returned js
            
        }

        // Returns the HTML formatted output for a Control, "div contained"
        public void HTML_FormControl(UIFS.ControlType type, UIFS.FormControl Control, ref string HTML, ref string Javascript) {
            // Format strings for proper display/output
            Control.prompt = HTML_Escape(Control.prompt);
            Control.tip = HTML_Escape(Control.tip);
            // Control div
            HTML = HTML + "<div id='Control_" + Control.id + "_div' class='ControlType_" + type.ToString() + "' >";
            switch (type)
                {
                    case UIFS.ControlType.Textbox:                        
                        FormControl.Textbox ControlTextbox = (FormControl.Textbox)Control;
                        HTML = HTML + "<table><tr title='"+ControlTextbox.tip+"' class='Control_tip'>"+
                            "<td class='Control_prompt'>" + ControlTextbox.prompt + "</td>" +
                            "<td class='Control_input'>";
                        if (ControlTextbox.lines == 1)
                        {
                            HTML = HTML + "<input type='text' id='Control_" + ControlTextbox.id + "' size='" + ControlTextbox.width + "' /></td>";
                        }
                        else {
                            HTML = HTML + "<textarea id='Control_" + ControlTextbox.id + "' rows='" + ControlTextbox.lines + "' cols='" + ControlTextbox.width + "'></textarea></td>";
                        }
                        HTML = HTML + "</tr></table>";
                        // onblur watching...
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "').onblur = function(){ FormControlsTouched.push('" + Control.id + "'); };";
                        break;

                    case UIFS.ControlType.List:
                        FormControl.List ControlList = (FormControl.List)Control;
                        HTML = HTML + "<table><tr title='" + ControlList.tip + "' class='Control_tip'>" +
                            "<td class='Control_prompt'>" + ControlList.prompt + "</td>";
                        if (ControlList.type == FormControl.List.listtype.slider) { HTML = HTML + "<td class='Control_input slider'>"; }
                        else { HTML = HTML + "<td class='Control_input'>"; }
                        switch (ControlList.type)
                        {
                            case FormControl.List.listtype.radio:
                                for (int t = 0; t < ControlList.Items.Length; t++)
                                {HTML = HTML + "<input type='radio' id='Control_" + ControlList.id + "' name='Control_" + ControlList.id + 
                                    "' value='" + ControlList.Items[t].value + "'>" + ControlList.Items[t].name + "<br/>";}
                                break;
                            case FormControl.List.listtype.dropdown:
                            case FormControl.List.listtype.slider:
                                HTML = HTML + "<select id='Control_" + ControlList.id + "' /><option value=''>-=Choose=-</option>";
                                for (int t = 0; t < ControlList.Items.Length; t++)
                                { HTML = HTML + "<option value='" + ControlList.Items[t].value + "'>" + ControlList.Items[t].name + "</option>"; }
                                HTML = HTML + "</select>";
                                break;
                        }
                        HTML = HTML + "</td></tr></table>";

                        // --[ Javascript ]--
                        // Since the dropdown and the slider use the same html...separated this out
                        if (ControlList.type == FormControl.List.listtype.slider)
                        {
                            Javascript = Javascript + "$('#Control_" + ControlList.id + "').selectToUISlider();";
                        }
                        // onblur watching...
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "').onblur = function(){ FormControlsTouched.push('" + Control.id + "'); };";

                        break;

                    case UIFS.ControlType.Checkbox:
                        FormControl.Checkbox ControlCheckbox = (FormControl.Checkbox)Control;
                        HTML = HTML + "<table><tr title='" + ControlCheckbox.tip + "' class='Control_tip'>" +
                            "<td class='Control_prompt'>" + ControlCheckbox.prompt + "</td>" +
                            "<td class='Control_input'>";
                        string initialState_checkbox="",initialState_select="",initialState_input="";
                        if (ControlCheckbox.initialstate)
                        { // setup initial state if chosen
                            initialState_checkbox = " checked='1' ";
                            initialState_select = " selected='1' ";
                            initialState_input = "display: inline;";
                        }
                        else {
                            initialState_input = "display: none;";
                        }
                        switch (ControlCheckbox.type)
                        {
                            case FormControl.Checkbox.checkboxtype.standard:
                                HTML = HTML + "<input type='checkbox' id='Control_" + ControlCheckbox.id + "' "+initialState_checkbox+" onchange=\"Checkbox_Change('"+ControlCheckbox.id+"', 1) \" />";
                                break;
                            case FormControl.Checkbox.checkboxtype.YesNo:
                                HTML = HTML + "<select id='Control_" + ControlCheckbox.id + "' onchange=\"Checkbox_Change('" + ControlCheckbox.id + "', 2)\"  /><option value='0'>No</option><option value='1' " + initialState_select + ">Yes</option></select>";
                                break;
                            case FormControl.Checkbox.checkboxtype.OnOff:
                                HTML = HTML + "<select id='Control_" + ControlCheckbox.id + "' onchange=\"Checkbox_Change('" + ControlCheckbox.id + "', 2)\"  /><option value='0'>Off</option><option value='1' " + initialState_select + ">On</option></select>";
                                break;
                        }
                        if (ControlCheckbox.hasinput)
                        {
                            HTML = HTML + "<input type='text' id='Control_" + ControlCheckbox.id + "_input' size='30' style='" + initialState_input + "' />";
                        }
                        HTML = HTML + "</td></tr></table>";
                        // onblur watching...
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "').onblur = function(){ FormControlsTouched.push('" + Control.id + "'); };";
                        break;

                    case UIFS.ControlType.DateTime:
                        FormControl.DateTime ControlDateTime = (FormControl.DateTime)Control;
                        string AnyTime_format = "", AnyTime_value="";
                        switch (ControlDateTime.type)
                        {
                            case FormControl.DateTime.datetimetype.datetime: // {format:'%m/%d/%Y %h:%i%p'}
                                AnyTime_format = "%m/%d/%Y %h:%i%p"; AnyTime_value= "1/1/1900 12:00AM";
                                break;
                            case FormControl.DateTime.datetimetype.date: // {format:'%m/%d/%Y'}
                                AnyTime_format = "%m/%d/%Y";AnyTime_value= "1/1/1900";
                                break;
                            case FormControl.DateTime.datetimetype.time: // {format:'%h:%i%p'}
                                AnyTime_format = "%h:%i%p";AnyTime_value= "12:00AM";
                                break;
                        }

                        HTML = HTML + "<table><tr title='" + ControlDateTime.tip + "' class='Control_tip'>" +
                            "<td class='Control_prompt'>" + ControlDateTime.prompt + "</td>" +
                            "<td class='Control_input'><input type='text' id='Control_" + ControlDateTime.id + "' size='23' class='Control_DateTime' value='"+AnyTime_value+"' onfocus=\"AnyTime.noPicker('Control_" + ControlDateTime.id + "'); $('#Control_" + ControlDateTime.id + "').AnyTime_picker({format:'" + AnyTime_format + "'}); this.onfocus=null; \" /></td>" +
                            "</tr></table>";
                        //Javascript = Javascript + "AnyTime.noPicker('Control_" + ControlDateTime.id + "');";
                        // Based on the type:
                        // Step #1: run the 'noPicker' function to remove any associated controls before creating a new AnyTime control
                        // Step #2: bind a function to create the picker to the *focus* event of the control  (this is b/c of display issues with anytime run on hidden elements)

                        // onblur watching...
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "').onblur = function(){ FormControlsTouched.push('" + Control.id + "'); };";

                        break;

                    case UIFS.ControlType.Number:
                        FormControl.Number ControlNumber = (FormControl.Number)Control;
                        // Standard text input field
                        HTML = HTML + "<table><tr title='" + ControlNumber.tip + "' class='Control_tip'>" +
                                "<td class='Control_prompt'>" + ControlNumber.prompt + "</td>" +
                                "<td class='Control_input'><input type='text' id='Control_" + ControlNumber.id + "' size='7' onchange=\"Number_Validate(this, '" + ControlNumber.min + "','" + ControlNumber.max + "','" + ControlNumber.interval + "') \" />";
                        if (ControlNumber.slider)
                        { // Slider type control
                            HTML = HTML + "<span id='Control_" + ControlNumber.id + "_slider' class='Control_Number_Slider'></span>";
                            Javascript = Javascript + "$(function(){  $('#Control_" + ControlNumber.id + "_slider').slider({min:" + ControlNumber.min + ", max:" + ControlNumber.max + ", step:" + ControlNumber.interval +
                                ", slide:function(event,ui){$('#Control_" + ControlNumber.id + "').val(ui.value);}  });   }); ";
                        }
                        HTML = HTML + "</td></tr></table>";
                        // onblur watching...
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "').onblur = function(){ FormControlsTouched.push('" + Control.id + "'); };";

                        break;
                        
                    case ControlType.Percentage:
                        FormControl.Percentage ControlPercentage = (FormControl.Percentage)Control;
                        // Dropdown with values per interval
                        HTML = HTML + "<table><tr title='" + ControlPercentage.tip + "' class='Control_tip'>" +
                            "<td class='Control_prompt'>" + ControlPercentage.prompt + "</td>" +
                            "<td class='Control_input'><select id='Control_" + ControlPercentage.id + "'>";
                        for (int t = 0; t < 100 ; t+=ControlPercentage.interval)
                        {
                            HTML = HTML + "<option value='"+t.ToString()+"'>"+t.ToString()+"%</option>";
                        }
                        HTML = HTML + "<option value='100'>100%</option></select>";
                        // Slider control
                        //HTML = HTML + "<div id='Control_" + ControlPercentage.id + "_slider' class='Control_slider'></div>";

                        HTML = HTML + "</td></tr></table>";
                        //Javascript = Javascript + "$(function(){  $('#Control_" + ControlPercentage.id + "_slider').slider({min:0, max:100, step:" + ControlPercentage.interval +
                        //    ", slide:function(event,ui){$('#Control_" + ControlPercentage.id + "').val(ui.value);}  });   }); ";
                        Javascript = Javascript + "$('#Control_" + ControlPercentage.id + "').selectToUISlider();";
                        // onblur watching... (PARENT YO!)
                        Javascript = Javascript + "$('#Control_" + ControlPercentage.id + "').parent().find('.ui-slider').slider({ change: function(){FormControlsTouched.push('" + Control.id + "');} });";
                        break;

                    case ControlType.Range:
                        FormControl.Range ControlRange = (FormControl.Range)Control;
                        string AnyTime_datetimeformat = "", AnyTime_start = "", AnyTime_end = "", AnyTime_size="";
                        HTML = HTML + "<table><tr title='" + ControlRange.tip + "' class='Control_tip'>" +
                            "<td class='Control_prompt'>" + ControlRange.prompt + "</td>" +
                            "<td class='Control_input'><table>";
                        switch (ControlRange.type)
                        {
                            case FormControl.Range.Rangetype.TimeRange:
                            case FormControl.Range.Rangetype.DateRange:
                            case FormControl.Range.Rangetype.DateTimeRange:
                                switch (ControlRange.type)
                                {
                                    case FormControl.Range.Rangetype.TimeRange:
                                        AnyTime_datetimeformat = "%h:%i%p";AnyTime_start="12:00AM";AnyTime_end="12:00AM";AnyTime_size="10";
                                        break;
                                    case FormControl.Range.Rangetype.DateRange:
                                        AnyTime_datetimeformat = "%m/%d/%Y";AnyTime_start="1/1/1900";AnyTime_end="1/1/1900";AnyTime_size="12";
                                        break;
                                    case FormControl.Range.Rangetype.DateTimeRange:
                                        AnyTime_datetimeformat = "%m/%d/%Y %h:%i%p";AnyTime_start="1/1/1900 12:00AM";AnyTime_end="1/1/1900 12:00AM";AnyTime_size="20";
                                        break;
                                }
                                HTML = HTML + "<tr><td class='label'>FROM</td><td class='value'><input type='text' id='Control_" + ControlRange.id + "_Start' size='" + AnyTime_size + "' class='Control_DateTime' value='" + AnyTime_start + "' " +
                                    "onfocus=\"AnyTime.noPicker('Control_" + ControlRange.id + "_Start'); $('#Control_" + ControlRange.id + "_Start').AnyTime_picker({format:'" + AnyTime_datetimeformat + "'}); this.onfocus=null; \" /></td></tr>" +
                                    "<tr><td class='label'>TO</td><td class='value'><input type='text' id='Control_" + ControlRange.id + "_End' size='" + AnyTime_size + "' class='Control_DateTime' value='" + AnyTime_end + "' " +
                                    "onfocus=\"AnyTime.noPicker('Control_" + ControlRange.id + "_End'); $('#Control_" + ControlRange.id + "_End').AnyTime_picker({format:'" + AnyTime_datetimeformat + "'}); this.onfocus=null; \" /></td></tr>";
                                break;
                            
                            // -----
                            // These control range types are combined...
                            case FormControl.Range.Rangetype.Currency:
                            case FormControl.Range.Rangetype.MinMax:
                                string Designator = "";
                                if (ControlRange.type == FormControl.Range.Rangetype.Currency) { Designator = "'$'+"; }

                                HTML = HTML + "<tr><td>From</td><td><input type='text' id='Control_" + ControlRange.id + "_Start' size='7' readonly='1'  /></td>" +
                                              "<td>To</td><td><input type='text' id='Control_" + ControlRange.id + "_End' size='7'  readonly='1' /></td></tr>";
                                HTML = HTML + "<tr class='slider'><td colspan='4'><div id='Control_" + ControlRange.id + "_slider'></div></td></tr>";
                                Javascript = Javascript + "$(function(){  $('#Control_" + ControlRange.id + "_slider').slider({range: true, min:" + ControlRange.min + ", max:" + ControlRange.max + ", values: [" + ControlRange.min + "," + ControlRange.max + "]" +
                                    ", slide:function(event,ui){$('#Control_" + ControlRange.id + "_Start').val(" + Designator + "ui.values[0]); $('#Control_" + ControlRange.id + "_End').val(" + Designator + "ui.values[1]);}  });  " +
                                    "$('#Control_" + ControlRange.id + "_Start').val(" + Designator + ControlRange.min+ "); $('#Control_" + ControlRange.id + "_End').val(" + Designator + ControlRange.max+ ");  }); ";

                                break;

                        }
                        HTML = HTML + "</table>"; // end input formatting...
                        HTML=HTML+"</td></tr></table>";
                        
                        // onblur watching...
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "_Start').onblur = function(){ FormControlsTouched.push('" + Control.id + "S'); };";
                        Javascript = Javascript + "document.getElementById('Control_" + Control.id + "_End').onblur = function(){ FormControlsTouched.push('" + Control.id + "E'); };";
                        break;

                    default:
                        break;

                }
            // End Control div
            HTML = HTML + "</div>";
        }

        /** --| PopulateForm_js
         * ---------- (based from: FormInput.GetInput_js
         * = outputs javascript to populate a form based on data input
         * | using Form_Input.InputValue[] same as Save routines..staying consistent here for now
         * | this Form_Input.InputValue[] is populated from the db via Load()
         */
        public string PopulateForm_js(FormDataStruct FormData, Form_Input.InputValue[] FormValues)
        {
            UIFS.FormControl Control;
            string script = "";
            int iFormInputValue;

            // build a query string for each control's value 
            foreach (FormDataStruct.ControlListDetail CtrlDetail in FormData.ControlList)
            {
                Control = FormData.Get_Control(CtrlDetail.id);
                iFormInputValue = -1;
                iFormInputValue = FormInputValue_findbyID(ref FormValues, Control.id);
                // IF we do not find a value, ignore control
                if (iFormInputValue != -1)
                {
                    switch (CtrlDetail.type)
                    {
                        // All of the following controls we can get their value directly from the Control object
                        case ControlType.Textbox:
                        case ControlType.Percentage:
                        case ControlType.Number:
                        case ControlType.DateTime:
                            script = script + "$('#Control_" + Control.id + "').val('" + FormValues[iFormInputValue].value + "');";
                            break;
                        case ControlType.List:
                            UIFS.FormControl.List L = (UIFS.FormControl.List)Control;
                            switch (L.type)
                            {
                                case FormControl.List.listtype.radio:
                                    //script = script + "$('#Control_" + Control.id + "').filter('[value=" + FormValues[iFormInputValue].value + "]').prop('checked',true);";
                                    script = script + "$('input[name=Control_" + Control.id + "]').filter('[value=" + FormValues[iFormInputValue].value + "]').prop('checked',true);";
                                    break;
                                case FormControl.List.listtype.dropdown:
                                case FormControl.List.listtype.slider:
                                    script = script + "$('#Control_" + Control.id + "').val('" + FormValues[iFormInputValue].value + "');";
                                    break;
                            }
                            break;
                        // This checkbox control is single (not grouped) and can have its state checked without having to filter through an array
                        // This checkbox control may have an attached text input field
                        case ControlType.Checkbox:
                            UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control; //FormData.Get_Control(CtrlDetail.id);;
                            switch (CB.type)
                            {
                                case FormControl.Checkbox.checkboxtype.standard:
                                    script = script + "$('#Control_" + Control.id + "').prop('checked'," + FormValues[iFormInputValue].value.ToLower() + ");";
                                    break;
                                case FormControl.Checkbox.checkboxtype.OnOff:
                                case FormControl.Checkbox.checkboxtype.YesNo:
                                    script = script + "$('#Control_" + Control.id + "').val('" + FormValues[iFormInputValue].value + "');";
                                    break;
                            }
                            if (CB.hasinput)
                            {
                                script = script + "$('#Control_" + Control.id + "_input').val('" + FormValues[iFormInputValue].input + "');";
                            }
                            break;

                        // Ranges have a *Start and *End set of values
                        case ControlType.Range:
                            UIFS.FormControl.Range Range = (UIFS.FormControl.Range)Control;
                            switch (Range.type)
                            {
                                case FormControl.Range.Rangetype.DateRange:
                                case FormControl.Range.Rangetype.DateTimeRange:
                                case FormControl.Range.Rangetype.TimeRange:
                                    script = script + "$('#Control_" + Control.id + "_Start').val('" + FormValues[iFormInputValue].Start + "');";
                                    script = script + "$('#Control_" + Control.id + "_End').val('" + FormValues[iFormInputValue].End + "');";
                                    break;
                                case FormControl.Range.Rangetype.Currency:
                                case FormControl.Range.Rangetype.MinMax:
                            script = script + "$('#Control_" + Control.id + "_Start').val('" + FormValues[iFormInputValue].Start + "');";
                            script = script + "$('#Control_" + Control.id + "_End').val('" + FormValues[iFormInputValue].End + "');";
                                    break;
                            }
                            break;

                    }
                }
            }
            return script;
        }

        public int FormInputValue_findbyID(ref Form_Input.InputValue[] FormValues, int id)
        {
            for (int t = 0; t < FormValues.Length; t++)
            {
                if (FormValues[t].Controlid == id) { return t; }
            }
            return -1;
        }


        // ---------------------------------------------------------------------------------------------------------------
        /* -- FormOutput.LoadData --
        // ---------------------------------------------------------------------------------------------------------------
         |  returns saved data on a specific form/version
         |  NOTE: expects data retrieved to match loaded form version controls via UIFS.Form.Load()
         |  -- this means that the user application should "know" and have loaded the right version of the Form..
        */
        public Form_Input.InputValue[] LoadData(FormDataStruct FormData, ref UIFS.SQL SQL, long formid)
        {
            Form_Input.InputValue[] FormValues = new Form_Input.InputValue[FormData.ControlList.Length];
            UIFS.FormControl Control;
            string query_from = "FROM [UIFS.Form_" + FormData.id + "]";
            string query_select = "SELECT ";
            string query_where = "WHERE [id]="+formid.ToString();
            string ctrl_dbver = "";
            int ctrl_currentversion;
            try
            {
                //. build a select statement from controls loaded
                // ?: do we need to parse values?

                // 1) For each control for the form: build insert statement with correct column names: id_ver
                // 2) and find the value(s) for the control from the form data and add to VALUES part of query
                for (int t = 0; t < FormData.ControlList.Length; t++)
                {
                    Control = FormData.Get_Control(FormData.ControlList[t].id);
                    //. determine if this control is the latest version or older..
                    SQL.Query = string.Format(SQL.SQLQuery.Form_ControlcurrentVersion, FormData.id, Control.id);
                    SQL.cmd = SQL.Command(SQL.Data);
                    ctrl_currentversion = Convert.ToInt32(SQL.cmd.ExecuteScalar());
                    if (Control.version != ctrl_currentversion)
                    { // NOT CURRENT version, specific column name required
                        ctrl_dbver = "_"+Control.version.ToString();
                    }
                    else { ctrl_dbver = ""; }
                    switch (FormData.ControlList[t].type)
                    {
                        // All of the following return a single string values
                        case ControlType.Textbox:
                        case ControlType.DateTime:
                        case ControlType.List:
                            query_select = query_select + "[" + Control.id + ctrl_dbver + "],";
                            break;
                        // The following return a single numeric values
                        case ControlType.Percentage:
                        case ControlType.Number:
                            query_select = query_select + "[" + Control.id + ctrl_dbver + "],";
                            break;
                        // Checkbox controls are always true/false with an optional input field
                        case ControlType.Checkbox:
                            UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control;
                            if (CB.hasinput)
                            {
                                query_select = query_select + "[" + Control.id + ctrl_dbver + "], [" + Control.id + ctrl_dbver + "_text],"; ;
                            }
                            else
                            {
                                query_select = query_select + "[" + Control.id + ctrl_dbver + "],";
                            }
                            break;
                        // Ranges have start/end values
                        case ControlType.Range:
                            query_select = query_select + "[" + Control.id + ctrl_dbver + "_Start], [" + Control.id + ctrl_dbver + "_End],";
                            break;
                    }
                }

                // dbread
                SQL.Query = query_select.Substring(0, query_select.Length - 1) + " " + query_from+" " +query_where;
                SQL.cmd = SQL.Command(SQL.Data);
                SQL.sdr = SQL.cmd.ExecuteReader();
                SQL.sdr.Read();
                if (SQL.sdr.HasRows)
                {
                    int fieldcount = 0;
                    // Get data in the SAME ORDER as query
                    for (int t = 0; t < FormData.ControlList.Length; t++)
                    {
                        Control = FormData.Get_Control(FormData.ControlList[t].id);                        
                        FormValues[t] = new Form_Input.InputValue();
                        FormValues[t].Controlid = Control.id;
                        switch (FormData.ControlList[t].type)
                        {
                            case ControlType.DateTime:
                                UIFS.FormControl.DateTime DT = (FormControl.DateTime)Control;
                                switch (DT.type)
                                {
                                    case FormControl.DateTime.datetimetype.date:
                                        FormValues[t].value = SQL.sdr.GetDateTime(fieldcount).ToString("MM/dd/yyyy");
                                        break;
                                    case FormControl.DateTime.datetimetype.datetime:
                                        FormValues[t].value = SQL.sdr.GetDateTime(fieldcount).ToString("MM/dd/yyyy hh:mmtt");
                                        break;
                                    case FormControl.DateTime.datetimetype.time:
                                        FormValues[t].value = SQL.sdr.GetDateTime(fieldcount).ToString("hh:mmtt");
                                        break;
                                }                                
                                break;
                            // All of the following return a single string values
                            case ControlType.Textbox:                            
                            case ControlType.List:
                                //. FILTER text...
                                FormValues[t].value = LoadData_parsetextinput(SQL.sdr.GetString(fieldcount));
                                break;
                            // The following return numeric values
                            case ControlType.Percentage:
                                FormValues[t].value = SQL.sdr.GetByte(fieldcount).ToString();
                                break;
                            case ControlType.Number:
                                FormValues[t].value = SQL.sdr.GetDecimal(fieldcount).ToString();
                                break;
                            // Checkbox controls are always true/false with an optional input field
                            case ControlType.Checkbox:
                                UIFS.FormControl.Checkbox CB = (UIFS.FormControl.Checkbox)Control;
                                if (CB.hasinput)
                                {
                                    FormValues[t].value = SQL.sdr.GetBoolean(fieldcount).ToString();
                                    fieldcount += 1; // advance to next value
                                    FormValues[t].input = LoadData_parsetextinput(SQL.sdr.GetString(fieldcount));
                                }
                                else
                                {
                                    FormValues[t].value = SQL.sdr.GetBoolean(fieldcount).ToString();
                                }
                                break;
                            // Ranges have start/end values
                            case ControlType.Range:
                                UIFS.FormControl.Range Range = (UIFS.FormControl.Range)Control;
                                switch (Range.type)
                                {
                                    case FormControl.Range.Rangetype.Currency:
                                    case FormControl.Range.Rangetype.MinMax:
                                        FormValues[t].Start = SQL.sdr.GetDecimal(fieldcount).ToString();
                                        fieldcount += 1; // advance to next value
                                        FormValues[t].End = SQL.sdr.GetDecimal(fieldcount).ToString();
                                        break;
                                    case FormControl.Range.Rangetype.DateRange:
                                        FormValues[t].Start = SQL.sdr.GetDateTime(fieldcount).ToString("MM/dd/yyyy");
                                        fieldcount += 1; // advance to next value
                                        FormValues[t].End = SQL.sdr.GetDateTime(fieldcount).ToString("MM/dd/yyyy");
                                        break;
                                    case FormControl.Range.Rangetype.DateTimeRange:
                                        FormValues[t].Start = SQL.sdr.GetDateTime(fieldcount).ToString("MM/dd/yyyy hh:mmtt");
                                        fieldcount += 1; // advance to next value
                                        FormValues[t].End = SQL.sdr.GetDateTime(fieldcount).ToString("MM/dd/yyyy hh:mmtt");
                                        break;
                                    case FormControl.Range.Rangetype.TimeRange:
                                        // our Any+Time plugin requires the date in a SPECIFIC format
                                        FormValues[t].Start = SQL.sdr.GetDateTime(fieldcount).ToString("hh:mmtt");
                                        fieldcount += 1; // advance to next value
                                        FormValues[t].End = SQL.sdr.GetDateTime(fieldcount).ToString("hh:mmtt");
                                        break;
                                }
                                break;
                        }
                        fieldcount += 1; // advance to next value
                    }
                }
                SQL.sdr.Close();
                return FormValues;
            }
            catch (Exception ex)
            {
                SQL.WriteLog_Error(ex, "Error retrieving saved form data: " + SQL.Query, "UIFS.FormOutput.LoadData");
                return null;
            }

        }

        public string LoadData_parsetextinput(string textinput)
        {
            string textoutput;
            textoutput = textinput.Replace("'", "\\'");
            return textoutput;
        }

        // --[ HTML_Escape ]--
        // needs to be moved to a more accessible location
        // REF: http://www.theukwebdesigncompany.com/articles/entity-escape-characters.php
        // 
        public string HTML_Escape(string input) 
        { 
            string output;
            output = input.Replace("'", "&#39;");
            return output;
        }

    }
}
