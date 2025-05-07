using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public interface IVisitService
{
    Task<VisitDTO> GetVisit(int userId);
    Task AddVisit(CreateVisitDTO visit);
}