using System;

namespace ReportsHandler.Models;

public class Report
{
    public required string ReservationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ReservationStart { get; set; }

    public DateTime ReservationEnd { get; set; }

    public required string WaiterId { get; set; }

    public required string LocationId { get; set; }

    public int ServiceRating { get; set; }

    public int CuisineRating { get; set; }

    public decimal Revenue { get; set; }
}