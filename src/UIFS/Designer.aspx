<%@ Page Language="C#" AutoEventWireup="true"  CodeBehind="Designer.aspx.cs" Inherits="UIFS.DesignerDisplay" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>UIFS - Designer</title>
    
    <meta http-equiv="Pragma" content="no-cache" />
    <meta http-equiv="Expires" content="-1" />
    <meta http-equiv="X-UA-Compatible" content="IE=8" /> <!-- we are IE8 Compatible -->
    
    <!-- jquery css -->
    <link href="jquery/css/ui-lightness/jquery-ui-1.8.23.custom.css" rel="stylesheet" type="text/css"/>    
    <!-- jquery "plugins" style sheets-->
    <link href="jquery/css/ui.slider.extras.css" rel="stylesheet" type="text/css"/>
    <link href="jquery/css/anytime.css" rel="stylesheet" type="text/css"/>
    <link href="jquery/css/datatables.css" rel="stylesheet" type="text/css"/>
    <!-- jquery js -->
    <script src="jquery/js/jquery-1.8.0.min.js" type="text/javascript"></script>
    <script src="jquery/js/jquery-ui-1.8.23.custom.min.js" type="text/javascript"></script>    
    <!-- jquery "plugins" -->
    <script src="jquery/js/selectToUISlider.jQuery.js" type="text/javascript"></script> <!-- Not sure if we want this anymore - using slider func in jquery now -->
    <script src="jquery/js/anytime.js" type="text/javascript"></script>
    <script src="jquery/js/jquery.dataTables.min.js" type="text/javascript"></script>

    <!-- OUR Scripts and style sheets -->
    <script src="ajax.js" type="text/javascript"></script>
    <script src="Designer.js" type="text/javascript"></script>
    <link href="Designer.css" rel="stylesheet" type="text/css" />
    <link href="UIFS_Form.css" rel="stylesheet" type="text/css" />
    <script src="UIFS_Form.js" type="text/javascript"></script>

</head>


<body>
    <div id='content'>        
        <div id='Controls'></div>
        <div id='Controls_Header'>CONTROLS</div>
        <div id='Controls_Container'></div>
        
        <div id='FormTemplate'></div>
            <div id='FormTemplate_Header_Menu' onclick="Form_Menu('toggle')" title='MENU'>
                <div id='FormMenu'>
                    <table>
                    <tr><td class='menuitem' onclick="Form_New()"><img src='Images/form_add.png' /> New Form</td></tr>
                    <tr><td class='menuitem' onclick="Form_Newbasedon()"><img src='Images/form_add.png' /> New Form based on..</td></tr>
                    <tr><td class='menuitem' onclick="Form_Open()"><img src='Images/form_edit.png' /> Open Form</td></tr>
                    <tr><td class='separator'></td></tr>
                    <tr><td class='menuitem' onclick="Form_Settings()"><img src='Images/wrench.png' /> Form Settings</td></tr>
                    <tr><td class='menuitem' onclick="Form_Save()"><img src='Images/disk.png' /> Save Form</td></tr>
                    <tr><td class='separator'></td></tr>
                    <tr><td class='menuitem' onclick="TEST()"><img src='Images/wrench.png' /> TEST</td></tr>
                    <!--<tr><td class='menuitem' ><img src='Images/application_key.png' /> Commit this version</td></tr>-->
                    <tr><td class='menuitem' onclick="SessionEnd()"><img src='Images/exit.png' /> Leave</td></tr>
                    </table>
                </div>
            </div>
            <div id='FormTemplate_Header_Preview' onclick="Form_Preview()" title='Preview!'></div>
        <div id='FormTemplate_Header'>FORM LAYOUT</div>        
        <div id='FormTemplate_Container'></div>
        
        
    </div>
    
<!-- This div is used for the popup dialog add a new control -->
    <div id='NewControl' title='Add Control' class='hiddendiv'>
    </div>


    <div id='MENU_Popup' title='' class='hiddendiv'>
    </div>
    
    
    <div id='Processing' class='hiddendiv'><img src='Images/processing.gif' alt='Processing' height='50px' width='50px' /></div>

    <!-- This div is used by ajax calls that do not return data to display, but rather perform a funciton.  These
        calls may still pass an Error Message or other notification and these will get picked up by ajax.  We just
        need a blank div to have an ajax output when we do not necessarily need one.
     -->
    <div id='ErrorConsole' class='hiddendiv'></div>
     
</body>


</html>


