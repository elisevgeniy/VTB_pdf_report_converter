using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using VTBpdfReportConverter.Converter;
using VTBpdfReportConverter.Exceptions;

namespace VTBpdfReportConverter
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

            try
            {
                ReportConverter reportConverter = new ReportConverter(pdfFilepath);
                result.AppendLine(reportConverter.GetOFX());
                string saveOfxToFile = reportConverter.SaveOFXToFile(pdfFilepath.Replace(".pdf", ".ofx"));

                OutputTextBox.Text = result.ToString();
                OutputLabel.Text = $"Файл в OFX формате сохранён по пути: {saveOfxToFile}";
            }
            catch (ConvertException ce)
            {
                OutputLabel.Text = $"Ошибка конвертации: {ce.Message}";
            }
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