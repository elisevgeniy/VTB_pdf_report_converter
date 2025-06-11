using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ReportConverterLib.Converter;
using ReportConverterLib.Exceptions;
using ReportConverterLib.Models;

namespace WindowsApp
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

            if (!File.Exists(pdfFilepath)) return;
            
            if (_formatType == FormatType.QIF) {
                OutputLabel.Text = $"Формат {_formatType} ещё не поддерживается";
                return;
            }
            
            try
            {
                ReportConverter reportConverter = new ReportConverter(pdfFilepath);
                reportConverter.SetExportFormat(_formatType);
                
                if (cbOutput.IsChecked ?? false)
                {
                    OutputTextBox.Visibility = Visibility.Visible;
                    OutputTextBox.Text = reportConverter.ConvertToString();
                }

                if (cbFile.IsChecked ?? false)
                {
                    string format = "." + _formatType.ToString();
                    string saveOfxToFile = reportConverter.ConvertToFile(pdfFilepath.Replace(".pdf", format));
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