#region usings

using AutoMapper;
using jostva.Restful.API.Entities;
using jostva.Restful.API.Helpers;
using jostva.Restful.API.Models;
using jostva.Restful.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        private readonly IUrlHelper urlHelper;
        private readonly IPropertyMappingService propertyMappingService;
        private readonly ITypeHelperService typeHelperService;

        #endregion

        #region constructor

        public AuthorsController(IMapper mapper,
                                ILibraryRepository libraryRepository,
                                IUrlHelper urlHelper,
                                IPropertyMappingService propertyMappingService,
                                ITypeHelperService typeHelperService)
        {
            this.libraryRepository = libraryRepository;
            this.mapper = mapper;
            this.urlHelper = urlHelper;
            this.propertyMappingService = propertyMappingService;
            this.typeHelperService = typeHelperService;
        }

        #endregion

        #region methods

        [HttpGet(Name = "GetAuthors")]
        //public IActionResult GetAuthors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            if (!propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            PagedList<Author> authorsFromRepo = libraryRepository.GetAuthors(authorsResourceParameters);

            string previousPageLink = authorsFromRepo.HasPrevious ?
                                        CreateAuthorsResourceUri(authorsResourceParameters,
                                        ResourceUriType.PreviousPage) : null;

            string nextPageLink = authorsFromRepo.HasNext ?
                                        CreateAuthorsResourceUri(authorsResourceParameters,
                                        ResourceUriType.NextPage) : null;

            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

            IEnumerable<AuthorDto> authors = mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
            return Ok(authors.ShapeData(authorsResourceParameters.Fields));
        }


        //  TODO:   urlHelper.Link()  INVESTIGAR EN CORE 2.2
        private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters,
                            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return urlHelper.Link("GetAuthors",
                                new
                                {
                                    fields = authorsResourceParameters.Fields,
                                    orderBy = authorsResourceParameters.OrderBy,
                                    searchQuery = authorsResourceParameters.SearchQuery,
                                    genre = authorsResourceParameters.Genre,
                                    pageNumber = authorsResourceParameters.PageNumber - 1,
                                    pageSize = authorsResourceParameters.PageSize
                                });

                case ResourceUriType.NextPage:
                    return urlHelper.Link("GetAuthors",
                                new
                                {
                                    fields = authorsResourceParameters.Fields,
                                    orderBy = authorsResourceParameters.OrderBy,
                                    searchQuery = authorsResourceParameters.SearchQuery,
                                    genre = authorsResourceParameters.Genre,
                                    pageNumber = authorsResourceParameters.PageNumber + 1,
                                    pageSize = authorsResourceParameters.PageSize
                                });

                default:
                    return urlHelper.Link("GetAuthors",
                                new
                                {
                                    fields = authorsResourceParameters.Fields,
                                    orderBy = authorsResourceParameters.OrderBy,
                                    searchQuery = authorsResourceParameters.SearchQuery,
                                    genre = authorsResourceParameters.Genre,
                                    pageNumber = authorsResourceParameters.PageNumber,
                                    pageSize = authorsResourceParameters.PageSize
                                });
            }
        }


        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
        {
            if (!typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            Author authorFromRepo = libraryRepository.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }

            AuthorDto author = mapper.Map<AuthorDto>(authorFromRepo);

            return Ok(author.ShapeData(fields));
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