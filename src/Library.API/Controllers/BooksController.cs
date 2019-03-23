using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<BooksController> _logger;

        //The Ilogger will log with the T type ex, ILogger<T> will automatically, use the type name as it s category name
        public BooksController(ILibraryRepository libraryRepository, ILogger<BooksController> logger)
        {
            _logger = logger;
            _libraryRepository = libraryRepository;
        }
        [HttpGet()]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);
            var booksForAuthor = AutoMapper.Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            return Ok(booksForAuthor);
        }
        [HttpGet("{id}", Name ="GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
                return NotFound();
            var bookForAuthor = AutoMapper.Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(bookForAuthor);
        }
        [HttpPost()]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BooksForCreationDto book)
        {
            if (book == null)
                return BadRequest();
            if (book.Description == book.Title)
            {
                //If title and description match we add a model error to the state
                //In a kvp format.
                ModelState.AddModelError(nameof(BooksForCreationDto), "The provided description should be different from the title."); 
            }
            if (!ModelState.IsValid) //If model state is not valid, this will happen for exanple, if the title is null since the BookForCreationDto has the attribute required on the title.
            {
                return new UnprocessableEntityObjectResult(ModelState); //We do this because there is no helper method in the controller to defin a 422 error, ex like Ok = 200 code.


            }
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();


            var bookEntity = AutoMapper.Mapper.Map<Entities.Book>(book);
            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!_libraryRepository.Save())
                throw new Exception($"Creating a book for author {authorId} failed on save");
            var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookEntity);
            return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = bookToReturn.Id }, bookToReturn); //2nd parameter is called an anonymous object.

        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
                return NotFound();
            _libraryRepository.DeleteBook(bookForAuthorFromRepo);
            if (!_libraryRepository.Save())
                throw new Exception($"Deleting book {id} for author {authorId} failed on save");
            _logger.LogInformation(100, $"Book {id} for author {authorId} was deleted."); //Log to the file.
            return NoContent();
        }
        [HttpPut("{id}")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book )
        {
            if (book == null) //If the book was not serialized correctly.
                return BadRequest();
            if (book.Description == book.Title)
            {
                //If title and description match we add a model error to the state
                //In a kvp format.
                ModelState.AddModelError(nameof(BooksForCreationDto), "The provided description should be different from the title.");
            }
            if (!ModelState.IsValid) //If model state is not valid, this will happen for exanple, if the title is null since the BookForCreationDto has the attribute required on the title.
            {
                return new UnprocessableEntityObjectResult(ModelState); //We do this because there is no helper method in the controller to defin a 422 error, ex like Ok = 200 code.


            }
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            //We are upserting here because we use post to create a book, but in this method
            //If we are given a book id that does not exist, instead of returning a not found response we create the reource
            //Hence the consumer creates the resource URI because the book id sent in the url does not exist in the server.
            if (bookForAuthorFromRepo == null)
            {
                // return NotFound(); To prepare for upserting we remove the NotFound() method. We will actually create the resource if it does not exist.
                var bookToAdd = AutoMapper.Mapper.Map<Entities.Book>(book); //Map book from body to Book entities.
                bookToAdd.Id = id; //Set the id
                _libraryRepository.AddBookForAuthor(authorId, bookToAdd); //Add the book to the author.
                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                }
                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new { authoorId = authorId, id = id }, bookToReturn ); //Return location of newly created resource
                
            }

            AutoMapper.Mapper.Map(book, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save");

            }

            return NoContent();

        }
        [HttpPatch("{id}")]
        //We use BookForUpdateDto because we do not want to change the id, and the bookforupdatedto does not have an id field.
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id, [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if(patchDoc == null)
            {
                return NotFound();
            }
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                //return NotFound(); //We will UPSERT
                var bookDto = new BookForUpdateDto(); //Create book update dto
                patchDoc.ApplyTo(bookDto, ModelState); //Apply patch doc to book update dto object.
                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto),
                        "The provided description should be different from the title.");

                    //return new UnprocessableEntityObjectResult(ModelState);
                }
                TryValidateModel(bookDto); //Validates bookToPatch any errors will end up in the model state.
                if (!ModelState.IsValid) //If model state is not valid, this will happen for exanple, if the title is null since the BookForCreationDto has the attribute required on the title.
                {
                    return new UnprocessableEntityObjectResult(ModelState); //We do this because there is no helper method in the controller to defin a 422 error, ex like Ok = 200 code.


                }



                var bookToAdd = AutoMapper.Mapper.Map<Entities.Book>(bookDto); //Not values that are not added to the patch document will retain their default values
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if (!_libraryRepository.Save())
                {
                    throw new Exception($"Updating book {id} for author {authorId} failed on save");

                }

                var bookToReturn = AutoMapper.Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId = authorId, id = id }, bookToReturn);
            }
            var bookToPatch = AutoMapper.Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(bookToPatch, ModelState); //If patch document is not valid then it will fail, like replacing a readonly value.

            if(bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");

                //return new UnprocessableEntityObjectResult(ModelState);
            }
            TryValidateModel(bookToPatch); //Validates bookToPatch any errors will end up in the model state.
            if (!ModelState.IsValid) //If model state is not valid, this will happen for exanple, if the title is null since the BookForCreationDto has the attribute required on the title.
            {
                return new UnprocessableEntityObjectResult(ModelState); //We do this because there is no helper method in the controller to defin a 422 error, ex like Ok = 200 code.


            }

            //add validation

            AutoMapper.Mapper.Map(bookToPatch, bookForAuthorFromRepo);
            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);
            if(!_libraryRepository.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save");

            }
            return NoContent();
        }


    }


}

