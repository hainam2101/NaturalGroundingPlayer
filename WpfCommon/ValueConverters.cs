﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace EmergenceGuardian.WpfCommon {
    /// <summary>
    /// Converts media lenght format such as 1:25
    /// </summary>
    [ValueConversion(typeof(short?), typeof(String))]
    public class MediaTimeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                TimeSpan TimeValue;
                if (value.GetType() != typeof(TimeSpan))
                    TimeValue = new TimeSpan(0, 0, 0, System.Convert.ToInt32(value));
                else
                    TimeValue = (TimeSpan)value;
                if (TimeValue.TotalHours < 1)
                    return TimeValue.ToString("m\\:ss");
                else
                    return TimeValue.ToString("h\\:mm\\:ss");
            } else
                return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            string strValue = value as string;
            if (string.IsNullOrEmpty(strValue))
                return null;
            else {
                try {
                    int SepCount = strValue.Count(x => x == ':');
                    DateTime ResultDate;
                    if (SepCount > 1)
                        ResultDate = DateTime.ParseExact(strValue, "h\\:mm\\:ss", CultureInfo.InvariantCulture);
                    else
                        ResultDate = DateTime.ParseExact(strValue, "m\\:ss", CultureInfo.InvariantCulture);
                    return (short)ResultDate.TimeOfDay.TotalSeconds;
                }
                catch {
                    return null;
                }
            }
        }
    }

    /// <summary>
    /// Converts ratings such as '9.5'. Parameter is maximum allowed value.
    /// </summary>
    [ValueConversion(typeof(double?), typeof(String))]
    public class RatingConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double? MaxValue = null;
            double MaxValueOut;
            if (parameter != null) {
                double.TryParse(parameter.ToString(), out MaxValueOut);
                MaxValue = MaxValueOut;
            }

            double? DecValue = value as double?;
            if (DecValue == null)
                return "";
            else if (MaxValue < 0 && DecValue > 9999)
                // Display max 9999
                return "9999";
            else
                // Display the digit for up to 10 for Height and Depth, and up to 100 for Power.
                return DecValue.Value.ToString(DecValue < (MaxValue == 10 ? 10 : 100) ? "0.0" : "0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            string strValue = value as string;
            if (string.IsNullOrEmpty(strValue))
                return null;
            else {
                double Result = 0;
                double? MaxValue = null;
                double MaxValueOut;
                if (parameter != null) {
                    double.TryParse(parameter.ToString(), out MaxValueOut);
                    MaxValue = MaxValueOut;
                }

                if (double.TryParse(strValue, out Result) && Result >= 0 && (!MaxValue.HasValue || Result <= MaxValue))
                    return (double)Math.Round(Result, Result < 100 ? 1 : 0); // Allow digit only for up to 100.
                else
                    return null;
            }
        }
    }

    /// <summary>
    /// Converts ratings into a color ranging from 0.0 = Hue 180 (Cyan) to 11.0 = Hue 0 (Red).
    /// </summary>
    public class RatingToColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Color Result = Color.FromRgb(0, 0, 0);
            double? DecValue = value as double?;
            if (DecValue != null) {
                if (DecValue > 10)
                    DecValue = 10;
                double Hue = (11 - DecValue.Value) / 11 * .5f;
                Result = HSBtoRGB(Hue, 1, .7f);
            }
            if (targetType.GetType() == typeof(Color))
                return Result;
            else
                return new SolidColorBrush(Result);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }

        public static Color HSBtoRGB(double hue, double saturation, double brightness) {
            int r = 0, g = 0, b = 0;
            if (saturation == 0) {
                r = g = b = (int)(brightness * 255.0f + 0.5f);
            } else {
                double h = (hue - (double)Math.Floor(hue)) * 6.0f;
                double f = h - (double)Math.Floor(h);
                double p = brightness * (1.0f - saturation);
                double q = brightness * (1.0f - saturation * f);
                double t = brightness * (1.0f - (saturation * (1.0f - f)));
                switch ((int)h) {
                    case 0:
                        r = (int)(brightness * 255.0f + 0.5f);
                        g = (int)(t * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int)(q * 255.0f + 0.5f);
                        g = (int)(brightness * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(brightness * 255.0f + 0.5f);
                        b = (int)(t * 255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(q * 255.0f + 0.5f);
                        b = (int)(brightness * 255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int)(t * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(brightness * 255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int)(brightness * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(q * 255.0f + 0.5f);
                        break;
                }
            }
            return Color.FromArgb(System.Convert.ToByte(255), System.Convert.ToByte(r), System.Convert.ToByte(g), System.Convert.ToByte(b));
        }
    }

    /// <summary>
    /// Converts an enumeration to bind in a 
    /// </summary>
    public class EnumToIntConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return Enum.ToObject(targetType, (int)value);
        }
    }

    [ValueConversion(typeof(long), typeof(string))]
    public class FileSizeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            double size = (long)value;
            int unit = 0;

            while (size >= 1024) {
                size /= 1024;
                ++unit;
            }

            return String.Format("{0:0.#} {1}", size, units[unit]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class RadioButtonCheckedConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    public class BooleanAndConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            foreach (object value in values) {
                if ((value is bool) && (bool)value == false) {
                    return false;
                }
            }
            return true;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
        }
    }


    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    public class VisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return (System.Windows.Visibility)value == System.Windows.Visibility.Visible;
        }
    }

    /// <summary>
    /// Allows Min/Max binding validation.
    /// </summary>
    public class NumericRangeRule : ValidationRule {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public int? Mod { get; set; }
        public bool AllowNull { get; set; }

        public NumericRangeRule() {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (AllowNull && string.IsNullOrEmpty((string)value))
                return new ValidationResult(true, null);

            double NumValue = 0;

            if (((string)value).Length > 0)
                if (!double.TryParse((String)value, out NumValue))
                    return new ValidationResult(false, "Invalid number");

            if ((Min.HasValue && NumValue < Min) || (Max.HasValue && NumValue > Max))
                return new ValidationResult(false, "Please enter a value in the range: " + Min + " - " + Max);
            else if (Mod.HasValue && NumValue % Mod != 0)
                return new ValidationResult(false, "Value must be a multiple of " + Mod);
            else
                return new ValidationResult(true, null);
        }
    }
}
