using BlazorMovies.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorMovies.Shared.DTOs
{
    public class DetailsPersonDTO
    {

        public  Person Person { get; set; }
        public List<Movie> Movies { get; set; }
        
    }
}
