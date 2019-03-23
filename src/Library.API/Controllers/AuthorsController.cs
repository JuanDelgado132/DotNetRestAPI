using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using Library.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace Library.API.Controllers
{
    //All controllers must inherit controller class in .net core mvc
    //Using the route attribute at controller level lets us not have to put route[api/authors] in every action atrribute
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorsController(ILibraryRepository libraryRepository)
        {
            //Dependency injection
            _libraryRepository = libraryRepository;
        }

        //Defines a contract that represent the result of an action method
        [HttpGet()]
        public IActionResult GetAuthors()
        {


            var authorsFromRepo = _libraryRepository.GetAuthors();
            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo); //Using automapper to map the properties
            return Ok(authors);




        }
        [HttpGet("{id}", Name ="GetAuthor")]
        //The paramether in the signature must have the same name as the route parameter.
        public IActionResult GetAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
                return NotFound();
            var author = Mapper.Map<AuthorDto>(authorFromRepo); //Map to AuthorDtoObject
            return Ok(author);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null) //Check to see if the author data was correctly serialize to author. If it isnt then it will be null.
                return BadRequest();
            var authorEntity = AutoMapper.Mapper.Map<Entities.Author>(author); //The data in the diamonds is what we will be mapping to the parameter is the data to be mapped.
            _libraryRepository.AddAuthor(authorEntity);
            var isSaved = _libraryRepository.Save();
            if (!isSaved)
            {
                throw new Exception("Creating an author failed on save."); //We can use statuscode method but we already set up the middleware to use exception. Will be good when we log since we only have write in one place compared to using the status code way
                                                                           // return StatusCode(500, "A problem happened with handling your request"); //We can throw exception, let but it is a performance hit
            }
            var authorToReturn = AutoMapper.Mapper.Map<AuthorDto>(authorEntity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn); //Alows us to return a response with a location header.
        }
        //Posting to a uri with an id at end like this should never happen. We want to block this so if an author exists there is a conflict if does not exist its not found.
        //Why because it is the server who is responsible for creating the resource URI and not the comsumer, remember post is not idempotent so it can not work.
        //We must no treat post like an idempotent action.
        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_libraryRepository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        } 
        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
                return NotFound();
            _libraryRepository.DeleteAuthor(authorFromRepo);

            if (!_libraryRepository.Save())
                throw new Exception($"Deleting author {id} failed to save");
            return NoContent();
             

        }
    }
}
