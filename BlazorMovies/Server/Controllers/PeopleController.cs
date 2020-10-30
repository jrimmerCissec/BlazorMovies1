using AutoMapper;
using BlazorMovies.Server.Helpers;
using BlazorMovies.Shared.DTOs;
using BlazorMovies.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BlazorMovies.Server.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IFileStorageService fileStorageService;
        private readonly IMapper mapper;

        public PeopleController(ApplicationDbContext context,
            IFileStorageService fileStorageService,
            IMapper mapper)
        {
           
            this.context = context;
            this.fileStorageService = fileStorageService;
            this.mapper = mapper; 
        }

        [HttpGet("search/{searchText}")]
        public async Task<ActionResult<List<Person>>> FilterByName(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return new List<Person>();
            }

            return await context.People.Where(x => x.Name.Contains(searchText))
                .Take(5)
                .ToListAsync();
        }


        [HttpGet]
        public async Task<ActionResult<List<Person>>> Get([FromQuery]PaginationDTO paginationDTO)
        {
            var queryable = context.People.AsQueryable();
            await HttpContext.InsertPaginationParameterInResponse(queryable, paginationDTO.RecordsPerPage);

            return await queryable.Paginate(paginationDTO).ToListAsync();
        }

        //[HttpGet("{id}")]
        //public async Task<ActionResult<Person>> Get(int Id)
        //{
           
           
        //   var person = await context.People.FirstOrDefaultAsync(x => x.Id == Id);
        //    if (person == null) { return NotFound(); }
        //    return person; 
        //}


        [HttpGet("{id}")]
        public async Task<ActionResult<DetailsPersonDTO>> Get(int Id)
        {

            
            var person = await context.People.Include(x => x.MoviesActors).ThenInclude(x => x.Movie).FirstOrDefaultAsync(x => x.Id == Id); 
            if (person == null) { return NotFound(); }

            var model = new DetailsPersonDTO();
            model.Person = person;

            Console.WriteLine($"THis is the person we are looking for {person.ToString()}");
            
            return model;
        }


        [HttpPost]
        public async Task<ActionResult<int>> Post(Person person)
        {
            if(!string.IsNullOrEmpty(person.Picture))
            {
                var personPicture = Convert.FromBase64String(person.Picture);
                person.Picture = await fileStorageService.SaveFile(personPicture, "jpg", "people");
            }

            context.Add(person);
            await context.SaveChangesAsync();
            return person.Id;
        }

        [HttpPut]
        public async Task<ActionResult> Put(Person person)
        {
            var PersonDb = await context.People.FirstOrDefaultAsync(x => x.Id == person.Id);
            if(PersonDb==null) { return NotFound(); }


            PersonDb = mapper.Map(person, PersonDb);

            if(!string.IsNullOrWhiteSpace(person.Picture))
            {
                var personPicture = Convert.FromBase64String(person.Picture);
                PersonDb.Picture = await fileStorageService.EditFile(personPicture,
                    "jpg", "people", PersonDb.Picture);
            }

            await context.SaveChangesAsync();
            return NoContent();

        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int Id)
        {
            var person = await context.People.FirstOrDefaultAsync(x => x.Id == Id);
            if (person == null) { return NotFound(); }
            context.Remove(person);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
