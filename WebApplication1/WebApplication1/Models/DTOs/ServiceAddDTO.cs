using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs;

public class ServiceAddDTO
{
    [MaxLength(100)]
    public string ServiceName { get; set; }
    public decimal ServiceFee { get; set; }
}