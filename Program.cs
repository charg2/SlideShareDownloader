Console.WriteLine( "Url을 입력해주세요 :" );
string url = Console.ReadLine();

if ( SlideShareDownloader.App.Instance.Download( url ) )
    Console.WriteLine( "다운로드 성공" );
else
    Console.WriteLine( "다운로드 실패" );

 