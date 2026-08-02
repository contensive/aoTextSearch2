
using Contensive.BaseClasses;
using Contensive.DesignBlockBase.Models.View;
using Contensive.Addons.TextSearch.Models.Db;
using System;
using System.Collections.Generic;

namespace Contensive.Addons.TextSearch.Models.View {
    public class TextSearchWidgetViewModel : DesignBlockViewBaseModel {
        //
        public string searchResultsHtml { get; set; }
        public bool hasResults { get; set; }
        public string wordList { get; set; }
        public string actionPage { get; set; }
        public string refreshHiddens { get; set; }
        //
        public static TextSearchWidgetViewModel create(CPBaseClass cp, TextSearchWidgetModel settings) {
            try {
                var result = create<TextSearchWidgetViewModel>(cp, settings);
                //
                string instanceId = cp.Doc.GetText("instanceid");
                bool allowTopicChoices = settings.includeTopicOptions;
                bool allowSectionChoices = settings.includeSectionOptions;
                int pageId = settings.searchResultsPageId;
                bool includeImage = settings.includeImageInResults;
                bool includeDescription = settings.includeSampleText;
                //
                bool sectionAll = cp.Doc.GetBoolean("tsSectionAll");
                string sectionIdList = "";
                if (!sectionAll) {
                    sectionIdList = cp.Doc.GetText("tsSectionIdList").Replace(" ", "");
                    if (sectionIdList == "") { sectionAll = true; }
                }
                bool topicAll = cp.Doc.GetBoolean("tsTopicAll");
                string topicIdList = "";
                if (!topicAll) {
                    topicIdList = cp.Doc.GetText("tsTopicIdList").Replace(" ", "");
                    if (topicIdList == "") { topicAll = true; }
                }
                //
                // -- determine search results page URL
                string searchResultsURL = "";
                if (pageId != 0) {
                    try {
                        searchResultsURL = cp.Content.GetPageLink(pageId, "");
                    } catch {
                        using (var cs = cp.CSNew()) {
                            if (cs.Open("Content Watch", $"recordid={pageId}")) {
                                searchResultsURL = cs.GetText("Link");
                                searchResultsURL = cp.Utils.ModifyLinkQueryString(searchResultsURL, "TextSearchWordList", "", false);
                                searchResultsURL = cp.Utils.ModifyLinkQueryString(searchResultsURL, "button", "", false);
                            }
                        }
                        if (searchResultsURL == "") {
                            searchResultsURL = $"/{cp.Site.GetText("SERVERPAGEDEFAULT")}?bid={pageId}";
                        }
                    }
                }
                //
                // -- process search
                string wordList = cp.Doc.GetText("TextSearchWordList");
                result.wordList = wordList;
                string searchResults = "";
                if (wordList != "") {
                    if (SearchHelper.IsBot(cp)) {
                        searchResults = "<p>Your search request could not be completed. Please try again later.</p>";
                    } else if (SearchHelper.IsRateLimited(cp)) {
                        searchResults = "<p>Too many searches. Please wait a moment and try again.</p>";
                    } else {
                        int pageSize = cp.Doc.GetText("PageSize") == "" ? 10 : cp.Doc.GetInteger("PageSize");
                        int pageNumber = cp.Doc.GetText("PageNumber") == "" ? 1 : cp.Doc.GetInteger("PageNumber");
                        string blockedSectionIdList = "";
                        searchResults = SearchHelper.GetTextSearch(cp, wordList, pageSize, pageNumber, includeImage, includeDescription, sectionAll, sectionIdList, topicAll, topicIdList, instanceId, ref blockedSectionIdList);
                    }
                    if (string.IsNullOrEmpty(searchResults)) {
                        searchResults = $"\r\n<div class=\"searchResultsCon\">\r\n<div class=\"summary\">Your search <i>{wordList}</i> returned no results.</div>\r\n</div>";
                    }
                }
                result.searchResultsHtml = searchResults;
                result.hasResults = !string.IsNullOrEmpty(searchResults);
                //
                // -- build form action and hidden fields
                string actionQS = "";
                if (searchResultsURL != "") {
                    string[] actionParts = searchResultsURL.Split('?');
                    result.actionPage = actionParts[0];
                    if (actionParts.Length > 1) { actionQS = actionParts[1]; }
                } else {
                    result.actionPage = "";
                    actionQS = cp.Doc.RefreshQueryString;
                }
                string refreshHiddens = "";
                if (actionQS != "") {
                    string[] qsParts = actionQS.Split('&');
                    for (int ptr = 0; ptr < qsParts.Length; ptr++) {
                        string[] qsNameValues = qsParts[ptr].Split('=');
                        if (qsNameValues.Length > 1) {
                            string qsName = cp.Utils.DecodeResponseVariable(qsNameValues[0]);
                            switch (qsName.ToLower()) {
                                case "textsearchwordlist":
                                case "button":
                                case "pagenumber":
                                case "pagesize":
                                    break;
                                default:
                                    string qsValue = cp.Utils.DecodeResponseVariable(qsNameValues[1]);
                                    refreshHiddens += $"\r\n<input type=\"hidden\" name=\"{System.Net.WebUtility.HtmlEncode(qsName)}\" value=\"{System.Net.WebUtility.HtmlEncode(qsValue)}\">";
                                    break;
                            }
                        }
                    }
                }
                result.refreshHiddens = refreshHiddens;
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "Error in TextSearchWidgetViewModel.create");
                return new TextSearchWidgetViewModel();
            }
        }
    }
}
