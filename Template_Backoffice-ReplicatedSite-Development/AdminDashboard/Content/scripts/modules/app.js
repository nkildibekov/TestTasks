// App module
define("app", ["extensions"], function () {
    
    var module = {
        path: function (path) {
            path = path || "";
            if (path.startsWith("/")) path = path.substr(1, path.length);

            var basePath = ($('#apppath').attr('href') || '/');
            if (!basePath.endsWith("/")) basePath = basePath + "/";

            return basePath + path;
        },
        authenticated: ($('#appauthenticated').attr('href') || 'false').toBoolean(),
        sessiontimeout: Number($('#appsessiontimeout').attr('href') || 30)
    };

    return module;

});