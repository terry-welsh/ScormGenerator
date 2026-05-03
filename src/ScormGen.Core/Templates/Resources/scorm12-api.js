
/**
 * SCORM 1.2 API Wrapper
 * Handles communication with the LMS via the LMS-prefixed runtime API.
 */
var SCORM = (function() {
    var api = null;
    var initialized = false;
    var terminated = false;
    var sessionCompleted = false;

    // SCORM 1.2 discovery: search for window.API, max 7 parent levels
    function findAPI(win) {
        var attempts = 0;
        var maxAttempts = 7;

        while (!win.API && win.parent && win.parent !== win && attempts < maxAttempts) {
            attempts++;
            win = win.parent;
        }

        if (win.API) return win.API;

        if (win.opener && !win.opener.closed) {
            return findAPI(win.opener);
        }

        return null;
    }

    function getAPI() {
        if (!api) api = findAPI(window);
        return api;
    }

    function getErrorString(a, errorCode) {
        return a ? a.LMSGetErrorString(errorCode) : "";
    }

    return {
        initialize: function() {
            if (initialized) return true;
            if (terminated) return false;

            var a = getAPI();
            if (!a) {
                console.warn("SCORM API not found - running in standalone mode");
                initialized = true;
                return true;
            }

            var result = a.LMSInitialize("");
            if (result === "true" || result === true) {
                initialized = true;
                return true;
            }
            console.error("LMSInitialize failed: " + getErrorString(a, a.LMSGetLastError()));
            return false;
        },

        terminate: function() {
            if (!initialized) return false;
            if (terminated) return true;

            var a = getAPI();
            if (!a) { terminated = true; return true; }

            // Set exit to "suspend" only when the session is not yet complete so the
            // LMS knows the learner will return; completed sessions use the default ("").
            if (!sessionCompleted) {
                a.LMSSetValue("cmi.core.exit", "suspend");
            }
            a.LMSCommit("");

            var result = a.LMSFinish("");
            if (result === "true" || result === true) {
                terminated = true;
                return true;
            }
            console.error("LMSFinish failed: " + getErrorString(a, a.LMSGetLastError()));
            return false;
        },

        getValue: function(element) {
            if (!initialized || terminated) return "";
            var a = getAPI();
            if (!a) return "";
            var result = a.LMSGetValue(element);
            var err = a.LMSGetLastError();
            if (err !== "0" && err !== 0) {
                console.warn("LMSGetValue(" + element + ") error: " + getErrorString(a, err));
            }
            return result;
        },

        setValue: function(element, value) {
            if (!initialized || terminated) return false;
            var a = getAPI();
            if (!a) {
                console.log("Would set " + element + " = " + value);
                return true;
            }
            var result = a.LMSSetValue(element, value);
            if (result !== "true" && result !== true) {
                console.error("LMSSetValue(" + element + ", " + value + ") error: " + getErrorString(a, a.LMSGetLastError()));
                return false;
            }
            return true;
        },

        commit: function() {
            if (!initialized || terminated) return false;
            var a = getAPI();
            if (!a) return true;
            var result = a.LMSCommit("");
            if (result !== "true" && result !== true) {
                console.error("LMSCommit failed: " + getErrorString(a, a.LMSGetLastError()));
                return false;
            }
            return true;
        },

        // Convenience methods — identical surface to the SCORM 2004 shim so all HTML
        // templates work without modification; values are mapped to SCORM 1.2 data model.

        setCompletionStatus: function(status) {
            // cmi.core.lesson_status: "completed", "incomplete", "not attempted", "browsed"
            if (status === "completed") sessionCompleted = true;
            return this.setValue("cmi.core.lesson_status", status);
        },

        setSuccessStatus: function(status) {
            // "passed" / "failed" override lesson_status for graded content
            if (status === "passed" || status === "failed") sessionCompleted = true;
            return this.setValue("cmi.core.lesson_status", status);
        },

        setScore: function(scaled, raw, min, max) {
            // SCORM 1.2 has no scaled score field; convert scaled (0–1) to a 0–100 raw score
            var rawScore = Math.round((scaled || 0) * 100);
            return this.setValue("cmi.core.score.raw", String(rawScore));
        },

        setLocation: function(location) {
            return this.setValue("cmi.core.lesson_location", location);
        },

        getLocation: function() {
            return this.getValue("cmi.core.lesson_location");
        },

        setSuspendData: function(data) {
            return this.setValue("cmi.suspend_data", JSON.stringify(data));
        },

        getSuspendData: function() {
            var data = this.getValue("cmi.suspend_data");
            try { return data ? JSON.parse(data) : {}; } catch(e) { return {}; }
        }
    };
})();

document.addEventListener('DOMContentLoaded', function() { SCORM.initialize(); });
window.addEventListener('beforeunload', function() { SCORM.terminate(); });
