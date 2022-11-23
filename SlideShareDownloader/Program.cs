using System.Diagnostics;


Console.Write( "Url을 입력해주세요: " );
string url = Console.ReadLine();

var watch = Stopwatch.StartNew();

bool success = SlideShareDownloader.App.Instance.Download( url, true );
Console.WriteLine( success ? "다운로드 성공" : "다운로드 실패" );

watch.Stop();
Console.WriteLine( watch.ElapsedMilliseconds );

//var consoleApp = new ConsoleApp();
//consoleApp.Run();