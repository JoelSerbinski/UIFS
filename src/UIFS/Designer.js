// REF: how to remove a button from the page
// button = document.getElementById('-1_TextboxPropSave');button.parentNode.removeChild(button);

// Global vars
var Ajax_outputHTML = ''; // used to wait for a specific AJAX return


// Initialize jquery controls
$(document).ready(function() {
    DrawScreen(); // Initialize Controls

    // Setup our processing div to be shown whenever waiting on the server
    $('#Processing').dialog({
        autoOpen: false, height: 100, width: 100, modal: true, position: ['center'], title: 'Processing'
        , closeOnEscape: false, resizable: false, dialogClass: 'Processing_dialog'
    });
});

function Processing() {
    if ($('#Processing').dialog('isOpen') ) {
        $('#Processing').dialog('close');
    }
    else {
        $('#Processing').dialog('open');
    }
}

function TEST() {
    Ajax('?cmd=TEST', 'FormTemplate_Container');
}

function SessionEnd() {
    Ajax('?cmd=0', 'ErrorConsole');
    window.close();
}

function DrawScreen() {
    ajaxparam = new Array();
    ajaxparam[0] = new Array('?cmd=1000&Option=Controls', 'Controls_Container'); // Get controls
    ajaxparam[1] = new Array('?cmd=1000&Option=Form', 'FormTemplate_Container'); // Get Form data if exists
    AjaxMultiple(ajaxparam, function() { Controls_Setup(); Form_SetupControls(); }, false); // Setup jquery after ajax calls return
}
function Controls_Setup() {
    // First, destroy any existing jquery for these 
    $("#FormTemplate_Container").sortable('destroy');
    $("#CTRL_Textbox").draggable('destroy');
    $("#CTRL_List").draggable('destroy');
    $("#CTRL_List_HourBlock").draggable('destroy');
    $("#CTRL_Checkbox").draggable('destroy');
    $("#CTRL_DateTime").draggable('destroy');
    $("#CTRL_Number").draggable('destroy');
    $("#CTRL_Percentage").draggable('destroy');
    $("#CTRL_Range").draggable('destroy');
    // Now setup the jquery functioning for the designer drag-drop controls
    $("#CTRL_Textbox").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_List").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_List_HourBlock").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_Checkbox").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_DateTime").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_Number").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_Percentage").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });
    $("#CTRL_Range").draggable({ revert: true, helper: 'clone', connectToSortable: '#FormTemplate_Container' });

    $("#FormTemplate_Container").sortable({
        connectWith: '#FormTemplate_Container',
        handle: '.portlet-header',
        update: function (event, ui) {
            sortindex = $(ui.item).parent().children().index(ui.item) + 1;
            // Added Control
            if (ui.item.hasClass('CTRL') || ui.item.hasClass('subCTRL')) { // This is a dropped new control                
                AddControl(ctrlid, sortindex); // Add a new control!
            }
            // Re-ordering happens here
            if (!ui.item.hasClass('CTRL')) {
                ctrlid = $(ui.item).find(".portlet-content").attr('id');
                // Call Ajax to set the new order
                Ajax('?cmd=1050&id=' + ctrlid + '&sortindex=' + sortindex, 'ErrorConsole');
            }
        },
        receive: function (event, ui) {
            ctrlid = ui.item.attr('id'); // sets the id here to be used by the *update* call which comes next (and cannot see the id anymore) this is b/c we need the sortindex from the new element...
        },
        stop: function (event, ui) { // this function has to exist or jquery will error on drag-drop
            if (ui.item.hasClass('CTRL')) { // Our drag-n-droppable controls
                ui.item.replaceWith(''); // this clears out the element on our dropped control so we are not left with a visual mess
            }
        }
    });

    // initialize our jquery buttons
    $(".Button_RemoveControl").button({
        icons: { primary: "ui-icon-closethick" }
    });

}

function Form_SetupControls() {

    $(".portlet").addClass("ui-widget ui-widget-content ui-helper-clearfix ui-corner-all")
        .find(".portlet-header").addClass("portlet-header ui-widget-header ui-corner-all")
    	.end();

    $(".portlet-header .ui-icon").click(function() {
        $(this).toggleClass("ui-icon-minusthick").toggleClass("ui-icon-plusthick");
        $(this).parents(".portlet").find(".portlet-content").toggle();
        // Re-apply the classes because of IE problem removing bottom spacing on portlet minimize content
        $(".portlet").addClass("ui-widget ui-widget-content ui-helper-clearfix ui-corner-all");
        
        //        state = $(this).parents(".portlet").find(".portlet-content").css('display');        
        //        if (state == 'block') {
        //            $(this).parents(".portlet").find(".portlet-content").css('display', 'none');
        //        }
        //        else {
        //            $(this).parents(".portlet").find(".portlet-content").css('display', 'block');
        //        }
    });



        //$("#FormTemplate_Container").sortable("refresh");
}


function AddControl(type, sortindex) {

    switch (type) {
        case "CTRL_Textbox":
            Title = 'New Control - Textbox';
            ajaxcmd = '1100.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_Lines', '-1_Width']; // New controls have an id of -1 (which will not exist in reality!)
            CheckboxList = ['-1_Req', '-1_fulltext']; // checkbox controls need manipulation
            break;
        case "CTRL_List":
            Title = 'New Control - List';
            ajaxcmd = '1101.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_type'];
            CheckboxList = ['-1_Req'];
            break;
        case "CTRL_List_HourBlock": // "predefined" controls call ajax directly, no need for dialog, redraw
            Ajax('?cmd=1101.2&sortindex=' + sortindex, 'ErrorConsole', function () { DrawScreen(); }, false);
            return;            
        case "CTRL_Checkbox":
            Title = 'New Control - Checkbox';
            ajaxcmd = '1102.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_type']; 
            CheckboxList = ['-1_Req', '-1_initialstate', '-1_hasinput']; 
            break;
        case "CTRL_DateTime":
            Title = 'New Control - DateTime';
            ajaxcmd = '1103.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_type'];
            CheckboxList = ['-1_Req'];
            break;
        case "CTRL_Number":
            Title = 'New Control - Number';
            ajaxcmd = '1104.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_min', '-1_max', '-1_interval'];
            CheckboxList = ['-1_Req', '-1_slider'];
            break;
        case "CTRL_Percentage":
            Title = 'New Control - Percentage';
            ajaxcmd = '1105.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_interval'];
            CheckboxList = ['-1_Req'];
            break;
        case "CTRL_Range":
            Title = 'New Control - Range';
            ajaxcmd = '1106.1';
            FieldList = ['-1_Name', '-1_Prompt', '-1_Tip', '-1_type', '-1_min', '-1_max'];
            CheckboxList = ['-1_Req'];
            break;            
                                    
        default:
            // Not a droppable AddControl
            return;
            break;
    }

    // Call to create dialog box
    Ajax('?cmd=1001&type=' + type, 'NewControl');

    $('#NewControl').dialog({
        autoOpen: false, height: 400, width: 450, modal: true, title: Title,
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            'Add': function () {
                // We are going to build a dynamic querystring that contains all the fields used on the form
                ajaxquerystring = "?cmd=" + ajaxcmd + "&sortindex=" + sortindex;
                for (var t = 0; t < FieldList.length; t = t + 1) {
                    ajaxquerystring = ajaxquerystring + "&" + FieldList[t] + "=" + escape($('#' + FieldList[t]).val());
                }
                if (CheckboxList) {
                    for (var t = 0; t < CheckboxList.length; t = t + 1) {
                        ajaxquerystring = ajaxquerystring + "&" + CheckboxList[t] + "=" + $('#' + CheckboxList[t]).prop('checked');
                    }
                }
                Ajax(ajaxquerystring, 'ErrorConsole', function () { DrawScreen(); }, false); // Call Add Control function, redraw when finished
                $(this).dialog('destroy'); // finally, close window
                //debug: alert(ajaxquerystring);
            }
        },
        close: function () { $(this).dialog('destroy'); } // Use $(this).remove(); if you want to recreate the modal with different elements
    });

    $('#NewControl').dialog('open'); // Can either use this or set the autoopen to true during initialization of the dialog
}

function Form_RemoveControl(ctrlid) {
    // Confirm first
    $('#MENU_Popup').dialog({
        autoOpen: true, height: 200, width: 200, modal: true, position: ['center', 25], title: 'Confirmation',        
        buttons: {
            NO: function () { $(this).dialog('destroy'); },
            YES: function () { // confirmation given
                Ajax('?cmd=1002&id=' + ctrlid, 'ErrorConsole', function () { DrawScreen(); }, false);
                $(this).dialog('destroy');
            }
        }
        , close: function () { $(this).dialog('destroy'); }
    }).html('<div style="text-align:center">Are you sure you want to REMOVE this control from the form?</div>');

}

function Form_Menu(option) {
    
    Menudiv = document.getElementById('FormMenu');
    switch (option) {
        case "show":
            Menudiv.style.display = 'block';
            break;
        case "hide":
            Menudiv.style.display = 'none';
            break;
        case "toggle":
            $('#FormMenu').animate({                    
                height: 'toggle'
            }, 1000 );
            //Menudiv.style.display = 'none';
            break;
    }
}


function Form_New() {
    NewFormMessage = Ajax('?cmd=900&confirmation=false', 'ErrorConsole', function () { }, false);
    if (NewFormMessage == 'UNSAVED CHANGES') {
        Form_New_Confirmation(function () { Ajax('?cmd=900&confirmation=true', 'ErrorConsole', function () { DrawScreen(); Form_Settings(); }, false) });
    }
    else { // no unsaved changes, so new form
        Ajax('?cmd=900&confirmation=true', 'ErrorConsole', function () { DrawScreen(); Form_Settings(); }, false);
    }
}
function Form_New_GO() {
}
function Form_Newbasedon() {
    //. check for unsaved changes
    NewFormMessage = Ajax('?cmd=900&confirmation=false', 'ErrorConsole',function(){}, false);
    if (NewFormMessage == 'UNSAVED CHANGES') {
        Form_New_Confirmation(function () { Form_Newbasedon_GO(); });
    }
    else {Form_Newbasedon_GO();}
}
function Form_Newbasedon_GO() {
    Ajax('?cmd=901', 'MENU_Popup'); // call to open form dialog
    // Setup 'Open Form' dialog
    $('#MENU_Popup').dialog({
        autoOpen: true, height: 500, width: 750, modal: true, position: ['center', 25], title: 'New Form based on:',
        dialogClass: 'Form_OpenDialog',
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            'USE': function () { //. open existing form and use as a base for new form
                Processing(); // throw-up the processing div
                $(this).dialog('destroy');
                Ajax('?cmd=901.2&formid=' + document.getElementById('FormID').value, 'ErrorConsole', function () { DrawScreen(); Processing(); Form_Settings(); }, false);                
            }
        },
        close: function () { $(this).dialog('destroy'); } // We need to destroy the dialog b/c we create it with new data every time
    });
}
function Form_New_Confirmation(callback) {
    $('#MENU_Popup').dialog({
        autoOpen: true, height: 200, width: 200, modal: true, position: ['center', 25], title: 'Confirmation',
        dialogClass: 'Form_NewConfirmationDialog',
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            New: function () { // confirmation given
                $(this).dialog('destroy');
                callback();
            }
        }
        , close: function () { $(this).dialog('destroy'); }
    }).html('There are unsaved changes to this form, are you sure you want to start a new form?');
}

function Form_Open() {
    // Call to create dialog box data
    Ajax('?cmd=901', 'MENU_Popup');

    // Setup 'Open Form' dialog
    $('#MENU_Popup').dialog({
        autoOpen:true, height: 500, width: 750, modal: true, position: ['center', 25], title: 'Open Form',
        dialogClass: 'Form_OpenDialog',
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            'Open': function () {
                Processing(); // throw-up the processing div
                Form_Load(document.getElementById('FormID').value);
                $(this).dialog('destroy');
            }
        },
        close: function () { $(this).dialog('destroy'); } // We need to destroy the dialog b/c we create it with new data every time
    });

}
function FormsList_Click(formid) {
    if (typeof(oldformid) != 'undefined') {
        $('#FormsList_' + oldformid).removeClass('FormsListDetail_Visible').addClass('hiddendiv');
    }
    $('#FormsList_' + formid).removeClass('hiddendiv').addClass('FormsListDetail_Visible');
    oldformid = formid;
    document.getElementById('FormID').value = formid;
}
function Form_Load(formid) {
    Ajax('?cmd=901.1&formid=' + formid, 'ErrorConsole', function () { DrawScreen(); Processing(); }, false);    // We initate a callback to DrawScreen() when ajax routines are finished (jqueryize ;)
}

function Form_Save() {
    // Call to create dialog box data
    Ajax('?cmd=903', 'MENU_Popup');

    // Setup dialog
    $('#MENU_Popup').dialog({
        autoOpen: true, height: 450, width: 600, modal: true, position: ['center', 25], title: 'Save Form',
        dialogClass: 'Form_SaveDialog',
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            'Save': function () {
                formname = document.getElementById('Form_Name').value;
                formdesc = document.getElementById('Form_Description').value;
                if (formname == "" || formdesc == "") {
                    alert('The form must have a name and description in order to save.');
                    return; // return to the dialog - no save
                }
                // Data input okay, save and close dialog!
                $(this).dialog('destroy');
                Processing(); // throw-up the processing div
                Form_Save_Commit(escape(formname), escape(formdesc));                
            }
        },
        close: function () { $(this).dialog('destroy'); } // We need to destroy the dialog b/c we create it with new data every time
    });

}

function Form_Save_Commit(name, desc) {
    Ajax('?cmd=903.1&name=' + name + '&desc=' + desc, 'ErrorConsole', function () { Processing(); DrawScreen(); } );  // callback to close the processing div and redraw screen
}

function Form_Preview() {
    PreviewButtonDIV = $('#FormTemplate_Header_Preview').css('background-color');
    if (PreviewButtonDIV == 'transparent') {
        Ajax('?cmd=910', 'FormTemplate_Container'); // Form Preview
        $('#FormTemplate_Header_Preview').css('background-color', 'Orange');
    }
    else {
        ajaxparam = new Array();        
        ajaxparam[0] = new Array('?cmd=1000&Option=Form', 'FormTemplate_Container');
        AjaxMultiple(ajaxparam, function(){Form_SetupControls();}, false); // We initate a callback to Form_SetupControls when ajax routines are finished (jqueryize ;)
        $('#FormTemplate_Header_Preview').css('background-color', 'transparent');
    }
}

function Form_Settings() {
    // Call to create dialog box data
    Ajax('?cmd=904', 'MENU_Popup');
    // Clear variable that holds which tab is selected
    oldtabid = 'General';
    // Setup dialog
    $('#MENU_Popup').dialog({
        autoOpen: true, height: 450, width: 650, modal: true, position: ['center', 25], title: 'Form Settings',
        dialogClass: 'Form_SettingsDialog',
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            'Save': function () {
                formname = document.getElementById('Form_Name').value;
                formdesc = document.getElementById('Form_Description').value;
                NumOfColumns = document.getElementById('Layout_NumOfColumns').value;
                if (formname == "" || formdesc == "") {
                    alert('Name and description cannot be blank.');
                    return; // return to the dialog - no save
                }
                // Data input okay, save and close dialog!
                Processing(); // throw-up the processing div
                $(this).dialog('destroy');
                Form_Settings_Save(escape(formname), escape(formdesc), NumOfColumns);
            }
        },
        close: function () { $(this).dialog('destroy'); } // We need to destroy the dialog b/c we create it with new data every time
    });
}
function Form_Settings_Click(id) { // When a setting TAB is clicked on
    selectedtab = document.getElementById('T_' + id);
    selectedtabdata = document.getElementById(id);
    if (typeof (oldtabid) == 'undefined') { 
        oldtabid ='General';
    }
    // hide old
    oldtab = document.getElementById('T_' + oldtabid);
    oldtabdata = document.getElementById(oldtabid);
    $(oldtab).removeClass('selected');
    $(oldtabdata).addClass('hiddendiv');
    // show new
    $(selectedtab).addClass('selected');
    $(selectedtabdata).removeClass('hiddendiv');
    oldtabid = id;
}
function Form_Settings_Save(formname, formdesc, Layout_NumOfColumns) {
    Ajax('?cmd=904.1&name=' + formname + '&desc=' + formdesc + '&Layout_NumOfColumns=' + Layout_NumOfColumns
        , 'ErrorConsole', function () { Processing(); DrawScreen(); });  // callback to close the processing div and redraw screen
}


/* These two functions were created to enable/disable standard input buttons and assign a new value at the same time (display value).   */
function Button_Enable(id, value) {
    document.getElementById(id).value = value;
    document.getElementById(id).disabled = false;
}
function Button_Disable(id, value) {
    document.getElementById(id).value = value;
    document.getElementById(id).disabled = true;
}

// This is for testing form submit values
function FakeFormSubmit_GetValues() {
    // Call to ajax to get script to pull values from page
    // .. WILL evaluate this automagically and we can just use the variable 'query' afterward!
    getvalues = $.ajax({ url: "ajax.aspx?cmd=910.1", async: false }).responseText;
    alert(getvalues);
    //Ajax('?cmd=910.1', 'ErrorConsole');
    eval(getvalues)
    alert(query);
}
function FakeFormSubmit_SaveData() {
    // Call to ajax to get script to pull values from page
    // .. WILL evaluate this automagically and we can just use the variable 'query' afterward!
    getvalues = $.ajax({ url: "ajax.aspx?cmd=910.1", async: false }).responseText;
    eval(getvalues) // this builds the 'query' var
    // Test form saving data!
    Ajax('?cmd=910.2'+query, 'ErrorConsole');
}
function FakeFormSubmit_Validate() {
    // calls dynamically created function for form validation of each control
    UIFS_ValidateForm();
}

/* -------------------------------------------------------------------------------------------------------------------
*** This is the section for editing form controls and their properties
/* -------------------------------------------------------------------------------------------------------------------
*/

function CommonProperties_Update(id) {
    // Get common properties for this control
    Control_name = escape(document.getElementById(id + '_Name').value);
    Control_prompt = escape(document.getElementById(id + '_Prompt').value);
    Control_tip = escape(document.getElementById(id + '_Tip').value);
    Control_required = document.getElementById(id + '_Req').checked;
    Ajax('?cmd=1099&id=' + id + '&name=' + Control_name + '&prompt=' + Control_prompt + '&tip=' + Control_tip + '&req='+Control_required, 'ErrorConsole');
    Button_Disable(id + '_SaveB', 'no Changes');
    // Update the header of the control display
    $('#CTRL' + id + '_Name').html(document.getElementById(id + '_Name').value);
}

function List_AddOption(id) {
    name = escape(document.getElementById(id + '_Opt_Name').value);
    //NOTE: read the code to understand this change (UIFS.Designer.ControlProperties)
    //value = escape(document.getElementById(id + '_Opt_Value').value);
    value = name;

    // Check for invalid chars
    if (name.indexOf(":") > 0 || value.indexOf(":") > 0) {
        // invalid!
        alert('You cannot use the following characters.\n  :');
        return;
    }
    Ajax('?cmd=1101&Option=Add&id=' + id + '&name=' + name + "&value=" + value, 'Control_' + id);
}

function List_RemoveOption(id, item) {
    Ajax('?cmd=1101&Option=Remove&id=' + id + '&i=' + item, 'Control_' + id);
    //OptList = document.getElementById(id+'_Opt_List');
    //alert(OptList[OptList.selectedIndex].text);
    //    if (OptList.selectedIndex != -1) {
    //        Ajax('?cmd=1101&Option=Remove&id=' + id + '&i=' + OptList.selectedIndex, 'Control_' + id);
    //    }
    //    else {
    //        alert('Please select an option from the list first!');
    //    }
}


function Textbox_UpdateProperties(id) {
    Textbox_lines = document.getElementById(id + '_Lines');
    Textbox_width = document.getElementById(id + '_Width');
    Textbox_fulltext = document.getElementById(id + '_fulltext');
    Ajax('?cmd=1100&id=' + id + '&lines=' + Textbox_lines[Textbox_lines.selectedIndex].text + '&width=' + Textbox_width[Textbox_width.selectedIndex].text + '&fulltext=' + Textbox_fulltext.checked, 'ErrorConsole');
    Button_Disable(id + '_TextboxSaveB', 'no changes');
}

function List_UpdateProperties(id) {
    List_type = document.getElementById(id + '_type');
    Ajax('?cmd=1101&Option=update&id=' + id + '&type=' + List_type[List_type.selectedIndex].value, 'ErrorConsole');
    Button_Disable(id + '_ListSaveB', 'no changes');
}

function Checkbox_UpdateProperties(id) {
    Checkbox_type = document.getElementById(id + '_type');
    Checkbox_initialstate = document.getElementById(id + '_initialstate');
    Checkbox_hasinput = document.getElementById(id + '_hasinput');
    Ajax('?cmd=1102&id=' + id + '&type=' + Checkbox_type[Checkbox_type.selectedIndex].value + '&initialstate=' + Checkbox_initialstate.checked + '&hasinput=' + Checkbox_hasinput.checked, 'ErrorConsole');
    Button_Disable(id + '_CheckboxSaveB', 'no changes');
}

function DateTime_UpdateProperties(id) {
    DateTime_type = document.getElementById(id + '_type');
    Ajax('?cmd=1103&id=' + id + '&type=' + DateTime_type[DateTime_type.selectedIndex].value, 'ErrorConsole');
    Button_Disable(id + '_DateTimeSaveB', 'no changes');
}

function Number_UpdateProperties(id) {
    Number_min = document.getElementById(id + '_min');
    Number_max = document.getElementById(id + '_max');
    Number_interval = document.getElementById(id + '_interval');
    Number_slider = document.getElementById(id + '_slider');
    Ajax('?cmd=1104&id=' + id + '&min=' + Number_min.value + '&max=' + Number_max.value + '&interval=' + Number_interval.value + '&slider=' + Number_slider.checked, 'ErrorConsole');
    Button_Disable(id + '_NumberSaveB', 'no changes');
}

function Percentage_UpdateProperties(id) {
    Percentage_interval = document.getElementById(id + '_interval');
    Ajax('?cmd=1105&id=' + id + '&interval=' + Percentage_interval.value, 'ErrorConsole');
    Button_Disable(id + '_PercentageSaveB', 'no changes');
}

function Range_UpdateProperties(id) {
    Range_type = document.getElementById(id + '_type');
    Range_min = document.getElementById(id + '_min');
    Range_max = document.getElementById(id + '_max');
    Ajax('?cmd=1106&id=' + id + '&type=' + Range_type.value + '&min=' + Range_min.value + '&max=' + Range_max.value, 'ErrorConsole');
    Button_Disable(id + '_RangeSaveB', 'no changes');
}