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
using System.Linq;

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
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters,
            [FromHeader(Name = "Accept")] string mediaType)
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
            IEnumerable<AuthorDto> authors = mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

                var links = CreateLinksForAuthors(authorsResourceParameters,
                                authorsFromRepo.HasNext,
                                authorsFromRepo.HasPrevious);

                var shapeAuthors = authors.ShapeData(authorsResourceParameters.Fields);
                IEnumerable<IDictionary<string, object>> shapedAuthorsWithLinks = shapeAuthors.Select(author =>
                    {
                        var authorAsDictionary = author as IDictionary<string, object>;
                        var authorLinks = CreateLinksForAuthor(
                            (Guid)authorAsDictionary["Id"], authorsResourceParameters.Fields);

                        authorAsDictionary.Add("links", authorLinks);

                        return authorAsDictionary;
                    });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links = links
                };

                return Ok(linkedCollectionResource);
            }
            else
            {
                var previousPageLink = authorsFromRepo.HasPrevious ?
                   CreateAuthorsResourceUri(authorsResourceParameters,
                   ResourceUriType.PreviousPage) : null;

                var nextPageLink = authorsFromRepo.HasNext ?
                    CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.NextPage) : null;

                var paginationMetadata = new
                {
                    previousPageLink = previousPageLink,
                    nextPageLink = nextPageLink,
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    JsonConvert.SerializeObject(paginationMetadata));

                return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            }
        }


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

                case ResourceUriType.Current:
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

            IEnumerable<LinkDto> links = CreateLinksForAuthor(id, fields);
            IDictionary<string, object> linkedResourceToReturn = author.ShapeData(fields) as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
        }


        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.marvin.author.full+json" })]
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
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }


        [HttpPost(Name = "AuthorForCreationWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.marvin.authorwithdateofdeath.full+json",
                                                               "application/vnd.marvin.authorwithdateofdeath.full+xml" })]
        //[RequestHeaderMatchesMediaType("Accept", new[] { "..."})]
        public IActionResult AuthorForCreationWithDateOfDeath([FromBody] AuthorForCreationWithDateOfDeathDto author)
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
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
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


        [HttpDelete("{id}", Name = "DeleteAuthor")]
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


        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            List<LinkDto> links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(
                    new LinkDto(urlHelper.Link("GetAuthor", new { id = id }),
                    "self",
                    "GET"));
            }
            else
            {
                links.Add(
                    new LinkDto(urlHelper.Link("GetAuthor", new { id = id, fields = fields }),
                    "self",
                    "GET"));
            }

            links.Add(
                   new LinkDto(urlHelper.Link("DeleteAuthor", new { id = id }),
                   "delete_author",
                   "DELETE"));

            links.Add(
                   new LinkDto(urlHelper.Link("CreateBookForAuthor", new { id = id }),
                   "create_book_for_author",
                   "POST"));

            links.Add(
                   new LinkDto(urlHelper.Link("GetBooksForAuthor", new { id = id }),
                   "books",
                   "GET"));

            return links;
        }


        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters,
            bool hasNext, bool hasPrevious)
        {
            List<LinkDto> links = new List<LinkDto>();

            //  self
            links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current),
                    "self", "GET"));

            if (hasNext)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                    "next_Page", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                    "previous_Page", "GET"));
            }

            return links;
        }

        #endregion
    }
}