using HtmlAgilityPack;
using System.IO;
using System.Text.RegularExpressions;

namespace SlideShareDownloader;

///--------------------------------------------------------------------------------
///
/// @brief 슬라이드 쉐어 이미지 다운로더
/// 
///--------------------------------------------------------------------------------
public class App : Mala.Core.Singleton<App>
{
    public List< string > ImgSrcLinkList  { get; set; } //< 이미지 링크 컨테이너
    public Regex          UrlValidator    { get; set; } //< URL 유효성 검사기
    public HtmlWeb        Web             { get; set; } //< HTML 웹 페이지 정보
    public HttpClient     HttpClient      { get; set; } //< 웹 클라이언트
    public string         SavePath        { get; set; } //< 저장 위치
    public string         SlideTitle      { get; set; } //< Slide 제목

    ///--------------------------------------------------------------------------------
    ///
    /// @brief 생성자
    /// 
    ///--------------------------------------------------------------------------------
    public App()
    {
        var _ = Initialize();
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief  초기화한다
    /// 
    /// @return 초기화 성공 여부
    /// 
    ///--------------------------------------------------------------------------------
    public bool Initialize()
    {
        ImgSrcLinkList = new List< string >();
        UrlValidator   = new Regex( "(https://){0,1}([a-zA-Z]*.){0,1}slideshare.net/[a-zA-Z0-9-./]+" );
        Web            = new HtmlWeb();
        HttpClient     = new HttpClient();
        SavePath       = string.Empty;

        return true;
    }
    
    ///--------------------------------------------------------------------------------
    ///
    /// @brief 재활용을 위해 초기화한다. Initialize() 이후
    /// 
    ///--------------------------------------------------------------------------------
    public void Reset()
    {
        ImgSrcLinkList.Clear();
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief 다운로드 한다.
    /// 
    /// @url   슬라이드의 url
    /// 
    ///--------------------------------------------------------------------------------
    public bool Download( string url )
    {
        // 1. URL을 검증한다.
        if ( !_CheckUrlUsingRegax( url ) )
            return false;

        // 2. URL을 통해 페이지 HTML 문서를 읽어들임
        var htmlDocument = Web.Load( url );

        // 3. HTML 문서에서 Img링크 추출
        if ( !_ExtractImgLinksFromHtmlDoc( htmlDocument ) )
            return false;

        // 4. 폴더가 없다면 폴더를 생성한다
        CreateDirectoryIfNotExist( htmlDocument );

        _DownloadImgAsync();

        return true;
    }

    private void CreateDirectoryIfNotExist( HtmlDocument htmlDocument )
    {
        var TitleNode = htmlDocument.DocumentNode.SelectSingleNode( "html/head/title" );

        SlideTitle = TitleNode.InnerText;

        if ( !Directory.Exists( SlideTitle ) )
        {
            var invalidChars = Path.GetInvalidFileNameChars();

            foreach ( var ch in invalidChars )
                SlideTitle = SlideTitle.Replace( ch, '_' );

            Directory.CreateDirectory( SlideTitle );
        }

    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief 이미지를 받는다.
    /// 
    ///--------------------------------------------------------------------------------
    private async Task _DownloadImgAsync()
    {
        int counter  = 0;
        var taskList = new List< Task >();

        foreach ( var imgLink in ImgSrcLinkList )
        {
            var response = await HttpClient.GetAsync( imgLink );

            byte[] responseContent = await response.Content.ReadAsByteArrayAsync();
            
            var task = File.WriteAllBytesAsync( $"{ SlideTitle }/{ counter++ }.jpg", responseContent );
            taskList.Add( task );
        }

        Task.WaitAll( taskList.ToArray() );
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief  정규 표현식을 통해서 Url을 검증한다.
    /// 
    /// @url    검증할 url
    /// 
    ///--------------------------------------------------------------------------------
    private bool _CheckUrlUsingRegax( string url )
    {
        if ( string.IsNullOrEmpty( url ) )
            return false;

        var UrlValidator = new Regex( "(https://){0,1}([a-zA-Z]*.){0,1}slideshare.net/[a-zA-Z0-9-./]+" );
        if ( !UrlValidator.IsMatch( url ) )
            return false;

        return true;
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief        Html Document로부터 Img링크를 추출한다.
    /// 
    /// @htmlDocument 추출을 진행할 Html Document 객체
    /// 
    ///--------------------------------------------------------------------------------
    private bool _ExtractImgLinksFromHtmlDoc( HtmlDocument htmlDocument )
    {
        // 1. 바디 노드 분리
        var bodyNode = htmlDocument.DocumentNode.SelectSingleNode( "html/body" );

        // 2. 바디 노드중 슬라이드 페이지의 컨테이너 노드 분리
        var slideContainerNodes = bodyNode.SelectNodes( "//div[@id='slide-container']/div" );

        // 
        Action< HtmlNode > findAndCollectSrcsetTask = ( HtmlNode node )=>
            {
                foreach ( var attribute in node.Attributes )
                {
                    if ( attribute.Name == "srcset" )
                    {
                        var imgSrcLinks = attribute.Value.Split( ',' );
                        if ( imgSrcLinks.Length != 0 )
                        {
                            string maximumSizeImgLink = imgSrcLinks[ imgSrcLinks.Length - 1 ];
                            ImgSrcLinkList.Add( maximumSizeImgLink.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries )[ 0 ] );
                        }
                    }
                }
            };

        // 하... 혼잡한 HTML의 계층구조...
        foreach ( var slideNode1 in slideContainerNodes )
        {
            findAndCollectSrcsetTask( slideNode1 );

            foreach ( var slideNode2 in slideNode1.ChildNodes )
            {
                findAndCollectSrcsetTask( slideNode2 );

                foreach ( var slideNode3 in slideNode2.ChildNodes )
                    findAndCollectSrcsetTask( slideNode3 );
            }
        }

        return ImgSrcLinkList.Count > 0;
    }

}

