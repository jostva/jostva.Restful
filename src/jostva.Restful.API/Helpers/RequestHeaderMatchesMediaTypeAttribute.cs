using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System;

namespace jostva.Restful.API.Helpers
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
    {
        private readonly string requestHeaderToMatch;
        private readonly string[] mediaTypes;


        public int Order => 0;


        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch, string[] mediaTypes)
        {
            this.requestHeaderToMatch = requestHeaderToMatch;
            this.mediaTypes = mediaTypes;
        }


        public bool Accept(ActionConstraintContext context)
        {
            IHeaderDictionary requestHeaders = context.RouteContext.HttpContext.Request.Headers;

            if (!requestHeaders.ContainsKey(requestHeaderToMatch))
            {
                return false;
            }

            // if one of the media types matches, return true
            foreach (string mediaType in mediaTypes)
            {
                bool mediaTypeMatches = string.Equals(requestHeaders[requestHeaderToMatch].ToString(),
                                            mediaType, StringComparison.OrdinalIgnoreCase);

                if (mediaTypeMatches)
                {
                    return true;
                }
            }

            return false;
        }
    }
}