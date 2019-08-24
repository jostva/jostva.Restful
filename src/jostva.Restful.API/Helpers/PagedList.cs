#region usings

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace jostva.Restful.API.Helpers
{
    public class PagedList<T> : List<T>
    {
        #region properties

        public int CurrentPage { get; private set; }

        public int TotalPages { get; private set; }

        public int PageSize { get; private set; }

        public int TotalCount { get; private set; }

        public bool HasPrevious { get { return (CurrentPage > 1); } }

        public bool HasNext { get { return (CurrentPage < TotalPages); } }

        #endregion

        #region constructor

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        #endregion

        #region methods

        public static PagedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            int count = source.Count();
            List<T> items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

        #endregion
    }
}