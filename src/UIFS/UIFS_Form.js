// ---------------= UIFS_Form.js =------------------- \\

var FormControlsTouched = new Array(); // Holds all the controls that have had activity..

// -[ UIFS_ValidateControl ]-
//:: this function is where you can customize your form field validation routines
// return true if valid, otherwise let fall through routine to end
function UIFS_ValidateControl(Control, type, required) {
    $('#' + Control + '_div').removeClass('invalid'); // make sure we remove this class if it exists already
    o = document.getElementById(Control);
    Ctrlid = Control.substring(8);
    switch (type) {
        case "Checkbox":
            // checkboxes are one or the other...
            //if (required) {
            //if (ControlTouched(Ctrlid)) { return true; } // If the user really wants to make sure the box was checked/unchecked
            //}
            return true;
            break;
        case "DateTime":
        case "Textbox":
            if (required) {
                // can be null
                if (!isNull(o.value)) { return true; }
            }
            else { return true; }
            break;
        case "List":
            if (required) {
                if (o.type == "radio") {
                    if (!isNull($("input[name=" + Control + "]:checked").val())) {return true; }
                }
                // The other types can be accessed directly
                else {
                    if (!isNull(o.value)) { return true; }                
                }
            }
            else { return true; }
            break;
        case "Number":
            if (required) {
                if (isNumber(o.value)) { return true; }
            }
            else { return true; }
            break;
        case "Percentage":
            if (required) {
                if (ControlTouched(Ctrlid)) {
                    if (isNumber(o.value)) { return true; }
                }
            }
            else { return true; }
            break;
        case "Range":
            if (required) {
                if (ControlTouched(Ctrlid + 'S') && ControlTouched(Ctrlid + 'E')) {
                    o1 = document.getElementById(Control + '_Start');
                    o2 = document.getElementById(Control + '_End');
                    if (!isNull(o1.value)) { return true; }
                    if (!isNull(o2.value)) { return true; }
                }
            }
            else { return true; }
            break;
    }
    // everything reaching here is invalid, so highlight!
    $('#' + Control + '_div').addClass('invalid');
    return false;
}
function ControlTouched(id) {
    for (t = 0; t < FormControlsTouched.length; t++) {
        if (FormControlsTouched[t] == id) { return true; } 
    }
    return false;
}

function Number_Validate(Control, min, max, interval) {
    enteredNumber = Control.value;
    if (!isNumber(enteredNumber)) { Control.value = 0; }
    else {
        if (Math.abs(enteredNumber) < min) { Control.value = min; alert('assigned to min'); }
        else {
            if (Math.abs(enteredNumber) > max) { Control.value = max; alert('assigned to max'); }
            else {
                if (interval != 0) {
                    intervalsCount = parseInt(enteredNumber / interval); // we want this to be an int (no decimals), so convert to fixed
                    correctednum = interval * intervalsCount;  //.toFixed(0));
                    if (correctednum != enteredNumber) {
                        Control.value = correctednum;
                        alert('assigned to interval');
                    }
                }
            }
        }
    }
    Control.focus();
}

function Checkbox_Change(id, type) {
// type: 1=checkbox control, 2=select control
    checkboxControl = document.getElementById('Control_' + id);
    // a checkbox control can have an optional input field display when set to true
    checkboxControl_input = document.getElementById('Control_' + id + '_input');
    switch (type) {
        case 1: // regular checkbox control
            // if checked
            if (checkboxControl.checked) {
                // if there is input ..
                if (checkboxControl_input != null) {
                    checkboxControl_input.style.display = 'inline';
                }
            }
            // if not checked
            else {
                // if there is input ..
                if (checkboxControl_input != null) {
                    checkboxControl_input.style.display = 'none';
                }
            }
            break;
        case 2: // Select Control: Yes/No, On/Off
            // if true
            if (checkboxControl[checkboxControl.selectedIndex].value == 1) {
                // if there is input ..
                if (checkboxControl_input != null) {
                    checkboxControl_input.style.display = 'inline';
                }
            }
            // if false
            else {
                // if there is input ..
                if (checkboxControl_input != null) {
                    checkboxControl_input.style.display = 'none';
                }
            }
            break;            
    }

}

// thanks to stackoverflow for this function
function isNumber(n) {
    return !isNaN(parseFloat(n)) && isFinite(n);
}
function isNull(o) {
    if (o==undefined) return true;
    if (o=='') return true;
    return false;
}