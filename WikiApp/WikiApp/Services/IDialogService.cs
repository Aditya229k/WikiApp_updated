using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikiApp.Data.Model;

namespace WikiApp.Services
{
    internal interface IDialogService
    {
        string ShowAddCategoryDialog();
        NoteDialogResultModel ShowAddNoteDialog(List<CategoryModel> categories);
    }
}
