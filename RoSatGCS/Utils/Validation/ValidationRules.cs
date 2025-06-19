using GMap.NET.MapProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RoSatGCS.Utils.Validation
{
    public class IPValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? address = value as string;
            if (address == null)
            {
                return new ValidationResult(false, "Invalid IP Address");
            }

            address = address.Trim();

            Match match = Regex.Match(address, @"^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$");
            if (match.Success)
            {
                return ValidationResult.ValidResult;
            }

            return new ValidationResult(false, "Invalid IP Address");
        }
    }

    public class PortValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (int.TryParse(value as string, out int number))
            {
                if (number >= 0 && number <= 65535)
                    return ValidationResult.ValidResult;
                else
                    return new ValidationResult(false, "Value must be between 0 and 65535");
            }
            return new ValidationResult(false, "Invalid number");
        }
    }
    public class FrequencyValidationRule : ValidationRule {
        public override ValidationResult Validate(object o, CultureInfo cultureInfo)
        {
            string? value = o as string;
            if (value == null)
            {
                return new ValidationResult(false, "Invalid Frequency");
            }

            value = value.ToLower().Trim();
            if (value.EndsWith("hz"))
            {
                if (value.EndsWith("ghz"))
                {
                    var val = value[..^3];
                    if (double.TryParse(val, out double d))
                    {
                        return ValidationResult.ValidResult;
                    }
                }
                else if (value.EndsWith("mhz"))
                {
                    var val = value[..^3];
                    if (double.TryParse(val, out double d))
                    {
                        return ValidationResult.ValidResult;
                    }
                }
                else if (value.EndsWith("khz"))
                {
                    var val = value[..^3];
                    if (double.TryParse(val, out double d))
                    {
                        return ValidationResult.ValidResult;
                    }
                }
                else if (value.EndsWith("hz"))
                {
                    var val = value[..^2];
                    if (double.TryParse(val, out double d))
                    {
                        return ValidationResult.ValidResult;
                    }
                }
            }
            else
            {
                if (long.TryParse(value, out long ret))
                {
                    return ValidationResult.ValidResult;
                }
            }
            return new ValidationResult(false, "Invalid Frequency");
        }
    }
    public class RadioMacAddressValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? macAddress = value as string;
            if (macAddress == null)
            {
                return new ValidationResult(false, "Invalid MAC Address");
            }

            macAddress = macAddress.Trim().ToLower();

            if (macAddress.StartsWith("0x") && macAddress.Length == 4)
            {
                if(!byte.TryParse(macAddress.Replace("0x", ""),System.Globalization.NumberStyles.AllowHexSpecifier, null, out byte result))
                {
                    return new ValidationResult(false, "Invalid MAC Address");
                }
            }
            else if (!byte.TryParse(macAddress, out byte result))
            {
                return new ValidationResult(false, "Invalid MAC Address");
            }
            return ValidationResult.ValidResult;
        }
    }

    public class RFConfigValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? rfconfig = value as string;
            if (rfconfig == null)
            {
                return new ValidationResult(false, "Invalid RF Config");
            }

            rfconfig = rfconfig.Trim().ToLower();

            if (rfconfig.StartsWith("0x") && rfconfig.Length == 4)
            {
                if (!byte.TryParse(rfconfig.Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier, null, out byte result))
                {
                    return new ValidationResult(false, "Invalid RF Config");
                }
            }
            else if (!byte.TryParse(rfconfig, out byte result))
            {
                return new ValidationResult(false, "Invalid RF Config");
            }
            return ValidationResult.ValidResult;
        }
    }

    public class AesIVValidationRule : ValidationRule
    {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (int.TryParse(value as string, out int number))
            {
                /*
                if (number >= Min && number <= Max)
                    return ValidationResult.ValidResult;
                else
                    return new ValidationResult(false, $"Value must be between {Min} and {Max}");*/
            }
            return new ValidationResult(false, "Invalid number");
        }
    }
}
