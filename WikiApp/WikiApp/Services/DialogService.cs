using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiApp.Data.Model;
using WikiApp.Views.Dialogs;

namespace WikiApp.Services
{
    internal class DialogService
    {
        public string ShowAddCategoryDialog()
        {
            var dialog = new AddCategoryDialog("", "Add Category");
            if (dialog.ShowDialog() == true)
            {
                return dialog.CategoryName;
            }

            return null;
        }

        public string ShowEditCategoryDialog(string currentName)
        {
            var dialog = new AddCategoryDialog(currentName, "Edit Category");
            if (dialog.ShowDialog() == true)
            {
                return dialog.CategoryName;
            }
            return null;
        }

        public NoteDialogResultModel ShowAddNoteDialog(List<CategoryModel> categories)
        {
            var dialog = new AddNoteDialog(categories);
            if (dialog.ShowDialog() == true)
            {
                return new NoteDialogResultModel
                {
                    Title = dialog.NoteTitle,
                    Category = dialog.SelectedCategory
                };
            }

            return null;
        }

    }
}
