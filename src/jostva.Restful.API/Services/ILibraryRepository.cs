﻿#region methods

using jostva.Restful.API.Entities;
using jostva.Restful.API.Helpers;
using System;
using System.Collections.Generic;

#endregion

namespace jostva.Restful.API.Services
{
    public interface ILibraryRepository
    {
        PagedList<Author> GetAuthors(AuthorsResourceParameters authorsResourceParameters);

        Author GetAuthor(Guid authorId);

        IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds);

        void AddAuthor(Author author);

        void DeleteAuthor(Author author);

        void UpdateAuthor(Author author);

        bool AuthorExists(Guid authorId);

        IEnumerable<Book> GetBooksForAuthor(Guid authorId);

        Book GetBookForAuthor(Guid authorId, Guid bookId);

        void AddBookForAuthor(Guid authorId, Book book);

        void UpdateBookForAuthor(Book book);

        void DeleteBook(Book book);

        bool Save();
    }
}