namespace SlideShareDownloader;

public record SlideItem
{
    public string         Link        { get; set; } = string.Empty; //< 링크
    public string         Title       { get; set; } = string.Empty; //< 제목
    public string         Path        { get; set; } = string.Empty; //< 저장 경로
    public List< string > ImgSrcLinks { get; set; } = new();        //< 이미지 링크 컨테이너
}