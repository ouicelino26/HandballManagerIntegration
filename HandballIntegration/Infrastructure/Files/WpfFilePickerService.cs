using HandballIntegration.Admin.Abstractions;
using Microsoft.Win32;

namespace HandballIntegration.Infrastructure.Files;

public sealed class WpfFilePickerService : IFilePickerService
{
    public Task<string?> PickFileAsync(string filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
            Title = "Selectionner une source d'integration"
        };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }
}
