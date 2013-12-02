<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ReportDesigner.aspx.cs" Inherits="UIFS.ReportDesigner" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html>
<head>
    <title>Report Designer :: UIFS</title>
    <meta http-equiv="Pragma" content="no-cache" />
    <meta http-equiv="Expires" content="-1" />
    <meta http-equiv="X-UA-Compatible" content="IE=9" />

    <!-- jquery css -->
    <link href="jquery/css/ui-lightness/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css"/>    
    <!-- jquery "plugins" style sheets-->
    <link href="jquery/css/ui.slider.extras.css" rel="stylesheet" type="text/css"/>
    <link href="jquery/css/anytime.css" rel="stylesheet" type="text/css"/>
    <link href="jquery/css/datatables.css" rel="stylesheet" type="text/css"/>
    <link href="jquery/css/jquery.jqplot.css" rel="stylesheet" type="text/css"/>
    <!-- jquery js -->
    <script src="jquery/js/jquery-1.8.0.min.js" type="text/javascript"></script>
    <script src="jquery/js/jquery-ui-1.8.23.custom.min.js" type="text/javascript"></script>    
    <!-- jquery "plugins" -->
    <script src="jquery/js/selectToUISlider.jQuery.js" type="text/javascript"></script> <!-- Not sure if we want this anymore - using slider func in jquery now -->
    <script src="jquery/js/anytime.js" type="text/javascript"></script>
    <script src="jquery/js/jquery.dataTables.min.js" type="text/javascript"></script>
    <script src="jquery/js/jquery.jqplot.min.js" type="text/javascript"></script>
    <script src="jquery/js/jqplot.pieRenderer.min.js" type="text/javascript"></script>

    <script src="jQ_extend.js" type="text/javascript"></script> <!-- our custom extensions -->

    <!-- OUR Scripts and style sheets -->
    <script src="ajax.js" type="text/javascript"></script>
    <link href="UIFS_Form.css" rel="stylesheet" type="text/css" />
    <script src="UIFS_Form.js" type="text/javascript"></script>
    <link href="Designer.css" rel="stylesheet" type="text/css" /> <!-- Loaded for the Form_Open dialog -->
    <link href="ReportDesigner.css" rel="stylesheet" type="text/css" />
    <script src="ReportDesigner.js" type="text/javascript"></script>

</head>
<body>
<div id='content'>
    <div id='ErrorConsole' class='hiddendiv'></div>
    <div id='Processing' class='hiddendiv'><img src='Images/processing.gif' alt='Processing' height='50px' width='50px' /></div>
    <div id='RDInput'></div>
    <div id='MainMenu'>
        <div class='NewReport' onclick="Subject_Choose(); $('#MainMenu').dialog('close');" title='Start New Report'></div>
        <div class='OpenReport' onclick="Display_OpenReport();" title='Open Existing Report'></div>
    </div>
    <div id='SaveReport'></div>
    <div id='OpenReport'></div>


    <div id='ReportSubject'></div>
    <div id='ReportPreview_button' onclick='Report_Preview();' title='Preview this Report !!!'></div>
    <div id='ReportSave_button' onclick='Display_SaveReport();' title='Save Report!'></div>

    <div id='ReportDefinition'></div>

    <div id='ReportOptionsSelectionbar'>
        <div id='ReportOptionsSelectionbar_selectL'></div>
        <div id='ReportOptionsSelectionbar_selectR'></div>
        <div id='ReportOptionsSelection_THATSHOWS' onclick="SelectionBar_choose('THAT SHOWS')">THAT SHOWS</div>
        <div id='ReportOptionsSelection_WHERE' onclick="SelectionBar_choose('WHERE')">WHERE</div>
    </div>

    <div id='ReportOptions'>
        <div id='ReportOptions_ThatShows'></div>
        <div id='ReportOptions_WHERE'></div>
    </div>
    
    <div id='ReportPreview'></div>
</div>

<div id='VerifyScreenSize'><div id='WaitforF11'></div></div>

</body>
</html>
