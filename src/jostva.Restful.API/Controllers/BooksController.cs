#region using

using AutoMapper;
using jostva.Restful.API.Entities;
using jostva.Restful.API.Models;
using jostva.Restful.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace jostva.Restful.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        #region attributes

        private readonly ILibraryRepository libraryRepository;
        private readonly IMapper mapper;
        private readonly ILogger<BooksController> logger;
        private readonly IUrlHelper urlHelper;

        #endregion

        #region constructor

        public BooksController(IMapper mapper,
                                ILibraryRepository libraryRepository,
                                ILogger<BooksController> logger,
                                IUrlHelper urlHelper)
        {
            this.libraryRepository = libraryRepository;
            this.mapper = mapper;
            this.logger = logger;
            this.urlHelper = urlHelper;
        }

        #endregion

        #region methods

        [HttpGet(Name = "GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            IEnumerable<Book> booksForAuthorsFromRepo = libraryRepository.GetBooksForAuthor(authorId);
            IEnumerable<BookDto> booksForAuthor = mapper.Map<IEnumerable<BookDto>>(booksForAuthorsFromRepo);

            booksForAuthor = booksForAuthor.Select(book =>
            {
                book = CreateLinksForBooks(book);
                return book;
            });

            LinkedCollectionResourceWrapperDto<BookDto> wrapper = new LinkedCollectionResourceWrapperDto<BookDto>(booksForAuthor);

            return Ok(CreateLinksForBooks(wrapper));
        }


        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            BookDto bookForAuthor = mapper.Map<BookDto>(bookForAuthorFromRepo);
            return Ok(CreateLinksForBooks(bookForAuthor));
        }


        [HttpPost(Name = "CreateBookForAuthor")]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDto), "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                // return 422                
                return new Helpers.UnprocessableEntityObjectResult(ModelState);
            }

            if (!libraryRepository.AuthorExists(authorId))
            {
                return BadRequest();
            }

            var bookEntity = mapper.Map<Book>(book);

            libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!libraryRepository.Save())
            {
                throw new Exception($"Creating a book for author {authorId} failed on save.");
            }

            BookDto bookToReturn = mapper.Map<BookDto>(bookEntity);
            return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, CreateLinksForBooks(bookToReturn));
        }


        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            libraryRepository.DeleteBook(bookForAuthorFromRepo);
            if (!libraryRepository.Save())
            {
                throw new Exception($"Deleting book {id} for author {authorId} failed on save.");
            }

            logger.LogInformation(100, $"Deleting book {id} for author {authorId} was deleted.");
            return NoContent();
        }


        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                // return 422
                return new Helpers.UnprocessableEntityObjectResult(ModelState);
            }

            if (!libraryRepository.AuthorExists(authorId))
            {
                return BadRequest();
            }

            if (!libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                Book bookToAdd = mapper.Map<Book>(book);
                bookToAdd.Id = id;

                libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!libraryRepository.Save())
                {
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                }

                BookDto bookToReturn = mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, bookToReturn);
            }

            mapper.Map(book, bookForAuthorFromRepo);

            libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!libraryRepository.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }


        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            if (!libraryRepository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                BookForUpdateDto bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto, ModelState);

                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");
                }

                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                {
                    return new Helpers.UnprocessableEntityObjectResult(ModelState);
                }

                Book bookToAdd = mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                libraryRepository.AddBookForAuthor(authorId, bookToAdd);
                if (!libraryRepository.Save())
                {
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                }

                BookDto bookToReturn = mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, bookToReturn);
            }

            BookForUpdateDto bookToPatch = mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            // patchDoc.ApplyTo(bookToPatch, ModelState);
            patchDoc.ApplyTo(bookToPatch);

            if (bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto), "The provided description should be different from the title.");
            }

            TryValidateModel(bookToPatch);
            if (!ModelState.IsValid)
            {
                return new Helpers.UnprocessableEntityObjectResult(ModelState);
            }

            mapper.Map(bookToPatch, bookForAuthorFromRepo);

            libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);
            if (!libraryRepository.Save())
            {
                throw new Exception($"Patching book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }


        private BookDto CreateLinksForBooks(BookDto book)
        {
            book.Links.Add(new LinkDto(urlHelper.Link("GetBookForAuthor",
                    new { id = book.Id }),
                    "self",
                    "GET"
                ));

            book.Links.Add(new LinkDto(urlHelper.Link("DeleteBookForAuthor",
                    new { id = book.Id }),
                    "delete_book",
                    "DELETE"
                ));

            book.Links.Add(new LinkDto(urlHelper.Link("UpdateBookForAuthor",
                    new { id = book.Id }),
                    "update_book",
                    "PUT"
                ));

            book.Links.Add(new LinkDto(urlHelper.Link("PartiallyUpdateBookForAuthor",
                    new { id = book.Id }),
                    "partially_update_book",
                    "PATCH"
                ));

            return book;
        }


        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            //  Link to self.
            booksWrapper.Links.Add(new LinkDto(urlHelper.Link("GetBooksForAuthor",
                    new { }),
                     "self",
                     "GET"));

            return booksWrapper;
        }

        #endregion
    }
}