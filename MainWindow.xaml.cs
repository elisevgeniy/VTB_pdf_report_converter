using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VTBpdfReportConverter.Converter;
using VTBpdfReportConverter.Exceptions;
using VTBpdfReportConverter.Models;

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

        private FormatType _formatType = FormatType.OFX;
        
        private void ProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            
            string pdfFilepath = Filepath.Text;
            StringBuilder result = new StringBuilder();

            if (!File.Exists(pdfFilepath)) return;
            
            try
            {
                ReportConverter reportConverter = new ReportConverter(pdfFilepath);

                switch (_formatType)
                {
                    case FormatType.OFX:
                        result.AppendLine(reportConverter.GetOFX());
                        break;
                    case FormatType.QIF:
                    case FormatType.CSV:
                        OutputLabel.Text = $"Формат {_formatType} ещё не поддерживается";
                        break;
                }

                if (cbOutput.IsChecked ?? false)
                {
                    OutputTextBox.Visibility = Visibility.Visible;
                    OutputTextBox.Text = result.ToString();
                }

                if (cbFile.IsChecked ?? false)
                {
                    string format = "." + _formatType.ToString();
                    string saveOfxToFile = reportConverter.SaveOFXToFile(pdfFilepath.Replace(".pdf", format));
                    OutputLabel.Text = $"Файл в {format} формате сохранён по пути: {saveOfxToFile}";
                }
            }
            catch (ConvertException ce)
            {
                OutputLabel.Text = $"Ошибка конвертации: {ce.Message}";
            }
        }

        private void ResetUI()
        {
            OutputTextBox.Visibility = Visibility.Collapsed;
            OutputLabel.Text = "";
            OutputTextBox.Text = "";
        }

        private void Openfile_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
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

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            var button = (RadioButton)sender;
            _formatType = button.Content?.ToString() switch
            {
                "OFX" => FormatType.OFX,
                "CSV" => FormatType.CSV,
                "QIF" => FormatType.QIF,
                _ => FormatType.OFX
            };
        }
    }
}