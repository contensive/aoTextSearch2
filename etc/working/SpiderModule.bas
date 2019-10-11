Attribute VB_Name = "SpiderModule"

Option Explicit
'
' Doc Errors
'
'Public Const DocErrorUnknown = 1000
Public Const DocErrorTooLarge = 1001
Public Const DocError400BadRequest = 1002
Public Const DocErrorUnknownHTTPResponseCode = 1003
Public Const DocErrorMultipleTitles = 1004
Public Const DocErrorParsingTitle = 1005
Public Const DocErrorDefaultTitleFound = 1006
Public Const DocErrorNoTitleFound = 1007
Public Const DocErrorParsingHTML = 1008
Public Const DocErrorBadURL = 1009
''
'' Buttons
''
'Public Const ButtonEnable = " Enable "
'Public Const ButtonDisable = " Disable "
'
' Task value defaults
'
Public Const URLOnSiteDefault = False
Public Const DocLinkCountMaxDefault = 1000
Public Const URIRootLimitDefault = 1
Public Const HonorRobotsDefault = 1
Public Const CookiesPageToPageDefault = 1
Public Const CookiesVisitToVisitDefault = 1
Public Const AuthUsernameDefault = ""
Public Const AuthPasswordDefault = ""
'
' Socket Request Block
'
Public Type RobotPathType
    Agent As String
    Path As String
    End Type
'
Public Type DocType
                                        ' Initialize before socket call
    URI As String                       ' <scheme>://<user>:<Password>@<host>:<Port>/<url-path>?<Query>
    URIScheme As String                 ' http,ftp, etc. (semi supported)
    URIUser As String                   ' (not supported yet)
    URIPassword As String               ' (not supported yet)
    URIPort As String                   ' (not supported yet)
    URIHost As String                   '
    URIPath As String                   '
    URIPage As String                   '
    URIQuery As String                  ' (not supported yet - stuck to path)
    ID As Long                          ' ID in database for this doc record
    DontGet As Boolean                ' if true, do not request or analyze the document
                                        ' values set by socket
    SocketResponse As String            ' Socket Response (if not "", socket error)
    HTTPResponse As String              ' HTTP response (version,code,description)
    HTTPResponseCode As String              ' HTTP response Code (200, etc)
    ResponseTime As Double              ' time to fetch this page
    RequestFilename As String           ' the client request (only saved if testing)
    ResponseFilename As String          ' the server response
    ResponseFileNumber As Long       ' filenumber for the response file
    EntityStart As Long                 ' character count of the first byte of entity in the response file
    ResponseFileLength As Long                      ' length of content read in from HTTP
    RetryCountAuth As Long           ' retires for authorization
    TextOnlyFilename As String          '
    RetryCountTimeout As Long        ' retires for timeouts
                                        ' Set by SpiderLink_AnalyzeDoc routine
    Found As Boolean                    ' if true, doc was found and read
    OffSite As Boolean                  ' true if URI is not on Host being tested, set during SpiderLink_AnalyzeDoc_AddLink
    HTML As Boolean                     ' content_type is html/text
    Title As String                     '
    MetaKeywords As String              '
    MetaDescription As String           '
    UnknownWordList As String           ' List not found by spell checker
                                        ' accumulated errors and warnings
    ErrorCount As Long                  ' count of site errors
    End Type
'
'
'
Public Sub HandleSpiderError(MethodName As String, Optional ResumeNext As Boolean)
    Call HandleError("SpiderForm", MethodName, Err.Number, Err.Source, Err.Description, True, ResumeNext)
    ' ##### added the error clear so if a resume next is included, the error is cleared before returning
    Err.Clear
    End Sub

