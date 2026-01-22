using Avalonia.Controls;

namespace Scrapile.Desktop.Views;

public partial class EditorView : UserControl
{
    public EditorView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Focuses the content editor.
    /// </summary>
    public void FocusContent()
    {
        ContentTextBox.Focus();
    }

    /// <summary>
    /// Focuses the title editor.
    /// </summary>
    public void FocusTitle()
    {
        TitleTextBox.Focus();
        TitleTextBox.SelectAll();
    }
}
