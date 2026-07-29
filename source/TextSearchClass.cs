
using System;
using Contensive.BaseClasses;
using Contensive.DesignBlockBase.Controllers;
using Contensive.Addons.TextSearch.Models.Db;
using Contensive.Addons.TextSearch.Models.View;

namespace Contensive.Addons.TextSearch {
    //
    // ====================================================================================================
    /// <summary>
    /// Text Search 2 addon - design block pattern
    /// </summary>
    public class SearchFormClass : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                return DesignBlockController.renderWidget<TextSearchWidgetModel, TextSearchWidgetViewModel>(cp,
                    widgetName: "Text Search Widget",
                    layoutGuid: Constants.guidLayoutTextSearch,
                    layoutName: Constants.nameLayoutTextSearch,
                    layoutPathFilename: Constants.pathFilenameLayoutTextSearch,
                    layoutBS5PathFilename: Constants.pathFilenameLayoutTextSearch);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "Error in aoTextSearch2.SearchFormClass.Execute");
                throw;
            }
        }
    }
    //
    // ====================================================================================================
    /// <summary>
    /// Static helper methods for text search operations
    /// </summary>
    public static class SearchHelper {
        //
        // ========================================================================
        /// <summary>
        /// Generate search results HTML with pagination and keyword highlighting
        /// </summary>
        public static string GetTextSearch(CPBaseClass cp, string KeyWordList, int PageSize, int InitialPageNumber, bool IncludeImage, bool IncludeDescription, bool sectionAll, string sectionIdList, bool topicAll, string topicIdList, string instanceId, ref string blockedSectionIdList) {
            string result = "";
            try {
                const short OverviewLength = 500;
                string iKeyWordList = KeyWordList;
                int DataRowCount = 0;
                int PageNumber = 0;
                string resultLineDetails = "";
                string ResultsLine = "";
                int iPageSize = 0;
                if (iKeyWordList == "") {
                    result = "<P>You must enter a key word to perform a search.</P>";
                } else {
                    iPageSize = cp.Utils.EncodeInteger(PageSize);
                    if (iPageSize <= 0) {
                        iPageSize = 20;
                    }
                    if (cp.Doc.GetText("PageNumber") != "") {
                        PageNumber = cp.Doc.GetInteger("PageNumber");
                    } else {
                        PageNumber = cp.Utils.EncodeInteger(InitialPageNumber);
                    }
                    if (PageNumber <= 0) {
                        PageNumber = 1;
                    }
                    resultLineDetails = $"<i>{KeyWordList}</i>";
                    using (CPCSBaseClass CSSearch = OpenCSTextSearch(cp, KeyWordList, cp.Visit.Id, iPageSize, PageNumber, sectionAll, sectionIdList, topicAll, topicIdList, instanceId, ref blockedSectionIdList)) {
                        if (CSSearch.OK()) {
                            string[] KeywordSplit = iKeyWordList.Split(' ');
                            string Keyword = "";
                            int Index;
                            for (Index = 0; Index < KeywordSplit.Length; Index++) {
                                Keyword = KeywordSplit[Index].Trim().ToLower();
                                if ((Keyword.Length > 2) && Keyword.StartsWith("\"") && Keyword.EndsWith("\"")) {
                                    Keyword = Keyword.Substring(1, Keyword.Length - 2);
                                }
                                KeywordSplit[Index] = Keyword;
                            }
                            int RowPointer = 0;
                            int RowBAse = (iPageSize * (PageNumber - 1));
                            string sql = CSSearch.GetSQL();
                            DataRowCount = TotalRowsFromFirstPage(cp, sql, PageSize, PageNumber);
                            int RowLast = RowBAse + PageSize;
                            if (RowLast > DataRowCount) {
                                RowLast = DataRowCount;
                            }
                            ResultsLine = $"Results {1 + RowBAse} to {RowLast} of {DataRowCount} for {resultLineDetails}";
                            while (CSSearch.OK() && (RowPointer < iPageSize)) {
                                string PrimaryImageLink = CSSearch.GetText("PrimaryImageLink");
                                int PrimaryImageWidth = CSSearch.GetInteger("PrimaryImageWidth");
                                int PrimaryImageHeight = CSSearch.GetInteger("PrimaryImageHeight");
                                int Length = CSSearch.GetInteger("Length");
                                string Body = "";
                                if (IncludeDescription) {
                                    string BodyText = CSSearch.GetText("BodyText");
                                    BodyText = System.Net.WebUtility.HtmlEncode(BodyText);
                                    string PostBodyText = "";
                                    string PreBodyText = "";
                                    if (BodyText.Length > OverviewLength) {
                                        int MinPosition = -1;
                                        int MaxPosition = -1;
                                        for (Index = 0; Index < KeywordSplit.Length; Index++) {
                                            int Position = BodyText.IndexOf(KeywordSplit[Index], StringComparison.OrdinalIgnoreCase);
                                            if ((Position >= 0) && ((MinPosition < 0) || (Position < MinPosition))) {
                                                MinPosition = Position;
                                            }
                                            Position = BodyText.LastIndexOf(KeywordSplit[Index], StringComparison.OrdinalIgnoreCase);
                                            if ((Position >= 0) && ((MaxPosition < 0) || (Position > MaxPosition))) {
                                                MaxPosition = Position;
                                            }
                                        }
                                        int SpanLength = (MinPosition >= 0 && MaxPosition >= 0) ? MaxPosition - MinPosition : 0;
                                        PreBodyText = "";
                                        PostBodyText = "";
                                        int StartPosition;
                                        if (MinPosition < 0) {
                                            StartPosition = 0;
                                            if (BodyText.Length > OverviewLength) {
                                                PostBodyText = "...";
                                            }
                                            BodyText = BodyText.Substring(StartPosition, OverviewLength);
                                        } else if ((SpanLength == 0) || (SpanLength > OverviewLength)) {
                                            StartPosition = Convert.ToInt32(MinPosition - (OverviewLength / 2.0));
                                            if (StartPosition < 0) {
                                                StartPosition = 0;
                                                if (BodyText.Length > OverviewLength) {
                                                    PostBodyText = "...";
                                                }
                                                BodyText = BodyText.Substring(StartPosition, OverviewLength);
                                            } else if ((StartPosition + OverviewLength) > BodyText.Length) {
                                                StartPosition = BodyText.Length - OverviewLength;
                                                BodyText = BodyText.Substring(StartPosition);
                                                PreBodyText = "...";
                                            } else {
                                                BodyText = BodyText.Substring(StartPosition, OverviewLength);
                                                PreBodyText = "...";
                                                PostBodyText = "...";
                                            }
                                        } else {
                                            StartPosition = Convert.ToInt32(MinPosition - ((OverviewLength - SpanLength) / 2.0));
                                            if (StartPosition < 0) {
                                                StartPosition = 0;
                                                if (BodyText.Length > OverviewLength) {
                                                    PostBodyText = "...";
                                                }
                                                BodyText = BodyText.Substring(StartPosition, OverviewLength);
                                            } else if ((StartPosition + OverviewLength) > BodyText.Length) {
                                                StartPosition = BodyText.Length - OverviewLength;
                                                BodyText = BodyText.Substring(StartPosition);
                                                PreBodyText = "...";
                                            } else {
                                                BodyText = BodyText.Substring(StartPosition, OverviewLength);
                                                PreBodyText = "...";
                                                PostBodyText = "...";
                                            }
                                        }
                                    }
                                    for (Index = 0; Index < KeywordSplit.Length; Index++) {
                                        Keyword = KeywordSplit[Index];
                                        int KeyWordLength = Keyword.Length;
                                        if (Keyword != "") {
                                            int KeywordPosition = BodyText.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase);
                                            int LoopCount = 0;
                                            while (KeywordPosition >= 0 && (LoopCount < 100)) {
                                                if (KeywordPosition == 0) {
                                                    BodyText = $"<b>{BodyText.Substring(KeywordPosition, KeyWordLength)}</b>{BodyText.Substring(KeywordPosition + KeyWordLength)}";
                                                } else {
                                                    BodyText = $"{BodyText.Substring(0, KeywordPosition)}<b>{BodyText.Substring(KeywordPosition, KeyWordLength)}</b>{BodyText.Substring(KeywordPosition + KeyWordLength)}";
                                                }
                                                int searchFrom = KeywordPosition + KeyWordLength + 7;
                                                KeywordPosition = (searchFrom < BodyText.Length) ? BodyText.IndexOf(Keyword, searchFrom, StringComparison.OrdinalIgnoreCase) : -1;
                                                LoopCount += 1;
                                            }
                                        }
                                    }
                                    Body = PreBodyText + BodyText + PostBodyText;
                                }
                                string LinkLabel = CSSearch.GetText("LinkLabel");
                                string Link = CSSearch.GetText("Link");
                                string ATag = $"<a href=\"{Link}\">";
                                string Caption = $"{ATag}{LinkLabel}</a>";
                                if (IncludeImage && (PrimaryImageLink != "")) {
                                    if (PrimaryImageWidth == 0 && PrimaryImageHeight == 0) {
                                        Body = $"{ATag}<img src=\"{PrimaryImageLink}\" width=\"100\" border=0 class=\"image\"></a>{Body}";
                                    } else if (PrimaryImageWidth > PrimaryImageHeight) {
                                        Body = $"{ATag}<img src=\"{PrimaryImageLink}\" width=\"100\" border=0 class=\"image\"></a>{Body}";
                                    } else {
                                        Body = $"{ATag}<img src=\"{PrimaryImageLink}\" height=\"100\" border=0 class=\"image\"></a>{Body}";
                                    }
                                }
                                string Detail = "";
                                if (Length <= 0) {
                                    Detail = $"{ATag}{Link}</a>";
                                } else if (Length < 1024) {
                                    Detail = $"{ATag}{Link}</a> {Length} characters";
                                } else {
                                    Detail = $"{ATag}{Link}</a> {cp.Utils.EncodeInteger(Length / 1024.0)}K characters";
                                }
                                result += $"\r\n\t<div class=\"listItemCon\">\r\n\t\t<div class=\"caption\">{Caption}</div>";
                                if (IncludeDescription || IncludeImage) {
                                    result += $"\r\n\t\t<div class=\"body\">{Body}</div>";
                                }
                                result += $"\r\n\t\t<div class=\"detail\">{Detail}</div>\r\n\t</div>";
                                CSSearch.GoNext();
                                RowPointer += 1;
                            }
                        }
                    }
                }
                if (DataRowCount == 0) {
                    ResultsLine = $"Your search {resultLineDetails} returned no results.";
                } else {
                    cp.Doc.AddRefreshQueryString("TextSearchWordList", KeyWordList);
                    cp.Doc.AddRefreshQueryString("PageNumber", PageNumber.ToString());
                    cp.Doc.AddRefreshQueryString("PageSize", iPageSize.ToString());
                    cp.Doc.AddRefreshQueryString("Button", "Search");
                    string QS = cp.Doc.RefreshQueryString;
                    double PageCountDbl = ((double)DataRowCount / PageSize);
                    int PageCount = cp.Utils.EncodeInteger(PageCountDbl);
                    if (PageCountDbl != PageCount) {
                        PageCount += 1;
                    }
                    const short PageLinkCnt = 20;
                    int PageLinkStart = 0;
                    int PageLinkEnd = 0;
                    if (PageCount < PageLinkCnt) {
                        PageLinkStart = 1;
                        PageLinkEnd = PageCount;
                    } else {
                        int PageMiddle = Convert.ToInt32(PageCount / 2.0);
                        if (PageNumber <= PageMiddle) {
                            PageLinkStart = 1;
                            PageLinkEnd = PageCount;
                        } else {
                            PageLinkStart = Convert.ToInt32(PageNumber - (PageMiddle / 2.0));
                            PageLinkEnd = Convert.ToInt32(PageNumber + (PageMiddle / 2.0));
                        }
                    }
                    string Footer = "";
                    if (PageLinkStart != 1) {
                        Footer += $"\r\n<li><div><a href=\"?{QS}&pagenumber=1\">1</a></div></li>";
                    }
                    int Ptr;
                    for (Ptr = PageLinkStart; Ptr <= PageLinkEnd; Ptr++) {
                        if (Ptr == PageNumber) {
                            Footer += $"\r\n\t\t<li><div>{Ptr}</div></li>";
                        } else {
                            QS = cp.Utils.ModifyQueryString(QS, "pagenumber", Convert.ToString(Ptr), true);
                            Footer += $"\r\n\t\t<li><div><a href=\"?{QS}\">{Ptr}</a></div></li>";
                        }
                    }
                    if (Ptr < PageCount) {
                        QS = cp.Utils.ModifyQueryString(QS, "pagenumber", Convert.ToString(PageCount), true);
                        Footer += $"\r\n\t\t<li><div><a href=\"?{QS}\">{PageCount}</a></div></li>";
                    }
                    result += $"\r\n<div class=\"pageLinkCon\">\r\n\t<ul>{Footer}\r\n\t</ul>\r\n</div>";
                }
                result = $"\r\n<div class=\"searchResultsCon\">\r\n<div class=\"summary\">{ResultsLine}</div>{result}\r\n</div>";
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return result;
            }
        }
        //
        // ========================================================================
        /// <summary>
        /// Open a content set with the results of a search on ccSpiderDocs.
        /// Supports LIKE queries (default) or SQL Server Full-Text CONTAINS queries
        /// when the "Text Search Use Full Text" site property is enabled.
        /// </summary>
        public static CPCSBaseClass OpenCSTextSearch(CPBaseClass cp, string KeyWordList, int VisitID, int PageSize, int PageNumber, bool sectionAll, string sectionIdList, bool topicAll, string topicIdList, string instanceId, ref string blockedSectionIdList) {
            var csResult = cp.CSNew();
            try {
                if (KeyWordList.Trim() != "") {
                    bool useFullText = cp.Site.GetBoolean(Constants.sitePropertyUseFullTextSearch);
                    string[] KeywordSplit = KeyWordList.Split(' ');
                    int WordCount = KeywordSplit.Length;
                    string WordSearchExcludeList = GetWordSearchExcludeList(cp);
                    string sqlFrom = "ccSpiderDocs d";
                    string sqlWhere = "(d.Active<>0)and((d.duplicatecontent<>1)or(d.duplicatecontent is null))";
                    string sqlSubWhere = "";
                    string SearchWord = "";
                    int WordPointer = 0;
                    string sqlNow = cp.Db.EncodeSQLDate(DateTime.Now);
                    string sql = "";
                    //
                    // -- collect search words, log them, and build the search clause
                    var fullTextTerms = new System.Collections.Generic.List<string>();
                    while (WordPointer < WordCount) {
                        SearchWord = KeywordSplit[WordPointer];
                        if ((SearchWord != "") && (WordSearchExcludeList.IndexOf($"\r\n{SearchWord}\r\n", StringComparison.OrdinalIgnoreCase) < 0)) {
                            if (SearchWord.Length > 2) {
                                if (SearchWord.StartsWith("\"") && SearchWord.EndsWith("\"")) {
                                    SearchWord = SearchWord.Substring(1, SearchWord.Length - 2);
                                }
                            }
                            if (useFullText) {
                                //
                                // -- collect terms for CONTAINS predicate
                                fullTextTerms.Add(SearchWord);
                            } else {
                                //
                                // -- legacy LIKE search
                                sqlWhere += $"and(d.BodyText like {cp.Db.EncodeSQLText($"%{SearchWord}%")})";
                            }
                            //
                            // -- log the search keyword
                            int cid = cp.Content.GetID("Search Keywords");
                            sql = $"insert into ccSearchKeywords (dateadded,active,contentcontrolid,name,visitid)values({sqlNow},1,{cid},{cp.Db.EncodeSQLText(SearchWord)},{cp.Visit.Id})";
                            cp.Db.ExecuteNonQuery(sql);
                        }
                        WordPointer += 1;
                    }
                    //
                    // -- build CONTAINS clause if full-text search is enabled
                    if (useFullText && fullTextTerms.Count > 0) {
                        var containsTerms = new System.Text.StringBuilder();
                        for (int i = 0; i < fullTextTerms.Count; i++) {
                            if (i > 0) {
                                containsTerms.Append(" AND ");
                            }
                            // -- quote each term for the CONTAINS predicate
                            string term = fullTextTerms[i].Replace("\"", "");
                            containsTerms.Append($"\"{term}\"");
                        }
                        sqlWhere += $"and CONTAINS(d.BodyText, {cp.Db.EncodeSQLText(containsTerms.ToString())})";
                    }
                    //
                    string[] RecordIDArray = null;
                    int RecordIDArrayCount;
                    int ArrayPointer;
                    int recordId;
                    //
                    // ----- topics
                    if (!topicAll) {
                        sqlFrom = $"(({sqlFrom} LEFT JOIN ccPageContent p ON d.pageid = p.id) LEFT JOIN ccPageContentTopicRules pt ON pt.pageid=p.id)";
                        sqlSubWhere = "";
                        RecordIDArray = topicIdList.Split(',');
                        RecordIDArrayCount = RecordIDArray.Length;
                        if (RecordIDArrayCount > 0) {
                            for (ArrayPointer = 0; ArrayPointer < RecordIDArrayCount; ArrayPointer++) {
                                recordId = cp.Utils.EncodeInteger(RecordIDArray[ArrayPointer]);
                                if (recordId != 0) {
                                    if (sqlSubWhere != "") {
                                        sqlSubWhere += "OR";
                                    }
                                    sqlSubWhere += $"(pt.TopicID={recordId})";
                                }
                            }
                            if (sqlSubWhere != "") {
                                sqlWhere += $"and({sqlSubWhere})";
                            }
                        }
                    }
                    if (blockedSectionIdList != "") {
                        blockedSectionIdList = blockedSectionIdList.TrimStart(',').TrimEnd(',');
                        if (!blockedSectionIdList.Contains(",")) {
                            sqlWhere += $"and (d.sectionid<>{blockedSectionIdList})";
                        } else {
                            sqlWhere += $"and not (d.sectionid in ({blockedSectionIdList}))";
                        }
                    }
                    //
                    // ----- sections to include
                    if (!sectionAll) {
                        sqlSubWhere = "";
                        RecordIDArray = sectionIdList.Split(',');
                        RecordIDArrayCount = RecordIDArray.Length;
                        if (RecordIDArrayCount > 0) {
                            for (ArrayPointer = 0; ArrayPointer < RecordIDArrayCount; ArrayPointer++) {
                                recordId = cp.Utils.EncodeInteger(RecordIDArray[ArrayPointer]);
                                if (recordId != 0) {
                                    if (sqlSubWhere != "") {
                                        sqlSubWhere += "OR";
                                    }
                                    sqlSubWhere += $"(d.sectionid={recordId})";
                                }
                            }
                            if (sqlSubWhere != "") {
                                sqlWhere += $"and({sqlSubWhere})";
                            }
                        }
                    }
                    //
                    // -- build and execute the final query
                    sql = $"SELECT d.ID AS ID, d.Name AS LinkLabel, d.Link AS Link, d.TextOnlyFilename as BriefFilename, d.dateLastModified as dateLastModified, d.MetaKeywords as MetaKeywords, d.MetaDescription as MetaDescription, d.Length as Length, d.BodyText as BodyText, d.PrimaryImageLink as PrimaryImageLink, d.PrimaryImageWidth as PrimaryImageWidth, d.PrimaryImageHeight as PrimaryImageHeight FROM {sqlFrom} WHERE {sqlWhere} ORDER BY d.dateadded desc, d.dateLastModified Desc;";
                    csResult.OpenSQL(sql, "", PageSize, PageNumber);
                }
                return csResult;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return csResult;
        }
        //
        // ----- Check if the current visit has exceeded the search rate limit (5 searches per 60 seconds)
        //
        public static bool IsRateLimited(CPBaseClass cp) {
            try {
                string sqlDate = cp.Db.EncodeSQLDate(DateTime.Now.AddSeconds(-60));
                string sql = $"SELECT COUNT(*) AS cnt FROM ccSearchKeywords WHERE visitid={cp.Visit.Id} AND dateadded>{sqlDate}";
                System.Data.DataTable dt = cp.Db.ExecuteQuery(sql);
                if (dt.Rows.Count > 0) {
                    return Convert.ToInt32(dt.Rows[0]["cnt"]) >= 5;
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "Error checking search rate limit");
            }
            return false;
        }
        //
        // ----- Check if the current visit is flagged as a bot by reCAPTCHA V3
        //
        public static bool IsBot(CPBaseClass cp) {
            try {
                string siteKey = cp.Site.GetText("ReCaptcha V3 Site Key");
                if (string.IsNullOrEmpty(siteKey)) {
                    return false;
                }
                return cp.Visit.GetBoolean("recaptchav3 is bot");
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "Error checking reCAPTCHA bot status");
            }
            return false;
        }
        //
        // ----- Load the Word Search Exclude List
        //
        public static string GetWordSearchExcludeList(CPBaseClass cp) {
            return cp.CdnFiles.Read("WordSearchExcludeList.txt") + "\r\n";
        }
        //
        // ----- Get total row count from a query (limited to 10,000)
        //
        public static int TotalRowsFromFirstPage(CPBaseClass cp, string sql, int pageSize, int pageNumber) {
            int rowCount = 0;
            try {
                System.Data.DataTable dt = cp.Db.ExecuteQuery(sql, 1, 10000);
                rowCount = dt.Rows.Count;
                return rowCount;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return rowCount;
            }
        }
    }
}
