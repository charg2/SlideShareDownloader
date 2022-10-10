using HtmlAgilityPack;
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
    public Queue< SlideItem >              WaitingQueue    { get; set; } = new();
    public bool                            Completed    => WaitingQueue.Count == 0;
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
    public bool Download( string url, bool waitUntilComplete = false )
    {
        // 1. URL을 검증한다.
        if ( !_CheckUrlUsingRegax( url ) )
            return false;

        // 2. URL을 통해 페이지 HTML 문서를 읽어들임( 동기식, 1차 병목 )
        var htmlDocument = Web.Load( url );

        // 3.
        var slideItem = new SlideItem(){ Link = url };
        if ( !Slides.TryAdd( url, slideItem ) )
        {
            Console.WriteLine( "이미 등록된 슬라이드" );
            return false;
        }

        // 4. HTML 문서에서 Img링크와 타이틀 추출
        if ( !slideItem.ExtractTitleFrom   ( htmlDocument ) ||
             !slideItem.ExtractImgLinksFrom( htmlDocument ) )
            return false;

        // 5. 폴더가 없다면 폴더를 생성한다
        CreateDirectoryIfNotExist( slideItem.Title );

        // 6. 스레드풀에 다운로드 위임
        slideItem.DownloadTask = Task.Run( () => _DownloadImgAsync( slideItem ) );

        if ( waitUntilComplete )
            slideItem.DownloadTask.Wait();
        else
            WaitingQueue.Enqueue( slideItem );

        return true;
    }

    public void Update( Action< SlideItem > onCompleted )
    {
        if ( WaitingQueue.TryPeek( out var slide ) )
        {
            if ( slide.IsCompleted )
            {
                WaitingQueue.Dequeue();

                onCompleted( slide );
            }
        }
    }

    private void CreateDirectoryIfNotExist( string title )
    {
        if ( !Directory.Exists( title ) )
            Directory.CreateDirectory( title );
    }

    /// --------------------------------------------------------------------------------
    /// 
    ///  @brief 이미지를 받는다.
    ///  
    /// --------------------------------------------------------------------------------
    /// <param name="slideItem"></param>
    private async Task _DownloadImgAsync( SlideItem slideItem )
    {
        int          counter  = 0;
        List< Task > taskList = new();
        foreach ( var imgLink in slideItem.ImgSrcLinks )
        {
            int counterCapture = counter;
            var downloadTask = Task.Run( async () =>
            {
                var httpResponseMessage = await HttpClient.GetAsync( imgLink );

                var imgBytes = await httpResponseMessage.Content.ReadAsByteArrayAsync();

                await File.WriteAllBytesAsync( $"{ slideItem.Title }/{ counterCapture }.jpg", imgBytes );
            } );

            counter += 1;
            taskList.Add( downloadTask );
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

}

