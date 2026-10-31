using System;
using System.IO;
using System.Linq;
using Avalonia.Dialogs;
using Avalonia.Dialogs.Internal;
using Avalonia.Platform.Storage;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class ManagedFileChooserTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

    public ManagedFileChooserTests()
    {
        _directory.CreateSubdirectory("folder");
        _directory.CreateSubdirectory("other");
        File.WriteAllText(Path.Combine(_directory.FullName, "file.txt"), "");
    }

    [Fact]
    public void Selecting_A_Folder_In_Open_File_Mode_Should_Keep_It_Selected()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateOpenViewModel();
        var folder = GetItem(vm, "folder");

        vm.SelectedItems.Add(folder);
        app.RunJobs();

        Assert.Same(folder, Assert.Single(vm.SelectedItems));
        Assert.True(vm.CanOk(null!));
    }

    [Fact]
    public void Ok_With_A_Single_Folder_Selected_Should_Navigate_Into_It()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateOpenViewModel();
        string[]? result = null;
        vm.CompleteRequested += r => result = r;

        vm.SelectedItems.Add(GetItem(vm, "folder"));
        app.RunJobs();
        vm.Ok();
        app.RunJobs();

        Assert.Null(result);
        Assert.Equal(Path.Combine(_directory.FullName, "folder"), vm.Location);
    }

    [Fact]
    public void Ok_Should_Not_Return_Selected_Folders()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateOpenViewModel(new FilePickerOpenOptions { AllowMultiple = true });
        string[]? result = null;
        vm.CompleteRequested += r => result = r;

        vm.SelectedItems.AddRange([GetItem(vm, "folder"), GetItem(vm, "file.txt")]);
        app.RunJobs();
        vm.Ok();

        Assert.NotNull(result);
        Assert.Equal([Path.Combine(_directory.FullName, "file.txt")], result);
        Assert.Equal("file.txt", vm.FileName);
    }

    [Fact]
    public void Ok_With_Only_Several_Folders_Selected_Should_Do_Nothing()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateOpenViewModel(new FilePickerOpenOptions { AllowMultiple = true });
        string[]? result = null;
        vm.CompleteRequested += r => result = r;

        vm.SelectedItems.AddRange([GetItem(vm, "folder"), GetItem(vm, "other")]);
        app.RunJobs();
        vm.Ok();
        app.RunJobs();

        Assert.Null(result);
        Assert.Equal(_directory.FullName, vm.Location);
    }

    [Fact]
    public void Ok_Should_Return_Selected_Folders_When_Directory_Selection_Is_Allowed()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateOpenViewModel(options: new ManagedFileDialogOptions { AllowDirectorySelection = true });
        string[]? result = null;
        vm.CompleteRequested += r => result = r;

        vm.SelectedItems.Add(GetItem(vm, "folder"));
        app.RunJobs();
        vm.Ok();

        Assert.NotNull(result);
        Assert.Equal([Path.Combine(_directory.FullName, "folder")], result);
    }

    [Fact]
    public void Selecting_A_Folder_In_Save_Mode_Should_Keep_The_File_Name()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateSaveViewModel("report.txt");

        vm.SelectedItems.Add(GetItem(vm, "folder"));
        app.RunJobs();

        Assert.Equal("report.txt", vm.FileName);
        Assert.Single(vm.SelectedItems);
        Assert.True(vm.CanOk(null!));
    }

    [Fact]
    public void Ok_With_A_Folder_Selected_In_Save_Mode_Should_Navigate_Into_It()
    {
        using var app = HeadlessUnitTestApplication.Start();
        var vm = CreateSaveViewModel("report.txt");
        string[]? result = null;
        vm.CompleteRequested += r => result = r;

        vm.SelectedItems.Add(GetItem(vm, "folder"));
        app.RunJobs();
        vm.Ok();
        app.RunJobs();

        Assert.Null(result);
        Assert.Equal(Path.Combine(_directory.FullName, "folder"), vm.Location);
        Assert.Equal("report.txt", vm.FileName);
    }

    public void Dispose() => _directory.Delete(true);

    private ManagedFileChooserViewModel CreateOpenViewModel(
        FilePickerOpenOptions? pickerOptions = null,
        ManagedFileDialogOptions? options = null)
    {
        var vm = new ManagedFileChooserViewModel(pickerOptions ?? new FilePickerOpenOptions(),
            options ?? new ManagedFileDialogOptions());
        vm.Navigate(_directory.FullName);
        return vm;
    }

    private ManagedFileChooserViewModel CreateSaveViewModel(string fileName)
    {
        var vm = new ManagedFileChooserViewModel(new FilePickerSaveOptions { SuggestedFileName = fileName },
            new ManagedFileDialogOptions());
        vm.Navigate(_directory.FullName);
        return vm;
    }

    private static ManagedFileChooserItemViewModel GetItem(ManagedFileChooserViewModel vm, string name) =>
        vm.Items.Single(i => i.DisplayName == name);
}
