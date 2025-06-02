using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;
using System.Xml.Linq;
using Tabula.Detectors;
using Tabula.Extractors;
using Tabula;
using VTBpdfReportConverter.Converter;

namespace VTBpdfTOcsv
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            string pdfFilepath = Filepath.Text;
            StringBuilder result = new StringBuilder();

            if (!File.Exists(pdfFilepath)) return;

            ReportConverter reportConverter = new ReportConverter(pdfFilepath);
            result.AppendLine(reportConverter.GetOFX());

            OutputTextBox.Text = result.ToString();
        }

        private void Openfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF файл|*.pdf";
            openFileDialog.CheckFileExists = true;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() != true) return;

            if (!String.IsNullOrEmpty(openFileDialog.FileName))
            {
                Filepath.Text = openFileDialog.FileName;
            }
        }
    }
}