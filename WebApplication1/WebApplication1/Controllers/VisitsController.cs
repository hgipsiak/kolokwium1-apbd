using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Exceptions;
using WebApplication1.Models.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;

        public VisitsController(IVisitService visitService)
        {
            _visitService = visitService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisitById(int id)
        {
            try
            {
                var visit = await _visitService.GetVisit(id);
                return Ok(visit);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddVisit(CreateVisitDTO dto)
        {
            if (!dto.Services.Any())
            {
                return BadRequest("At least one service must be provided");
            }

            try
            {
                await _visitService.AddVisit(dto);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (ConflictException e)
            {
                return Conflict(e.Message);
            }
            
            return CreatedAtAction(nameof(GetVisitById), dto.VisitId, dto);
        }

    }
}
