using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RoSatGCS.Utils.Validation
{
    public class SatelliteNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string s)
            {
                if (s.Length > 24)
                {
                    return new ValidationResult(false, "Satellite Name must be 24 characters or less.");
                }
                else
                {
                    return ValidationResult.ValidResult;

                }
            }
            return new ValidationResult(false, "Invalid satellite name.");
        }
    }

    public class NoradIdValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            Regex.IsMatch(value as string ?? "", @"^\d{1,5}$")
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "NORAD ID must be 1–5 digits.");
    }

    public class IntlDesignatorValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            Regex.IsMatch(value as string ?? "", @"^\d{2}\d{3}[A-Z]?$")
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Intl. Designator format: YYNNN[A-Z]");
    }
    public class EpochValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if(Regex.IsMatch(value as string ?? "", @"^\d{5}\.\d{1,8}$"))
            {
                return ValidationResult.ValidResult;
            }

            if(value is string str)
            {
                if(DateTime.TryParse(str, out DateTime dt))
                {
                    return ValidationResult.ValidResult;
                }
            }

            return new ValidationResult(false, "Epoch format: YYDDD.FFFFFFFF");
        }

    }

    public class SetNumberValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
             Regex.IsMatch(value as string ?? "", @"^\d{1,4}$")
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Set Number must be numeric.");
    }

    public class InclinationValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d) && d >= 0 && d <= 180
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Inclination must be 0–180°.");
    }

    public class RAANValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d) && d >= 0 && d <= 360
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "RAAN must be 0–360°.");
    }

    public class EccentricityValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Eccentricity must be 7-digit integer (decimal omitted).");
    }

    public class ArgPerigeeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d) && d >= 0 && d <= 360
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Argument of Perigee must be 0–360°.");
    }

    public class MeanAnomalyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d) && d >= 0 && d <= 360
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Mean Anomaly must be 0–360°.");
    }

    public class MeanMotionValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d) && d > 0 && d < 20
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Mean Motion must be in realistic range (0–20 rev/day).");
    }

    public class RevAtEpochValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            int.TryParse(value as string, out _)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Revolution number must be numeric.");
    }

    public class MeanMotionDtValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out _)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "First derivative of Mean Motion must be a number.");
    }

    public class MeanMotionDt2ValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out double d) && d < 1
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Second derivative of Mean Motion must be a number less than 1.");
    }

    public class BStarValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
            double.TryParse(value as string, out _)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "BSTAR must be in scientific format (e.g., 1.23456E-05).");
    }

}
