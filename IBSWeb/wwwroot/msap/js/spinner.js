$(document).ready(function () {
    const spinnerWrapper = $('.loader-container');

    if (!spinnerWrapper.parent().is('body')) {
        $('body').append(spinnerWrapper);
    }

    spinnerWrapper.hide();

    $('form:not([data-download])').on('submit', function () {
        if ($(this).data('submitting')) return false;
        $(this).data('submitting', 'true');

        $(this).validate();
        if (!$(this).valid()) {
            $(this).removeData('submitting');
            return false;
        }

        spinnerWrapper.show();
        $('body').addClass('loading');
    });
});
