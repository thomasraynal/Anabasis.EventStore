using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Anabasis.Common
{
    public abstract class BaseConfiguration : ICanValidate
    {
        public void Validate()
        {
            var context = new ValidationContext(this, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(this, context, validationResults, true);

            if (!isValid)
                throw new ValidationException(string.Join(",", validationResults.Select(validationResult => validationResult.ErrorMessage)));

        }
    }
}
