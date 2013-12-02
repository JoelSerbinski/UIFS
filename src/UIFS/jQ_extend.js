
// This adds a center function to jQuery we use for centering divs.  Usage: $('#div').center();
(function ($) {
    $.fn.extend({
        center: function () {
            return this.each(function () {
                var top = ($(window).height() - $(this).outerHeight()) / 2;
                var left = ($(window).width() - $(this).outerWidth()) / 2;
                $(this).css({ position: 'absolute', margin: 0, top: (top > 0 ? top : 0) + 'px', left: (left > 0 ? left : 0) + 'px' });
            });
        }
    });
})(jQuery);