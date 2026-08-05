using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration.Components;

public sealed class AdminPageHeader : Control
{
    public static readonly DependencyProperty EyebrowProperty = DependencyProperty.Register(
        nameof(Eyebrow), typeof(string), typeof(AdminPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(AdminPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(AdminPageHeader), new PropertyMetadata(string.Empty));

    public string Eyebrow
    {
        get => (string)GetValue(EyebrowProperty);
        set => SetValue(EyebrowProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
}

public sealed class StatusBadge : Control
{
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty));

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
}

public sealed class AdminScopeBar : ContentControl { }
public sealed class LoadingState : ContentControl { }
public sealed class EmptyState : ContentControl { }
public sealed class ErrorState : ContentControl { }
public sealed class PermissionDeniedState : ContentControl { }
public sealed class ValidationSummary : ContentControl { }
public sealed class ConfirmationDialog : ContentControl { }
public sealed class ImpactPreviewDialog : ContentControl { }
public sealed class PaginationControl : ContentControl { }
