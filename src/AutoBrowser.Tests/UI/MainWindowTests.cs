using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Patterns;
using Xunit;

namespace AutoBrowser.Tests.UI;

public class MainWindowTests : IDisposable
{
    private readonly AppLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    [Fact]
    public void App_Launches_MainWindow_IsVisible()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();

        Assert.NotNull(mainWindow);
        Assert.True(mainWindow.IsAvailable);
    }

    [Fact]
    public void MainWindow_HasCorrect_Title()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();

        Assert.Contains("AutoBrowser", mainWindow?.Name);
    }

    [Fact]
    public void MainWindow_ContainsNavigationView()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();

        var navView = mainWindow?.FindFirstDescendant(cf => cf.ByControlType(ControlType.List));

        Assert.NotNull(navView);
    }

    [Fact]
    public void MainWindow_HomePage_IsDefault()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(2000);

        var routingRulesText = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText("Routing Rules"));

        Assert.NotNull(routingRulesText);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("About")]
    [InlineData("Settings")]
    public void MainWindow_CanNavigateToPage(string pageName)
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(2000);

        var navItem = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText(pageName));

        navItem?.Click();
        Thread.Sleep(1000);

        var pageTitle = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText(pageName));

        Assert.NotNull(pageTitle);
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Edit")]
    [InlineData("Delete")]
    [InlineData("Move Up")]
    [InlineData("Move Down")]
    [InlineData("Test URL")]
    [InlineData("Check Update")]
    public void MainWindow_Toolbar_HasButton(string buttonText)
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(2000);

        var button = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText(buttonText)));

        Assert.NotNull(button);
    }

    [Fact]
    public void MainWindow_AddButton_OpensRuleEditor()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(3000);

        var addButton = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        addButton?.Click();
        Thread.Sleep(2000);

        var editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
            .FirstOrDefault(w => w != mainWindow && (
                w.Name.Contains("Routing Rule") ||
                w.FindFirstDescendant(cf => cf.ByText("Rule Name")) != null));

        Assert.NotNull(editorWindow);
    }

    [Fact]
    public void MainWindow_AddRule_FillForm_Save()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(3000);

        var addButton = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        addButton?.Click();
        Thread.Sleep(2000);

        var editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
            .FirstOrDefault(w => w != mainWindow && (
                w.Name.Contains("Routing Rule") ||
                w.Name.Contains("Rule") ||
                w.FindFirstDescendant(cf => cf.ByText("Rule Name")) != null ||
                w.FindFirstDescendant(cf => cf.ByText("URL Pattern")) != null));

        if (editorWindow == null)
        {
            Thread.Sleep(2000);
            editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
                .FirstOrDefault(w => w != mainWindow);
        }

        Assert.NotNull(editorWindow);

        var nameBox = editorWindow!.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox();
        Assert.NotNull(nameBox);
        nameBox.WaitUntilClickable();
        nameBox?.Enter("Test Rule from UI");
        Thread.Sleep(500);

        var patternBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PatternBox"))?.AsTextBox();
        Assert.NotNull(patternBox);
        patternBox?.Enter("test-example.com");
        Thread.Sleep(500);

        var browserPathBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("BrowserPathBox"))?.AsTextBox();
        Assert.NotNull(browserPathBox);
        browserPathBox?.Enter(@"C:\test\browser.exe");
        Thread.Sleep(500);

        var okButton = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(okButton);
        okButton?.Click();
        Thread.Sleep(5000);

        var ruleInList = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText("Test Rule from UI"));

        Assert.NotNull(ruleInList);
    }

    [Fact]
    public void MainWindow_EditRule_FillForm_Save()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(3000);

        var addButton = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        addButton?.Click();
        Thread.Sleep(2000);

        var editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
            .FirstOrDefault(w => w != mainWindow && (
                w.Name.Contains("Routing Rule") ||
                w.Name.Contains("Rule") ||
                w.FindFirstDescendant(cf => cf.ByText("Rule Name")) != null ||
                w.FindFirstDescendant(cf => cf.ByText("URL Pattern")) != null));

        if (editorWindow == null)
        {
            Thread.Sleep(2000);
            editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
                .FirstOrDefault(w => w != mainWindow);
        }

        Assert.NotNull(editorWindow);

        var nameBox = editorWindow!.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox();
        Assert.NotNull(nameBox);
        nameBox.WaitUntilClickable();
        nameBox?.Enter("Rule To Edit");
        Thread.Sleep(500);

        var patternBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PatternBox"))?.AsTextBox();
        Assert.NotNull(patternBox);
        patternBox?.Enter("edit-me.com");
        Thread.Sleep(500);

        var browserPathBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("BrowserPathBox"))?.AsTextBox();
        Assert.NotNull(browserPathBox);
        browserPathBox?.Enter(@"C:\test\browser.exe");
        Thread.Sleep(500);

        var okButton = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(okButton);
        okButton?.Click();
        Thread.Sleep(2000);

        var addedRule = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit"));
        Assert.NotNull(addedRule);
        
        // Select the rule
        addedRule?.Click();
        Thread.Sleep(1000);

        // Click Edit
        var editButton = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Edit")));
        editButton?.Click();
        Thread.Sleep(2000);

        var editWindow = app.GetAllTopLevelWindows(_launcher.Automation)
            .FirstOrDefault(w => w != mainWindow && (
                w.Name.Contains("Routing Rule") ||
                w.Name.Contains("Rule") ||
                w.FindFirstDescendant(cf => cf.ByText("Rule Name")) != null ||
                w.FindFirstDescendant(cf => cf.ByText("URL Pattern")) != null));

        if (editWindow == null)
        {
            Thread.Sleep(2000);
            editWindow = app.GetAllTopLevelWindows(_launcher.Automation)
                .FirstOrDefault(w => w != mainWindow);
        }

        Assert.NotNull(editWindow);

        var editNameBox = editWindow!.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox();
        Assert.NotNull(editNameBox);
        editNameBox?.Click();
        editNameBox?.Enter(" Edited");
        Thread.Sleep(500);

        var editOkButton = editWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(editOkButton);
        editOkButton?.Click();
        Thread.Sleep(2000);

        var editedRule = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText("Rule To Edit Edited"));
        Assert.NotNull(editedRule);
    }

    [Fact]
    public void MainWindow_DeleteRule()
    {
        var app = _launcher.Launch();
        var mainWindow = app.GetMainWindow(_launcher.Automation, TimeSpan.FromSeconds(10));
        _launcher.DismissBlockingDialogs();
        Thread.Sleep(3000);

        var addButton = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Add")));
        addButton?.Click();
        Thread.Sleep(2000);

        var editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
            .FirstOrDefault(w => w != mainWindow && (
                w.Name.Contains("Routing Rule") ||
                w.Name.Contains("Rule") ||
                w.FindFirstDescendant(cf => cf.ByText("Rule Name")) != null ||
                w.FindFirstDescendant(cf => cf.ByText("URL Pattern")) != null));

        if (editorWindow == null)
        {
            Thread.Sleep(2000);
            editorWindow = app.GetAllTopLevelWindows(_launcher.Automation)
                .FirstOrDefault(w => w != mainWindow);
        }

        Assert.NotNull(editorWindow);

        var nameBox = editorWindow!.FindFirstDescendant(cf =>
            cf.ByAutomationId("NameBox"))?.AsTextBox();
        Assert.NotNull(nameBox);
        nameBox.WaitUntilClickable();
        nameBox?.Enter("Rule To Delete");
        Thread.Sleep(500);

        var patternBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("PatternBox"))?.AsTextBox();
        Assert.NotNull(patternBox);
        patternBox?.Enter("delete-me.com");
        Thread.Sleep(500);

        var browserPathBox = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("BrowserPathBox"))?.AsTextBox();
        Assert.NotNull(browserPathBox);
        browserPathBox?.Enter(@"C:\test\browser.exe");
        Thread.Sleep(500);

        var okButton = editorWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("OkButton"));
        Assert.NotNull(okButton);
        okButton?.Click();
        Thread.Sleep(2000);

        var addedRule = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText("Rule To Delete"));
        Assert.NotNull(addedRule);
        
        // Select the rule
        addedRule?.Click();
        Thread.Sleep(1000);

        // Click Delete
        var deleteButton = mainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByText("Delete")));
        deleteButton?.Click();
        Thread.Sleep(2000);

        var deletedRule = mainWindow?.FindFirstDescendant(cf =>
            cf.ByText("Rule To Delete"));
        Assert.Null(deletedRule);
    }
}
