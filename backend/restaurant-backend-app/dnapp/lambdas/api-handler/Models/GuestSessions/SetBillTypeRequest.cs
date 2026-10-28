using APIHandler.Entities;
using System.ComponentModel.DataAnnotations;

namespace APIHandler.Models.GuestSessions;

public record SetBillTypeRequest([property: Required] BillType? BillType);
