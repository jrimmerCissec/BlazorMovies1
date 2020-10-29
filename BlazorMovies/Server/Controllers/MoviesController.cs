using System;
using BlazorMovies.Server.Helpers;
using BlazorMovies.Shared.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using BlazorMovies.Shared.DTOs;
using AutoMapper;
using System.Security.Cryptography.X509Certificates;

namespace BlazorMovies.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IFileStorageService fileStorageService;
        private readonly IMapper mapper;
        private string containerName = "movies";


        public MoviesController(ApplicationDbContext context, IFileStorageService fileStorageService, IMapper mapper)
        {

            this.context = context;
            this.fileStorageService = fileStorageService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IndexPageDTO>> Get()
        {
            var limit = 6;
            var moviesInTheaters = await context.Movies
                .Where(x => x.InTheaters).Take(limit)
                .OrderByDescending(x => x.ReleaseDate)
                .ToListAsync();

            var todaysDate = DateTime.Today;

            var upcomingReleases = await context.Movies
                .Where(x => x.ReleaseDate > todaysDate)
                .OrderBy(x => x.ReleaseDate).Take(limit)
                .ToListAsync();


            var response = new IndexPageDTO();
            response.InTheateres = moviesInTheaters;
            response.UpcomingReleases = upcomingReleases;
            return response;

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DetailsMovieDTO>> Get(int id)
        {
            var movie = await context.Movies.Where(x => x.Id == id)
                .Include(x => x.MoviesGenres).ThenInclude(x => x.Genre)
                .Include(x => x.MoviesActors).ThenInclude(x => x.Person)
                .FirstOrDefaultAsync();

            if (movie == null) { return NotFound(); }

            movie.MoviesActors.OrderBy(x => x.Order).ToList();
            var model = new DetailsMovieDTO();

            model.Movie = movie;
            model.Genres = movie.MoviesGenres.Select(x => x.Genre).ToList();
            model.Actors = movie.MoviesActors.Select(x =>
           new Person
           {
               Name = x.Person.Name,
               Picture = x.Person.Picture,
               Character = x.Person.Character,
               Id = x.Person.Id

           }).ToList();

            return model;
        }


        [HttpPost("filter")]
        public async Task<ActionResult<List<Movie>>> Filter( FilterMoviesDTO filterMoviesDTO)
        {
            var moviesQuerable = context.Movies.AsQueryable();

            if(!string.IsNullOrWhiteSpace(filterMoviesDTO.Title))
            {
                moviesQuerable = moviesQuerable
                    .Where(x => x.Title.Contains(filterMoviesDTO.Title));
            }

            if(filterMoviesDTO.InTheaters)
            {
                moviesQuerable = moviesQuerable.Where(x => x.InTheaters);
                    
            }

            if (filterMoviesDTO.UpComingReleases)
            {
                var today = DateTime.Today;
                moviesQuerable = moviesQuerable.Where(x => x.ReleaseDate > today);
            }

            if(filterMoviesDTO.GenreId!=0)
            {
                moviesQuerable = moviesQuerable
                    .Where(x => x.MoviesGenres.Select(y => y.GenreId)
                    .Contains(filterMoviesDTO.GenreId));
            }

            await HttpContext.InsertPaginationParameterInResponse(moviesQuerable, filterMoviesDTO.RecordsPerPage);

            var movies   =await moviesQuerable.Paginate(filterMoviesDTO.Pagination).ToListAsync();

            return movies;
        }




        [HttpGet("update/{id}")]
        public async Task<ActionResult<MovieUpdateDTO>> PutGet(int Id)
        {
            var movieActionResult = await Get(Id);
            if (movieActionResult.Result is NotFoundResult) { return NotFound(); }

            var movieDetailDto = movieActionResult.Value;
            var selectedGenreIds = movieDetailDto.Genres.Select(x => x.Id).ToList();
            var notSelectedGenres = await context.Genres
                .Where(x => !selectedGenreIds.Contains(x.Id))
                .ToListAsync();
            var model = new MovieUpdateDTO();
            model.Movie = movieDetailDto.Movie;
            model.Actors = movieDetailDto.Actors;
            model.NotSelectedGenres = notSelectedGenres;
            model.SelectedGenres = movieDetailDto.Genres;

            return model;
        }


        [HttpPost]
        public async Task<ActionResult<int>> Post(Movie movie)
        {
            if (!string.IsNullOrEmpty(movie.Poster))
            {
                var moviePoster = Convert.FromBase64String(movie.Poster);
                movie.Poster = await fileStorageService.SaveFile(moviePoster, "jpg", containerName);
            }

            if (movie.MoviesActors != null)
            {
                for (int i = 0; i < movie.MoviesActors.Count; i++)
                {
                    movie.MoviesActors[i].Order = i + 1; 
                }
            }


            context.Add(movie);
            await context.SaveChangesAsync();
            return movie.Id;
        }

        [HttpPut]
        public async Task<ActionResult> Put(Movie movie)
        {
            var MovieDb = await context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
            if (MovieDb == null) { return NotFound(); }

            
            MovieDb = mapper.Map(movie, MovieDb);

            if (!string.IsNullOrWhiteSpace(movie.Poster))
            {
                var moviePoster = Convert.FromBase64String(movie.Poster);
                MovieDb.Poster = await fileStorageService.EditFile(moviePoster,
                    "jpg", containerName, MovieDb.Poster);
            }

            await context.Database.ExecuteSqlInterpolatedAsync($"delete from MoviesActors where MovieId = {movie.Id}; delete from MoviesGenres where MovieId = {movie.Id} ");

            if (movie.MoviesActors != null)
            {
                for (int i = 0; i < movie.MoviesActors.Count; i++)
                {
                    movie.MoviesActors[i].Order = i + 1;
                }
            }

            MovieDb.MoviesActors = movie.MoviesActors;
            MovieDb.MoviesGenres = movie.MoviesGenres;

            await context.SaveChangesAsync();
            return NoContent();

        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int Id)
        {
            var movie = await context.Movies.FirstOrDefaultAsync(x => x.Id == Id);
            if (movie == null) { return NotFound(); }
            context.Remove(movie);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
