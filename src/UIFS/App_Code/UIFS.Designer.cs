using System;
using System.Collections.Generic;
using System.Configuration;



namespace UIFS
{

    /* --------------------------------------------------------------------------------------------------------------------------------------
     * --------------------------------------------------------------------------------------------------------------------------------------
     * 
     * */
    public class Designer
    {
        private string html;
        static byte[] CRLF = { (byte)'\r', (byte)'\n' };
        
        public void New() // Initializier
        {
        }

        // First get the common (shared) properties for all control types
        public string ControlProperties(ControlType type, FormControl Control)
        {
            FormControl.Textbox ControlTextbox = new FormControl.Textbox();
            FormControl.List ControlList = new FormControl.List();
            FormControl.Checkbox ControlCheckbox = new FormControl.Checkbox();
            FormControl.DateTime ControlDateTime = new FormControl.DateTime();
            FormControl.Number ControlNumber = new FormControl.Number();
            FormControl.Percentage ControlPercentage = new FormControl.Percentage();
            FormControl.Range ControlRange = new FormControl.Range();

            string ControlRequired;

            // Now begin the table and show the shared/common properties
            html = "<table class='Properties'>";

            // Do not display our "toolbar" if a new control..
            if (Control.id != -1) {
                html = html + "<tr class='Toolbar'><td colspan='2'><button class='Button_RemoveControl' onclick=\"Form_RemoveControl('" + Control.id.ToString() + "')\">Remove Control</button></td></tr>";
            }
            if (Control.required) { ControlRequired = "checked='1'"; } else { ControlRequired = ""; }
            // Common Properties
            html = html + "<tr class='CommonProp'><td title='The name of the control'>Name: </td><td><input type='text' id='" + Control.id.ToString() + "_Name' size='50' value='" + Control.name + "' onkeypress=\"Button_Enable('" + Control.id.ToString() + "_SaveB','Unsaved Changes!'); \"/></td></tr>" +
                "<tr class='CommonProp'><td title='The prompt the user will see'>Prompt: </td><td><textarea id='" + Control.id.ToString() + "_Prompt' rows='3' cols='50' onkeypress=\"Button_Enable('" + Control.id.ToString() + "_SaveB','Unsaved Changes!'); \">" + Control.prompt + "</textarea></td></tr>" +
                "<tr class='CommonProp'><td title='The help message that will appear when the user moves the mouse over this control'>Tip: </td><td><textarea id='" + Control.id.ToString() + "_Tip' rows='2' cols='50' onkeypress=\"Button_Enable('" + Control.id.ToString() + "_SaveB','Unsaved Changes!'); \">" + Control.tip + "</textarea></td></tr>" +
                "<tr class='CommonProp'><td title='Requires the control to be completed before submitting form'>Required: </td><td><input type='checkbox' "+ControlRequired+" id='" + Control.id.ToString() + "_Req' onclick=\"Button_Enable('" + Control.id.ToString() + "_SaveB','Unsaved Changes!'); \" /></td></tr>" +
                "<tr class='CommonProp'><td colspan='2' class='CommonProp_Submit'><span id='" + Control.id.ToString() + "_Save' ><input type='submit' id='" + Control.id.ToString() + "_SaveB' value='no changes' disabled='true' onclick=\"CommonProperties_Update('" + Control.id.ToString() + "')\" /></span></td>" +
                "<tr><td colspan='2' class='Table_SeparatorRow' /></tr>";

            // Cycle through each type again and display control properties
            switch (type)
            {
                case ControlType.Textbox:
                    ControlTextbox = (FormControl.Textbox)Control;
                    string fulltext;

                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>Textbox Properties</td></tr>";
                    // # of lines
                    html = html + "<tr><td colspan='2'>Lines: <select id='" + Control.id.ToString() + "_Lines' onChange=\"Button_Enable('" + Control.id.ToString() + "_TextboxSaveB','Unsaved Changes!'); \">";
                    for (int a = 1; a < 4; a++)
                    {
                        if (a == ControlTextbox.lines)
                        { html = html + "<option selected='1'>" + a.ToString() + "</option>"; }
                        else { html = html + "<option>" + a.ToString() + "</option>"; }
                    }
                    html = html + "</select>";
                    // # of columns
                    html = html + "&nbsp;&nbsp;&nbsp; Width: <select id='" + Control.id.ToString() + "_Width' onChange=\"Button_Enable('" + Control.id.ToString() + "_TextboxSaveB','Unsaved Changes!'); \">";
                    for (int a = 20; a < 61; a+=5)
                    {
                        if (a == ControlTextbox.width)
                        { html = html + "<option selected='1'>" + a.ToString() + "</option>"; }
                        else { html = html + "<option>" + a.ToString() + "</option>"; }
                    }
                    html = html + "</select>" +
                        "<span class='Button_floatRight' id='" + Control.id.ToString() + "_TextboxSave'><input type='submit' id='" + Control.id.ToString() + "_TextboxSaveB' value='no changes' disabled='true' onclick=\"Textbox_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td>" +
                    "</tr>";
                    // Fulltext or not
                    if (ControlTextbox.FullText)
                    { fulltext = " checked='true' "; }
                    else { fulltext = ""; }
                    html = html + "<tr><td colspan='2'><input type='checkbox' id='" + Control.id.ToString() + "_fulltext'" + fulltext + " onClick=\"Button_Enable('" + Control.id.ToString() + "_TextboxSaveB','Unsaved Changes!'); \"/>Full Textbox (no length limits)</td></tr>";

                    ControlTextbox = null;
                    break;

                case ControlType.List:
                    ControlList = (FormControl.List)Control;
                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>List Properties</td></tr>" +
                        "<tr><td colspan='2'><table class='List_Options'>";
                // begin list options table
                    if (Control.id != -1)
                    { // Do not display if a new control
                        html = html + "<tr><th colspan='2'>List Options</th></tr>" +
                        // NOTE: Removed because the average user DOES NOT UNDERSTAND why they would need two values
                        // The DB now stores the "name" as both name and value
                        // The DB will still contain all the possibility for having different values..but it is up to the advanced user to implement
                        //"<tr><td>Name:<br/><input type='text' id='" + Control.id.ToString() + "_Opt_Name' /><br/>Value:<br/><input type='text' id='" + Control.id.ToString() + "_Opt_Value' />" +
                        "<tr><td>Name:<br/><input type='text' id='" + Control.id.ToString() + "_Opt_Name' />" +
                        "<br/><input type='button' name='Add New' value='Add New' onclick=\"javascript:List_AddOption('" + ControlList.id + "')\" /></td>" +
                        "<td><div class='List_Options_List'><ul id='" + Control.id.ToString() + "_sortable'>"; 
                        if (ControlList.Items != null)
                        { // If there are any items...list them
                            for (int a = 0; a < ControlList.Items.Length; a++)
                            {
                                html = html + "<li class='ui-state-default' value='" + a.ToString() + "'>"
                                    + "<img border='0' src='Images/x.png' title='Delete Item' onclick=\"javascript:List_RemoveOption('" + ControlList.id + "','" + a.ToString() + "')\" />"
                                    + ControlList.Items[a].name + "</li>"; //+ " : " + ControlList.Items[a].value (removed, see above)
                            }
                        }
                        html = html + "</ul></div></td></tr>";
                    }
                    // List type selection
                    html = html + "<tr><td colspan='2'>Type: <select id='" + Control.id.ToString() + "_type' onChange=\"Button_Enable('" + Control.id.ToString() + "_ListSaveB','Unsaved Changes!'); \">";
                    switch (ControlList.type)
                    {
                        case FormControl.List.listtype.radio:
                            html = html + "<option selected='1' value='0'>radio</option><option value='1'>dropdown</option><option value='2'>slider</option>";
                            break;
                        case FormControl.List.listtype.dropdown:
                            html = html + "<option value='0'>radio</option><option selected='1' value='1'>dropdown</option><option value='2'>slider</option>";
                            break;
                        case FormControl.List.listtype.slider:
                            html = html + "<option value='0'>radio</option><option value='1'>dropdown</option><option selected='1' value='2'>slider</option>";
                            break;
                    }
                    html = html + "</select></td></tr>";

                    // Save button
                    html = html + "<tr><td colspan='2'><span class='Button_floatRight' id='" + Control.id.ToString() + "_ListSave'><input type='submit' id='" + Control.id.ToString() + "_ListSaveB' value='no changes' disabled='true' onclick=\"List_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td></tr>";

                    html=html+"</table></td></tr>"; // close out list options table
                    ControlList = null;
                    break;

                case ControlType.Checkbox:
                    ControlCheckbox = (FormControl.Checkbox)Control;
                    string initialstate, hasinput;

                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>Checkbox Properties</td></tr>";
                    
                    // checkbox type selection
                    html = html + "<tr><td colspan='2'>Type: <select id='" + Control.id.ToString() + "_type' onChange=\"Button_Enable('" + Control.id.ToString() + "_CheckboxSaveB','Unsaved Changes!'); \">";
                    switch (ControlCheckbox.type)
                    {
                        case FormControl.Checkbox.checkboxtype.standard:
                            html = html + "<option selected='1' value='0'>Standard</option><option value='1'>Yes/No</option><option value='2'>On/Off</option>";
                            break;
                        case FormControl.Checkbox.checkboxtype.YesNo:
                            html = html + "<option value='0'>Standard</option><option selected='1' value='1'>Yes/No</option><option value='2'>On/Off</option>";
                            break;
                        case FormControl.Checkbox.checkboxtype.OnOff:
                            html = html + "<option value='0'>Standard</option><option value='1'>Yes/No</option><option selected='1' value='2'>On/Off</option>";
                            break;
                    }
                    html = html + "</select></td></tr>";
                
                    // initialstate checked or not
                    if (ControlCheckbox.initialstate){ initialstate = " checked='true' "; } else { initialstate = ""; }
                    // has input?
                    if (ControlCheckbox.hasinput) { hasinput = " checked='true' "; } else { hasinput = ""; }
                    // options
                    html = html + "<tr><td colspan='2'><input type='checkbox' id='" + Control.id.ToString() + "_initialstate'" + initialstate + " onClick=\"Button_Enable('" + Control.id.ToString() + "_CheckboxSaveB','Unsaved Changes!'); \"/>Initial State of checkbox</td></tr>";
                    html = html + "<tr><td colspan='2'><input type='checkbox' id='" + Control.id.ToString() + "_hasinput'" + hasinput + " onClick=\"Button_Enable('" + Control.id.ToString() + "_CheckboxSaveB','Unsaved Changes!'); \"/>Include text entry alongside checkbox</td></tr>";
                    // Save button
                    html = html + "<tr><td colspan='2'><span class='Button_floatRight' id='" + Control.id.ToString() + "_CheckboxSave'><input type='submit' id='" + Control.id.ToString() + "_CheckboxSaveB' value='no changes' disabled='true' onclick=\"Checkbox_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td></tr>";

                    ControlCheckbox = null;
                    break;

                case ControlType.DateTime:
                    ControlDateTime = (FormControl.DateTime)Control;
                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>DateTime Properties</td></tr>";

                    //  type selection
                    html = html + "<tr><td colspan='2'>Type: <select id='" + Control.id.ToString() + "_type' onChange=\"Button_Enable('" + Control.id.ToString() + "_DateTimeSaveB','Unsaved Changes!'); \">";
                    switch (ControlDateTime.type)
                    {
                        case FormControl.DateTime.datetimetype.datetime:
                            html = html + "<option selected='1' value='0'>Date and Time</option><option value='1'>just Date</option><option value='2'>just Time</option>";
                            break;
                        case FormControl.DateTime.datetimetype.date:
                            html = html + "<option value='0'>Date and Time</option><option value='1' selected='1'>just Date</option><option value='2'>just Time</option>";
                            break;
                        case FormControl.DateTime.datetimetype.time:
                            html = html + "<option value='0'>Date and Time</option><option value='1'>just Date</option><option value='2' selected='1'>just Time</option>";
                            break;
                    }
                    html = html + "</select></td></tr>";

                    html = html + "<tr><td colspan='2'><span class='Button_floatRight' id='" + Control.id.ToString() + "_DateTimeSave'><input type='submit' id='" + Control.id.ToString() + "_DateTimeSaveB' value='no changes' disabled='true' onclick=\"DateTime_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td></tr>";

                    ControlDateTime = null;
                    break;

                case ControlType.Number:
                    ControlNumber = (FormControl.Number)Control;
                    string slider;

                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>Number Properties</td></tr>";

                    // Min, Max, Interval
                    html = html + "<tr><td>Minimum Value: </td><td><input type='text' id='" + Control.id.ToString() + "_min' size='50' value='" + ControlNumber.min + "' onChange=\"Button_Enable('" + Control.id.ToString() + "_NumberSaveB','Unsaved Changes!'); \"/></td></tr>" +
                        "<tr><td>Maximum Value: </td><td><input type='text' id='" + Control.id.ToString() + "_max' size='50' value='" + ControlNumber.max + "' onChange=\"Button_Enable('" + Control.id.ToString() + "_NumberSaveB','Unsaved Changes!'); \"></td></tr>" +
                        "<tr><td>Interval: </td><td><input type='text' id='" + Control.id.ToString() + "_interval' size='50' value='" + ControlNumber.interval + "' onChange=\"Button_Enable('" + Control.id.ToString() + "_NumberSaveB','Unsaved Changes!'); \"></td></tr>";

                    // slider checked or not
                    if (ControlNumber.slider) { slider = " checked='true' "; } else { slider = ""; }
                    // Slider option
                    html = html + "<tr><td colspan='2'><input type='checkbox' id='" + Control.id.ToString() + "_slider'" + slider + " onClick=\"Button_Enable('" + Control.id.ToString() + "_NumberSaveB','Unsaved Changes!'); \"/>use slider for selection</td></tr>";
                    // Save button
                    html = html + "<tr><td colspan='2'><span class='Button_floatRight' id='" + Control.id.ToString() + "_NumberSave'><input type='submit' id='" + Control.id.ToString() + "_NumberSaveB' value='no changes' disabled='true' onclick=\"Number_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td></tr>";

                    ControlNumber = null;
                    break;

                case ControlType.Percentage:
                    ControlPercentage = (FormControl.Percentage)Control;
                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>Percentage Properties</td></tr>";

                    // Interval
                    html = html + "<tr><td>Interval: </td><td><input type='text' id='" + Control.id.ToString() + "_interval' size='5' value='" + ControlPercentage.interval + "' onChange=\"Button_Enable('" + Control.id.ToString() + "_PercentageSaveB','Unsaved Changes!'); \"></td></tr>";

                    // Save button
                    html = html + "<tr><td colspan='2'><span class='Button_floatRight' id='" + Control.id.ToString() + "_PercentageSave'><input type='submit' id='" + Control.id.ToString() + "_PercentageSaveB' value='no changes' disabled='true' onclick=\"Percentage_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td></tr>";

                    ControlNumber = null;
                    break;

                case ControlType.Range:
                    ControlRange = (FormControl.Range)Control;
                    string RangeTypeName="", RangeTypeDesc="";
                    html = html + "<tr><td colspan='2' class='Table_SeparatorRow_SectionHeader'>Range Properties</td></tr>";
                    //  type selection
                    html = html + "<tr><td colspan='2'>Type: <select id='" + Control.id.ToString() + "_type' onChange=\"Button_Enable('" + Control.id.ToString() + "_RangeSaveB','Unsaved Changes!'); \">";
                    FormControl.Range RangeTypes = new FormControl.Range();
                    foreach (FormControl.Range.Rangetype RangeType in Enum.GetValues(typeof(UIFS.FormControl.Range.Rangetype)))
                    {
                        switch (RangeType) {
                            case FormControl.Range.Rangetype.TimeRange:
                                RangeTypeName="Time Range";
                                RangeTypeDesc = "start/end time values (min and max are irrelevant here)";
                                break;
                            case FormControl.Range.Rangetype.DateRange:
                                RangeTypeName="Date Range";
                                RangeTypeDesc="start/end date values (min and max are irrelevant here)";
                                break;
                            case FormControl.Range.Rangetype.DateTimeRange:
                                RangeTypeName="DateTime Range";
                                RangeTypeDesc="start/end date and time values (min and max are irrelevant here)";
                                break;
                            case FormControl.Range.Rangetype.Currency:
                                RangeTypeName="Currency Range";
                                RangeTypeDesc="a currency range according to min and max";
                                break;
                            case FormControl.Range.Rangetype.MinMax:
                                RangeTypeName="Number Range";
                                RangeTypeDesc="a range according to min and max";
                                break;
                        }
                        if (ControlRange.type == RangeType) {
                            html = html + "<option selected='1' value='"+ Convert.ToInt32(RangeType) +"' title='"+RangeTypeDesc+"'>"+RangeTypeName+"</option>";
                        }
                        else {
                            html = html + "<option value='" + Convert.ToInt32(RangeType) + "' title='" + RangeTypeDesc + "'>" + RangeTypeName + "</option>";
                        }
                    }
                    html = html + "</select></td></tr>";

                    // Min and Max
                    html = html + "<tr><td>Minimum Value: </td><td><input type='text' id='" + Control.id.ToString() + "_min' size='50' value='" + ControlRange.min + "' onChange=\"Button_Enable('" + Control.id.ToString() + "_RangeSaveB','Unsaved Changes!'); \"/></td></tr>" +
                        "<tr><td>Maximum Value: </td><td><input type='text' id='" + Control.id.ToString() + "_max' size='50' value='" + ControlRange.max + "' onChange=\"Button_Enable('" + Control.id.ToString() + "_RangeSaveB','Unsaved Changes!'); \"></td></tr>";

                    // Save button
                    html = html + "<tr><td colspan='2'><span class='Button_floatRight' id='" + Control.id.ToString() + "_RangeSave'><input type='submit' id='" + Control.id.ToString() + "_RangeSaveB' value='no changes' disabled='true' onclick=\"Range_UpdateProperties('" + Control.id.ToString() + "')\" /></span></td></tr>";

                    ControlNumber = null;
                    break;
            }

            html = html + "</table>"; // End of Properties table

            return html;
        }



    }
}
