#region usings

using AutoMapper;
using jostva.Restful.API.Entities;
using jostva.Restful.API.Models;
using jostva.Restful.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

#endregion

namespace jostva.Restful.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthorsController : Controller
    {
        #region attributes

        private readonly ILibraryRepository libraryRepository;
        private readonly IMapper mapper;

        #endregion

        #region constructor

        public AuthorsController(IMapper mapper, ILibraryRepository libraryRepository)
        {
            this.libraryRepository = libraryRepository;
            this.mapper = mapper;
        }

        #endregion

        #region methods

        [HttpGet()]
        public IActionResult GetAuthors()
        {
            IEnumerable<Author> authorsFromRepo = libraryRepository.GetAuthors();
            IEnumerable<AuthorDto> authors = mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors);
        }


        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            Author authorFromRepo = libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }

            AuthorDto author = mapper.Map<AuthorDto>(authorFromRepo);

            return Ok(author);
        }


        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }

            Author authorEntity = mapper.Map<Author>(author);
            libraryRepository.AddAuthor(authorEntity);

            if (!libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save.");
                //return StatusCode(500, "A problem happend with handling your request.");
            }

            AuthorDto authorToReturn = mapper.Map<AuthorDto>(authorEntity);
            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }


        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (libraryRepository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }


        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }

            libraryRepository.DeleteAuthor(authorFromRepo);

            if (!libraryRepository.Save())
            {
                throw new Exception($"Deleting author {id} failed on save.");
            }

            return NoContent();
        }

        #endregion
    }
}