using System.Collections.Concurrent;

public class ConsoleApp
{
    public volatile bool                      _canRun = true;
    private         ConcurrentQueue< string > _urls   = new();

    public void Run()
    {
        ThreadPool.QueueUserWorkItem( ( _ ) =>
        {
            while ( _canRun ) 
                Update();
        } );

        while ( _canRun )
            Input();
    }

    private void Input()
    {
        Console.Write( "URL을 입력해주세요.: " );
        _urls.Enqueue( Console.ReadLine() );
    }

    void Update()
    {
        Thread.Sleep( 500 );

        SlideShareDownloader.App.Instance.Update( ( slide ) =>
        {
            string resultText = $"{ slide.Title } ";
            resultText += slide.DownloadTask.IsCompletedSuccessfully ? "다운로드 성공" : "다운로드 실패";

            Console.WriteLine( resultText );
            Console.Write( "URL을 입력해주세요.: " );
        } );

        if ( _urls.TryDequeue( out var url ) )
            SlideShareDownloader.App.Instance.Download( url );
    }
};