using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class AuthorForCreationDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTimeOffset DateOfBirth { get; set; }
        public string Genre { get; set; }

        //ICoolection is used for a list of object that need to iterated through and modified
        //List<> is the same except that it can be sorted etc.
        //IEnumeralble is just for a list of objects that need to iterated through.
        public ICollection<BooksForCreationDto> books { get; set; } = new List<BooksForCreationDto>();
    }
}
