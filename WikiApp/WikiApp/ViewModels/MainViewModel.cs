using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WikiApp.Data.Model;
using WikiApp.Services;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Markdig;

namespace WikiApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<CategoryViewModel> Categories { get; set; } = new();
        public ObservableCollection<NoteSearchResult> SearchResults { get; } = new();

        private NoteViewModel _selectedNote;
        public NoteViewModel SelectedNote
        {
            get => _selectedNote;
            set { _selectedNote = value; OnPropertyChanged(); }
        }

        private CategoryViewModel _selectedCategory;
        public CategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        private string _noteContent;
        public string NoteContent
        {
            get => _noteContent;
            set
            {
                _noteContent = value;
                OnPropertyChanged();

                if (IsEditing)
                {
                    RenderPreview(_noteContent); // 🔁 Live preview update
                }
            }
        }


        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        private string _tagsInput;
        public string TagsInput
        {
            get => _tagsInput;
            set { _tagsInput = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();

                if (!_suppressSearch)
                {
                    ApplyTreeFilter();
                }
            }
        }
        private ObservableCollection<CategoryViewModel> _filteredCategories = new();
        public ObservableCollection<CategoryViewModel> FilteredCategories
        {
            get => _filteredCategories;
            set { _filteredCategories = value; OnPropertyChanged(); }
        }

        private void ApplyTreeFilter()
        {
            FilteredCategories.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var cat in Categories)
                    FilteredCategories.Add(cat);
                return;
            }

            
            var results = noteService.SearchNotes(SearchText);
            var matchedPaths = results.Select(r => r.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var cat in Categories)
            {
                var matchedNotes = cat.Notes
                    .Where(n => matchedPaths.Contains(n.FilePath))
                    .ToList();

                if (matchedNotes.Any())
                {
                    var newCat = new CategoryViewModel
                    {
                        Id = cat.Id,
                        Name = cat.Name
                    };

                    foreach (var note in matchedNotes)
                        newCat.Notes.Add(note);

                    FilteredCategories.Add(newCat);
                }
            }

            
            var topMatch = results.FirstOrDefault();
            if (topMatch != null && File.Exists(topMatch.FullPath))
            {
                SelectedNote = Categories
                    .SelectMany(c => c.Notes)
                    .FirstOrDefault(n => n.FilePath == topMatch.FullPath);

                NoteContent = File.ReadAllText(topMatch.FullPath);
                RenderPreviewWithHighlight(NoteContent, topMatch.MatchStartIndex);
            }
        }





        private NoteSearchResult _selectedResult;
        public NoteSearchResult SelectedResult
        {
            get => _selectedResult;
            set
            {
                _selectedResult = value;
                OnPropertyChanged();

                if (_selectedResult != null)
                {
                    OpenNoteWithHighlight(_selectedResult.FullPath, _selectedResult.MatchStartIndex);
                }
            }
        }



        private readonly NoteService noteService = new();

        public ICommand EditCommand => new RelayCommand(_ => StartEditing(), _ => SelectedNote != null);
        public ICommand SaveCommand => new RelayCommand(_ => SaveChanges(), _ => SelectedNote != null && IsEditing);
        public ICommand CancelCommand => new RelayCommand(_ => CancelEditing(), _ => IsEditing);
        public ICommand AddCategoryCommand => new RelayCommand(_ => AddNewCategory());
        public ICommand AddNoteCommand => new RelayCommand(_ => AddNewNote(), _ => SelectedCategory != null);


        public ICommand ClearSearchCommand => new RelayCommand(_ => ClearSearch());





        public ICommand DeleteCategoryCommand => new RelayCommand(categoryObj =>
        {
            if (categoryObj is CategoryViewModel category)
            {
                if (MessageBox.Show($"Delete category '{category.Name}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    NoteService.DeleteCategory(category.Id);
                    LoadFromDatabase();
                }
            }
        });

        public ICommand EditCategoryCommand => new RelayCommand(catObj =>
        {
            if (catObj is CategoryViewModel category)
            {
                var dialogService = new DialogService();
                var updatedName = dialogService.ShowEditCategoryDialog(category.Name);
                if (!string.IsNullOrWhiteSpace(updatedName) && updatedName != category.Name)
                {
                    NoteService.UpdateCategory(category.Id, updatedName);
                    category.Name = updatedName;
                }
            }
        });

        public ICommand DeleteNoteCommand => new RelayCommand(noteObj =>
        {
            if (noteObj is NoteViewModel note)
            {
                if (MessageBox.Show($"Delete note '{note.Title}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    NoteService.DeleteNote(note.Id);
                    LoadFromDatabase();
                }
            }
        });

        public ICommand EditNoteCommand => new RelayCommand(noteObj =>
        {
            if (noteObj is NoteViewModel note && File.Exists(note.FilePath))
            {
                NoteContent = File.ReadAllText(note.FilePath);
                SelectedNote = note;
                SelectedCategory = Categories.FirstOrDefault(c => c.Notes.Contains(note));
                TagsInput = string.Join(", ", NoteService.GetTagsForNote(note.Id));
                IsEditing = true;
            }
        });

        public MainViewModel() => LoadFromDatabase();

        private void LoadFromDatabase()
        {
            try
            {
                Categories.Clear();
                var categories = NoteService.GetCategories();
                var allNotes = new List<NoteModel>();

                foreach (var cat in categories)
                {
                    var catVM = new CategoryViewModel { Id = cat.Id, Name = cat.Name };
                    var notes = NoteService.GetNotesByCategory(cat.Id);

                    foreach (var note in notes)
                    {
                        catVM.Notes.Add(new NoteViewModel
                        {
                            Id = note.Id,
                            Title = note.Title,
                            FilePath = note.FilePath,
                            Tags = NoteService.GetTagsForNote(note.Id)
                        });

                        allNotes.Add(note);
                    }

                    Categories.Add(catVM);
                }

                noteService.BuildIndex(allNotes);

             
                FilteredCategories = new ObservableCollection<CategoryViewModel>(Categories);
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                MessageBox.Show("Failed to load notes.");
            }
        }


        private void StartEditing()
        {
            IsEditing = true;
            if (!string.IsNullOrWhiteSpace(NoteContent))
            {
                RenderPreview(NoteContent); 
            }
        }


        private void CancelEditing()
        {
            LoadNoteContent();
            IsEditing = false;
        }

        private void SaveChanges()
        {
            try
            {
                if (SelectedNote?.FilePath == null) return;
                File.WriteAllText(SelectedNote.FilePath, NoteContent);

                var tagList = TagsInput?.Split(',')?.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                NoteService.UpdateTagsForNote(SelectedNote.Id, tagList ?? new());

                IsEditing = false;

              
                string refreshed = File.ReadAllText(SelectedNote.FilePath);
                NoteContent = refreshed;
                RenderPreview(refreshed);

            }
            catch (Exception ex)
            {
                AppLogger.Log(ex);
                MessageBox.Show("Error saving the note.");
            }
        }

        private void LoadNoteContent()
        {
            if (SelectedNote?.FilePath != null && File.Exists(SelectedNote.FilePath))
            {
                NoteContent = File.ReadAllText(SelectedNote.FilePath);
                RenderPreview(NoteContent);
                IsEditing = false;
            }
        }

        private void AddNewCategory()
        {
            var dialogService = new DialogService();
            var name = dialogService.ShowAddCategoryDialog();

            if (!string.IsNullOrWhiteSpace(name))
            {
                NoteService.CreateCategory(name);
                LoadFromDatabase();
            }
        }

        private void AddNewNote()
        {
            var dialogService = new DialogService();
            var allCategories = NoteService.GetCategories();
            var result = dialogService.ShowAddNoteDialog(allCategories);

            if (result != null && !string.IsNullOrWhiteSpace(result.Title) && result.Category != null)
            {
                string folder = Path.Combine("Notes", result.Category.Name);
                Directory.CreateDirectory(folder);
                string filePath = Path.Combine(folder, $"{result.Title.Replace(" ", "_")}.md");
                File.WriteAllText(filePath, "# " + result.Title);

                int noteId = NoteService.CreateNote(result.Category.Id, result.Title, filePath);
                LoadFromDatabase();

                SelectedCategory = Categories.FirstOrDefault(c => c.Id == result.Category.Id);
                SelectedNote = SelectedCategory?.Notes.FirstOrDefault(n => n.Id == noteId);
                IsEditing = true;
            }
        }

        private NoteSearchResult _topMatch;
        public NoteSearchResult TopMatch
        {
            get => _topMatch;
            set
            {
                _topMatch = value;
                OnPropertyChanged();
            }
        }

        private void RunSearch()
        {
            SearchResults.Clear();


            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            var results = noteService.SearchNotes(SearchText);

            if (results.Count > 0)
            {
                SearchResults.Add(results[0]);
            }
        }



        private bool _suppressSearch = false;

        private void ClearSearch()
        {
            _suppressSearch = true;

            SearchText = string.Empty;
            TopMatch = null;
            SearchResults.Clear();

            if (SelectedNote != null && File.Exists(SelectedNote.FilePath))
            {
                string content = File.ReadAllText(SelectedNote.FilePath);
                NoteContent = content;
                RenderPreview(content);
            }

            
            FilteredCategories = new ObservableCollection<CategoryViewModel>(Categories);
            OnPropertyChanged(nameof(FilteredCategories));

            _suppressSearch = false;
        }






        private void OpenNoteWithHighlight(string filePath, int index)
        {
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                NoteContent = content;
                RenderPreviewWithHighlight(content, index);
            }
        }

        public async void RenderPreview(string markdown)
        {
     
            var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UsePipeTables()
            .UseGenericAttributes()
            .UseEmphasisExtras()
        
            .Build();
            string html = Markdig.Markdown.ToHtml(markdown, pipeline);
       


            string baseUri = "https://local.notes/";
            string? noteDir = Path.GetDirectoryName(SelectedNote?.FilePath ?? "");

            Application.Current.Dispatcher.Invoke(async () =>
            {
                string previewElementName = IsEditing ? "EditPreviewBrowser" : "PreviewBrowser";

                if (Application.Current.MainWindow is Window window &&
                    window.FindName(previewElementName) is Microsoft.Web.WebView2.Wpf.WebView2 webView)

                {
                    await webView.EnsureCoreWebView2Async();
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "local.notes", noteDir!,
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.DenyCors
                    );

                    string fullHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <base href='{baseUri}'>
</head>
<body>
    {html}
</body>
</html>";

                    webView.NavigateToString(fullHtml);
                }
            });
        }





        internal void RenderPreviewWithHighlight(string markdown, int index)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                RenderPreview(markdown);
                return;
            }

            string keyword = SearchText;

            // Escape characters that can break regex
            string safeKeyword = Regex.Escape(keyword);

            // Highlight in raw markdown instead of rendered HTML
            string highlightedMarkdown = Regex.Replace(
                markdown,
                safeKeyword,
                m => $"<mark>{m.Value}</mark>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            RenderPreview(highlightedMarkdown);  // ← no need to re-do HTML parsing here
        }








        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
