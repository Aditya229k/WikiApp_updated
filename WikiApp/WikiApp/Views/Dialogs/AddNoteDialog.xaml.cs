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
using System.Windows.Shapes;
using WikiApp.Data.Model;

namespace WikiApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddNoteDialog.xaml
    /// </summary>
    public partial class AddNoteDialog : Window
    {
        public string NoteTitle { get; private set; }
        public CategoryModel SelectedCategory { get; private set; }
        public AddNoteDialog(List<CategoryModel> categories)
        {
            InitializeComponent();
            CategoryComboBox.ItemsSource = categories;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NoteTitleTextBox.Text) && CategoryComboBox.SelectedItem != null)
            {
                NoteTitle = NoteTitleTextBox.Text.Trim();
                SelectedCategory = CategoryComboBox.SelectedItem as CategoryModel;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
