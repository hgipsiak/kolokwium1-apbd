using System.Data.Common;
using Microsoft.Data.SqlClient;
using WebApplication1.Exceptions;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public class VisitService : IVisitService
{
    private readonly string _connectionString;

    public VisitService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<VisitDTO> GetVisit(int userId)
    {
        string query =
            @"SELECT Visit.date, Client.first_name, Client.last_name, Client.date_of_birth, Mechanic.mechanic_id, licence_number, s.name, service_fee
                        FROM Visit
                        JOIN Client ON Visit.client_id = Client.client_id
                        JOIN Mechanic ON Visit.mechanic_id = Mechanic.mechanic_id
                        JOIN Visit_Service ON Visit.visit_id = Visit_Service.visit_id
                        JOIN ""Service"" s ON Visit_Service.service_id = s.service_id
                        WHERE Visit.visit_id = 1";
        
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = query;
        
        await connection.OpenAsync();
        
        var reader = await command.ExecuteReaderAsync();
        
        VisitDTO? result = null;

        while (await reader.ReadAsync())
        {
            if (result is null)
            {
                result = new VisitDTO()
                {
                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                    Client = new ClientDTO()
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                        LastName = reader.GetString(reader.GetOrdinal("last_name")),
                        DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth"))
                    },
                    Mechanic = new MechanicDTO()
                    {
                        MechanicId = reader.GetInt32(reader.GetOrdinal("mechanic_id")),
                        LicenceNumber = reader.GetString(reader.GetOrdinal("licence_number"))
                    },
                    VisitServices = new List<VisitServicesDTO>()
                };
            }
            
            result.VisitServices.Add(new VisitServicesDTO()
            {
                Name = reader.GetString(reader.GetOrdinal("name")),
                ServiceFee = reader.GetDecimal(reader.GetOrdinal("service_fee"))
            });
        }

        if (result is null)
        {
            throw new NotFoundException($"Visit with id:{userId} does not exists");
        }
        
        return result;
    }

    public async Task AddVisit(CreateVisitDTO visit)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = @"SELECT Client_id FROM Client WHERE client_id = @clientId";
            command.Parameters.AddWithValue("@clientId", visit.ClientId);

            var client = (int)await command.ExecuteScalarAsync();
            if (client == 0)
            {
                throw new NotFoundException("Client does not exists");
            }

            command.Parameters.Clear();
            command.CommandText = @"SELECT Count(*) FROM Mechanic WHERE licence_number = @licenceNumber";
            command.Parameters.AddWithValue("@licenceNumber", visit.MechanicLicenceNumber);

            var mechanic = (int)await command.ExecuteScalarAsync();
            if (mechanic == 0)
            {
                throw new NotFoundException("Mechanic does not exists");
            }

            command.Parameters.Clear();
            command.CommandText =
                @"INSERT INTO Visit VALUES(@visitId, @clientId, @mechanicLicenceNumber, @licenceNumber)";
            command.Parameters.AddWithValue("@visitId", visit.VisitId);
            command.Parameters.AddWithValue("@clientId", visit.ClientId);
            command.Parameters.AddWithValue("@mechanicLicenceNumber", visit.MechanicLicenceNumber);

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new ConflictException("Visit already exists");
            }

            foreach (var service in visit.Services)
            {
                command.Parameters.Clear();
                command.CommandText = "SELECT Service_id FROM Service WHERE Service.name = @serviceName";
                command.Parameters.AddWithValue("@serviceId", service.ServiceName);

                var serviceId = (int)await command.ExecuteScalarAsync();
                if (serviceId == 0)
                {
                    throw new NotFoundException("Service does not exists");
                }

                command.Parameters.Clear();
                command.CommandText = "INSERT INTO Visit_Service (visit_id, service_id) VALUES(@visitId, @serviceId)";
                command.Parameters.AddWithValue("@visitId", visit.VisitId);
                command.Parameters.AddWithValue("@serviceId", serviceId);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}