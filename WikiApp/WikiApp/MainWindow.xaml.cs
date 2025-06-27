using Markdig;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WikiApp.Data.Access;
using WikiApp.Data.Model;
using WikiApp.ViewModels;

namespace WikiApp
{
    public partial class MainWindow : Window
    {
        private string currentFilePath = "";
        private const string NotesRoot = "Notes";
        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await PreviewBrowser.EnsureCoreWebView2Async(null);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (e.NewValue is CategoryViewModel categoryModel)
                {
                    viewModel.SelectedCategory = categoryModel;
                }
                else if (e.NewValue is NoteViewModel note)
                {
                    viewModel.SelectedNote = note;
                    var category = viewModel.Categories.FirstOrDefault(c => c.Notes.Contains(note));
                    if (category != null)
                        viewModel.SelectedCategory = category;

                    if (!string.IsNullOrWhiteSpace(note.FilePath) && File.Exists(note.FilePath))
                    {
                        viewModel.NoteContent = File.ReadAllText(note.FilePath);
                        viewModel.IsEditing = false;
                        viewModel.RenderPreview(viewModel.NoteContent);
                    }
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddButton.ContextMenu != null)
            {
                AddButton.ContextMenu.PlacementTarget = AddButton;
                AddButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                AddButton.ContextMenu.IsOpen = true;
            }
        }

        private void SearchResult_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is NoteSearchResult result)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectedResult = result;
                }
            }
        }

        private void EditorTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void EditorTextBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;

            string droppedFilePath = files[0];
            string ext = Path.GetExtension(droppedFilePath).ToLower();

            if (DataContext is MainViewModel vm && vm.SelectedNote != null)
            {
                string noteDir = Path.GetDirectoryName(vm.SelectedNote.FilePath)!;
                string fileName = Path.GetFileName(droppedFilePath);
                string destPath = Path.Combine(noteDir, fileName);

                File.Copy(droppedFilePath, destPath, true);

                if (ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".gif")
                {
                    string markdown = $"![{fileName}](./{fileName})";
                    InsertTextAtCaret(EditorTextBox, markdown);
                }
                else if (ext == ".pdf")
                {
                    string markdown = $"<pdf>{destPath}</pdf>";
                    InsertTextAtCaret(EditorTextBox, markdown);

                    // Auto-render PDF as image preview
                    vm.RenderPreview(vm.NoteContent);
                }
            }
        }

        private void InsertTextAtCaret(TextBox textBox, string text)
        {
            int caretIndex = textBox.CaretIndex;
            textBox.Text = textBox.Text.Insert(caretIndex, text);
            textBox.CaretIndex = caretIndex + text.Length;
        }
    }
}

    

