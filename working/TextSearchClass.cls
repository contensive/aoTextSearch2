VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "SearchFormClass"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True

Option Explicit
'
' This class will survive the Add-on transition. It remains unchanged.
'
'   All other classes in the this object will be replaced with the Spider Collection
'   th Spider Collection will only install and run on Contensive 3.3.990+
'
'
'
'
Private Main As Object
Private Csv As Object

Public Function Execute(CsvObject As Object, MainObject As Object, OptionString As String, FilterInput As String) As String
    On Error GoTo ErrorTrap
    '
    Set Csv = CsvObject
    Set Main = MainObject
    Execute = GetContent(OptionString)
    
    Exit Function
ErrorTrap:
    Call Main.reportError("trap error in aoTextSearch2.SearchFormClass.Execute")
End Function
'
'=================================================================================
'   Get content
'=================================================================================
'
Private Function GetContent(OptionString As String) As String
    On Error GoTo ErrorTrap
    '
    Dim sectionCnt As Long
    Dim RS As Recordset
    Dim cnt As Long
    Dim Ptr As Long
    Dim sections() As String
    Dim sectionid As Long
    Dim sqlInstanceId As String
    Dim blockedSectionIdList As String
    Dim sql As String
    Dim toolbar As String
    Dim subForm As String
    Dim topicAll As Boolean
    Dim sectionAll As Boolean
    Dim sectionIdList As String
    Dim topicIdList As String
    Dim allCheckedArg As String
    Dim checkedArg As String
    Dim recordId As Long
    Dim Caption As String
    Dim list As String
    Dim SearchForm As String
    Dim searchResults As String
    Dim ActionParts() As String
    Dim ActionPage As String
    Dim ActionQS As String
    Dim QSParts() As String
    Dim QSNameValues() As String
    Dim QSName As String
    Dim QSValue As String
    Dim RefreshHiddens As String
    Dim Stream As New fastStringClass
    Dim WordList As String
    Dim CSPointer As Long
    Dim Filename As String
    Dim Copy As String
    'Dim LanguageID As Long
    Dim Link As String
    Dim PageSize As Long
    Dim PageNumber As Long
    Dim Button As String
    Dim SearchResultsURL As String
    Dim IncludeSearchResults As Boolean
    Dim IncludeImage As Boolean
    Dim IncludeDescription As Boolean
    Dim PageID As Long
    Dim QS As String
    Dim CS As Long
    Dim instanceId As String
    Dim allowSectionChoices As Boolean
    Dim allowTopicChoices As Boolean
    Dim adminMsg As String
    '
    instanceId = Main.GetAddonOption("instanceid", OptionString)
    allowTopicChoices = kmaEncodeBoolean(Main.GetAddonOption("include Topic Options", OptionString))
    allowSectionChoices = kmaEncodeBoolean(Main.GetAddonOption("include Section Options", OptionString))
    PageID = kmaEncodeInteger(Main.GetAddonOption("Search Results Page", OptionString))
    IncludeSearchResults = kmaEncodeBoolean(Main.GetAddonOption("include Search Results", OptionString))
    IncludeImage = kmaEncodeBoolean(Main.GetAddonOption("Include Image in Results", OptionString))
    IncludeDescription = kmaEncodeBoolean(Main.GetAddonOption("Include Sample Text in Results", OptionString))
    '
    sectionAll = kmaEncodeBoolean(Main.getstreamtext("tsSectionAll"))
    If Not sectionAll Then
        sectionIdList = Main.getstreamtext("tsSectionIdList")
        sectionIdList = Replace(sectionIdList, " ", "")
        If sectionIdList = "" Then
            sectionAll = True
        End If
    End If
    
    topicAll = kmaEncodeBoolean(Main.getstreamtext("tsTopicAll"))
    If Not topicAll Then
        topicIdList = Main.getstreamtext("tsTopicIdList")
        topicIdList = Replace(topicIdList, " ", "")
        If topicIdList = "" Then
            topicAll = True
        End If
    End If
    '
    If PageID <> 0 Then
        On Error Resume Next
        SearchResultsURL = Main.GetPageLink3(PageID, "", False)
        If Err.Number <> 0 Then
            Err.Clear
            CS = Main.OpenCSContent("Content Watch", "recordid=" & PageID)
            If Main.IsCSOK(CS) Then
                SearchResultsURL = Main.GetCSText(CS, "Link")
                SearchResultsURL = kmaModifyLinkQuery(SearchResultsURL, "TextSearchWordList", "", False)
                SearchResultsURL = kmaModifyLinkQuery(SearchResultsURL, "button", "", False)
            End If
            Call Main.CloseCS(CS)
            If SearchResultsURL = "" Then
                SearchResultsURL = "/" & Main.GetSiteProperty("SERVERPAGEDEFAULT") & "?bid=" & PageID
            End If
        End If
    End If
    '
    ' ----- process sections to block
    '
    If (Main.getstreamtext("button") = ButtonApply) Then
        If Main.isediting("text search section rules") Then
            sqlInstanceId = KmaEncodeSQLText(instanceId)
            blockedSectionIdList = Main.getstreamtext("blockedSectionIdList")
            Call Main.DeleteContentRecords("text search section rules", "instanceid=" & sqlInstanceId)
            blockedSectionIdList = Replace(blockedSectionIdList, " ", "")
            If blockedSectionIdList <> "" Then
                sections = Split(blockedSectionIdList, ",")
                cnt = UBound(sections) + 1
                For Ptr = 0 To cnt - 1
                    sectionid = kmaEncodeInteger(sections(Ptr))
                    CS = Main.InsertCSRecord("text search section rules")
                    If Main.IsCSOK(CS) Then
                        Call Main.SetCS(CS, "instanceid", instanceId)
                        Call Main.SetCS(CS, "sectionid", sectionid)
                    End If
                    Call Main.CloseCS(CS)
                Next
            End If
            
        End If
    End If
    '
    ' ----- load sections to block
    '
    blockedSectionIdList = ""
    sql = "select sectionid from textSearchSectionRules where instanceid=" & KmaEncodeSQLText(instanceId)
    Set RS = Main.ExecuteSQL("", sql)
    If Not (RS Is Nothing) Then
        If Not RS.EOF Then
            blockedSectionIdList = RS.GetString(, , "", ",")
        End If
    End If
    '
    ' Search Results
    '
    If IncludeSearchResults Then
        '
        WordList = Main.getstreamtext("TextSearchWordList")
        Button = Main.getstreamtext("Button")
        If WordList = "" Then
            '
            ' Button Method
            '
            If (Button <> "") Then
                '
                ' Button + 'WordList' (support legacy hard-coded forms)
                '
                WordList = Main.getstreamtext("WordList")
                If WordList = "" Then
                    '
                    ' Button + 'QWordList' (support legacy hard-coded forms)
                    '
                    WordList = Main.getstreamtext("QWordList")
                End If
            End If
        End If
        If WordList <> "" Then
            If Main.getstreamtext("PageSize") = "" Then
                PageSize = 10
            Else
                PageSize = Main.GetStreamInteger("PageSize")
            End If
            If Main.getstreamtext("PageNumber") = "" Then
                PageNumber = 1
            Else
                PageNumber = Main.GetStreamInteger("PageNumber")
            End If
            searchResults = searchResults & GetTextSearch(WordList, PageSize, PageNumber, IncludeImage, IncludeDescription, sectionAll, sectionIdList, topicAll, topicIdList, instanceId, blockedSectionIdList)
            'searchResults = searchResults & GetTextSearch(WordList, PageSize, PageNumber, LanguageID, IncludeImage, IncludeDescription, sectionIdList, topicIdList)
        End If
    End If
    '
    ' ----- Build Search Form
    '
    If SearchResultsURL <> "" Then
        '
        ' This add-on will never handle it's own posts so ignore RefreshQueryString
        ' and form get to the SearchResultsPage
        '
        ActionParts = Split(SearchResultsURL, "?")
        ActionPage = ActionParts(0)
        If UBound(ActionParts) > 0 Then
            ActionQS = ActionParts(1)
        End If
    Else
        '
        ' This add-on can handle it's own posts, so build page and hiddens from RQS
        '
        ActionPage = ""
        ActionQS = Main.refreshquerystring
    End If
    If ActionQS <> "" Then
        QSParts = Split(ActionQS, "&")
        For Ptr = 0 To UBound(QSParts)
            QSNameValues = Split(QSParts(Ptr), "=")
            If UBound(QSNameValues) = 0 Then
                QSName = kmaDecodeResponseVariable(QSNameValues(0))
            Else
                QSName = kmaDecodeResponseVariable(QSNameValues(0))
                Select Case LCase(QSName)
                    Case "qwordlist", "button", "pagenumber", "pagesize"
                    Case Else
                        QSValue = kmaDecodeResponseVariable(QSNameValues(1))
                        RefreshHiddens = RefreshHiddens & vbCrLf & "<input type=""hidden"" name=""" & kmaEncodeHTML(QSName) & """ value=""" & kmaEncodeHTML(QSValue) & """>"
                End Select
            End If
        Next
    End If
    '
    SearchForm = "" _
        & SearchForm _
        & RefreshHiddens _
        & vbCrLf & vbTab & Main.getformbutton("Search") & "&nbsp;" & Main.GetFormInputText("TextSearchWordList", WordList, , 15) _
        & ""
    '
    ' list sections
    '
    If allowSectionChoices Then
        subForm = ""
        list = ""
        CS = Main.OpenCSContent("site sections", , "name,caption,id")
        Do While Main.IsCSOK(CS)
            recordId = Main.GetCSText(CS, "id")
            If (blockedSectionIdList = "") Or (InStr(1, "," & blockedSectionIdList & ",", "," & recordId & ",") = 0) Then
                Caption = Main.GetCSText(CS, "caption")
                If Caption = "" Then
                    Caption = Main.GetCSText(CS, "name")
                End If
                If Caption <> "" Then
                    If (InStr(1, "," & sectionIdList & ",", "," & recordId & ",") <> 0) Then
                        checkedArg = " checked=checked"
                    Else
                        checkedArg = ""
                    End If
                    list = list & vbCrLf & vbTab & "<li><input type=""checkbox"" name=""tsSectionIdList"" value=""" & recordId & """ onclick=""if(this.checked){textSearchClr('tsSectionAll')};""" & checkedArg & "><p>" & Caption & "</p></li>"
                    sectionCnt = sectionCnt + 1
                End If
            End If
            Call Main.NextCSRecord(CS)
        Loop
        Call Main.CloseCS(CS)
        If sectionCnt = 0 Then
            '
            ' no sections
            '
        ElseIf sectionCnt = 1 Then
            '
            ' one section, block choice and add hidden for all
            '
            SearchForm = SearchForm & vbCrLf & vbTab & Main.getforminputhidden("tsSectionAll", "1")
        Else
            '
            ' display choices
            '
            If sectionAll Then
                checkedArg = " checked=checked"
            Else
                checkedArg = ""
            End If
            subForm = "" _
                & vbCrLf & vbTab & "<h2 class=""tsSectionHead"">Sections</h2>" _
                & vbCrLf & vbTab & "<ul class=""tsSectionList"">" _
                & vbCrLf & vbTab & "<li><input type=""checkbox"" name=""tsSectionAll"" value=""1"" onclick=""textSearchClr('tsSectionIdList');"" " & checkedArg & "><p>All</p></li>" _
                & kmaIndent(list) _
                & vbCrLf & vbTab & "</ul>" _
                & ""
            SearchForm = SearchForm _
                & vbCrLf & vbTab & "<div class=""tsSections"">" _
                & kmaIndent(subForm) _
                & vbCrLf & vbTab & "</div>" _
                & ""
        End If
    End If
    '
    ' list topics
    '
    If allowTopicChoices Then
        subForm = ""
        list = ""
        CS = Main.OpenCSContent("topics", , "name,id")
        Do While Main.IsCSOK(CS)
            recordId = Main.GetCSText(CS, "id")
            Caption = Main.GetCSText(CS, "name")
                If (InStr(1, "," & topicIdList & ",", "," & recordId & ",") <> 0) Then
                    checkedArg = " checked=checked"
                Else
                    checkedArg = ""
                End If
            If Caption <> "" Then
                list = list & vbCrLf & vbTab & "<li><input type=""checkbox"" name=""tsTopicIdList"" value=""" & recordId & """ onclick=""textSearchClr('tsTopicAll');""" & checkedArg & "><p>" & Caption & "</p></li>"
            End If
            Call Main.NextCSRecord(CS)
        Loop
        Call Main.CloseCS(CS)
        If list <> "" Then
            If topicAll Then
                checkedArg = " checked=checked"
            Else
                checkedArg = ""
            End If
            subForm = "" _
                & vbCrLf & vbTab & "<h2 class=""tsTopicHead"">Topics</h2>" _
                & vbCrLf & vbTab & "<ul class=""tsTopicList"">" _
                & vbCrLf & vbTab & "<li><input type=""checkbox"" name=""tsTopicAll"" value=""1"" onclick=""textSearchClr('tsTopicIdList');"" onclick=""alert('clear all others');"" " & checkedArg & "><p>All</p></li>" _
                & kmaIndent(list) _
                & vbCrLf & vbTab & "</ul>" _
                & ""
            SearchForm = SearchForm _
                & vbCrLf & vbTab & "<div class=""tsTopics"">" _
                & kmaIndent(subForm) _
                & vbCrLf & vbTab & "</div>" _
                & ""
        End If
    End If
    '
    ' ----- custom edit toolbar for sections to include
    '
Dim lastid As Long
    If IncludeSearchResults And Main.isediting("text search section rules") Then
        toolbar = "Sections to Block from this search form"
        subForm = ""
        list = ""
        sql = "select s.caption,s.name,s.id as sectionid,r.id as ruleid from ccsections s left join (select instanceid,sectionid,id from textsearchsectionrules where instanceid=" & KmaEncodeSQLText(instanceId) & ") r on r.sectionid=s.id order by s.caption,s.name"
        CS = Main.OpenCSSQL("", sql)
        lastid = -1
        Do While Main.IsCSOK(CS)
            recordId = Main.GetCSText(CS, "sectionid")
            If recordId <> lastid Then
                Caption = Main.GetCSText(CS, "caption")
                If Caption = "" Then
                    Caption = Main.GetCSText(CS, "name")
                End If
                If Caption <> "" Then
                    If Main.GetCSBoolean(CS, "ruleid") Then
                        checkedArg = " checked=checked"
                    Else
                        checkedArg = ""
                    End If
                    list = list & vbCrLf & vbTab & "<li><input type=""checkbox"" name=""blockedSectionIdList"" value=""" & recordId & """ " & checkedArg & "><p>" & Caption & "</p></li>"
                End If
            End If
            lastid = recordId
            Call Main.NextCSRecord(CS)
        Loop
        Call Main.CloseCS(CS)
        '
        If list <> "" Then
            toolbar = toolbar _
                & vbCrLf & vbTab & "<ul class=""tsSectionBlockList"">" _
                & kmaIndent(list) _
                & vbCrLf & vbTab & "</ul>" _
                & ""
            toolbar = toolbar _
                & vbCrLf & vbTab & "<div class=""tsButton"">" _
                & Main.getformbutton(ButtonApply) _
                & vbCrLf & vbTab & "</div>" _
                & ""
        End If
        '
        SearchForm = SearchForm _
            & vbCrLf & vbTab & "<div style=""clear:both;"">&nbsp;</div>" _
            & vbCrLf & vbTab & "<div class=""tsToolBar"">" _
            & kmaIndent(Main.GetAdminHintWrapper(toolbar)) _
            & vbCrLf & vbTab & "</div>" _
            & ""
    End If
    '
    SearchForm = "" _
        & vbCrLf & vbTab & "<form action=""" & ActionPage & """ METHOD=""get"" style=""display:inline;"" >" _
        & kmaIndent(SearchForm) _
        & vbCrLf & vbTab & Main.GetFormEnd _
        & ""
'    If SearchResultsURL = "" Then
'        SearchForm = "" _
'            & vbCrLf & vbTab & "<form action=""" & ActionPage & """ METHOD=""get"" style=""display:inline;"" >" _
'            & kmaIndent(SearchForm) _
'            & vbCrLf & vbTab & Main.GetFormEnd _
'            & ""
'    Else
'        SearchForm = "" _
'            & vbCrLf & vbTab & "<FORM action=""" & ActionPage & """ METHOD=""POST"" style=""display: inline;"">" _
'            & kmaIndent(SearchForm) _
'            & vbCrLf & vbTab & Main.GetFormEnd _
'            & ""
''        SearchForm = "" _
''            & vbCrLf & vbTab & "<FORM action=""" & SearchResultsURL & """ METHOD=""POST"" style=""display: inline;"">" _
''            & kmaIndent(SearchForm) _
''            & vbCrLf & vbTab & Main.GetFormEnd _
''            & ""
'    End If
    '
    SearchForm = "" _
        & vbCrLf & vbTab & "<div class=""searchFormCon"">" _
        & kmaIndent(SearchForm) _
        & vbCrLf & vbTab & "</div>" _
        & ""
    If Main.IsAdmin() Then
        '
        ' validate settings
        '
        If Not kmaEncodeBoolean(Main.GetSiteProperty("AllowContentSpider", "")) Then
            adminMsg = "" _
                & adminMsg _
                & "<p>Warning: The spider process is currently disabled. Text search results will empty or incomplete until the spider is enabled. Use the <a href=""" & Main.GetSiteProperty("adminUrl") & "?addonguid=%7B8DFEC2DA-1AC5-4B74-9BC7-8B07DBE83915%7D"">spider settings page</a> to enable the spider process.</p>" _
                & ""
            SearchForm = "" _
                & SearchForm _
                & Main.GetAdminHintWrapper(adminMsg) _
                & ""
        End If
    End If
    '
    GetContent = "" _
        & vbCrLf & "<div class=""textSearch2Con"">" _
        & searchResults _
        & SearchForm _
        & vbCrLf & "</div>"
    '
    Exit Function
ErrorTrap:
    Call Main.reportError("trap error in aoTextSearch2.SearchFormClass.getContent")
End Function
'
'=============================================================================
'
'=============================================================================
'
Private Function GetTextSearchForm(SearchResultsURL As String, IncludeSearchResults As Boolean) As String
    On Error GoTo ErrorTrap
    Exit Function
    '
ErrorTrap:
    Call HandleError("TextSearchClass", "GetTextSearchForm", Err.Number, Err.Source, Err.Description, True, False)
End Function
'
'=============================================================================
'
'=============================================================================
'
Private Function GetTextSearch(KeyWordList As String, PageSize As Long, InitialPageNumber As Long, IncludeImage As Boolean, IncludeDescription As Boolean, sectionAll As Boolean, sectionIdList As String, topicAll As Boolean, topicIdList As String, instanceId As String, blockedSectionIdList As String) As String
'Private Function GetTextSearch(KeyWordList As String, PageSize As Long, InitialPageNumber As Long, LanguageID As Long, IncludeImage As Boolean, IncludeDescription As Boolean, sectionIdList As String, topicIdList As String) As String
    On Error GoTo ErrorTrap
    '
    Const ColumnCount = 1
    Const PageLinkCnt = 20
    '
    Dim resultLineDetails As String
    Dim Footer As String
    Dim PageMiddle As Long
    Dim PageLinkStart As Long
    Dim QS As String
    Dim Ptr As Long
    Dim PageLinkEnd As Long
    Dim ATag As String
    Dim PrimaryImageLink As String
    Dim PrimaryImageWidth As Long
    Dim PrimaryImageHeight As Long
    Dim RowLast As Long
    Dim ResultsLine As String
    Dim Stream As New fastStringClass
    Dim Caption As String
    Dim Length As Long
    Dim Body As String
    Dim Detail As String
    Dim BodyText As String
    Dim iKeyWordList As String
    Dim PageNumber As Long
    Dim RowPointer As Long
    Dim KeywordSplit() As String
    Dim Keyword As String
    Dim KeywordPosition As Long
    Dim KeyWordLength As Long
    Dim RowEven As Boolean
    Dim LoopCount As Long
    Dim Position As Long
    Dim MinPosition As Long
    Dim MaxPosition As Long
    Dim SpanLength As Long
    Dim CSSearch As Long
    '
    Dim ColCaption() As String
    Dim ColAlign() As String
    Dim ColWidth() As String
    Dim Content() As String
    Dim PreTableCopy As String
    Dim PostTableCopy As String
    Dim ColumnPointer As Long
    '
    Dim RecordPointer As Long
    Dim Index As Long
    Dim StartPosition As Long
    Dim SourceWord As String
    Dim LinkLabel As String
    Dim Link As String
    Dim EvenRow As Boolean
    Dim BreakTag As String
    Dim RowBAse As Long
    Dim DataRowCount As Long
    Dim iPageSize As Long
    Dim PostBodyText As String
    Dim PreBodyText As String
    Dim PageCountDbl As Double
    Dim PageCount As Long
    '
    Const OverviewLength = 500
    '
    iKeyWordList = kmaEncodeText(KeyWordList)
    If iKeyWordList = "" Then
        '
        ' ----- key word list is required
        '
        GetTextSearch = "<P>You must enter a key word to perform a search.</P>"
        'Call Stream.Add(kmaGetTableCell("<b>You must enter a key word to perform a search.</b>"))
    Else
        '
        ' ----- Get current Page Number
        '
        iPageSize = kmaEncodeInteger(PageSize)
        If iPageSize <= 0 Then
            iPageSize = 20
        End If
        If Main.getstreamtext(RequestNamePageNumber) <> "" Then
            PageNumber = Main.GetStreamInteger(RequestNamePageNumber)
        Else
            PageNumber = KmaEncodeMissingInteger(InitialPageNumber, "1")
        End If
        If PageNumber <= 0 Then
            PageNumber = 1
        End If
        '
        ' ----- Get the results
        '
        resultLineDetails = "<i>" & KeyWordList & "</i>"
        CSSearch = OpenCSTextSearch(KeyWordList, Main.VisitID, iPageSize, PageNumber, sectionAll, sectionIdList, topicAll, topicIdList, instanceId, blockedSectionIdList)
        'CSSearch = OpenCSTextSearch(KeyWordList, Main.VisitID, iPageSize, PageNumber, LanguageID, sectionIdList, topicIdList)
        'CSSearch = Main.OpenCSTextSearch(KeyWordList, TopicIDList, ContentIDList, iPageSize, PageNumber, LanguageID)
        '
        ' ----- Prep the keywords for text matching
        '
        KeywordSplit = kmaSplit(Trim(iKeyWordList), " ")
        For Index = 0 To UBound(KeywordSplit)
            Keyword = Trim(LCase(KeywordSplit(Index)))
            If (Len(Keyword) > 2) And (Left(Keyword, 1) = """") And (Right(Keyword, 1) = """") Then
                Keyword = Mid(Keyword, 2, Len(Keyword) - 2)
            End If
            KeywordSplit(Index) = Keyword
        Next
        '
        ' ----- Display the results
        '
        ReDim ColAlign(ColumnCount)
        ReDim ColCaption(ColumnCount)
        ReDim ColWidth(ColumnCount)
        ReDim Content(iPageSize, ColumnCount)
        '
        For ColumnPointer = 0 To ColumnCount - 1
            ColAlign(ColumnPointer) = "Left"
            ColCaption(ColumnPointer) = ""
        Next
        ColWidth(0) = "100%"
        '

        RowPointer = 0
        RowBAse = (iPageSize * (PageNumber - 1))
        DataRowCount = Main.GetCSRowCount(CSSearch)
        RowLast = RowBAse + PageSize
        If RowLast > DataRowCount Then
            RowLast = DataRowCount
        End If
        ResultsLine = "Results " & (1 + RowBAse) & " to " & RowLast & " of " & DataRowCount & " for " & resultLineDetails
        Do While Main.IsCSOK(CSSearch) And (RowPointer < iPageSize)
            PrimaryImageLink = Main.GetCSText(CSSearch, "PrimaryImageLink")
            PrimaryImageWidth = Main.GetCSInteger(CSSearch, "PrimaryImageWidth")
            PrimaryImageHeight = Main.GetCSInteger(CSSearch, "PrimaryImageHeight")
            Length = Main.GetCSInteger(CSSearch, "Length")
            Body = ""
            '
            If IncludeDescription Then
                BodyText = Main.GetCSText(CSSearch, "BodyText")
                BodyText = kmaEncodeHTML(BodyText)
                PostBodyText = ""
                PreBodyText = ""
                '
                ' trim BodyText down to size
                '
                If Len(BodyText) > OverviewLength Then
                    '
                    ' Determine Min and Max positions
                    '
                    For Index = 0 To UBound(KeywordSplit)
                        Position = InStr(1, BodyText, KeywordSplit(Index), vbTextCompare)
                        If (Position <> 0) And ((MinPosition = 0) Or (Position < MinPosition)) Then
                            MinPosition = Position
                        End If
                        Position = InStrRev(BodyText, KeywordSplit(Index), , vbTextCompare)
                        If (Position <> 0) And ((MaxPosition = 0) Or (Position > MaxPosition)) Then
                            MaxPosition = Position
                        End If
                    Next
                    SpanLength = MaxPosition - MinPosition
                    PreBodyText = ""
                    PostBodyText = ""
                    If (MinPosition = 0) Then
                        '
                        ' not found, return first 200 characters
                        '
                        StartPosition = 1
                        If Len(BodyText) > OverviewLength Then
                            PostBodyText = "..."
                        End If
                        BodyText = Mid(BodyText, StartPosition, OverviewLength)
                    ElseIf (SpanLength = 0) Or (SpanLength > OverviewLength) Then
                        '
                        ' only one found or the span is long, return 100 before min, 100 after min
                        '
                        StartPosition = MinPosition - (OverviewLength / 2)
                        If StartPosition < 1 Then
                            '
                            ' Start at the first position of bodytext
                            '
                            StartPosition = 1
                            If Len(BodyText) > OverviewLength Then
                                PostBodyText = "..."
                            End If
                            BodyText = Mid(BodyText, StartPosition, OverviewLength)
                        ElseIf ((StartPosition + OverviewLength) > Len(BodyText)) Then
                            '
                            ' End the copy at the end of the bodytext
                            '
                            StartPosition = Len(BodyText) - OverviewLength + 1
                            BodyText = Mid(BodyText, StartPosition)
                            PreBodyText = "..."
                        Else
                            BodyText = Mid(BodyText, StartPosition, OverviewLength)
                            PreBodyText = "..."
                            PostBodyText = "..."
                        End If
                    Else
                        '
                        ' Span is short, return the span, some before, some after
                        '
                        StartPosition = MinPosition - ((OverviewLength - SpanLength) / 2)
                        If StartPosition < 1 Then
                            '
                            ' Start at the first position of bodytext
                            '
                            StartPosition = 1
                            If Len(BodyText) > OverviewLength Then
                                PostBodyText = "..."
                            End If
                            BodyText = Mid(BodyText, StartPosition, OverviewLength)
                        ElseIf ((StartPosition + OverviewLength) > Len(BodyText)) Then
                            '
                            ' End the copy at the end of the bodytext
                            '
                            StartPosition = Len(BodyText) - OverviewLength + 1
                            BodyText = Mid(BodyText, StartPosition)
                            PreBodyText = "..."
                        Else
                            BodyText = Mid(BodyText, StartPosition, OverviewLength)
                            PreBodyText = "..."
                            PostBodyText = "..."
                        End If
'                    Else
'                        '
'                        ' Span is long, return 100 before the min and 100 after the min
'                        '
'                        StartPosition = MinPosition - (OverviewLength / 2)
'                        If StartPosition < 1 Then
'                            '
'                            ' Start at the first position of bodytext
'                            '
'                            StartPosition = 1
'                            BodyText = Mid(BodyText, StartPosition, OverviewLength)
'                            PostBodyText = "..."
'                        ElseIf ((StartPosition + OverviewLength) > Len(BodyText)) Then
'                            '
'                            ' End the copy at the end of the bodytext
'                            '
'                            StartPosition = Len(BodyText) - OverviewLength + 1
'                            BodyText = Mid(BodyText, StartPosition)
'                            PreBodyText = "..."
'                        Else
'                            BodyText = Mid(BodyText, StartPosition, OverviewLength)
'                            PreBodyText = "..."
'                            PostBodyText = "..."
'                        End If
                    End If
                End If
                '
                ' bold all occurances of each keywords, keep the min and max position
                '
                For Index = 0 To UBound(KeywordSplit)
                    Keyword = KeywordSplit(Index)
                    KeyWordLength = Len(Keyword)
                    If Keyword <> "" Then
                        KeywordPosition = InStr(1, BodyText, Keyword, vbTextCompare)
                        LoopCount = 0
                        Do While KeywordPosition <> 0 And (LoopCount < 100)
                            SourceWord = Mid(BodyText, KeywordPosition, KeyWordLength)
                            If KeywordPosition = 1 Then
                                BodyText = "<b>" & Mid(BodyText, KeywordPosition, KeyWordLength) & "</b>" & Mid(BodyText, KeywordPosition + KeyWordLength)
                            Else
                                BodyText = Mid(BodyText, 1, KeywordPosition - 1) & "<b>" & Mid(BodyText, KeywordPosition, KeyWordLength) & "</b>" & Mid(BodyText, KeywordPosition + KeyWordLength)
                            End If
                            KeywordPosition = InStr((KeywordPosition + KeyWordLength + 6), BodyText, Keyword, vbTextCompare)
                            LoopCount = LoopCount + 1
                        Loop
                    End If
                Next
                Body = PreBodyText & BodyText & PostBodyText
            End If
            '
            LinkLabel = Main.GetCSText(CSSearch, "LinkLabel")
            Link = Main.GetCSText(CSSearch, "Link")
            ATag = "<a href=""" & kmaEncodeHTML(Main.EncodeAppRootPath(Link)) & """>"
            '
            Caption = ATag & LinkLabel & "</a>"
            'If (LinkLabel <> "") Then
            '    Content(RowPointer, 0) = ATag & LinkLabel & "</a>" _
            '        & "<BR>" & PreBodyText & BodyText & PostBodyText
            'End If
            If IncludeImage And (PrimaryImageLink <> "") Then
                If PrimaryImageWidth = 0 And PrimaryImageHeight = 0 Then
                    '
                    ' No dimensions, set width=100
                    '
                    Body = ATag & "<img src=""" & PrimaryImageLink & """ width=""100"" border=0 class=""image""></a>" & Body
                ElseIf PrimaryImageWidth > PrimaryImageHeight Then
                    '
                    ' width > height, set width=100
                    '
                    Body = ATag & "<img src=""" & PrimaryImageLink & """ width=""100"" border=0 class=""image""></a>" & Body
                Else
                    '
                    ' height > width, set height=100
                    '
                    Body = ATag & "<img src=""" & PrimaryImageLink & """ height=""100"" border=0 class=""image""></a>" & Body
                End If
            End If
            If Length < 1024 Then
                Detail = ATag & Link & "</a> " & Length & " characters"
            Else
                Detail = ATag & Link & "</a> " & Int(Length / 1024) & "K characters"
            End If
            GetTextSearch = GetTextSearch _
                & vbCrLf & vbTab & "<div class=""listItemCon"">" _
                & vbCrLf & vbTab & vbTab & "<div class=""caption"">" & Caption & "</div>"
            If IncludeDescription Or IncludeImage Then
                GetTextSearch = GetTextSearch _
                    & vbCrLf & vbTab & vbTab & "<div class=""body"">" & Body & "</div>"
            End If
            GetTextSearch = GetTextSearch _
                & vbCrLf & vbTab & vbTab & "<div class=""detail"">" & Detail & "</div>" _
                & vbCrLf & vbTab & "</div>"
            Main.NextCSRecord (CSSearch)
            RecordPointer = RecordPointer + 1
            RowPointer = RowPointer + 1
        Loop
        Call Main.CloseCS(CSSearch)
        '
        If DataRowCount = 0 Then
            ResultsLine = "Your search " & resultLineDetails & " returned no results."
        Else
            Call Main.AddRefreshQueryString("QWordList", KeyWordList)
            'Call Main.AddRefreshQueryString("ContentIDList", ContentIDList)
            'Call Main.AddRefreshQueryString("TopicIDList", TopicIDList)
            Call Main.AddRefreshQueryString("PageNumber", PageNumber)
            Call Main.AddRefreshQueryString("PageSize", iPageSize)
            Call Main.AddRefreshQueryString("Button", "Search")
            '
            ' Display Next buttons
            '
            QS = Main.refreshquerystring
            PageCountDbl = (DataRowCount / PageSize)
            PageCount = Int(PageCountDbl)
            If PageCountDbl <> PageCount Then
                PageCount = PageCount + 1
            End If
            If PageCount < PageLinkCnt Then
                '
                ' [1]...[20]
                '
                PageLinkStart = 1
                PageLinkEnd = PageCount
            Else
                '
                ' [1],[middle-10]...[middle+20],[PastLast]
                '
                PageMiddle = PageCount / 2
                If PageNumber <= PageMiddle Then
                    PageLinkStart = 1
                    PageLinkEnd = PageCount
                Else
                    PageLinkStart = PageNumber - (PageMiddle / 2)
                    PageLinkEnd = PageNumber + (PageMiddle / 2)
                End If
            End If
                        
            If PageLinkStart <> 1 Then
                Footer = Footer & vbCrLf & "<li><div><a href=""?" & QS & "&pagenumber=1"">1</a></div></li>"
            End If
            For Ptr = PageLinkStart To PageLinkEnd
                If Ptr = PageNumber Then
                    Footer = Footer & vbCrLf & vbTab & vbTab & "<li><div>" & Ptr & "</div></li>"
                Else
                    QS = ModifyQueryString(QS, "pagenumber", CStr(Ptr), True)
                    Footer = Footer & vbCrLf & vbTab & vbTab & "<li><div><a href=""?" & QS & """>" & Ptr & "</a></div></li>"
                End If
            Next
            If Ptr < PageCount Then
                QS = ModifyQueryString(QS, "pagenumber", CStr(PageCount), True)
                Footer = Footer & vbCrLf & vbTab & vbTab & "<li><div><a href=""?" & QS & """>" & (PageCount) & "</a></div></li>"
            End If
            GetTextSearch = GetTextSearch _
                & vbCrLf & "<div class=""pageLinkCon"">" _
                & vbCrLf & vbTab & "<ul>" & Footer _
                & vbCrLf & vbTab & "</ul>" _
                & vbCrLf & "</div>"
        End If
    End If
    GetTextSearch = "" _
        & vbCrLf & "<div class=""searchResultsCon"">" _
        & vbCrLf & "<div class=""summary"">" & ResultsLine & "</div>" _
        & GetTextSearch _
        & vbCrLf & "</div>"
    Exit Function

    '
ErrorTrap:
    Call HandleError("SpiderAO.TextSearchClass", "GetTextSearch", Err.Number, Err.Source, Err.Description, True, False)
    End Function
'
'========================================================================
'   Open a content set with the results of a search on ccSpiderDocs
'
'   KeyWord List supports:
'           All keywords ANDed together
'           Phrases are surrounded by quotes
'========================================================================
'
Private Function OpenCSTextSearch(KeyWordList As String, VisitID As Long, PageSize As Long, PageNumber As Long, sectionAll As Boolean, sectionIdList As String, topicAll As Boolean, topicIdList As String, instanceId As String, blockedSectionIdList As String) As String
'Private Function OpenCSTextSearch(KeyWordList As String, VisitID As Long, PageSize As Long, PageNumber As Long, LanguageID As Long, sectionIdList As String, topicIdList As String) As String
    On Error GoTo ErrorTrap
    '
    Dim RS As Recordset
    Dim sql As String
    Dim sqlNow As String
    Dim sqlFrom As String
    Dim sqlWhere As String
    Dim sqlSubWhere As String
    '
    Dim MySortFieldList As String
    Dim MyActiveOnly As Boolean
    Dim WordPointer As Long
    Dim SearchWord As String
    Dim SearchBuffer As String
    Dim WordCount As Long
    Dim MethodName As String
    Dim RecordIDArray() As String
    Dim RecordIDArrayCount As Long
    Dim ArrayPointer As Long
    Dim recordId As Long
    Dim WordSearchExcludeList As String
    Dim KeywordSplit() As String
    Dim CS As Long
    Dim ContentName As String
    Dim ContentID As Long
    Dim CDef As CDefType
    Dim ChildContentIDList As String
    Dim LenSearchWord As String
    Dim TempName As String
    Dim cid As Long
    Dim resultLineSub As String
    '
    MethodName = "OpenCSTextSearch"
    '
    OpenCSTextSearch = -1
    If (Trim(KeyWordList) <> "") Then
        KeywordSplit = kmaSplit(KeyWordList, " ")
        WordCount = UBound(KeywordSplit) + 1
        WordSearchExcludeList = GetWordSearchExcludeList
        sqlFrom = "ccSpiderDocs d"
        sqlWhere = "(d.Active<>0)and((d.duplicatecontent<>1)or(d.duplicatecontent is null))"
        sqlSubWhere = ""
        SearchBuffer = KeyWordList & " "
        SearchWord = ""
        WordPointer = 0
        sqlNow = KmaEncodeSQLDate(Now())
        Do While WordPointer < WordCount
            SearchWord = KeywordSplit(WordPointer)
            If (SearchWord <> "") And (InStr(1, WordSearchExcludeList, vbCrLf & SearchWord & vbCrLf, vbTextCompare) = 0) Then
                LenSearchWord = Len(SearchWord)
                If (LenSearchWord > 2) Then
                    If (Mid(SearchWord, 1, 1) = """") And (Mid(SearchWord, LenSearchWord, 1) = """") Then
                        SearchWord = Mid(SearchWord, 2, LenSearchWord - 2)
                    End If
                End If
                sqlWhere = sqlWhere & "and(d.BodyText like " & KmaEncodeSQLText("%" & SearchWord & "%") & ")"
                '
                ' Update the Search Keywords table
                '
                cid = Main.GetContentID("Search Keywords")
                sql = "insert into ccSearchKeywords (dateadded,active,contentcontrolid,name,visitid)values(" & sqlNow & ",1," & cid & "," & KmaEncodeSQLText(SearchWord) & "," & Main.VisitID & ")"
                Call Main.ExecuteSQL("", sql)
'                CS = Main.OpenCSContent("Search Keywords", "(name=" & KmaEncodeSQLText(SearchWord) & ")and(visitid=" & VisitID & ")")
'                If Not Main.IsCSOK(CS) Then
'                    Call Main.CloseCS(CS)
'                    CS = Main.InsertCSRecord("Search Keywords")
'                    If Main.IsCSOK(CS) Then
'                        Call Main.SetCS(CS, "name", SearchWord)
'                        Call Main.SetCS(CS, "visitid", VisitID)
'                    End If
'                End If
'                If Main.IsCSOK(CS) Then
'                    Call Main.SetCS(CS, "count", Main.GetCSInteger(CS, "count") + 1)
'                End If
'                Call Main.CloseCS(CS)
            End If
            WordPointer = WordPointer + 1
        Loop
        '
        ' ----- topics
        '
        If Not topicAll Then
            '
            ' Add the join to pages and topics
            '
            sqlFrom = "((" & sqlFrom _
                & " LEFT JOIN ccPageContent p ON d.pageid = p.id)" _
                & " LEFT JOIN ccPageContentTopicRules pt ON pt.pageid=p.id)" _
                & ""
            '
            ' Integrate Topics in SQLSubWhere
            '
            sqlSubWhere = ""
            RecordIDArray = Split(topicIdList, ",")
            RecordIDArrayCount = UBound(RecordIDArray) + 1
            If RecordIDArrayCount > 0 Then
                For ArrayPointer = 0 To RecordIDArrayCount - 1
                    recordId = kmaEncodeInteger(RecordIDArray(ArrayPointer))
                    If recordId <> 0 Then
                        If sqlSubWhere <> "" Then
                            sqlSubWhere = sqlSubWhere & "OR"
                        End If
                        sqlSubWhere = sqlSubWhere & "(pt.TopicID=" & recordId & ")"
                    End If
                Next
                If sqlSubWhere <> "" Then
                    sqlWhere = sqlWhere & "and(" & sqlSubWhere & ")"
                End If
            End If
        End If
        If (blockedSectionIdList <> "") Then
            Do While Left(blockedSectionIdList, 1) = ","
                blockedSectionIdList = Right(blockedSectionIdList, Len(blockedSectionIdList) - 1)
            Loop
            Do While Right(blockedSectionIdList, 1) = ","
                blockedSectionIdList = Left(blockedSectionIdList, Len(blockedSectionIdList) - 1)
            Loop
            If InStr(1, blockedSectionIdList, ",") = 0 Then
                sqlWhere = sqlWhere & "and (d.sectionid<>" & blockedSectionIdList & ")"
            Else
                sqlWhere = sqlWhere & "and not (d.sectionid in (" & blockedSectionIdList & "))"
            End If
        End If
        '
        ' ----- sections to include
        '
        If Not sectionAll Then
            '
            ' Integrate Topics in SQLSubWhere
            '
            sqlSubWhere = ""
            RecordIDArray = Split(sectionIdList, ",")
            RecordIDArrayCount = UBound(RecordIDArray) + 1
            If RecordIDArrayCount > 0 Then
                For ArrayPointer = 0 To RecordIDArrayCount - 1
                    recordId = kmaEncodeInteger(RecordIDArray(ArrayPointer))
                    If recordId <> 0 Then
                        If sqlSubWhere <> "" Then
                            sqlSubWhere = sqlSubWhere & "OR"
                        End If
                        sqlSubWhere = sqlSubWhere & "(d.sectionid=" & recordId & ")"
                    End If
                Next
                If sqlSubWhere <> "" Then
                    sqlWhere = sqlWhere & "and(" & sqlSubWhere & ")"
                End If
            End If
        End If
        '
        ' Get results
        '
        'SQLNoLanguageSubWhere = "Select Min(d.ID) as ID from ccSpiderDocs d where (" & SQLNoLanguageSubWhere & ") group by d.PageID,d.name"
        sql = "SELECT d.ID AS ID, d.Name AS LinkLabel, d.Link AS Link, d.TextOnlyFilename as BriefFilename, d.dateLastModified as dateLastModified, d.MetaKeywords as MetaKeywords, d.MetaDescription as MetaDescription, d.Length as Length, d.BodyText as BodyText, d.PrimaryImageLink as PrimaryImageLink, d.PrimaryImageWidth as PrimaryImageWidth, d.PrimaryImageHeight as PrimaryImageHeight" _
            & " FROM " & sqlFrom _
            & " WHERE " & sqlWhere _
            & " ORDER BY d.dateLastModified Desc;"
        OpenCSTextSearch = Main.OpenCSSQL("Default", sql, PageSize, PageNumber)
    End If
    '
    Exit Function
    '
    ' ----- Error Trap
    '
ErrorTrap:
    Call HandleError("SpiderAO.TextSearchClass", "OpenCSTextSearch", Err.Number, Err.Source, Err.Description, True, False)
End Function
'
' ----- Load the Word Search Exclude List
'
Private Function GetWordSearchExcludeList() As String
    On Error GoTo ErrorTrap
    '
    Dim MethodName As String
    Const WordSearchExcludeListFilename = "WordSearchExcludeList.txt"
    '
    MethodName = "GetWordSearchExcludeList"
    '
    GetWordSearchExcludeList = Main.ReadVirtualFile(WordSearchExcludeListFilename) & vbCrLf
    '
    Exit Function
    '
ErrorTrap:
    Call HandleError("SpiderAO.TextSearchClass", "GetWordSearchExcludeList", Err.Number, Err.Source, Err.Description, True, False)
End Function

