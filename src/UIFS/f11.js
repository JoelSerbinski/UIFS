/* ---------------------[ F11 ]--------------------------------------
Purpose:: This module makes sure the screen stays the needed size for proper operation of the application
*/


var f11; // holds the interval id# for waiting for correct screen size

$(document).ready(function () {
    VerifyScreenSize(); // make certain we have enough screen "real-estate" and teach the user how to do so
    window.onresize = function () { VerifyScreenSize(); };

});

function VerifyScreenSize() {
    if (document.documentElement.clientWidth < 760 || document.documentElement.clientHeight < 540) {
        if (f11 == null) { // if already displayed..no need
            f11 = setInterval('WaitforF11()', 1000);
            $('#VerifyScreenSize').show();
        }
        $('#VerifyScreenSize #WaitforF11').center(); // always re-center
    }

}

function WaitforF11() {
    if (document.documentElement.clientWidth >= 760 && document.documentElement.clientHeight >= 540) {
        clearInterval(f11); f11 = null;
        $('#VerifyScreenSize').hide();
    }
}

