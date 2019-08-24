using System.Collections.Generic;

namespace jostva.Restful.API.Services
{
    public class PropertyMappingValue
    {
        #region properties

        public IEnumerable<string> DestinationProperties { get; private set; }

        public bool Revert { get; private set; }

        #endregion

        #region constructor

        public PropertyMappingValue(IEnumerable<string> destinationProperties, bool revert = false)
        {
            DestinationProperties = destinationProperties;
            Revert = revert;
        } 

        #endregion
    }
}