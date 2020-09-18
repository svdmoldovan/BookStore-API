﻿using System.Collections.Generic;

namespace BookStore_API.Mappings
{
    public class AuthorDTO
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Bio { get; set; }

        public virtual ICollection<BookDTO> Books { get; set; }
    }
}