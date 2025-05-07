using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs;

public class CreateVisitDTO
{
    [Range(0, int.MaxValue)]
    public int VisitId { get; set; }
    [Range(0, int.MaxValue)]
    public int ClientId { get; set; }
    [MaxLength(14)]
    public string MechanicLicenceNumber { get; set; }
    public List<ServiceAddDTO> Services { get; set; }
}