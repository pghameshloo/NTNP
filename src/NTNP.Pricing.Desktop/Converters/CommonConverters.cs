using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NTNP.Pricing.Desktop.Converters;

/// <summary>true/non-null/non-empty → Visible, else Collapsed.</summary>
public sealed class TruthyToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var truthy = value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            System.Collections.ICollection c => c.Count > 0,
            System.Collections.IEnumerable e => e.GetEnumerator().MoveNext(),
            _ => true,
        };
        if (Invert) truthy = !truthy;
        return truthy ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool b && !b;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => value is bool b && !b;
}

/// <summary>Formats a decimal with thousands separators, always LTR regardless of ambient FlowDirection (Section 23 tabular figures).</summary>
public sealed class IrrAmountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d ? d.ToString("N0", CultureInfo.InvariantCulture) : string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ForeignAmountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d ? d.ToString("N2", CultureInfo.InvariantCulture) : string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class PercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d ? (d * 100).ToString("N1", CultureInfo.InvariantCulture) + "%" : string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Maps a project/revision/template Status string (e.g. "Approved", "Locked") to one of the Section 23 badge styles' key suffix ("Success"/"Warning"/"Error"/"Info"/"Neutral").</summary>
public sealed class StatusToBadgeKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value as string) switch
    {
        "Approved" => "Success",
        "Locked" => "Neutral",
        "Rejected" => "Error",
        "PendingApproval" => "Warning",
        "UnderEngineeringReview" or "UnderCommercialReview" => "Info",
        "Draft" => "Neutral",
        "Superseded" => "Neutral",
        _ => "Info",
    };

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Persian display labels for the closed set of English-valued status/enum strings the API returns.</summary>
public sealed class StatusToPersianLabelConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["Draft"] = "پیش‌نویس",
        ["UnderEngineeringReview"] = "بررسی فنی",
        ["UnderCommercialReview"] = "بررسی بازرگانی",
        ["PendingApproval"] = "در انتظار تأیید",
        ["Approved"] = "تأیید شده",
        ["Rejected"] = "رد شده",
        ["Locked"] = "قفل شده",
        ["Superseded"] = "جایگزین شده",
        ["Admin"] = "مدیر سیستم",
        ["Engineering"] = "مهندسی",
        ["Commercial"] = "بازرگانی",
        ["Approver"] = "تأییدکننده",
        ["Viewer"] = "بیننده",
        ["Markup"] = "درصد سود",
        ["GrossMargin"] = "حاشیه سود ناخالص",
        ["Draft".ToLowerInvariant()] = "پیش‌نویس",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s && Labels.TryGetValue(s, out var label) ? label : value?.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>0..1 fraction → a pixel width, for the Dashboard's hand-drawn bar charts (ConverterParameter = max width in px, default 220).</summary>
public sealed class FractionToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fraction = value is double d ? d : 0d;
        var maxWidth = parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var m) ? m : 220d;
        return Math.Max(2d, fraction * maxWidth);
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Bind a role membership check directly in XAML: Converter={StaticResource RoleVisibilityConverter} ConverterParameter=Admin,Commercial</summary>
public sealed class RolesContainsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<string> roles || parameter is not string allowedCsv) return Visibility.Collapsed;
        var allowed = allowedCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return roles.Any(allowed.Contains) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
