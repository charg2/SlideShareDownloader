using HtmlAgilityPack;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text.RegularExpressions;

namespace SlideShareDownloader;

///--------------------------------------------------------------------------------
///
/// @brief 슬라이드 쉐어 이미지 다운로더
/// 
///--------------------------------------------------------------------------------
public class App : Mala.Core.Singleton< App >
{
    public Dictionary< string, SlideItem > Slides       { get; set; } = new(); //< 슬라이드 컨테이너
    public Regex                           UrlValidator { get; set; }          //< URL 유효성 검사기
    public HtmlWeb                         Web          { get; set; } = new(); //< HTML 웹 페이지 정보
    public HttpClient                      HttpClient   { get; set; } = new(); //< 웹 클라이언트

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
        UrlValidator = new Regex( "(https://){0,1}([a-zA-Z]*.){0,1}slideshare.net/[a-zA-Z0-9-./]+" );

        return true;
    }
    
    ///--------------------------------------------------------------------------------
    ///
    /// @brief 재활용을 위해 초기화한다. Initialize() 이후
    /// 
    ///--------------------------------------------------------------------------------
    public void Reset()
    {
        Slides.Clear();
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

        var slideItem = new SlideItem(){ Link = url };
        Slides.Add( url, slideItem );

        // 3. HTML 문서에서 Img링크 추출
        if ( !_ExtractImgLinksFromHtmlDoc( htmlDocument, slideItem ) )
            return false;

        // 4. 폴더가 없다면 폴더를 생성한다
        CreateDirectoryIfNotExist( htmlDocument, slideItem );

        var task = _DownloadImgAsync( slideItem );
        task.Wait();

        return true;
    }

    private void CreateDirectoryIfNotExist( HtmlDocument htmlDocument, SlideItem slideItem )
    {
        var TitleNode = htmlDocument.DocumentNode.SelectSingleNode( "html/head/title" );

        slideItem.Title = TitleNode.InnerText;
        if ( !Directory.Exists( slideItem.Title ) )
        {
            var invalidChars = Path.GetInvalidFileNameChars();

            foreach ( var ch in invalidChars )
                slideItem.Title = slideItem.Title.Replace( ch, '_' );

            Directory.CreateDirectory( slideItem.Title );
        }
    }

    /// --------------------------------------------------------------------------------
    /// 
    ///  @brief 이미지를 받는다.
    ///  
    /// --------------------------------------------------------------------------------
    /// <param name="slideItem"></param>
    private async Task _DownloadImgAsync( SlideItem slideItem )
    {
        var httpTaskList = new List< Task< HttpResponseMessage > >();
        foreach ( var imgLink in slideItem.ImgSrcLinks )
        {
            var task = HttpClient.GetAsync( imgLink );
            httpTaskList.Add( task );
        }

        Task.WaitAll( httpTaskList.ToArray() );

        var downloadTaskList = new List< Task< byte[] > >();
        foreach ( var httpTask in httpTaskList )
        {
            var task = httpTask.Result.Content.ReadAsByteArrayAsync();
            downloadTaskList.Add( task );
        }

        Task.WaitAll( downloadTaskList.ToArray() );


        int counter       = 0;
        var writeTaskList = new List< Task >();
        foreach ( var downloadTask in downloadTaskList )
        {
            var task = File.WriteAllBytesAsync( $"{ slideItem.Title }/{ counter++ }.jpg", downloadTask.Result );
            writeTaskList.Add( task );
        }

        Task.WaitAll( writeTaskList.ToArray() );
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
    private bool _ExtractImgLinksFromHtmlDoc( HtmlDocument htmlDocument, SlideItem slideItem )
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
                            slideItem.ImgSrcLinks.Add( maximumSizeImgLink.Split( new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries )[ 0 ] );
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

        return slideItem.ImgSrcLinks.Count > 0;
    }

}

