namespace OrderProcessorService.Models;

public record Order(Guid Id, string Description, decimal Amount);
