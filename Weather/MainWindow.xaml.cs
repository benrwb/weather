using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace Weather
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer t = new Timer();


        public MainWindow()
        {
            InitializeComponent();

            textBox1.Text = GetWeather();

            t.Elapsed += t_Elapsed;
            t.Interval = 100;
            t.Enabled = true;
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            t.Enabled = false;

            string filename = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "out.bmp");
            
            Dispatcher.Invoke((Action)delegate { Screenshot(textBox1, filename); });
            
            SetWallpaper(filename);
            
            Dispatcher.Invoke((Action)delegate { this.Close(); });
        }




        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, 
            int uParam, string lpvParam, int fuWinIni);

        const int SPI_SETDESKWALLPAPER = 0x14;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        void SetWallpaper(string filename)
        {
            // The wallpaper will be Centered (as opposed to Tiled or Stretched)
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                // http://technet.microsoft.com/en-us/library/cc978626.aspx
                key.SetValue("TileWallpaper", "0");
                key.SetValue("WallpaperStyle", "0"); 
            }

            // Set wallpaper
            SystemParametersInfo(SPI_SETDESKWALLPAPER,
                0,
                filename,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }




        public string GetWeather()
        {
            var sb = new StringBuilder();

            var uri = "http://www.metoffice.gov.uk/public/data/PWSCache/RegionalForecast/Area/wm?v=3.10";
            var req = (HttpWebRequest)WebRequest.Create(uri);

            var response = (HttpWebResponse)req.GetResponse();
            var xml = new StreamReader(response.GetResponseStream()).ReadToEnd();

            XDocument doc = XDocument.Parse(xml);
            var issuedAt = Convert.ToDateTime(doc.Root.Attribute("issuedAt").Value);
            sb.Append("Issued at " + issuedAt.ToString());
            sb.AppendLine("");
            sb.AppendLine("");
            sb.AppendLine("");

            // Have to use LocalName because the doc has a namespace
            // http://stackoverflow.com/q/19142606/58241

            foreach (var paragraph in doc.Descendants().Where(el => 
                el.Name.LocalName == "Period" && (string)el.Attribute("id") == "day1to2")
                .Elements())
            {
                sb.AppendLine(paragraph.Attribute("title").Value);
                sb.AppendLine("");
                sb.AppendLine(paragraph.Value);
                sb.AppendLine("");
                sb.AppendLine("");
            }
            return sb.ToString();
        }



        void Screenshot(Control control, string filename)
        {
            var dpi = 150;

            // render InkCanvas' visual tree to the RenderTargetBitmap
            Rect bounds = VisualTreeHelper.GetDescendantBounds(control);
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96.0),
                                                            (int)(bounds.Height * dpi / 96.0),
                                                            dpi,
                                                            dpi,
                                                            PixelFormats.Pbgra32);
            rtb.Render(control);

            // add the RenderTargetBitmap to a Bitmapencoder
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            // save file to disk
            using (FileStream fs = File.Open(filename, FileMode.OpenOrCreate))
            {
                encoder.Save(fs);
            }
        }

        
    }
}
