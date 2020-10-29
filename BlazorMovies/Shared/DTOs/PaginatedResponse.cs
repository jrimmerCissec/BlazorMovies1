using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BlazorMovies.Shared.DTOs
{
    public class PaginatedResponse<T>
    {
        public  T Response { get; set; }
        public int TotalAmountOfPages { get; set; }
    }
}
