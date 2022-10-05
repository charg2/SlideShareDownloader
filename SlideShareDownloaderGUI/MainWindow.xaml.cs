using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlideShareDownloaderGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _url = string.Empty; //< url

        public MainWindow()
        {
            InitializeComponent();

            listBox.ItemsSource = new List< Item >() 
                {
                    new (){ Name = "ABC", Link = "www.naver.com", Progressed = 33, Max = 100 },
                    new (){ Name = "CDE", Link = "www.kakao.com", Progressed = 33, Max = 100 },
                    new (){ Name = "EFG", Link = "www.google.com", Progressed = 78, Max = 100 }
                };
        }

        /// <summary>
        /// 다운로드 버튼 클릭시 호출
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_StartDownload( object sender, RoutedEventArgs e )
        {
            if ( SlideShareDownloader.App.Instance.Download( _url ) )
                ;
        }

        /// <summary>
        /// 텍스트 박스의 텍스트가 변경시 호출
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged( object sender, RoutedEventArgs e )
        {
            TextBox textBox = sender as TextBox;
            _url = textBox.Text;
        }

        private void ListBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
        }

        public class Slide
        {
            public string Name       { get; set; } = string.Empty;
            public int    Downloaded { get; set; } = 0;
        }

        private void TextBox_TextChanged( object sender, TextChangedEventArgs e )
        {

        }

        public record Item
        {
            public string Name       { get; set; } = string.Empty;
            public string Link       { get; set; } = string.Empty;
            public int    Progressed { get; set; } = 0;
            public int    Max        { get; set; } = 100;
        }

    }
}
