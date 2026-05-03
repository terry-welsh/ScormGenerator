
/**
 * SCORM 2004 3rd Edition API Wrapper
 * Handles communication with the LMS
 */
var SCORM = (function() {
    var API = null;
    var initialized = false;
    var terminated = false;

    // Find the SCORM API
    function findAPI(win) {
        var attempts = 0;
        var maxAttempts = 500;

        while ((!win.API_1484_11) && (win.parent) && (win.parent != win) && (attempts < maxAttempts)) {
            attempts++;
            win = win.parent;
        }

        if (win.API_1484_11) {
            return win.API_1484_11;
        }

        // Check opener
        if (win.opener && !win.opener.closed) {
            return findAPI(win.opener);
        }

        return null;
    }

    function getAPI() {
        if (API == null) {
            API = findAPI(window);
        }
        return API;
    }

    function getErrorString(errorCode) {
        var api = getAPI();
        if (api) {
            return api.GetErrorString(errorCode);
        }
        return "";
    }

    function getDiagnostic(errorCode) {
        var api = getAPI();
        if (api) {
            return api.GetDiagnostic(errorCode);
        }
        return "";
    }

    return {
        // Initialize communication with LMS
        initialize: function() {
            if (initialized) return true;
            if (terminated) return false;

            var api = getAPI();
            if (api == null) {
                console.warn("SCORM API not found - running in standalone mode");
                initialized = true;
                return true;
            }

            var result = api.Initialize("");
            if (result === "true" || result === true) {
                initialized = true;
                return true;
            } else {
                var errorCode = api.GetLastError();
                console.error("Initialize failed: " + getErrorString(errorCode));
                return false;
            }
        },

        // Terminate communication with LMS
        terminate: function() {
            if (!initialized) return false;
            if (terminated) return true;

            var api = getAPI();
            if (api == null) {
                terminated = true;
                return true;
            }

            var result = api.Terminate("");
            if (result === "true" || result === true) {
                terminated = true;
                return true;
            } else {
                var errorCode = api.GetLastError();
                console.error("Terminate failed: " + getErrorString(errorCode));
                return false;
            }
        },

        // Get a value from the LMS
        getValue: function(element) {
            if (!initialized || terminated) return "";

            var api = getAPI();
            if (api == null) return "";

            var result = api.GetValue(element);
            var errorCode = api.GetLastError();

            if (errorCode !== "0" && errorCode !== 0) {
                console.warn("GetValue(" + element + ") error: " + getErrorString(errorCode));
            }

            return result;
        },

        // Set a value in the LMS
        setValue: function(element, value) {
            if (!initialized || terminated) return false;

            var api = getAPI();
            if (api == null) {
                console.log("Would set " + element + " = " + value);
                return true;
            }

            var result = api.SetValue(element, value);
            if (result !== "true" && result !== true) {
                var errorCode = api.GetLastError();
                console.error("SetValue(" + element + ", " + value + ") error: " + getErrorString(errorCode));
                return false;
            }

            return true;
        },

        // Commit data to LMS
        commit: function() {
            if (!initialized || terminated) return false;

            var api = getAPI();
            if (api == null) return true;

            var result = api.Commit("");
            if (result !== "true" && result !== true) {
                var errorCode = api.GetLastError();
                console.error("Commit failed: " + getErrorString(errorCode));
                return false;
            }

            return true;
        },

        // Convenience methods
        setCompletionStatus: function(status) {
            return this.setValue("cmi.completion_status", status);
        },

        setSuccessStatus: function(status) {
            return this.setValue("cmi.success_status", status);
        },

        setScore: function(scaled, raw, min, max) {
            var success = true;
            if (scaled !== undefined) success = this.setValue("cmi.score.scaled", scaled) && success;
            if (raw !== undefined) success = this.setValue("cmi.score.raw", raw) && success;
            if (min !== undefined) success = this.setValue("cmi.score.min", min) && success;
            if (max !== undefined) success = this.setValue("cmi.score.max", max) && success;
            return success;
        },

        setLocation: function(location) {
            return this.setValue("cmi.location", location);
        },

        getLocation: function() {
            return this.getValue("cmi.location");
        },

        setSuspendData: function(data) {
            return this.setValue("cmi.suspend_data", JSON.stringify(data));
        },

        getSuspendData: function() {
            var data = this.getValue("cmi.suspend_data");
            try {
                return data ? JSON.parse(data) : {};
            } catch(e) {
                return {};
            }
        }
    };
})();

// Auto-initialize when document is ready
document.addEventListener('DOMContentLoaded', function() {
    SCORM.initialize();
});

// Auto-terminate when leaving page
window.addEventListener('beforeunload', function() {
    SCORM.terminate();
});
