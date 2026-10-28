using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace APIHandler.Helpers;

public class DataAnnotationsValidator
{
    public static bool TryValidate(object value, [NotNullWhen(false)] out APIGatewayProxyResponse? errorResponse)
    {
        var list = new List<ValidationResult>();
        Console.WriteLine(value);
        bool valid = Validator.TryValidateObject(value, new ValidationContext(value), list, validateAllProperties: true);
        errorResponse = valid ? null : ResponseBuilder.CreateJsonResponse(400, new
        {
            Title = "One or more validation errors occurred.",
            Errors = list.SelectMany(e => e.MemberNames, (e, member) => (member, e.ErrorMessage))
                .GroupBy(x => x.member, x => x.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.AsEnumerable()),
        });
        return valid;
    }
}
