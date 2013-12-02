

$(document).ready(function () {
    // Setup our processing div to be shown whenever waiting on the server
    $('#Processing').dialog({
        autoOpen: false, height: 100, width: 100, modal: true, position: ['center'], title: 'Processing'
        , closeOnEscape: false, resizable: false, dialogClass: 'Processing_dialog'
    });

    //. setup 
    $('#SaveReport').dialog({
        autoOpen: false, height: 300, width: 500, modal: true, position: ['center', 25], title: 'Save Report'
        , buttons: {
            Cancel: function () { $(this).dialog('close'); },
            'Save': function () { 
            title = document.getElementById('ReportTitle').value;
            Processing();
            Ajax('?cmd=803.1&title='+title,'ErrorConsole', function(){Processing();},false);
            $(this).dialog('close');
            }
        },
    });
    $('#OpenReport').dialog({
        autoOpen: false, height: 300, width: 500, modal: true, position: ['center', 25], title: 'Open Report'
    });
    //. setup: jqplot
    $.jqplot.config.enablePlugins = true;

    // align report options selection bar, selection
    SelectionBar_choose("WHERE");

    //. Start New or Load Existing
    MainMenu();

    //OpenReport(4);
    //Subject_Choose();

});

function Processing() {
    if ($('#Processing').dialog('isOpen')) $('#Processing').dialog('close'); else $('#Processing').dialog('open');
}

function MainMenu() {
    $('#MainMenu').dialog({
        autoOpen: true, height: 300, width: 500, modal: true, position: ['center', 25], title: 'What would you like to do?',
        dialogClass: '',        
        close: function () { $(this).dialog('destroy'); }
    });
}

function Display_OpenReport() {
    $('#MainMenu').dialog('close');
    Ajax('?cmd=801','OpenReport');
    $('#OpenReport').dialog('open');
}
function Display_SaveReport() {
    Ajax('?cmd=803','SaveReport');
    $('#SaveReport').dialog('open');
}
function DrawScreen(option) {
    switch (option) {
        case "THAT SHOWS":
            Ajax('?cmd=811', 'ReportOptions_ThatShows', function () {  }, false);
            break;
        case "WHERE":
            // Now the rest can run asynchronously
            ajaxparam = new Array();
            ajaxparam[0] = new Array('?cmd=809', 'ReportDefinition');
            ajaxparam[1] = new Array('?cmd=810', 'ReportOptions_WHERE');
            AjaxMultiple(ajaxparam, function () { Processing(); }); // expects the processing div to be up already...
            break;
    }
}

function OpenReport(reportid) {
    $('#OpenReport').dialog('close');
    Processing();
    ajaxparam = new Array();
    ajaxparam[0] = new Array('?cmd=801.1&id=' + reportid, 'ErrorConsole'); // This has to run first to setup the report and data (application)
    ajaxparam[1] = new Array('?cmd=808', 'ReportSubject');
    AjaxMultiple(ajaxparam, null, false);
    DrawScreen("WHERE");
}
function NewReport(formid) {
    Processing();
    ajaxparam = new Array();
    ajaxparam[0] = new Array('?cmd=802&id=' + formid, 'ErrorConsole');  // This has to run first to setup the report and data (application)
    ajaxparam[1] = new Array('?cmd=808', 'ReportSubject');
    AjaxMultiple(ajaxparam, null, false);
    DrawScreen("WHERE");
}

/* This function has some hardcoded elements...tied to the css values
*/
function SelectionBar_choose(item) {
    switch (item) {
        case "THAT SHOWS":
            $('#ReportOptionsSelectionbar_selectL').css('left', 33); // 50-17
            w = $('#ReportOptionsSelection_THATSHOWS').innerWidth();
            $('#ReportOptionsSelectionbar_selectR').css('left', 50 + w);
            $('#ReportOptionsSelection_WHERE').css('color', '');
            $('#ReportOptionsSelection_THATSHOWS').css('color', '#0066dd');
            
            $('#ReportOptions_WHERE').hide();
            $('#ReportOptions_ThatShows').show();
            DrawScreen("THAT SHOWS");
            break;
        case "WHERE":
            $('#ReportOptionsSelectionbar_selectL').css('left', 233); // 250-17
            w = $('#ReportOptionsSelection_WHERE').innerWidth();
            $('#ReportOptionsSelectionbar_selectR').css('left', 250 + w);
            $('#ReportOptionsSelection_THATSHOWS').css('color', '');
            $('#ReportOptionsSelection_WHERE').css('color', '#0066dd');
            $('#ReportOptions_ThatShows').hide();
            $('#ReportOptions_WHERE').show();
            break;
    }
}

function ToggleButton(o, a) {
    switch (a) {
        case 0:
            $(o).find('div.button_down').removeClass('button_down').addClass('button'); // if the user is holding down the mouse button
            $(o).find('div.button').hide();
            $(o).find('td.name').css('background-color', '');
            break;
        case 1:
            $(o).find('div.button').show();
            $(o).find('td.name').css('background-color', 'White');
            break;
        case 2:
            $(o).find('div.button').removeClass('button').addClass('button_down');
            break;
        case 3:
            $(o).find('div.button_down').removeClass('button_down').addClass('button');
            break;

    }
}

function Subject_Choose() {
    //. get whether single, multiple, or all forms...

    Ajax('?cmd=300', 'RDInput'); // call to open form dialog
    // Setup 'Open Form' dialog
    $('#RDInput').dialog({
        autoOpen: true, height: 500, width: 750, modal: true, position: ['center', 25], title: 'Choose form(s) to report on:',
        dialogClass: '',
        buttons: {
            Cancel: function () { $(this).dialog('destroy'); },
            'USE': function () { //. open existing form and use as a base for new form                
                $(this).dialog('destroy');
                subjectselected = DataTables_GetSelected(SubjectTable);
                subject = SubjectTable.fnGetData(subjectselected[0]);
                NewReport(subject[0]);
            }
        },
        close: function () { $(this).dialog('destroy'); } // We need to destroy the dialog b/c we create it with new data every time
    });

}

function Option_Redraw(id) {
    phrase = document.getElementById(id+'_phrase').value;
    Ajax('?cmd=810.1&id='+id+'&phrase='+phrase, 'ROpt_'+id); // just get display info for this option
}

function Option_Use(id, Ctrltype) { // Use this option button clicked
    if (!UIFS_ValidateControl('Control_' + id, Ctrltype)) {
        alert('bad value');
    }
    else {
        Processing();
        //. Get input value(s)
        getvalues = Ajax('?cmd=810.2&id='+id, 'ErrorConsole',null, false);
        eval(getvalues);  // this builds the 'query' var
        //. Get language selection
        lang = document.getElementById(id + '_phrase').value;
        //. Add to report!
        Ajax('?cmd=810.3&id=' + id + '&lang=' + lang + query, 'ErrorConsole', null, false);
        // Redisplay...
        ajaxparam = new Array();
        ajaxparam[0] = new Array('?cmd=809', 'ReportDefinition');
        ajaxparam[1] = new Array('?cmd=810', 'ReportOptions_WHERE');
        AjaxMultiple(ajaxparam, function () { Processing(); });
    }
}
function Option_Remove(id) {
    Processing();
    //. remove from report
    Ajax('?cmd=810.4&id=' + id, 'ErrorConsole', null, false);
    // Redisplay...
    ajaxparam = new Array();
    ajaxparam[0] = new Array('?cmd=809', 'ReportDefinition');
    ajaxparam[1] = new Array('?cmd=810', 'ReportOptions_WHERE');
    AjaxMultiple(ajaxparam, function () { Processing(); });
}

function DataTables_GetSelected(oTableLocal) {
    var aReturn = new Array();
    var aTrs = oTableLocal.fnGetNodes();

    for (var i = 0; i < aTrs.length; i++) {
        if ($(aTrs[i]).hasClass('row_selected')) {
            aReturn.push(aTrs[i]);
        }
    }
    return aReturn;
}

function Report_Preview() {
    Processing();    
    $('#ReportPreview').dialog({
        autoOpen: true, height:510, width:750, modal: true, position: ['center', 15], title: 'Previewing Report',
        dialogClass: 'ReportPreviewDialog', resizable:false,
        close: function () { $(this).dialog('destroy'); }
    });
  
    //. load/display data
    Ajax('?cmd=890', 'ReportPreview', function () { Processing(); }, false);

}

function RS_ALL() { // Check every field selection
    $('#ReportOptions_ThatShows').find('input').attr('checked',true);
}
function RS_RESET() { // Check every field selection
    $('#ReportOptions_ThatShows').find('select').attr('selectedIndex', 0);
    $('#ReportOptions_ThatShows').find('input').attr('checked', false);
}

function Aggregate_Add(id) {
    manipulation = document.getElementById('RShow_'+id).value;
    Ajax('?cmd=811.1&id='+id+'&mani='+manipulation, 'ErrorConsole');
}