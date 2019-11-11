
using System;
using Contensive.BaseClasses;
using Microsoft.VisualBasic;

namespace Contensive.Addons.TextSearch {
    public class SearchFormClass : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            string GetContent;
            try {   // On Error GoTo ErrorTrap
                string instanceId = cp.Doc.GetText("instanceid");
                bool allowTopicChoices = (cp.Doc.GetBoolean("include Topic Options"));
                bool allowSectionChoices = (cp.Doc.GetBoolean("include Section Options"));
                int PageID = (cp.Doc.GetInteger("Search Results Page"));
                bool IncludeSearchResults = (cp.Doc.GetBoolean("include Search Results"));
                bool IncludeImage = (cp.Doc.GetBoolean("Include Image in Results"));
                bool IncludeDescription = (cp.Doc.GetBoolean("Include Sample Text in Results"));
                // 
                bool sectionAll = cp.Doc.GetBoolean("tsSectionAll");
                string sectionIdList = "";
                if (!sectionAll) {
                    sectionIdList = cp.Doc.GetText("tsSectionIdList");
                    sectionIdList = sectionIdList.Replace(" ", "");
                    if (sectionIdList == "") {
                        sectionAll = true;
                    }
                }

                bool topicAll = (cp.Doc.GetBoolean("tsTopicAll"));
                string topicIdList = "";
                if (!topicAll) {
                    topicIdList = cp.Doc.GetText("tsTopicIdList");
                    topicIdList = topicIdList.Replace(" ", "");
                    if (topicIdList == "") {
                        topicAll = true;
                    }
                }
                string SearchResultsURL = "";
                // 
                if (PageID != 0) {
                    /*? On Error Resume Next  */
                    SearchResultsURL = cp.Content.GetPageLink(PageID, "", false);
                    if (Information.Err().Number != 0) {
                        Information.Err().Clear();
                        //using ( var cs = cp.CSNew() ) {
                        //    if ( cs.Open()) {

                        //    }
                        //}
                        using (var cs = cp.CSNew()) {
                            if (cs.Open("Content Watch", "recordid=" + Convert.ToString(PageID))) {
                                SearchResultsURL = cs.GetText("Link");
                                SearchResultsURL = cp.Utils.ModifyLinkQueryString(SearchResultsURL, "TextSearchWordList", "", false);
                                SearchResultsURL = cp.Utils.ModifyLinkQueryString(SearchResultsURL, "button", "", false);
                            }
                        }
                        if (SearchResultsURL == "") {
                            SearchResultsURL = "/" + cp.Site.GetText("SERVERPAGEDEFAULT") + "?bid=" + Convert.ToString(PageID);
                        }
                    }
                }
                string searchResults = "";
                string WordList = "";
                // 
                // Search Results
                // 
                if (IncludeSearchResults) {
                    // 
                    WordList = cp.Doc.GetText("TextSearchWordList");
                    string Button = cp.Doc.GetText("Button");
                    if (WordList != "") {
                        int PageSize;
                        if (cp.Doc.GetText("PageSize") == "") {
                            PageSize = 10;
                        } else {
                            PageSize = cp.Doc.GetInteger("PageSize");
                        }
                        int PageNumber;
                        if (cp.Doc.GetText("PageNumber") == "") {
                            PageNumber = 1;
                        } else {
                            PageNumber = cp.Doc.GetInteger("PageNumber");
                        }
                        string xblockedSectionIdList = "";
                        searchResults += GetTextSearch(cp, WordList, PageSize, PageNumber, IncludeImage, IncludeDescription, sectionAll, sectionIdList, topicAll, topicIdList, instanceId, ref xblockedSectionIdList);
                    }
                }
                string ActionPage;
                string ActionQS = "";
                // 
                // ----- Build Search Form
                // 
                if (SearchResultsURL != "") {
                    // 
                    // This add-on will never handle it's own posts so ignore RefreshQueryString
                    // and form get to the SearchResultsPage
                    // 
                    string[] ActionParts = Strings.Split(SearchResultsURL, "?", -1, CompareMethod.Binary);
                    ActionPage = ActionParts[0];
                    if (Information.UBound(ActionParts, 1) > 0) {
                        ActionQS = ActionParts[1];
                    }
                } else {
                    // 
                    // This add-on can handle it's own posts, so build page and hiddens from RQS
                    // 
                    ActionPage = "";
                    ActionQS = cp.Doc.RefreshQueryString;
                }
                string RefreshHiddens = "";
                if (ActionQS != "") {
                    string[] QSParts = Strings.Split(ActionQS, "&", -1, CompareMethod.Binary);
                    // 
                    int Ptr;
                    for (Ptr = 0; Ptr <= Information.UBound(QSParts, 1); Ptr++) {
                        string[] QSNameValues = Strings.Split(QSParts[Ptr], "=", -1, CompareMethod.Binary);
                        string QSName = "";
                        if (Information.UBound(QSNameValues, 1) == 0) {
                            QSName = cp.Utils.DecodeResponseVariable(QSNameValues[0]);
                        } else {
                            QSName = cp.Utils.DecodeResponseVariable(QSNameValues[0]);
                            switch (Strings.LCase(QSName)) {

                                case "textsearchwordlist":
                                case "button":
                                case "pagenumber":
                                case "pagesize": {
                                        break;
                                    }
                                default: {
                                        string QSValue = cp.Utils.DecodeResponseVariable(QSNameValues[1]);
                                        RefreshHiddens += "\r\n" + "<input type=\"hidden\" name=\"" + System.Net.WebUtility.HtmlEncode(QSName) + "\" value=\"" + System.Net.WebUtility.HtmlEncode(QSValue) + "\">";
                                        break;
                                    }
                            } //end switch
                        }
                    }
                }
                // 
                string SearchForm = RefreshHiddens + "\r\n" + "\t" + cp.Html.Button("Search") + "&nbsp;" + cp.Html5.InputText("TextSearchWordList", 255, WordList) + "";
                SearchForm = "" + "\r\n" + "\t" + "<form action=\"" + ActionPage + "\" METHOD=\"get\" style=\"display:inline;\" >" + (SearchForm) + "\r\n" + "\t" + "</form>";
                SearchForm = "" + "\r\n" + "\t" + "<div class=\"searchFormCon\">" + (SearchForm) + "\r\n" + "\t" + "</div>" + "";
                GetContent = "" + "\r\n" + "<div class=\"textSearch2Con\">" + searchResults + SearchForm + "\r\n" + "</div>";
                // 
                return GetContent;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "trap error in aoTextSearch2.SearchFormClass.getContent");
                return string.Empty;
            }
        }
        // 
        // =============================================================================
        // 
        // =============================================================================
        // 
        private string GetTextSearch(CPBaseClass cp, string KeyWordList, int PageSize, int InitialPageNumber, bool IncludeImage, bool IncludeDescription, bool sectionAll, string sectionIdList, bool topicAll, string topicIdList, string instanceId, ref string blockedSectionIdList) {
            string GetTextSearch = "";
            try {
                const short OverviewLength = 500;
                string iKeyWordList = KeyWordList;
                int DataRowCount = 0;
                int PageNumber = 0;
                string resultLineDetails = "";
                string ResultsLine = "";
                int iPageSize = 0;
                if (iKeyWordList == "") {
                    // 
                    // ----- key word list is required
                    // 
                    GetTextSearch = "<P>You must enter a key word to perform a search.</P>";
                } else {
                    // 
                    // ----- Get current Page Number
                    // 
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
                    // 
                    // ----- Get the results
                    // 
                    resultLineDetails = "<i>" + KeyWordList + "</i>";
                    using (CPCSBaseClass CSSearch = OpenCSTextSearch(cp, KeyWordList, cp.Visit.Id, iPageSize, PageNumber, sectionAll, sectionIdList, topicAll, topicIdList, instanceId, ref blockedSectionIdList)) {
                        // 
                        // ----- Prep the keywords for text matching
                        // 
                        
                        string[] KeywordSplit = iKeyWordList.Split(' ');
                        string Keyword = "";
                        int Index;
                        for (Index = 0; Index <= Information.UBound(KeywordSplit, 1); Index++) {
                            Keyword = Strings.Trim(Strings.LCase(KeywordSplit[Index]));
                            if ((Keyword.Length > 2) && (Strings.Left(Keyword, 1) == "\"") && (Strings.Right(Keyword, 1) == "\"")) {
                                Keyword = Strings.Mid(Keyword, 2, Keyword.Length - 2);
                            }
                            KeywordSplit[Index] = Keyword;
                        }
                        const short ColumnCount = 1;
                        // 
                        // ----- Display the results
                        // 
                        string[] ColAlign = new string[ColumnCount + 1];
                        string[] ColCaption = new string[ColumnCount + 1];
                        string[] ColWidth = new string[ColumnCount + 1];
                        string[,] Content = new string[iPageSize + 1, ColumnCount + 1];
                        int ColumnPointer;
                        // 
                        for (ColumnPointer = 0; ColumnPointer <= ColumnCount - 1; ColumnPointer++) {
                            ColAlign[ColumnPointer] = "Left";
                            ColCaption[ColumnPointer] = "";
                        }
                        ColWidth[0] = "100%";
                        // 

                        int RowPointer = 0;
                        int RowBAse = (iPageSize * (PageNumber - 1));
                        string sql = CSSearch.GetSQL();
                        DataRowCount = TotalRowsFromFirstPage(cp, sql, PageSize, PageNumber);
                        //DataRowCount = CSSearch.GetRowCount();
                        int RowLast = RowBAse + PageSize;
                        if (RowLast > DataRowCount) {
                            RowLast = DataRowCount;
                        }
                        ResultsLine = "Results " + Convert.ToString(1 + RowBAse) + " to " + Convert.ToString(RowLast) + " of " + Convert.ToString(DataRowCount) + " for " + resultLineDetails;
                        while (CSSearch.OK() && (RowPointer < iPageSize)) {
                            string PrimaryImageLink = CSSearch.GetText("PrimaryImageLink");
                            int PrimaryImageWidth = CSSearch.GetInteger("PrimaryImageWidth");
                            int PrimaryImageHeight = CSSearch.GetInteger("PrimaryImageHeight");
                            int Length = CSSearch.GetInteger("Length");
                            string Body = "";
                            // 
                            if (IncludeDescription) {
                                string BodyText = CSSearch.GetText("BodyText");
                                BodyText = System.Net.WebUtility.HtmlEncode(BodyText);
                                string PostBodyText = "";
                                string PreBodyText = "";
                                // 
                                // trim BodyText down to size
                                // 
                                if (BodyText.Length > OverviewLength) {
                                    int MinPosition = 0;
                                    int MaxPosition = 0;
                                    // 
                                    // Determine Min and Max positions
                                    // 
                                    for (Index = 0; Index <= Information.UBound(KeywordSplit, 1); Index++) {
                                        int Position = Strings.InStr(1, BodyText, KeywordSplit[Index], CompareMethod.Text);
                                        if ((Position != 0) && ((MinPosition == 0) || (Position < MinPosition))) {
                                            MinPosition = Position;
                                        }
                                        Position = Strings.InStrRev(BodyText, KeywordSplit[Index], -1, CompareMethod.Text);
                                        if ((Position != 0) && ((MaxPosition == 0) || (Position > MaxPosition))) {
                                            MaxPosition = Position;
                                        }
                                    }
                                    int SpanLength = MaxPosition - MinPosition;
                                    PreBodyText = "";
                                    PostBodyText = "";
                                    int StartPosition;
                                    if (MinPosition == 0) {
                                        // 
                                        // not found, return first 200 characters
                                        // 
                                        StartPosition = 1;
                                        if (BodyText.Length > OverviewLength) {
                                            PostBodyText = "...";
                                        }
                                        BodyText = Strings.Mid(BodyText, StartPosition, OverviewLength);
                                    } else if ((SpanLength == 0) || (SpanLength > OverviewLength)) {
                                        // 
                                        // only one found or the span is long, return 100 before min, 100 after min
                                        // 
                                        StartPosition = Convert.ToInt32(MinPosition - (OverviewLength / 2.0));
                                        if (StartPosition < 1) {
                                            // 
                                            // Start at the first position of bodytext
                                            // 
                                            StartPosition = 1;
                                            if (BodyText.Length > OverviewLength) {
                                                PostBodyText = "...";
                                            }
                                            BodyText = Strings.Mid(BodyText, StartPosition, OverviewLength);
                                        } else if ((StartPosition + OverviewLength) > BodyText.Length) {
                                            // 
                                            // End the copy at the end of the bodytext
                                            // 
                                            StartPosition = BodyText.Length - OverviewLength + 1;
                                            BodyText = Strings.Mid(BodyText, StartPosition);
                                            PreBodyText = "...";
                                        } else {
                                            BodyText = Strings.Mid(BodyText, StartPosition, OverviewLength);
                                            PreBodyText = "...";
                                            PostBodyText = "...";
                                        }
                                    } else {
                                        // 
                                        // Span is short, return the span, some before, some after
                                        // 
                                        StartPosition = Convert.ToInt32(MinPosition - ((OverviewLength - SpanLength) / 2.0));
                                        if (StartPosition < 1) {
                                            // 
                                            // Start at the first position of bodytext
                                            // 
                                            StartPosition = 1;
                                            if (BodyText.Length > OverviewLength) {
                                                PostBodyText = "...";
                                            }
                                            BodyText = Strings.Mid(BodyText, StartPosition, OverviewLength);
                                        } else if ((StartPosition + OverviewLength) > BodyText.Length) {
                                            // 
                                            // End the copy at the end of the bodytext
                                            // 
                                            StartPosition = BodyText.Length - OverviewLength + 1;
                                            BodyText = Strings.Mid(BodyText, StartPosition);
                                            PreBodyText = "...";
                                        } else {
                                            BodyText = Strings.Mid(BodyText, StartPosition, OverviewLength);
                                            PreBodyText = "...";
                                            PostBodyText = "...";
                                        }
                                    }
                                }
                                // 
                                // bold all occurances of each keywords, keep the min and max position
                                // 
                                for (Index = 0; Index <= Information.UBound(KeywordSplit, 1); Index++) {
                                    Keyword = KeywordSplit[Index];
                                    int KeyWordLength = Keyword.Length;
                                    if (Keyword != "") {
                                        int KeywordPosition = Strings.InStr(1, BodyText, Keyword, CompareMethod.Text);
                                        int LoopCount = 0;
                                        while (KeywordPosition != 0 && (LoopCount < 100)) {
                                            string SourceWord = Strings.Mid(BodyText, KeywordPosition, KeyWordLength);
                                            if (KeywordPosition == 1) {
                                                BodyText = "<b>" + Strings.Mid(BodyText, KeywordPosition, KeyWordLength) + "</b>" + Strings.Mid(BodyText, KeywordPosition + KeyWordLength);
                                            } else {
                                                BodyText = Strings.Mid(BodyText, 1, KeywordPosition - 1) + "<b>" + Strings.Mid(BodyText, KeywordPosition, KeyWordLength) + "</b>" + Strings.Mid(BodyText, KeywordPosition + KeyWordLength);
                                            }
                                            KeywordPosition = Strings.InStr((KeywordPosition + KeyWordLength + 6), BodyText, Keyword, CompareMethod.Text);
                                            LoopCount += 1;
                                        }
                                    }
                                }
                                Body = PreBodyText + BodyText + PostBodyText;
                            }
                            // 
                            string LinkLabel = CSSearch.GetText("LinkLabel");
                            string Link = CSSearch.GetText("Link");
                            string ATag = "<a href=\"" + Link + "\">";
                            // 
                            string Caption = ATag + LinkLabel + "</a>";
                            if (IncludeImage && (PrimaryImageLink != "")) {
                                if (PrimaryImageWidth == 0 && PrimaryImageHeight == 0) {
                                    // 
                                    // No dimensions, set width=100
                                    // 
                                    Body = ATag + "<img src=\"" + PrimaryImageLink + "\" width=\"100\" border=0 class=\"image\"></a>" + Body;
                                } else if (PrimaryImageWidth > PrimaryImageHeight) {
                                    // 
                                    // width > height, set width=100
                                    // 
                                    Body = ATag + "<img src=\"" + PrimaryImageLink + "\" width=\"100\" border=0 class=\"image\"></a>" + Body;
                                } else {
                                    // 
                                    // height > width, set height=100
                                    // 
                                    Body = ATag + "<img src=\"" + PrimaryImageLink + "\" height=\"100\" border=0 class=\"image\"></a>" + Body;
                                }
                            }
                            string Detail = "";
                            if (Length <= 0) {
                                Detail = ATag + Link + "</a>";
                            } else if (Length < 1024) {
                                Detail = ATag + Link + "</a> " + Convert.ToString(Length) + " characters";
                            } else {
                                Detail = ATag + Link + "</a> " + cp.Utils.EncodeInteger(Length / 1024.0) + "K characters";
                            }
                            GetTextSearch = GetTextSearch + "\r\n" + "\t" + "<div class=\"listItemCon\">" + "\r\n" + "\t" + "\t" + "<div class=\"caption\">" + Caption + "</div>";
                            if (IncludeDescription || IncludeImage) {
                                GetTextSearch = GetTextSearch + "\r\n" + "\t" + "\t" + "<div class=\"body\">" + Body + "</div>";
                            }
                            GetTextSearch = GetTextSearch + "\r\n" + "\t" + "\t" + "<div class=\"detail\">" + Detail + "</div>" + "\r\n" + "\t" + "</div>";
                            CSSearch.GoNext();
                            int RecordPointer = 0;
                            RecordPointer += 1;
                            RowPointer += 1;
                        }
                    }
                }

                // 
                if (DataRowCount == 0) {
                    ResultsLine = "Your search " + resultLineDetails + " returned no results.";
                } else {
                    cp.Doc.AddRefreshQueryString("TextSearchWordList", KeyWordList);
                    cp.Doc.AddRefreshQueryString("PageNumber", PageNumber.ToString());
                    cp.Doc.AddRefreshQueryString("PageSize", iPageSize.ToString());
                    cp.Doc.AddRefreshQueryString("Button", "Search");
                    // 
                    // Display Next buttons
                    // 
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
                        // 
                        // [1]...[20]
                        // 
                        PageLinkStart = 1;
                        PageLinkEnd = PageCount;
                    } else {
                        // 
                        // [1],[middle-10]...[middle+20],[PastLast]
                        // 
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
                        Footer += "\r\n" + "<li><div><a href=\"?" + QS + "&pagenumber=1\">1</a></div></li>";
                    }
                    int Ptr;
                    for (Ptr = PageLinkStart; Ptr <= PageLinkEnd; Ptr++) {
                        if (Ptr == PageNumber) {
                            Footer += "\r\n" + "\t" + "\t" + "<li><div>" + Convert.ToString(Ptr) + "</div></li>";
                        } else {
                            QS = cp.Utils.ModifyQueryString(QS, "pagenumber", Convert.ToString(Ptr), true);
                            Footer += "\r\n" + "\t" + "\t" + "<li><div><a href=\"?" + QS + "\">" + Convert.ToString(Ptr) + "</a></div></li>";
                        }
                    }
                    if (Ptr < PageCount) {
                        QS = cp.Utils.ModifyQueryString(QS, "pagenumber", Convert.ToString(PageCount), true);
                        Footer += "\r\n" + "\t" + "\t" + "<li><div><a href=\"?" + QS + "\">" + (Convert.ToString(PageCount)) + "</a></div></li>";
                    }
                    GetTextSearch = GetTextSearch + "\r\n" + "<div class=\"pageLinkCon\">" + "\r\n" + "\t" + "<ul>" + Footer + "\r\n" + "\t" + "</ul>" + "\r\n" + "</div>";
                }
                GetTextSearch = "" + "\r\n" + "<div class=\"searchResultsCon\">" + "\r\n" + "<div class=\"summary\">" + ResultsLine + "</div>" + GetTextSearch + "\r\n" + "</div>";
                return GetTextSearch;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return GetTextSearch;
            }
        }
        // 
        // ========================================================================
        // Open a content set with the results of a search on ccSpiderDocs
        // 
        // KeyWord List supports:
        // All keywords ANDed together
        // Phrases are surrounded by quotes
        // ========================================================================
        // 
        // VBto upgrade warning: 'Return' As string	OnWrite(short)
        private CPCSBaseClass OpenCSTextSearch( CPBaseClass cp,  string KeyWordList, int VisitID, int PageSize, int PageNumber, bool sectionAll, string sectionIdList, bool topicAll, string topicIdList, string instanceId, ref string blockedSectionIdList) {
            var OpenCSTextSearch = cp.CSNew();
            try {
                if (Strings.Trim(KeyWordList) != "") {
                    string[] KeywordSplit = KeyWordList.Split(' ');
                    int WordCount = Information.UBound(KeywordSplit, 1) + 1;
                    string WordSearchExcludeList = GetWordSearchExcludeList(cp);
                    string sqlFrom = "ccSpiderDocs d";
                    string sqlWhere = "(d.Active<>0)and((d.duplicatecontent<>1)or(d.duplicatecontent is null))";
                    string sqlSubWhere = "";
                    string SearchBuffer = KeyWordList + " ";
                    string SearchWord = "";
                    int WordPointer = 0;
                    string sqlNow = cp.Db.EncodeSQLDate(DateTime.Now);
                    string sql = "";
                    while (WordPointer < WordCount) {
                        SearchWord = KeywordSplit[WordPointer];
                        if ((SearchWord != "") && (Strings.InStr(1, WordSearchExcludeList, "\r\n" + SearchWord + "\r\n", CompareMethod.Text) == 0)) {
                            string LenSearchWord = Convert.ToString(SearchWord.Length);
                            if (Convert.ToDouble(LenSearchWord) > 2) {
                                if ((SearchWord[1 - 1].ToString() == "\"") && (SearchWord[cp.Utils.EncodeInteger(LenSearchWord) - 1].ToString() == "\"")) {
                                    SearchWord = Strings.Mid(SearchWord, 2, Convert.ToInt32(float.Parse(LenSearchWord) - 2));
                                }
                            }
                            sqlWhere += "and(d.BodyText like " + cp.Db.EncodeSQLText("%" + SearchWord + "%") + ")";
                            // 
                            // Update the Search Keywords table
                            // 
                            int cid = cp.Content.GetID("Search Keywords");
                            sql = "insert into ccSearchKeywords (dateadded,active,contentcontrolid,name,visitid)values(" + sqlNow + ",1," + Convert.ToString(cid) + "," + cp.Db.EncodeSQLText(SearchWord) + "," + cp.Visit.Id + ")";
                            cp.Db.ExecuteNonQuery(sql);
                        }
                        WordPointer += 1;
                    }
                    string[] RecordIDArray = null;
                    int RecordIDArrayCount;
                    int ArrayPointer;
                    int recordId;
                    // 
                    // ----- topics
                    // 
                    if (!topicAll) {
                        // 
                        // Add the join to pages and topics
                        // 
                        sqlFrom = "((" + sqlFrom + " LEFT JOIN ccPageContent p ON d.pageid = p.id)" + " LEFT JOIN ccPageContentTopicRules pt ON pt.pageid=p.id)" + "";
                        // 
                        // Integrate Topics in SQLSubWhere
                        // 
                        sqlSubWhere = "";
                        RecordIDArray = Strings.Split(topicIdList, ",", -1, CompareMethod.Binary);
                        RecordIDArrayCount = Information.UBound(RecordIDArray, 1) + 1;
                        if (RecordIDArrayCount > 0) {
                            for (ArrayPointer = 0; ArrayPointer <= RecordIDArrayCount - 1; ArrayPointer++) {
                                recordId = cp.Utils.EncodeInteger(RecordIDArray[ArrayPointer]);
                                if (recordId != 0) {
                                    if (sqlSubWhere != "") {
                                        sqlSubWhere += "OR";
                                    }
                                    sqlSubWhere += "(pt.TopicID=" + Convert.ToString(recordId) + ")";
                                }
                            }
                            if (sqlSubWhere != "") {
                                sqlWhere += "and(" + sqlSubWhere + ")";
                            }
                        }
                    }
                    if (blockedSectionIdList != "") {
                        while (Strings.Left(blockedSectionIdList, 1) == ",") {
                            blockedSectionIdList = Strings.Right(blockedSectionIdList, blockedSectionIdList.Length - 1);
                        }
                        while (Strings.Right(blockedSectionIdList, 1) == ",") {
                            blockedSectionIdList = Strings.Left(blockedSectionIdList, blockedSectionIdList.Length - 1);
                        }
                        if (Strings.InStr(1, blockedSectionIdList, ",", CompareMethod.Binary) == 0) {
                            sqlWhere += "and (d.sectionid<>" + blockedSectionIdList + ")";
                        } else {
                            sqlWhere += "and not (d.sectionid in (" + blockedSectionIdList + "))";
                        }
                    }
                    // 
                    // ----- sections to include
                    // 
                    if (!sectionAll) {
                        // 
                        // Integrate Topics in SQLSubWhere
                        // 
                        sqlSubWhere = "";
                        RecordIDArray = Strings.Split(sectionIdList, ",", -1, CompareMethod.Binary);
                        RecordIDArrayCount = Information.UBound(RecordIDArray, 1) + 1;
                        if (RecordIDArrayCount > 0) {
                            for (ArrayPointer = 0; ArrayPointer <= RecordIDArrayCount - 1; ArrayPointer++) {
                                recordId = cp.Utils.EncodeInteger(RecordIDArray[ArrayPointer]);
                                if (recordId != 0) {
                                    if (sqlSubWhere != "") {
                                        sqlSubWhere += "OR";
                                    }
                                    sqlSubWhere += "(d.sectionid=" + Convert.ToString(recordId) + ")";
                                }
                            }
                            if (sqlSubWhere != "") {
                                sqlWhere += "and(" + sqlSubWhere + ")";
                            }
                        }
                    }
                    // 
                    // Get results
                    sql = "SELECT d.ID AS ID, d.Name AS LinkLabel, d.Link AS Link, d.TextOnlyFilename as BriefFilename, d.dateLastModified as dateLastModified, d.MetaKeywords as MetaKeywords, d.MetaDescription as MetaDescription, d.Length as Length, d.BodyText as BodyText, d.PrimaryImageLink as PrimaryImageLink, d.PrimaryImageWidth as PrimaryImageWidth, d.PrimaryImageHeight as PrimaryImageHeight" + " FROM " + sqlFrom + " WHERE " + sqlWhere + " ORDER BY d.dateLastModified Desc;";
                    OpenCSTextSearch.OpenSQL(sql,"", PageSize, PageNumber);
                }
                // 
                return OpenCSTextSearch;
                // 
                // ----- Error Trap
                // 
            } catch ( Exception ex ) { 
                cp.Site.ErrorReport(ex);
            }
            return OpenCSTextSearch;
        }
        // 
        // ----- Load the Word Search Exclude List
        // 
        private string GetWordSearchExcludeList(CPBaseClass cp) {
            return cp.CdnFiles.Read("WordSearchExcludeList.txt") + "\r\n";
        }

        // 
        // =============================================================================
        // 
        // =============================================================================
        // 
        private int TotalRowsFromFirstPage(CPBaseClass cp, string sql, int pageSize, int pageNumber)
        {
            var csTotalRows = cp.CSNew();
            int rowCount = 0;
            try
            {
                System.Data.DataTable dt = cp.Db.ExecuteQuery(sql, 1, 10000);               
                rowCount = dt.Rows.Count;
                return rowCount;
            }
            catch (Exception ex)
            {
                cp.Site.ErrorReport(ex);
                return rowCount;
            }
        }

    }
}