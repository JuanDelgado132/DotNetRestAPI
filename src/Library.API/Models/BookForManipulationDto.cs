using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    //Class so that BookForUpdateDto and BookForCreationDto can share the same tags.
    public abstract class BookForManipulationDto
    {
        [Required(ErrorMessage = "You should fil out a title")] //Comes from System.ComponentModel.DataAnnotations
        [MaxLength(100, ErrorMessage = "The title should be no more than 100 character long.")]
        public string Title { get; set; }
        [MaxLength(500, ErrorMessage = "The description should be no more than 500 characters long.")] //Remember virtual allows overriding of methods.
        public virtual string Description { get; set; }
    }
}
