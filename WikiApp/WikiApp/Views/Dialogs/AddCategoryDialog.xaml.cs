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

namespace WikiApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddCategoryDialog.xaml
    /// </summary>
    public partial class AddCategoryDialog : Window
    {
        public string CategoryName { get; private set; }

        public AddCategoryDialog(string existingName = "", string windowTitle = "Add Category")
        {
            InitializeComponent();

            CategoryTextBox.Text = existingName;
            this.Title = windowTitle;

            CategoryTextBox.SelectAll();
            CategoryTextBox.Focus();            
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CategoryTextBox.Text))
            {
                CategoryName = CategoryTextBox.Text.Trim();
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
