#region usings

using jostva.Restful.API.Entities;
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

        #endregion

        #region constructor

        public LibraryRepository(LibraryContext context)
        {
            this.context = context;
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


        public IEnumerable<Author> GetAuthors()
        {
            return context.Authors.OrderBy(a => a.FirstName).ThenBy(a => a.LastName);
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