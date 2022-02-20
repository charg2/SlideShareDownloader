using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

namespace SlideShareDownloader;

///--------------------------------------------------------------------------------
///
/// @brief 슬라이드 쉐어 이미지 다운로더
/// 
///--------------------------------------------------------------------------------
public class App : Mala.Core.Singleton<App>
{
    public List<string> ImgSrcLinkList  { get; set; } //< 이미지 링크 컨테이너
    public Regex        LinkValidator   { get; set; } //< 링크 유효성 검사기
    public string       Url             { get; set; } //< PPT 링크
    public HtmlWeb      Web             { get; set; } //< HTML 웹 페이지 정보
    public WebClient    WebClient       { get; set; } //< 웹 클라이언트
    public string       SavePath        { get; set; } //< 저장 위치

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
        ImgSrcLinkList = new List<string>();
        Url            = String.Empty;
        LinkValidator  = new Regex( "(https://){0,1}([a-zA-Z]*.){0,1}slideshare.net/[a-zA-Z0-9-./]+" );
        Web            = new HtmlWeb();
        WebClient      = new WebClient();
        SavePath       = String.Empty;

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
        Url = String.Empty;
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief 다운로드를 한다.
    /// 
    /// @url   슬라이드의 url
    /// 
    ///--------------------------------------------------------------------------------
    public bool Download( string url )
    {
        if ( false == _CheckUrlUsingRegax( url ) )
            return false;

        var htmlDocument = Web.Load( Url );
        if ( false == ExtractImgLinksFromHtmlDoc( htmlDocument ) )
            return false;

        if ( false == _RecieveImg() )
            return false;

        return true;
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief 이미지를 받는다.
    /// 
    ///--------------------------------------------------------------------------------
    private bool _RecieveImg()
    {
        int counter = 0;
        foreach ( var imgLink in ImgSrcLinkList )
        {
            // TODO : 비동기 태스크 방식으로..
            WebClient.DownloadFile( new Uri( imgLink ), SavePath + $"slide_share{ counter++ }.jpg" );

            Console.WriteLine( $" { counter } / { ImgSrcLinkList.Count }" );
        }

        return true;
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief  정규 표현식을 통해서 Url을 검증한다.
    /// 
    ///--------------------------------------------------------------------------------
    private bool _CheckUrlUsingRegax( string url )
    {
        if ( true == string.IsNullOrEmpty( url ) )
            return false;

        var UrlValidator = new Regex( "(https://){0,1}([a-zA-Z]*.){0,1}slideshare.net/[a-zA-Z0-9-./]+" );
        if ( false == UrlValidator.IsMatch( url ) )
            return false;

        Url = url;

        return true;
    }

    ///--------------------------------------------------------------------------------
    ///
    /// @brief Html Document로부터 Img링크를 추출한다.
    /// 
    ///--------------------------------------------------------------------------------
    public bool ExtractImgLinksFromHtmlDoc( HtmlDocument htmlDocument )
    {
        // 1. 바디 노드 분리
        var bodyNode            = htmlDocument.DocumentNode.SelectSingleNode( "html/body" );
        // 2. 슬라이드 페이지의 컨테이너 노드들 분리
        var slideContainerNodes = bodyNode.SelectNodes( "//div[@id='slide-container']/div" );

        Action< HtmlNode > findAndCollectSrcset = ( HtmlNode node ) =>
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
            findAndCollectSrcset( slideNode1 );

            foreach ( var slideNode2 in slideNode1.ChildNodes )
            {
                findAndCollectSrcset( slideNode2 );

                foreach ( var slideNode3 in slideNode2.ChildNodes )
                {
                    findAndCollectSrcset( slideNode3 );
                }
            }
        }

        return ImgSrcLinkList.Count > 0;
    }

}

