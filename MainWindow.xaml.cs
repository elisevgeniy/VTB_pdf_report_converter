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

            using (PdfDocument document = PdfDocument.Open(pdfFilepath, new ParsingOptions() { ClipPaths = true }))
            {
                for (int i = 1; i <= document.NumberOfPages; i++)
                {
                    result.AppendLine($"Page #{i}");
                    result.AppendLine();

                    PageArea page = ObjectExtractor.Extract(document, i);

                    IExtractionAlgorithm ea = new SpreadsheetExtractionAlgorithm();
                    IReadOnlyList<Table> tables = ea.Extract(page);

                    result.AppendLine($"Table count: {tables.Count}");

                    foreach (var table in tables)
                    {
                        result.AppendLine($"Table rows: {table.RowCount}, columns: {table.ColumnCount}");
                        for (int r = 0; r < table.RowCount; r++)
                        {
                            for (int c = 0; c < table.ColumnCount; c++)
                            {
                                result.Append($"{table[r, c].ToString().Replace("\r", "")}\t\t\t");
                            }
                            result.AppendLine();
                            result.AppendLine();
                        }
                        result.AppendLine();
                        result.AppendLine();
                        result.AppendLine();
                    }
                }
            }

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