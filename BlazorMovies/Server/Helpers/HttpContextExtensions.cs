using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Blob.Protocol;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorMovies.Server.Helpers
{
    public static class HttpContextExtensions
    {
        public async static Task InsertPaginationParameterInResponse<T>(this HttpContext httpContext, IQueryable<T> querable, int recordsPerPage)
        {
            if (httpContext == null) { throw new ArgumentNullException(nameof(httpContext)); }

            double count = await querable.CountAsync();
            double totalAmountOfPages = Math.Ceiling(count / recordsPerPage);
            httpContext.Response.Headers.Add("totalAmountPages", totalAmountOfPages.ToString());



        }
    }
}
