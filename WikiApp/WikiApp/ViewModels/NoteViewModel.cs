using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiApp.ViewModels
{
    public class NoteViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public List<string> Tags { get; set; } = new();
        public string TagsDisplay => string.Join(", ", Tags);
    }
}
