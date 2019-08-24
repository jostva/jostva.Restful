#region usings

using jostva.Restful.API.Entities;
using jostva.Restful.API.Helpers;
using jostva.Restful.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace jostva.Restful.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        #region attributes

        private readonly LibraryContext context;
        private readonly IPropertyMappingService propertyMappingService;

        #endregion

        #region constructor

        public LibraryRepository(LibraryContext context, IPropertyMappingService propertyMappingService)
        {
            this.context = context;
            this.propertyMappingService = propertyMappingService;
        }

        #endregion

        #region methods

        public void AddAuthor(Author author)
        {
            author.Id = Guid.NewGuid();
            context.Authors.Add(author);

            // the repository fills the id (instead of using identity columns)
            if (author.Books.Any())
            {
                foreach (var book in author.Books)
                {
                    book.Id = Guid.NewGuid();
                }
            }
        }


        public void AddBookForAuthor(Guid authorId, Book book)
        {
            var author = GetAuthor(authorId);
            if (author != null)
            {
                // if there isn't an id filled out (ie: we're not upserting),
                // we should generate one
                if (book.Id == Guid.Empty)
                {
                    book.Id = Guid.NewGuid();
                }
                author.Books.Add(book);
            }
        }


        public bool AuthorExists(Guid authorId)
        {
            return context.Authors.Any(a => a.Id == authorId);
        }


        public void DeleteAuthor(Author author)
        {
            context.Authors.Remove(author);
        }


        public void DeleteBook(Book book)
        {
            context.Books.Remove(book);
        }


        public Author GetAuthor(Guid authorId)
        {
            return context.Authors.FirstOrDefault(a => a.Id == authorId);
        }


        public PagedList<Author> GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            //IQueryable<Author> collectionBeforePaging = context.Authors
            //                                                    .OrderBy(a => a.FirstName)
            //                                                    .ThenBy(a => a.LastName)
            //                                                    .AsQueryable();

            var collectionBeforePaging = context.Authors.ApplySort(authorsResourceParameters.OrderBy,
                                            propertyMappingService.GetPropertyMapping<AuthorDto, Author>());


            if (!string.IsNullOrEmpty(authorsResourceParameters.Genre))
            {
                //  trim and ignore casing
                var genreForWhereClause = authorsResourceParameters.Genre.Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging.Where(item => item.Genre.ToLowerInvariant() == genreForWhereClause);
            }

            if (!string.IsNullOrEmpty(authorsResourceParameters.SearchQuery))
            {
                //  trim and ignore casing
                string searchQueryForWhereClause = authorsResourceParameters.SearchQuery.Trim().ToLowerInvariant();

                collectionBeforePaging = collectionBeforePaging.Where(item => item.Genre.ToLowerInvariant().Contains(searchQueryForWhereClause)
                                            || item.FirstName.ToLowerInvariant().Contains(searchQueryForWhereClause)
                                            || item.LastName.ToLowerInvariant().Contains(searchQueryForWhereClause));

            }

            return PagedList<Author>.Create(collectionBeforePaging,
                                            authorsResourceParameters.PageNumber,
                                            authorsResourceParameters.PageSize);
        }


        public IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds)
        {
            return context.Authors.Where(a => authorIds.Contains(a.Id))
                .OrderBy(a => a.FirstName)
                .OrderBy(a => a.LastName)
                .ToList();
        }


        public void UpdateAuthor(Author author)
        {
            // no code in this implementation
        }


        public Book GetBookForAuthor(Guid authorId, Guid bookId)
        {
            return context.Books
              .Where(b => b.AuthorId == authorId && b.Id == bookId).FirstOrDefault();
        }


        public IEnumerable<Book> GetBooksForAuthor(Guid authorId)
        {
            return context.Books
                        .Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToList();
        }


        public void UpdateBookForAuthor(Book book)
        {
            // no code in this implementation
        }


        public bool Save()
        {
            return (context.SaveChanges() >= 0);
        }

        #endregion
    }
}