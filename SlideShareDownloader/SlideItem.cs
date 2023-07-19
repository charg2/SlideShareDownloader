using System.Dynamic;
using HtmlAgilityPack;

namespace SlideShareDownloader;

public record SlideItem
{
    public string         Link         { get; set; } = string.Empty; //< 링크
    public string         Title        { get; set; } = string.Empty; //< 제목
    public string         Path         { get; set; } = string.Empty; //< 저장 경로
    public List< string > ImgSrcLinks  { get; set; } = new();        //< 이미지 링크 컨테이너
    public Task           DownloadTask { get; set; }                 //< 작업
    public int            TotalPage    => ImgSrcLinks.Count;         //< 총 장표 수
    public bool           IsCompleted    => DownloadTask.IsCompleted;  //< 완료 여부

    // Html Document로부터 Img링크를 추출한다.
    public bool ExtractImgLinksFrom( HtmlDocument htmlDoc )
    {
         // 1. 바디 노드 분리
        var bodyNode = htmlDoc.DocumentNode.SelectSingleNode( "html/body" );

        // 2. 이미지를 감싸고 있는 노드를 획득한다.
        var slideContainerNodes = bodyNode.SelectNodes( "//picture[@data-testid='slide-image-picture']/source" ).ToList();

        var findAndCollectSrcsetTask = ( HtmlNode node )=>
            {
                foreach ( var attribute in node.Attributes )
                {

                    if ( attribute.Name == "srcset" )
                    {
                        var imgSrcLinks = attribute.Value.Split( ',' );
                        if ( imgSrcLinks.Length != 0 )
                        {
                            string maximumSizeImgLink = imgSrcLinks[ imgSrcLinks.Length - 1 ];
                            ImgSrcLinks.Add( maximumSizeImgLink.Split( new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries )[ 0 ] );
                        }
                    }
                }
            };

        var findAndCollectSrcsetTask = ( HtmlNode node )=>
            {
                foreach ( var attribute in node.Attributes )
                {

                    if ( attribute.Name == "srcset" )
                    {
                        var imgSrcLinks = attribute.Value.Split( ',' );
                        if ( imgSrcLinks.Length != 0 )
                        {
                            string maximumSizeImgLink = imgSrcLinks[ imgSrcLinks.Length - 1 ];
                            ImgSrcLinks.Add( maximumSizeImgLink.Split( new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries )[ 0 ] );
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

        return ImgSrcLinks.Count > 0;
    }

    // Html Document로부터 타이틀을 추출한다.
    public bool ExtractTitleFrom( HtmlDocument htmlDocument )
    {
        var TitleNode = htmlDocument.DocumentNode.SelectSingleNode( "html/head/title" );

        string title = TitleNode.InnerText;

        var invalidChars = System.IO.Path.GetInvalidFileNameChars();

        foreach ( var ch in invalidChars )
            title = title.Replace( ch, '_' );

        Title = title;

        return !string.IsNullOrEmpty( title );
    }
}
