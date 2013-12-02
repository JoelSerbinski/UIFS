// -------------------------------------------------------------------
// REF: This is the official version modified: 5/11/2011
// --
// FEATURES:
// : Asynch or Synch calls with callback
// : Synchronous calls return the output as well as populate the object
// AjaxMultiple:
// : Runs all params passed either synchronously or asynch..can have callback when complete with all params

var divobj; // our html output object
var ajaxparam; // ajaxmultiple
var ajaxcallbackfunction; // callback storage

function Ajax(str, ele, callback, async) {
    divobj = ele;
    xmlHttp = GetXmlHttpObject();
    var url = "ajax.aspx"; url = url + str;
    // setup our callback function
    if (callback != null) {
        ajaxcallbackfunction = callback;
    } else { ajaxcallbackfunction = null; } // clear out to be safe
    // Test compatibility
    if (xmlHttp == null) {
        alert("Your browser does not support AJAX!");
        return;
    }
    // Check for asynchronous call or synchronous
    if (typeof (async) == 'undefined') { async = true; } // default to asynch calls
    if (async == true) { // ASync call
        xmlHttp.onreadystatechange = stateChanged;
        xmlHttp.open("GET", url, true);
    }
    else { // Synchronous call
        xmlHttp.open("GET", url, false);
    }
    // Process request
    xmlHttp.send(null);
    // Synchronous routines
    if (async == false) { // If this is a synchronous call, action happens here vs. stateChanged()
        outputHTML = xmlHttp.responseText;
        checkforAlerts(outputHTML);
        document.getElementById(divobj).innerHTML = stripjavascript(outputHTML);
        executejavascript(outputHTML);
        if (ajaxcallbackfunction != null) { ajaxcallbackfunction(); ajaxcallbackfunction = null; }
        return outputHTML; // Synchronous calls return the output...
    }

}

//function AjaxSpecific(str, ele, page) {
//    divobj = ele;
//    xmlHttp = GetXmlHttpObject();
//    if (xmlHttp == null) {
//        alert("Your browser does not support AJAX!");
//        return;
//    }
//    var url = page;
//    url = url + str;
//    xmlHttp.onreadystatechange = stateChanged;
//    xmlHttp.open("GET", url, true);
//    xmlHttp.send(null);
//}

function stateChanged() {
    if (xmlHttp.readyState == 4) // Reports Finished
    {
        // you could add another conditional "if (xmlHttp.status == 200)" statement to eliminate showing errors..
        outputHTML = xmlHttp.responseText;
        checkforAlerts(outputHTML);
        // If there is javascript, we DO NOT want to pass it as output
        document.getElementById(divobj).innerHTML = stripjavascript(outputHTML); // pass output to container
        executejavascript(outputHTML); // if it exists it will execute now that we have the html in the container
        if (ajaxcallbackfunction != null) { ajaxcallbackfunction(); ajaxcallbackfunction = null; }
    }
}

function GetXmlHttpObject() {
    var xmlHttp = null;
    try {
        // Firefox, Opera 8.0+, Safari
        xmlHttp = new XMLHttpRequest();
    }
    catch (e) {
        // Internet Explorer
        try {
            xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
        }
        catch (e) {
            xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
        }
    }
    return xmlHttp;
}

// --[ AjaxMultiple ]--
// This routine takes a 2 paramater array of [querystring][objectforoutput]
// It parses either ALL as synchronous (default) or asynchronous as passed...
// The callback will occur after ALL calls have been completed.  
// :: This behavior is the purpose of the routine.  Single Ajax calls can be used in sequence if you need more granularity
// __
function AjaxMultiple(ajaxparams, callback, async) {
    xmlHttp = GetXmlHttpObject();
    // Test compatibility
    if (xmlHttp == null) {alert("Your browser does not support AJAX!"); return;}
    // BEGIN
    var url = "";
    ajaxparam = ajaxparams;
    if (callback != null) {
        ajaxcallbackfunction = callback; // setup our callback for when ALL parameters are parsed
    } else { ajaxcallbackfunction = null; } // clear out to be safe

    // Check for asynchronous call or synchronous
    if (typeof (async) == 'undefined') { async = true; } // default to asynch calls
    if (async == true) { // ASync call
        url = "ajax.aspx" + ajaxparam[0][0];
        xmlHttp.onreadystatechange = AjaxMultiple_stateChanged;
        xmlHttp.open("GET", url, true);
        xmlHttp.send(null);
    }
    else { // Synchronous call
        for (NumCalls = 0; NumCalls < ajaxparam.length; NumCalls++) {
            url = "ajax.aspx" + ajaxparam[NumCalls][0];
            xmlHttp.open("GET", url, false);
            xmlHttp.send(null);
            // after server finished, implement
            outputHTML = xmlHttp.responseText;
            checkforAlerts(outputHTML);
            document.getElementById(ajaxparam[NumCalls][1]).innerHTML = stripjavascript(outputHTML);
            executejavascript(outputHTML);
        }
        if (ajaxcallbackfunction != null) { ajaxcallbackfunction(); ajaxcallbackfunction = null; }
    }

}
function AjaxMultiple_nextSync(ajaxparam) {
    xmlHttp = GetXmlHttpObject();
    url = "ajax.aspx" + ajaxparam[0][0];
    xmlHttp.onreadystatechange = AjaxMultiple_stateChanged;
    xmlHttp.open("GET", url, true);
    xmlHttp.send(null);
}

function AjaxMultiple_stateChanged() {
    if (xmlHttp.readyState == 4) // Reports Finished
    {
        // you could add another conditional "if (xmlHttp.status == 200)" statement to eliminate showing errors..
        outputHTML = xmlHttp.responseText;
        checkforAlerts(outputHTML);
        // If there is javascript, we DO NOT want to pass it as output
        document.getElementById(ajaxparam[0][1]).innerHTML = stripjavascript(outputHTML);
        executejavascript(outputHTML); // If it exists it will execute        
        AjaxMultiple_ShiftArray(ajaxparam);
    }
}

function AjaxMultiple_ShiftArray(ajaxparams) {
    ajaxparams.shift();
    ajaxparam = ajaxparams;
    if (ajaxparam.length > 0) {
        AjaxMultiple_nextSync(ajaxparam);
    }
    else { // End of params - intiate callback if exists
        if (ajaxcallbackfunction != null) { ajaxcallbackfunction(); ajaxcallbackfunction = null; }
    }
}

function checkforAlerts(outputHTML) {
    alertindex = outputHTML.indexOf(":SystemAlert:");
    alertendindex = outputHTML.indexOf(":SystemAlertEnd:");
    if (alertindex >= 0) {
        // substr() method extracts a specified number of characters in a string, from a start index.
        // substring() method extracts the chars in a string between two specified indexes.
        Message = outputHTML.substring(alertindex + 13, alertendindex);
        alert(Message);
        // If the session has expired, we need to restart the app...
        if (Message == 'Your Session has Expired!\n\nPlease exit the application') {
            if (window.location.href.indexOf("?") > 0) {
                window.location = window.location + '&expired=1';
            }
            else {
                window.location = window.location + '?expired=1';
            }
        }
    }
}

function stripjavascript(outputHTML) {
    // if javascript exists in the returned html...remove it
    jsindex = outputHTML.indexOf("<script type='text/javascript'>");
    if (jsindex >= 0) {
        return outputHTML.substring(0, jsindex); // strip out javascript
    }
    else { // just return it all
        return outputHTML;
    }
}
function executejavascript(outputHTML) {
    // if javascript exists in the returned html...evaluate it!
    jsindex = outputHTML.indexOf("<script type='text/javascript'>");
    while (jsindex >= 0) {
        jsendindex = outputHTML.indexOf("</script>", jsindex);
        eval(outputHTML.substring(jsindex + 31, jsendindex));
        jsindex = outputHTML.indexOf("<script type='text/javascript'>", jsendindex);
    }
}
