using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiApp.Data.Model
{
    public class NoteModel
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public bool IsDeleted { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
