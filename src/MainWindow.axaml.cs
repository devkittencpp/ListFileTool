using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using System.Windows.Input; // For ICommand

// Ensure this using directive matches the namespace where your file handlers are defined.
using ListFileTool.FileHandlers;

namespace ListFileTool
{
    public partial class MainWindow : Window
    {
        // Config file path (adjust if needed)
        private const string ConfigFilePath = "config.ini";

        // Use case-insensitive HashSets to avoid duplicate entries regardless of casing.
        public HashSet<string> ListFileElem { get; private set; }
        public List<string> DataFiles { get; private set; }

        // ObservableCollection for the ListBox's items.
        private ObservableCollection<string> _listFilesCollection;

        public MainWindow()
        {
            InitializeComponent();

            this.Icon = new WindowIcon("assets/appicon.ico");

            // Initialize collections with case-insensitive comparers where applicable.
            ListFileElem = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DataFiles = new List<string>();

            _listFilesCollection = new ObservableCollection<string>();

            txtLFFolder.Foreground = new SolidColorBrush(Colors.Green);

            txtCleanerInfo.Text = "";

            // Attempt to load configuration on startup.
            LoadConfig();
        }

        string listfilePath = GlobalStorage.ListFilePath;  // Update with your listfile path
        string sourceDirectory = GlobalStorage.DataFolderPath;    // Update with your source directory
        string targetDirectory = GlobalStorage.OutputFolderPath;     // Update with your target directory

        /// <summary>
        /// Loads settings from the config.ini file, if it exists, and updates global storage and UI elements.
        /// </summary>
        private void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    bool autoClean = false; // Local variable to hold the AutoClean value.
                    var lines = File.ReadAllLines(ConfigFilePath);
                    foreach (var line in lines)
                    {
                        // Skip empty lines or comments
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var tokens = line.Split(new char[] { '=' }, 2);
                        if (tokens.Length != 2)
                            continue;

                        var key = tokens[0].Trim();
                        var value = tokens[1].Trim();

                        switch (key)
                        {
                            case "OutputFolderPath":
                                GlobalStorage.OutputFolderPath = value;
                                break;
                            case "InputFolderPath":
                                GlobalStorage.InputFolderPath = value;
                                break;
                            case "DataFolderPath":
                                GlobalStorage.DataFolderPath = value;
                                break;
                            case "ListFilePath":
                                GlobalStorage.ListFilePath = value;
                                break;
                            case "AutoClean":
                                // Parse the value to a Boolean and update the local autoClean variable and checkbox.
                                if (bool.TryParse(value, out bool parsedAutoClean))
                                {
                                    autoClean = parsedAutoClean;
                                    autoCleanOption.IsChecked = parsedAutoClean;
                                }
                                break;
                            case "ClientListFileClean":
                                GlobalStorage.ClientListFileClean = value;
                                break;
                        }
                    }

                    // Update UI elements on the Settings tab and left panel
                    tbOutputFolder.Text = GlobalStorage.OutputFolderPath;
                    tbInputFolder.Text = GlobalStorage.InputFolderPath;
                    tbDataFolder.Text = GlobalStorage.DataFolderPath;
                    tbOutputListfile.Text = GlobalStorage.ListFilePath;

                    txtOutputFolder.Text = $"Output Folder: {GlobalStorage.OutputFolderPath}";
                    txtInputFolder.Text = $"Input Folder: {GlobalStorage.InputFolderPath}";
                    txtDataFolder.Text = $"Data Folder: {GlobalStorage.DataFolderPath}";
                    txtLFFolder.Text = GlobalStorage.ListFilePath;

                    // If AutoClean is true and ClientListFileClean is set, update the cleaner UI.
                    if (autoClean && !string.IsNullOrEmpty(GlobalStorage.ClientListFileClean))
                    {
                        autoCleanOption.IsChecked = true;
                        txtCleanerAlert.Text = "Auto File/Folder Cleaner is enabled";
                        txtCleanerInfo.Text = $"Selected file: {GlobalStorage.ClientListFileClean}";
                        txtCleanerInfo.Foreground = new SolidColorBrush(Colors.GreenYellow);
                        fileDirectory.Text = GlobalStorage.ClientListFileClean;
                    }
                    else
                    {
                        autoCleanOption.IsChecked = false;
                        txtCleanerAlert.Text = "";
                        txtCleanerInfo.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus("Error loading config: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Saves the current settings to a config.ini file.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(ConfigFilePath))
                {
                    writer.WriteLine($"OutputFolderPath={GlobalStorage.OutputFolderPath}");
                    writer.WriteLine($"InputFolderPath={GlobalStorage.InputFolderPath}");
                    writer.WriteLine($"DataFolderPath={GlobalStorage.DataFolderPath}");
                    writer.WriteLine($"ListFilePath={GlobalStorage.ListFilePath}");
                    writer.WriteLine($"AutoClean={(autoCleanOption.IsChecked == true ? "true" : "false")}");
                    writer.WriteLine($"ClientListFileClean={GlobalStorage.ClientListFileClean}");
                }
                UpdateStatus("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error saving settings: " + ex.Message);
            }
        }

        private async void load_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_listFilesCollection.Count == 0)
            {
                await ShowMessage("No file");
                return;
            }
            await RunSearchAsync();
        }

        // settings tab ui
        private async void SelectOutputListfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select Output Listfile";
            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                tbOutputListfile.Text = result[0];
                txtLFFolder.Text = result[0];
                GlobalStorage.ListFilePath = result[0];
            }
        }

        private async void SelectInputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            dialog.Title = "Select Input Folder";
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                // Store the result globally
                GlobalStorage.InputFolderPath = result;

                // Update the Settings tab TextBox
                tbInputFolder.Text = result;

                // Update the left panel TextBlock and change its color to green
                txtInputFolder.Text = $"Input Folder: {result}";
                txtInputFolder.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private async void SelectDataFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            dialog.Title = "Select Data Folder";
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                // Store the result globally
                GlobalStorage.DataFolderPath = result;

                // Update the Settings tab TextBox
                tbDataFolder.Text = result;

                // Update the left panel TextBlock and change its color to green
                txtDataFolder.Text = $"Data Folder: {result}";
                txtDataFolder.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private async void SelectOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            dialog.Title = "Select Output Folder";
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                // Store the result globally
                GlobalStorage.OutputFolderPath = result;

                // Update the Settings tab TextBox
                tbOutputFolder.Text = result;

                // Update the left panel TextBlock and change its color to green
                txtOutputFolder.Text = $"Output Folder: {result}";
                txtOutputFolder.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private async void btnProcessAll_Click(object? sender, RoutedEventArgs e)
        {
            GlobalStorage.FilePaths.Clear();

            UpdateStatus("Process complete!");

            // Validate that required paths are configured
            if (string.IsNullOrWhiteSpace(GlobalStorage.InputFolderPath) ||
                string.IsNullOrWhiteSpace(GlobalStorage.DataFolderPath) ||
                string.IsNullOrWhiteSpace(GlobalStorage.OutputFolderPath))
            {
                await ShowErrorMessage("Error: One or more paths are not configured in settings.");
                return; // Stop execution
            }

            if (!Directory.Exists(GlobalStorage.InputFolderPath))
            {
                Console.WriteLine("Directory does not exist.");
                await ShowErrorMessage($"{GlobalStorage.InputFolderPath} Directory does not exist.");

                return; // Stop execution
            }

            LoadingBar.IsVisible = true;  // Show loading bar

            UpdateStatus("Starting process...");

            // Process the Input folder
            ProcessLoadDirectory(GlobalStorage.InputFolderPath);
            UpdateStatus("Processing Input folder...");
            await RunSearchAsync();
            UpdateStatus("Search completed in Input folder. Copying files...");

            // Copy files after processing Input folder
            ListFileTool.FileExtractor.CopyFiles(
                GlobalStorage.ListFilePath,
                GlobalStorage.DataFolderPath,
                GlobalStorage.OutputFolderPath,
                UpdateStatus);

            // Clear collections after first file copy
            _listFilesCollection.Clear();
            ListFileElem.Clear();

            // Process the output folder without copying files this time
            UpdateStatus("Processing output folder...");
            ProcessLoadDirectory(GlobalStorage.OutputFolderPath);
            await RunSearchAsync();

            // Finalize and perform the last copy
            UpdateStatus("Finalizing...");
            ProcessLoadDirectory(GlobalStorage.OutputFolderPath);
            await RunSearchAsync();
            UpdateStatus("Final search completed. Copying final files...");

            ListFileTool.FileExtractor.CopyFiles(
                GlobalStorage.ListFilePath,
                GlobalStorage.DataFolderPath,
                GlobalStorage.OutputFolderPath,
                UpdateStatus);

            if (autoCleanOption.IsChecked == true && !string.IsNullOrEmpty(GlobalStorage.ClientListFileClean))
            {
                UpdateStatus("Cleaning up files...");
                try
                {
                    var cleaner = new FileCleaner.FolderCleaner(GlobalStorage.OutputFolderPath, GlobalStorage.ClientListFileClean);
                    int removedFiles = cleaner.RemoveFilesInList();
                    Console.WriteLine($"Removed {removedFiles} files.");
                    await ShowMessage($"Removed {removedFiles} files");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    await ShowMessage($"An error occurred: {ex.Message}");
                }
            }

            bool saveButtonExists = rightPanel.Children
                .OfType<Button>()
                .Any(b => b.Name == "SaveButton");

            if (!saveButtonExists)
            {
                // Create the Save button.
                var saveButton = new Button
                {
                    Name = "SaveButton", // Unique name to identify the button.
                    Content = "Save Settings",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                // Attach a click event handler.
                saveButton.Click += SaveButton_Click;

                // Add the Save button to the right-side panel (below ProcessAllButton).
                rightPanel.Children.Add(saveButton);
            }

            LoadingBar.IsVisible = false; // Hide loading bar
        }

        // Helper method to update UI
        private void UpdateStatus(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusTextBlock.Text = message; // Assuming you have a TextBlock named StatusTextBlock in XAML
            });
        }

        private async Task RunSearchAsync()
        {
            bool appendOutput = true;

            try
            {
                await Task.Run(() =>
                {
                    // Process each file in the collection.
                    foreach (var item in _listFilesCollection)
                    {
                        // Use the original file path (do not convert to lower-case).
                        string filePath = item;

                        // Check the file extension using a case-insensitive comparison.
                        if (!(filePath.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase) ||
                              filePath.EndsWith(".adt", StringComparison.OrdinalIgnoreCase) ||
                              filePath.EndsWith(".m2", StringComparison.OrdinalIgnoreCase) ||
                              filePath.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        // Skip files matching the specific .wmo pattern.
                        if (filePath.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase) &&
                            Regex.IsMatch(filePath, @".*_\d{3}\.(wmo)$", RegexOptions.IgnoreCase))
                        {
                            continue;
                        }
                        ProcessFile(filePath);
                    }

                    // Write out the results.
                    // Load existing lines from the file into a HashSet.
                    var existingLines = new HashSet<string>();
                    string outputFile = GlobalStorage.ListFilePath;
                    if (File.Exists(outputFile))
                    {
                        foreach (var line in File.ReadAllLines(outputFile))
                        {
                            // Normalize the line if necessary (for example, replace backslashes)
                            existingLines.Add(line.Trim());
                        }
                    }

                    using (StreamWriter sw = new StreamWriter(outputFile, appendOutput, Encoding.UTF8))
                    {
                        foreach (string s in ListFileElem)
                        {
                            // Replace backslashes with forward slashes for consistency.
                            string formattedPath = s.Replace("\\", "/").Replace("//", "/").Trim();

                            // Check if the formatted path is already in the file.
                            if (!existingLines.Contains(formattedPath))
                            {
                                sw.WriteLine(formattedPath);
                                sw.Flush();
                                // Add to the HashSet so that future duplicates in ListFileElem are not written.
                                existingLines.Add(formattedPath);
                            }
                        }
                    }
                });
                //await ShowMessage("Done!");
            }
            catch (Exception ex)
            {
                await ShowMessage("Error: " + ex.Message);
            }
        }

        private void ProcessFile(string file)
        {
            // Check if the file exists before processing.
            if (!File.Exists(file))
            {
                // Optionally log that the file was not found.
                return;
            }

            FileHandler handler = null;
            if (file.EndsWith(".m2", StringComparison.OrdinalIgnoreCase))
            {
                handler = new M2Handler(file);
            }
            else if (file.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase))
            {
                // Skip files that match the undesired .wmo pattern.
                if (Regex.IsMatch(file, @".*_\d{3}\.(wmo)$", RegexOptions.IgnoreCase))
                    return;
                handler = new WmoHandler(file);
            }
            else if (file.EndsWith(".adt", StringComparison.OrdinalIgnoreCase))
            {
                handler = new AdtHandler(file);
            }
            else
            {
                handler = new FileHandler(file);
            }

            handler.GenerateListfile();

            // Recursively process any new files found.
            foreach (string s in handler.Listfile)
            {
                if (!ListFileElem.Contains(s))
                {
                    ListFileElem.Add(s);
                    if (s.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".m2", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessFile(s);
                    }
                }
            }
        }

        private void ProcessLoadDirectory(string dir)
        {
            //List<string> dataList = GenerateData();

            // Use original file paths while checking for duplicates in a case-insensitive manner.
            foreach (string s in Directory.GetFiles(dir))
            {
                if (s.EndsWith(".m2", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".wmo", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".adt", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_listFilesCollection.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
                    {
                        _listFilesCollection.Add(s);
                    }
                }
            }
            foreach (string s in Directory.GetDirectories(dir))
            {
                ProcessLoadDirectory(s);
            }
        }

        private async void button5_Click(object sender, RoutedEventArgs e)
        {
            string directoryToClean = tb_cleaner_dir.Text;
            string listFilePath = tb_cleaner_lf.Text;

            // Validate inputs
            if (!Directory.Exists(directoryToClean))
            {
                await ShowMessage("Directory does not exist.");
                return;
            }
            if (!File.Exists(listFilePath))
            {
                await ShowMessage("List file does not exist.");
                return;
            }

            try
            {
                var cleaner = new FileCleaner.FolderCleaner(directoryToClean, listFilePath);
                int removedFiles = cleaner.RemoveFilesInList();
                Console.WriteLine($"Removed {removedFiles} files.");
                await ShowMessage($"Removed {removedFiles} files");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await ShowMessage($"An error occurred: {ex.Message}");
            }
        }

        private async void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder"
            };
            var result = await dialog.ShowAsync(this);
            if (result != null)
            {
                tb_cleaner_dir.Text = result;
            }
        }

        private async void btnSelectListfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select listfile"
            };
            dialog.Filters.Add(new FileDialogFilter() { Name = "Text Files", Extensions = { "txt" } });
            dialog.Filters.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                tb_cleaner_lf.Text = result[0];
            }
        }

        private async void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                fileDirectory.Text = result[0]; // Set the selected file path in the TextBox
                GlobalStorage.ClientListFileClean = result[0]; // Store the selected file path globally
                txtCleanerAlert.Text = "Auto File/Folder Cleaner is enabled"; // Update the alert text
                txtCleanerInfo.Text = $"Selected file: {result[0]}"; // Update the info text
                txtCleanerInfo.Foreground = new SolidColorBrush(Colors.GreenYellow);
            }
        }

        private void MyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GlobalStorage.ClientListFileClean))
            {
                txtCleanerAlert.Text = "Auto File/Folder Cleaner is enabled"; // Update the alert text
                txtCleanerInfo.Text = $"Selected file: {GlobalStorage.ClientListFileClean}"; // Update the info text
                txtCleanerInfo.Foreground = new SolidColorBrush(Colors.GreenYellow);
                fileDirectory.Text = GlobalStorage.ClientListFileClean;
            }
        }

        private void MyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            txtCleanerAlert.Text = ""; // Clear the alert text
            txtCleanerInfo.Text = ""; // Clear the info text
        }

        private void tb_settings_OutputDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            string inputText = tbOutputFolder.Text.Trim();
            txtOutputFolder.Text = $"Output Folder: {inputText}";

            if (string.IsNullOrEmpty(inputText))
            {
                txtOutputFolder.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtOutputFolder.Foreground = new SolidColorBrush(Colors.Green);
            }

            GlobalStorage.OutputFolderPath = inputText;
        }

        private void tb_settings_DataDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            string inputText = tbDataFolder.Text.Trim();
            txtDataFolder.Text = $"Data Folder: {inputText}";

            if (string.IsNullOrEmpty(inputText))
            {
                txtDataFolder.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtDataFolder.Foreground = new SolidColorBrush(Colors.Green);
            }

            GlobalStorage.DataFolderPath = inputText;
        }

        private void tb_settings_InputDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            string inputText = tbInputFolder.Text.Trim();
            txtInputFolder.Text = $"Input Folder: {inputText}";

            if (string.IsNullOrEmpty(inputText))
            {
                txtInputFolder.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtInputFolder.Foreground = new SolidColorBrush(Colors.Green);
            }

            GlobalStorage.InputFolderPath = inputText;
        }

        private void tb_settings_olf_TextChanged(object sender, TextChangedEventArgs e)
        {
            string inputText = tbOutputListfile.Text.Trim();
            txtLFFolder.Text = inputText;

            if (string.IsNullOrEmpty(inputText))
            {
                txtLFFolder.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                txtLFFolder.Foreground = new SolidColorBrush(Colors.Green);
            }

            GlobalStorage.ListFilePath = inputText;
        }

        private void clnListFileChangedx(object sender, TextChangedEventArgs e)
        {
            string inputText = fileDirectory.Text.Trim();
            if (File.Exists(inputText))
            {
                fileDirectory.Text = inputText;
                txtCleanerAlert.Text = "Auto File/Folder Cleaner is enabled"; // Update the alert text
                txtCleanerInfo.Text = $"Selected file: {inputText}"; // Update the info text
                txtCleanerInfo.Foreground = new SolidColorBrush(Colors.GreenYellow);
                fileDirectory.Background = Brushes.Transparent;
                GlobalStorage.ClientListFileClean = inputText;
            }
            else
            {
                fileDirectory.Background = Brushes.Red;
            }
        }

        private async Task ShowMessage(string message, string title = "Info")
        {
            // Create the dialog window first.
            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // Build the inner content (StackPanel).
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var okButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Use the dialog variable directly in the command.
            okButton.Command = new DelegateCommand(_ => dialog.Close());

            // Add controls to the stack panel.
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(okButton);

            // Wrap the stack panel in a border with a DeepPink border.
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Avalonia.Media.Colors.DeepPink),
                BorderThickness = new Thickness(2),
                Child = stackPanel
            };

            // Set the dialog's content to the bordered panel.
            dialog.Content = border;

            // Show the dialog.
            await dialog.ShowDialog(this);
        }

        private async Task ShowErrorMessage(string message)
        {
            var errorDialog = new Window
            {
                Title = "Configuration Error",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // Create the stack panel to hold the content
            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            // Create the error message TextBlock
            var textBlock = new TextBlock { Text = message, Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap };

            // Create the OK button to close the dialog
            var closeButton = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(10) };
            closeButton.Click += (s, e) => errorDialog.Close();

            // Add the text block and button to the stack panel
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(closeButton);

            // Create a pink border around the content
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.DeepPink),  // Set the border color to DeepPink
                BorderThickness = new Thickness(2), // Set the border thickness
                Padding = new Thickness(10), // Padding inside the border
                Child = stackPanel // Set the stack panel as the content inside the border
            };

            // Set the content of the error dialog
            errorDialog.Content = border;

            // Show the error dialog as modal
            await errorDialog.ShowDialog(this);
        }

    }

    // A basic implementation of ICommand.
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
