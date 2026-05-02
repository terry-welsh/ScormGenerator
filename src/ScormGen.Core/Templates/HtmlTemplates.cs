namespace ScormGen.Core.Templates;

public static class HtmlTemplates
{
    // =========================================================================
    // Brand colors
    // =========================================================================

    private const string Primary      = "#98c93d";
    private const string PrimaryHover = "#6fb03a";
    private const string PrimaryLight = "#f9fdef";
    private const string PrimaryBg    = "#f1f8e8";
    private const string Accent       = "#49c6e5";
    private const string Success      = "#6ca437";
    private const string SuccessBg    = "#e8f4d0";
    private const string SuccessText  = "#3d6b1e";
    private const string Danger       = "#da7552";
    private const string DangerBg     = "#faeae4";
    private const string DangerText   = "#7a3018";
    private const string Heading      = "#353535";
    private const string Subheading   = "#4c4d4f";
    private const string Muted        = "#4d4e50";
    private const string BodyBg       = "#f8f8f8";
    private const string TextMain     = "#353535";
    private const string TextWhite    = "#ffffff";
    private const string BorderLight  = "#ececec";
    private const string CardShadow   = "rgba(53,53,53,0.08)";

    // =========================================================================
    // SCORM 2004 3rd Edition API Wrapper — verbatim from Python source
    // =========================================================================

    public static readonly string ScormApiJs = """

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
""";

    // =========================================================================
    // CSS — computed once with brand colors baked in
    // =========================================================================

    public static readonly string BaseStyles = $$"""
    <style>
        * {
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            line-height: 1.6;
            color: {{TextMain}};
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
            background: {{BodyBg}};
        }

        .content-wrapper {
            background: {{TextWhite}};
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 4px {{CardShadow}};
        }

        h1 {
            color: {{Heading}};
            border-bottom: 3px solid {{Primary}};
            padding-bottom: 10px;
            margin-top: 0;
        }

        h2 {
            color: {{Subheading}};
            margin-top: 30px;
        }

        h3 {
            color: {{Muted}};
        }

        h4 {
            color: {{Muted}};
        }

        p {
            margin: 1em 0;
        }

        ul, ol {
            padding-left: 25px;
        }

        li {
            margin: 0.5em 0;
        }

        blockquote {
            border-left: 4px solid {{Primary}};
            margin: 1em 0;
            padding: 0.5em 1em;
            background: {{PrimaryBg}};
            border-radius: 0 4px 4px 0;
        }

        code {
            background: #ecf0f1;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Monaco', 'Consolas', monospace;
        }

        pre {
            background: {{Heading}};
            color: #ecf0f1;
            padding: 15px;
            border-radius: 4px;
            overflow-x: auto;
        }

        pre code {
            background: none;
            padding: 0;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin: 1em 0;
        }

        th, td {
            border: 1px solid #ddd;
            padding: 12px;
            text-align: left;
        }

        th {
            background: {{Primary}};
            color: {{TextWhite}};
        }

        tr:nth-child(even) {
            background: #f9f9f9;
        }

        .callout {
            background: {{PrimaryBg}};
            border-left: 4px solid {{Primary}};
            padding: 15px;
            margin: 1em 0;
            border-radius: 0 4px 4px 0;
        }

        .callout-warning {
            background: #fef3e2;
            border-left-color: #f39c12;
        }

        .callout-success {
            background: {{SuccessBg}};
            border-left-color: {{Success}};
        }

        .callout-danger {
            background: {{DangerBg}};
            border-left-color: {{Danger}};
        }

        .completion-marker {
            position: fixed;
            bottom: 20px;
            right: 20px;
            background: {{Success}};
            color: {{TextWhite}};
            padding: 10px 20px;
            border-radius: 4px;
            display: none;
            animation: fadeIn 0.3s;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(10px); }
            to { opacity: 1; transform: translateY(0); }
        }

        /* Assessment Styles */
        .question-container {
            background: white;
            border: 1px solid #ddd;
            border-radius: 8px;
            padding: 25px;
            margin: 20px 0;
        }

        .question-number {
            display: inline-block;
            background: {{Primary}};
            color: {{TextWhite}};
            width: 30px;
            height: 30px;
            border-radius: 50%;
            text-align: center;
            line-height: 30px;
            margin-right: 10px;
            font-weight: bold;
        }

        .question-text {
            font-size: 1.1em;
            margin: 15px 0;
            color: {{Subheading}};
        }

        .options-list {
            list-style: none;
            padding: 0;
        }

        .option-item {
            margin: 10px 0;
        }

        .option-label {
            display: flex;
            align-items: flex-start;
            padding: 12px 15px;
            border: 2px solid #ddd;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
        }

        .option-label:hover {
            border-color: {{Primary}};
            background: #f8f9fa;
        }

        .option-item input[type="radio"] {
            opacity: 0;
            position: absolute;
            width: 0;
            height: 0;
        }

        .option-item input[type="radio"]:checked + .option-label,
        .option-item input[type="radio"]:checked ~ .option-label {
            border-color: {{Primary}};
            background: {{BodyBg}};
        }

        .option-letter {
            font-weight: bold;
            margin-right: 8px;
            color: {{Accent}};
        }

        .feedback {
            margin-top: 15px;
            padding: 15px;
            border-radius: 6px;
            display: none;
        }

        .feedback.correct {
            background: {{SuccessBg}};
            border: 1px solid {{Success}};
            color: {{SuccessText}};
        }

        .feedback.incorrect {
            background: {{DangerBg}};
            border: 1px solid {{Danger}};
            color: {{DangerText}};
        }

        .feedback.neutral {
            background: {{PrimaryBg}};
            border: 1px solid #aed6f1;
            color: {{Primary}};
        }

        .explanation {
            margin-top: 10px;
            font-style: italic;
        }

        /* Buttons */
        .btn {
            display: inline-block;
            padding: 12px 24px;
            font-size: 1em;
            font-weight: bold;
            text-align: center;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s;
        }

        .btn-primary {
            background: {{Primary}};
            color: {{TextWhite}};
        }

        .btn-primary:hover {
            background: {{PrimaryHover}};
        }

        .btn-success {
            background: {{Success}};
            color: {{TextWhite}};
        }

        .btn-success:hover {
            background: #196f3d;
        }

        .btn:disabled {
            background: #bdc3c7;
            cursor: not-allowed;
        }

        /* Progress indicator */
        .progress-bar {
            width: 100%;
            height: 8px;
            background: #ecf0f1;
            border-radius: 4px;
            margin: 20px 0;
            overflow: hidden;
        }

        .progress-fill {
            height: 100%;
            background: {{Primary}};
            border-radius: 4px;
            transition: width 0.3s;
        }

        /* Results */
        .results-container {
            text-align: center;
            padding: 40px;
        }

        .score-display {
            font-size: 3em;
            font-weight: bold;
            margin: 20px 0;
        }

        .score-display.passed {
            color: {{Success}};
        }

        .score-display.failed {
            color: {{Danger}};
        }

        .results-message {
            font-size: 1.2em;
            margin: 20px 0;
        }

        /* Scenario Styles */
        .scenario-situation {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid {{Primary}};
        }

        .scenario-options {
            margin: 20px 0;
        }

        .scenario-option {
            background: {{TextWhite}};
            border: 2px solid {{BorderLight}};
            border-radius: 8px;
            padding: 20px;
            margin: 15px 0;
            cursor: pointer;
            transition: all 0.2s;
        }

        .scenario-option:hover {
            border-color: {{Primary}};
            box-shadow: 0 2px 8px {{CardShadow}};
        }

        .scenario-option.selected {
            border-color: {{Primary}};
            background: {{BodyBg}};
        }

        .scenario-option.correct-highlight {
            border-color: {{Success}};
            background: {{SuccessBg}};
        }

        .scenario-option h4 {
            margin: 0 0 10px 0;
            color: {{Accent}};
        }

        .analysis-section {
            background: {{PrimaryBg}};
            border: 1px solid #aed6f1;
            border-radius: 8px;
            padding: 25px;
            margin: 20px 0;
            display: none;
        }

        .analysis-section h3 {
            color: {{Primary}};
            margin-top: 0;
        }

        .additional-info {
            background: #fff3cd;
            border: 1px solid #ffeeba;
            border-radius: 8px;
            padding: 20px;
            margin: 15px 0;
        }

        .additional-info h4 {
            color: #856404;
            margin-top: 0;
        }
    </style>
    """;

    // =========================================================================
    // imsmanifest.xml
    // =========================================================================

    public static string GetManifest(string identifier, string title, string contentType, double passingScore)
    {
        var sequencing = GetSequencingRules(contentType, passingScore);
        return $$"""
<?xml version="1.0" encoding="UTF-8"?>
<manifest identifier="{{identifier}}" version="1.0"
    xmlns="http://www.imsglobal.org/xsd/imscp_v1p1"
    xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_v1p3"
    xmlns:adlseq="http://www.adlnet.org/xsd/adlseq_v1p3"
    xmlns:adlnav="http://www.adlnet.org/xsd/adlnav_v1p3"
    xmlns:imsss="http://www.imsglobal.org/xsd/imsss"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.imsglobal.org/xsd/imscp_v1p1 imscp_v1p1.xsd
                        http://www.adlnet.org/xsd/adlcp_v1p3 adlcp_v1p3.xsd
                        http://www.adlnet.org/xsd/adlseq_v1p3 adlseq_v1p3.xsd
                        http://www.adlnet.org/xsd/adlnav_v1p3 adlnav_v1p3.xsd
                        http://www.imsglobal.org/xsd/imsss imsss_v1p0.xsd">

    <metadata>
        <schema>ADL SCORM</schema>
        <schemaversion>2004 3rd Edition</schemaversion>
    </metadata>

    <organizations default="ORG-{{identifier}}">
        <organization identifier="ORG-{{identifier}}" structure="hierarchical">
            <title>{{title}}</title>
            <item identifier="ITEM-{{identifier}}" identifierref="RES-{{identifier}}">
                <title>{{title}}</title>
                {{sequencing}}
            </item>
        </organization>
    </organizations>

    <resources>
        <resource identifier="RES-{{identifier}}" type="webcontent" adlcp:scormType="sco" href="index.html">
            <file href="index.html"/>
            <file href="scorm_api.js"/>
        </resource>
    </resources>
</manifest>
""";
    }

    private static string GetSequencingRules(string contentType, double passingScore) =>
        string.Equals(contentType, "graded", StringComparison.OrdinalIgnoreCase)
            ? $$"""

                <imsss:sequencing>
                    <imsss:deliveryControls completionSetByContent="true" objectiveSetByContent="true"/>
                    <imsss:objectives>
                        <imsss:primaryObjective objectiveID="primary_obj" satisfiedByMeasure="true">
                            <imsss:minNormalizedMeasure>{{passingScore}}</imsss:minNormalizedMeasure>
                        </imsss:primaryObjective>
                    </imsss:objectives>
                </imsss:sequencing>
"""
            : """

                <imsss:sequencing>
                    <imsss:deliveryControls completionSetByContent="true"/>
                </imsss:sequencing>
""";

    // =========================================================================
    // HTML page templates
    // =========================================================================

    public static string GetInformationalHtml(string title, string contentHtml) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    {{BaseStyles}}
    <script>{{ScormApiJs}}</script>
</head>
<body>
    <div class="content-wrapper">
        <h1>{{title}}</h1>
        <div class="progress-bar">
            <div class="progress-fill" id="progressFill" style="width: 0%"></div>
        </div>
        <div id="content">
            {{contentHtml}}
        </div>
        <div class="completion-marker" id="completionMarker">
            ✓ Content Completed
        </div>
    </div>

    <script>
        (function() {
            var completed = false;
            var scrollThreshold = 0.9;

            function updateProgress() {
                var scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                var scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
                var progress = scrollHeight > 0 ? (scrollTop / scrollHeight) * 100 : 100;

                document.getElementById('progressFill').style.width = Math.min(progress, 100) + '%';

                if (progress >= scrollThreshold * 100 && !completed) {
                    markComplete();
                }
            }

            function markComplete() {
                if (completed) return;
                completed = true;

                SCORM.setCompletionStatus('completed');
                SCORM.commit();

                document.getElementById('completionMarker').style.display = 'block';
            }

            window.addEventListener('scroll', updateProgress);
            window.addEventListener('load', function() {
                updateProgress();
                // Auto-complete if content is short
                if (document.documentElement.scrollHeight <= window.innerHeight) {
                    setTimeout(markComplete, 2000);
                }
            });
        })();
    </script>
</body>
</html>
""";

    public static string GetScenarioHtml(
        string title,
        string situation,
        string optionsHtml,
        string analysisHtml,
        string additionalHtml,
        string correctOption) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    {{BaseStyles}}
    <script>{{ScormApiJs}}</script>
</head>
<body>
    <div class="content-wrapper">
        <h1>{{title}}</h1>

        <div class="scenario-situation">
            <h3>Situation</h3>
            {{situation}}
        </div>

        <div class="scenario-options" id="optionsContainer">
            <h3>What would you do?</h3>
            {{optionsHtml}}
        </div>

        <div class="analysis-section" id="analysisSection">
            <h3>Analysis</h3>
            <div id="analysisContent">{{analysisHtml}}</div>
            {{additionalHtml}}
        </div>
    </div>

    <script>
        (function() {
            var hasInteracted = false;

            window.selectOption = function(optionLetter) {
                if (hasInteracted) return;
                hasInteracted = true;

                // Mark selected option
                var options = document.querySelectorAll('.scenario-option');
                var correctOption = '{{correctOption}}';

                options.forEach(function(opt) {
                    opt.classList.remove('selected', 'correct-highlight');
                    if (opt.dataset.option === optionLetter) {
                        opt.classList.add('selected');
                    }
                    if (correctOption && opt.dataset.option === correctOption) {
                        opt.classList.add('correct-highlight');
                    }
                });

                // Show analysis
                document.getElementById('analysisSection').style.display = 'block';

                // Scroll to analysis
                document.getElementById('analysisSection').scrollIntoView({ behavior: 'smooth' });

                // Mark complete (ungraded - no score)
                SCORM.setCompletionStatus('completed');
                SCORM.commit();
            };
        })();
    </script>
</body>
</html>
""";

    public static string GetUngradedQuizHtml(string title, string questionsHtml, int questionCount) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    {{BaseStyles}}
    <script>{{ScormApiJs}}</script>
</head>
<body>
    <div class="content-wrapper">
        <h1>{{title}}</h1>
        <p>This is a knowledge check to help reinforce your learning. Your answers are not scored.</p>

        <div class="progress-bar">
            <div class="progress-fill" id="progressFill" style="width: 0%"></div>
        </div>

        <div id="questionsContainer">
            {{questionsHtml}}
        </div>

        <div class="completion-marker" id="completionMarker">
            ✓ Knowledge Check Completed
        </div>
    </div>

    <script>
        (function() {
            var totalQuestions = {{questionCount}};
            var answeredQuestions = new Set();

            window.checkAnswer = function(questionNum, selectedAnswer, correctAnswer) {
                if (answeredQuestions.has(questionNum)) return;
                answeredQuestions.add(questionNum);

                var feedbackEl = document.getElementById('feedback-' + questionNum);
                var feedbackTextEl = document.getElementById('feedback-text-' + questionNum);
                var isCorrect = selectedAnswer === correctAnswer;

                if (isCorrect) {
                    feedbackEl.className = 'feedback correct';
                    feedbackTextEl.innerHTML = 'Correct!'
                } else {
                    feedbackEl.className = 'feedback incorrect';
                    feedbackTextEl.innerHTML = 'Not quite. The correct answer is ' + correctAnswer + '.';
                }
                feedbackEl.style.display = 'block';

                // Disable options for this question
                var options = document.querySelectorAll('input[name="q' + questionNum + '"]');
                options.forEach(function(opt) {
                    opt.disabled = true;
                });

                // Update progress
                var progress = (answeredQuestions.size / totalQuestions) * 100;
                document.getElementById('progressFill').style.width = progress + '%';

                // Check if all questions answered
                if (answeredQuestions.size >= totalQuestions) {
                    SCORM.setCompletionStatus('completed');
                    SCORM.commit();
                    document.getElementById('completionMarker').style.display = 'block';
                }
            };
        })();
    </script>
</body>
</html>
""";

    public static string GetGradedQuizHtml(
        string title,
        string questionsHtml,
        int questionCount,
        double passingScore,
        int passingPercent,
        int minCorrect,
        string correctAnswersJson,
        string explanationsJson) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    {{BaseStyles}}
    <script>{{ScormApiJs}}</script>
</head>
<body>
    <div class="content-wrapper">
        <h1>{{title}}</h1>
        <p><strong>Passing Score:</strong> {{passingPercent}}% ({{minCorrect}}/{{questionCount}} correct answers required)</p>

        <div class="progress-bar">
            <div class="progress-fill" id="progressFill" style="width: 0%"></div>
        </div>

        <form id="quizForm">
            <div id="questionsContainer">
                {{questionsHtml}}
            </div>

            <div style="text-align: center; margin-top: 30px;">
                <button type="submit" class="btn btn-success" id="submitBtn">
                    Submit Assessment
                </button>
            </div>
        </form>

        <div class="results-container" id="resultsContainer" style="display: none;">
            <h2>Assessment Results</h2>
            <div class="score-display" id="scoreDisplay"></div>
            <p class="results-message" id="resultsMessage"></p>
            <div id="reviewContainer"></div>
        </div>
    </div>

    <script>
        (function() {
            var totalQuestions = {{questionCount}};
            var passingScore = {{passingScore}};
            var correctAnswers = {{correctAnswersJson}};
            var explanations = {{explanationsJson}};

            document.getElementById('quizForm').addEventListener('submit', function(e) {
                e.preventDefault();

                var score = 0;
                var answered = 0;
                var review = [];

                for (var i = 1; i <= totalQuestions; i++) {
                    var selected = document.querySelector('input[name="q' + i + '"]:checked');
                    if (selected) {
                        answered++;
                        var isCorrect = selected.value === correctAnswers[i];
                        if (isCorrect) score++;

                        review.push({
                            question: i,
                            selected: selected.value,
                            correct: correctAnswers[i],
                            isCorrect: isCorrect,
                            explanation: explanations[i] || ''
                        });
                    }
                }

                if (answered < totalQuestions) {
                    alert('Please answer all questions before submitting.');
                    return;
                }

                var scaledScore = score / totalQuestions;
                var passed = scaledScore >= passingScore;
                var percent = Math.round(scaledScore * 100);

                // Report to LMS
                SCORM.setScore(scaledScore, score, 0, totalQuestions);
                SCORM.setCompletionStatus('completed');
                SCORM.setSuccessStatus(passed ? 'passed' : 'failed');
                SCORM.commit();

                // Display results
                document.getElementById('quizForm').style.display = 'none';
                document.getElementById('resultsContainer').style.display = 'block';

                var scoreDisplay = document.getElementById('scoreDisplay');
                scoreDisplay.textContent = percent + '%';
                scoreDisplay.className = 'score-display ' + (passed ? 'passed' : 'failed');

                var message = passed
                    ? 'Congratulations! You passed the assessment.'
                    : 'You did not pass. Review the material and try again.';
                document.getElementById('resultsMessage').textContent = message;

                // Show review
                var reviewHtml = '<h3>Review Your Answers</h3>';
                review.forEach(function(r) {
                    reviewHtml += '<div class="question-container">';
                    reviewHtml += '<p><strong>Question ' + r.question + ':</strong> ';
                    reviewHtml += r.isCorrect
                        ? '<span style="color: {{Success}};">✓ Correct</span>'
                        : '<span style="color: {{Danger}};">✗ Incorrect (Correct answer: ' + r.correct + ')</span>';
                    reviewHtml += '</p>';
                    if (r.explanation) {
                        reviewHtml += '<p class="explanation">' + r.explanation + '</p>';
                    }
                    reviewHtml += '</div>';
                });
                document.getElementById('reviewContainer').innerHTML = reviewHtml;
            });

            // Track progress
            document.querySelectorAll('input[type="radio"]').forEach(function(input) {
                input.addEventListener('change', function() {
                    var uniqueAnswered = new Set();
                    document.querySelectorAll('input[type="radio"]:checked').forEach(function(checked) {
                        uniqueAnswered.add(checked.name);
                    });
                    var progress = (uniqueAnswered.size / totalQuestions) * 100;
                    document.getElementById('progressFill').style.width = progress + '%';
                });
            });
        })();
    </script>
</body>
</html>
""";
}
