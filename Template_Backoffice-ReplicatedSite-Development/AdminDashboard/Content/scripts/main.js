require(["app", "pubsub", "containers"], function (app, pubsub) {

    $(function () {
        // Put a small link at the top of all links that will open a new window
        $('a[target="_blank"], a[target="blank"]').append(' <sup><i class="fa-external-link"></i></sup>');

        // Add tooltips to each links and buttons with titles
        $('a[title], input[title], button[title]').tooltip();

        // Auto-create popovers for all links configured to be popovers
        $('a[data-toggle="popover"]').popover({ html: true, placement: 'top' });

        // Collapse navbars if the clicked link is ajaxified
        $(".navbar-collapse").on("click", ".ajaxify", function () {
            $(this).parents('.navbar-collapse').first().collapse('hide');
        });

    });





    // Make each form use MVC's validation system
    $(function () {
        $("form").each(function () {
            $(this).validate({
                showErrors: function (errorMap, errorList) {
                    // Clean up any tooltips for valid elements
                    $.each(this.validElements(), function (index, element) {
                        var $element = $(element);
                        var $formgroup = $element.parents('.form-group');

                        $formgroup.addClass('has-feedback');
                        if ($formgroup.find('.form-control-feedback').length == 0) {
                            $element.after('<span class="form-control-feedback"></span>');
                        }

                        $element.removeClass('bg-danger');

                        if ($formgroup.length > 0) {
                            $formgroup.removeClass('has-error').addClass('has-success');
                            $('.form-control-feedback', $formgroup).removeClass('fa-times').addClass('fa-check');
                        }

                        $element.data("title", "") // Clear the title - there is no error associated anymore
                          .removeClass("error")
                          .tooltip("destroy");
                    });


                    // Create new tooltips for invalid elements
                    $.each(errorList, function (index, error) {
                        var $element = $(error.element);
                        var $formgroup = $element.parents('.form-group');

                        $formgroup.addClass('has-feedback');
                        if ($formgroup.find('.form-control-feedback').length == 0) {
                            $element.after('<span class="form-control-feedback"></span>');
                        }

                        $element.addClass('bg-danger');

                        if ($formgroup.length > 0) {
                            $formgroup.removeClass('has-success').addClass('has-error');
                            $('.form-control-feedback', $formgroup).removeClass('fa-check').addClass('fa-times');
                        }

                        $element.tooltip("destroy") // Destroy any pre-existing tooltip so we can repopulate with new tooltip content
                          .data("title", error.message)
                          .addClass("error")
                          .tooltip(); // Create a new tooltip based on the error messsage we just set in the title
                    });
                }
            });
        });
    });





    // Start an application timeout feature
    if (app.authenticated) {
        require(["idletimeout"], function () {
            $(function () {

                // Ensure that we have everything wrapped up for the smudging
                if ($('#__idlemask').length == 0) {
                    $('body').wrapInner('<div id="__idlemask"></div>');
                }

                // Inject the modal
                var modalHtml = [
                '<div class="modal" id="idletimeoutmodal" data-backdrop="static" data-keyboard="false" tabindex="-1" role="dialog" aria-labelledby="idletimeoutmodalLabel" aria-hidden="true">',
                    '<div class="modal-dialog">',
                        '<div class="modal-content">',
                            '<div class="modal-header">',
                                '<h4 class="modal-title" id="idletimeoutmodalLabel">Are you still there?</h4>',
                            '</div>',
                            '<div class="modal-body">',
                                '<p>We notice you\'ve been idle for a while. To keep your account safe, we will sign you out in <span id="idletimeoutcountdown" style="font-weight:bold"></span> seconds.</p>',
                                '<p>Do you want to continue your session?</p>',
                            '</div>',
                            '<div class="modal-footer">',
                                '<button type="button" class="btn btn-primary">Yes, keep working</button>',
                                '<button type="button" class="btn btn-default" data-dismiss="modal" onclick="$.idleTimeout.options.onTimeout.call(this);">No, sign out</button>',
                            '</div>',
                        '</div>',
                    '</div>',
                '</div>'].join('');
                $('body').append(modalHtml);


                // Start the idle timer
                $.idleTimeout('#idletimeoutmodal', '#idletimeoutmodal .modal-footer button:first', {
                    keepAliveURL: app.path("app/keepalive"),
                    serverResponseEquals: "OK",
                    pollingInterval: 59,
                    idleAfter: (app.sessiontimeout - 3) * 60,
                    warningLength: 60,
                    onTimeout: function () {
                        window.location = app.path("logout");
                    },
                    onIdle: function () {
                        $(this).modal("show");
                        $('#__idlemask').addClass('smudged');
                    },
                    onResume: function () {
                        $(this).modal("hide");
                        $('#__idlemask').removeClass('smudged');
                    },
                    onCountdown: function (counter) {
                        $("#idletimeoutcountdown").html(counter); // update the counter
                    }
                });
            });
        });
    }
});