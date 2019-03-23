using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//1, 2, 3 Array key
//Composite key is in kvp format ex. key1=1,key2=2
namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        //Side not delegates are what let you pass  anonymous functions as parameters, delegate example is commented out.
       // public delegate int PerformCalculation(int x, int y);

        private ILibraryRepository _libraryRepository;

        public AuthorCollectionsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }
        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            if(authorCollection == null)
            {
                return BadRequest();
            }
            var authorEntities = AutoMapper.Mapper.Map<IEnumerable<Entities.Author>>(authorCollection);
            foreach (var author in authorEntities)
            {
                _libraryRepository.AddAuthor(author);
            }
            if (!_libraryRepository.Save())
            {
                throw new Exception("Creathing author collection failed on save.");
            }
            var authorCollectionResult = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            var idsAsString = string.Join(",",
                authorCollectionResult.Select(a => a.Id));
            return CreatedAtRoute("GetAuthorCollection",new { ids = idsAsString}, authorCollectionResult);
        }
        //(key1,key2,....) that is for accepting keys they are put in round brackets no standards on the way how to do it.
        //An example of using the delegate so that we can pass it, lambda expression, to the function. it is commented out
        //Remember the modelbinder binds the keys from the URI to the our IEumerable of guids.
        [HttpGet("({ids})", Name ="GetAuthorCollection")]
        public IActionResult GetAuthorCollection([ModelBinder(BinderType = typeof( ArrayModelBinder))] IEnumerable<Guid> ids /*, PerformCalculation calc*/)
        {
            if (ids == null)
                return BadRequest();
            var authorEntities = _libraryRepository.GetAuthors(ids);
            //Check if all the authors were found.
            if (ids.Count() != authorEntities.Count())
                return NotFound();
            var authorsToReturn = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            return Ok(authorsToReturn);
        }
    }
}
