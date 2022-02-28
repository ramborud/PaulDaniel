var reports = window.reports;
var datasets = window.datasets;
var embedToken = window.embedToken;
var models = window['powerbi-client'].models;

// Generate nav links for reports and datasets
$(function () {
    var reportsList = $("#reports-list");
    var datasetsList = $("#datasets-list");

    if (reports.length == 0) {
        reportsList.append($("<li>").text("[None]"));
    }
    else {
        reports.forEach((report) => {
            var li = $("<li>");
            li.append($("<a>", {
                "href": "javascript:void(0);"
            }).text(report.Name).click(() => { embedReport(report) }));
            reportsList.append(li);
        });
    }

    if (datasets.length == 0) {
        datasetsList.append($("<li>").text("[None]"));
    }
    else {
        datasets.forEach((dataset) => {
            var li = $("<li>");
            li.append($("<a>", {
                "href": "javascript:void(0);"
            }).text(dataset.Name).click(() => { embedQnaDataset(dataset) }));
            datasetsList.append(li);
        });
    }
});

// Embed a report
var embedReport = (report, editMode) => {

    // Create the report embed config object
    var config = {
        type: 'report',
        id: report.Id,
        embedUrl: report.EmbedUrl,
        accessToken: embedToken,
        tokenType: models.TokenType.Embed,
        permissions: models.Permissions.All,
        viewMode: editMode ? models.ViewMode.Edit : models.ViewMode.View,
        settings: {
            panes: {
                filters: { visible: false },
                pageNavigation: { visible: false }
            }
        }
    };

    // Get a reference to the embed container
    var embedContainer = document.getElementById('embed-container');

    // Embed the report
    var embeddedReport = powerbi.embed(embedContainer, config);
}

// Embed the Q&A experience
var embedQnaDataset = (dataset) => {

    // Create the Q&A embed config object
    var config = {
        type: 'qna',
        tokenType: models.TokenType.Embed,
        accessToken: embedToken,
        embedUrl: dataset.EmbedUrl,
        datasetIds: [dataset.Id],
        viewMode: models.QnaMode.Interactive
    };

    // Get a reference to the embed container
    var embedContainer = document.getElementById('embed-container');

    // Embed the Q&A experience
    var embeddedObject = powerbi.embed(embedContainer, config);
}