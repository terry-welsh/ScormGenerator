namespace ScormGen.Core.Templates;

public static class HtmlTemplates
{
    // =========================================================================
    // SCORM API wrapper JS and base CSS are loaded from embedded resources.
    // Edit Templates/Resources/scorm-api.js and base-styles.css directly.
    // =========================================================================

    public static string ScormApiJs => TemplateResources.ScormApiJs;
    private static string BaseStyles => TemplateResources.BaseStyles;

    // =========================================================================
    // Encoding helpers
    // =========================================================================

    private static string H(string text) => System.Net.WebUtility.HtmlEncode(text);
    private static string X(string text) => System.Security.SecurityElement.Escape(text) ?? string.Empty;

    // =========================================================================
    // imsmanifest.xml
    // =========================================================================

    public static string GetManifest(string identifier, string title, string contentType, double passingScore)
    {
        var sequencing = GetSequencingRules(contentType, passingScore);
        var sid = X(identifier);
        var st = X(title);
        return $$"""
<?xml version="1.0" encoding="UTF-8"?>
<manifest identifier="{{sid}}" version="1.0"
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

    <organizations default="ORG-{{sid}}">
        <organization identifier="ORG-{{sid}}" structure="hierarchical">
            <title>{{st}}</title>
            <item identifier="ITEM-{{sid}}" identifierref="RES-{{sid}}">
                <title>{{st}}</title>
                {{sequencing}}
            </item>
        </organization>
    </organizations>

    <resources>
        <resource identifier="RES-{{sid}}" type="webcontent" adlcp:scormType="sco" href="index.html">
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

    public static string GetInformationalHtml(string title, string contentHtml, string? apiJs = null)
    {
        var js = apiJs ?? ScormApiJs;
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{H(title)}}</title>
    {{BaseStyles}}
    <script>{{js}}</script>
</head>
<body>
    <main class="content-wrapper">
        <h1>{{H(title)}}</h1>
        <div class="progress-bar" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="0" aria-label="Course progress" id="progressBar">
            <div class="progress-fill" id="progressFill" style="width: 0%"></div>
        </div>
        <div id="content">
            {{contentHtml}}
        </div>
        <div class="completion-marker" id="completionMarker" role="status" aria-live="polite">
            ✓ Content Completed
        </div>
    </main>

    <script>
        (function() {
            var completed = false;
            var scrollThreshold = 0.9;

            function updateProgress() {
                var scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                var scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
                var progress = scrollHeight > 0 ? (scrollTop / scrollHeight) * 100 : 100;

                var pct = Math.min(progress, 100);
                document.getElementById('progressFill').style.width = pct + '%';
                document.getElementById('progressBar').setAttribute('aria-valuenow', Math.round(pct));

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
    }

    public static string GetScenarioHtml(
        string title,
        string situation,
        string optionsHtml,
        string analysisHtml,
        string additionalHtml,
        string correctOption,
        string? apiJs = null)
    {
        var js = apiJs ?? ScormApiJs;
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{H(title)}}</title>
    {{BaseStyles}}
    <script>{{js}}</script>
</head>
<body>
    <main class="content-wrapper">
        <h1>{{H(title)}}</h1>

        <div class="scenario-situation">
            <h3>Situation</h3>
            {{situation}}
        </div>

        <div class="scenario-options" id="optionsContainer" role="group" aria-labelledby="options-heading">
            <h3 id="options-heading">What would you do?</h3>
            {{optionsHtml}}
        </div>

        <div class="analysis-section" id="analysisSection" aria-live="polite" tabindex="-1">
            <h3>Analysis</h3>
            <div id="analysisContent">{{analysisHtml}}</div>
            {{additionalHtml}}
        </div>
    </main>

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
                    opt.disabled = true;
                });

                // Show analysis and move focus for screen readers
                var analysis = document.getElementById('analysisSection');
                analysis.style.display = 'block';
                analysis.scrollIntoView({ behavior: 'smooth' });
                analysis.focus();

                // Mark complete (ungraded - no score)
                SCORM.setCompletionStatus('completed');
                SCORM.commit();
            };
        })();
    </script>
</body>
</html>
""";
    }

    public static string GetUngradedQuizHtml(string title, string questionsHtml, int questionCount, string? apiJs = null)
    {
        var js = apiJs ?? ScormApiJs;
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{H(title)}}</title>
    {{BaseStyles}}
    <script>{{js}}</script>
</head>
<body>
    <main class="content-wrapper">
        <h1>{{H(title)}}</h1>
        <p>This is a knowledge check to help reinforce your learning. Your answers are not scored.</p>

        <div class="progress-bar" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="0" aria-label="Questions answered" id="progressBar">
            <div class="progress-fill" id="progressFill" style="width: 0%"></div>
        </div>

        <div id="questionsContainer">
            {{questionsHtml}}
        </div>

        <div class="completion-marker" id="completionMarker" role="status" aria-live="polite">
            ✓ Knowledge Check Completed
        </div>
    </main>

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
                var pct = (answeredQuestions.size / totalQuestions) * 100;
                document.getElementById('progressFill').style.width = pct + '%';
                document.getElementById('progressBar').setAttribute('aria-valuenow', Math.round(pct));

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
    }

    public static string GetGradedQuizHtml(
        string title,
        string questionsHtml,
        int questionCount,
        double passingScore,
        int passingPercent,
        int minCorrect,
        string correctAnswersJson,
        string explanationsJson,
        string? apiJs = null)
    {
        var js = apiJs ?? ScormApiJs;
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{H(title)}}</title>
    {{BaseStyles}}
    <script>{{js}}</script>
</head>
<body>
    <main class="content-wrapper">
        <h1>{{H(title)}}</h1>
        <p><strong>Passing Score:</strong> {{passingPercent}}% ({{minCorrect}}/{{questionCount}} correct answers required)</p>

        <div class="progress-bar" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow="0" aria-label="Questions answered" id="progressBar">
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

        <div class="results-container" id="resultsContainer" style="display: none;" role="status" aria-live="polite" tabindex="-1">
            <h2>Assessment Results</h2>
            <div class="score-display" id="scoreDisplay"></div>
            <p class="results-message" id="resultsMessage"></p>
            <div id="reviewContainer"></div>
        </div>
    </main>

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

                // Display results and move focus for screen readers
                document.getElementById('quizForm').style.display = 'none';
                var results = document.getElementById('resultsContainer');
                results.style.display = 'block';
                results.focus();

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
                        ? '<span style="color: #6ca437;">✓ Correct</span>'
                        : '<span style="color: #da7552;">✗ Incorrect (Correct answer: ' + r.correct + ')</span>';
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
                    var pct = (uniqueAnswered.size / totalQuestions) * 100;
                    document.getElementById('progressFill').style.width = pct + '%';
                    document.getElementById('progressBar').setAttribute('aria-valuenow', Math.round(pct));
                });
            });
        })();
    </script>
</body>
</html>
""";
    }
}
