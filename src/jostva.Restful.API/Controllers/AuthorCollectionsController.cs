#region usings

using AutoMapper;
using jostva.Restful.API.Entities;
using jostva.Restful.API.Helpers;
using jostva.Restful.API.Models;
using jostva.Restful.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace jostva.Restful.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthorCollectionsController : Controller
    {
        #region attributes

        private readonly ILibraryRepository libraryRepository;
        private readonly IMapper mapper;

        #endregion

        #region constructor

        public AuthorCollectionsController(IMapper mapper, ILibraryRepository libraryRepository)
        {
            this.libraryRepository = libraryRepository;
            this.mapper = mapper;
        }

        #endregion

        #region methods

        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            if (authorCollection == null)
            {
                return BadRequest();
            }

            var authorEntities = mapper.Map<IEnumerable<Author>>(authorCollection);
            foreach (var author in authorEntities)
            {
                libraryRepository.AddAuthor(author);
            }

            if (!libraryRepository.Save())
            {
                throw new Exception("Creating an author collection failed on save.");
            }

            IEnumerable<AuthorDto> authorCollectionToReturn = mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            string idsAsString = string.Join(",", authorCollectionToReturn.Select(item => item.Id));

            return CreatedAtRoute("GetAuthorCollection",
                                    new { ids = idsAsString },
                                    authorCollectionToReturn);

            //return Ok();            
        }


        // (key, key2, ..)
        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection([ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                return BadRequest();
            }

            IEnumerable<Author> authorEntities = libraryRepository.GetAuthors(ids);
            if (ids.Count() != authorEntities.Count())
            {
                return NotFound();
            }

            IEnumerable<AuthorDto> authorsToReturn = mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            return Ok(authorsToReturn);
        }

        #endregion
    }
}