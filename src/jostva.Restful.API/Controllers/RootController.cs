#region usings

using jostva.Restful.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

#endregion

namespace jostva.Restful.API.Controllers
{
    [Route("api")]
    public class RootController : Controller
    {
        private IUrlHelper urlHelper;


        public RootController(IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }


        [HttpGet(Name = "GetRoot")]
        public IActionResult GetRoot([FromHeader(Name = "Accept")] string mediaType)
        {
            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                List<LinkDto> links = new List<LinkDto>();

                links.Add(new LinkDto(urlHelper.Link("GetRoot", new { }),
                    "self",
                    "GET"));

                links.Add(new LinkDto(urlHelper.Link("GetAuthors", new { }),
                    "authors",
                    "GET"));

                links.Add(new LinkDto(urlHelper.Link("CreateAuthor", new { }),
                    "create_author",
                    "POST"));

                return Ok(links);
            }

            return NoContent();
        }
    }
}