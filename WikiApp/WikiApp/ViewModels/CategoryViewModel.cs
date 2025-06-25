using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiApp.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ObservableCollection<NoteViewModel> Notes { get; set; } = new();
    }
}
