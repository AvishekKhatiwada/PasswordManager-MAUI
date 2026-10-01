#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
#elif WINDOWS
using Windows.Storage.Pickers;
using WinRT.Interop;
#endif

namespace SecurePasswordManager.Services;

public sealed class FileSaverService : IFileSaverService
{
    public async Task<bool> SaveToDownloadsAsync(string sourceFilePath, string suggestedFileName, CancellationToken cancellationToken = default)
    {
#if ANDROID
        var resolver = Android.App.Application.Context.ContentResolver;
        if (resolver is null)
        {
            return false;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            using var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, suggestedFileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "application/octet-stream");
            values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

            var uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri!, values);
            if (uri is null)
            {
                return false;
            }

            await using var output = resolver.OpenOutputStream(uri);
            if (output is null)
            {
                return false;
            }

            await using var input = File.OpenRead(sourceFilePath);
            await input.CopyToAsync(output, cancellationToken);
            return true;
        }
        else
        {
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                return false;
            }

            var downloadsDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)!.AbsolutePath;
            System.IO.Directory.CreateDirectory(downloadsDir);
            var destination = Path.Combine(downloadsDir, suggestedFileName);
            File.Copy(sourceFilePath, destination, overwrite: true);
            return true;
        }
#elif WINDOWS
        var window = (Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView) as Microsoft.UI.Xaml.Window;
        if (window is null)
        {
            return false;
        }

        var picker = new FileSavePicker
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
            SuggestedStartLocation = PickerLocationId.Downloads
        };
        var extension = Path.GetExtension(suggestedFileName);
        picker.FileTypeChoices.Add("Encrypted backup", [string.IsNullOrEmpty(extension) ? ".bin" : extension]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return false;
        }

        using var stream = await file.OpenStreamForWriteAsync();
        await using var input = File.OpenRead(sourceFilePath);
        await input.CopyToAsync(stream, cancellationToken);
        return true;
#else
        await Task.CompletedTask;
        return false;
#endif
    }
}
