namespace Scrapile.Desktop.Behaviors;

using System;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

/// <summary>
/// Behavior that enables two-way binding between TextEditor.Text and a ViewModel property.
/// AvaloniaEdit's Text property doesn't support direct two-way binding, so this behavior
/// bridges the gap by handling TextChanged events and property updates.
/// </summary>
public class DocumentTextBindingBehavior : Behavior<TextEditor>
{
    private TextEditor? _textEditor;
    private bool _isUpdating;

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<DocumentTextBindingBehavior, string>(
            nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is TextEditor editor)
        {
            _textEditor = editor;
            _textEditor.TextChanged += OnTextEditorTextChanged;

            // Initial sync from property to editor
            if (!string.IsNullOrEmpty(Text))
            {
                _textEditor.Text = Text;
            }
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (_textEditor != null)
        {
            _textEditor.TextChanged -= OnTextEditorTextChanged;
            _textEditor = null;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty && !_isUpdating && _textEditor != null)
        {
            var newText = change.GetNewValue<string>() ?? string.Empty;
            if (_textEditor.Text != newText)
            {
                // Preserve caret position when possible
                var caretOffset = Math.Min(_textEditor.CaretOffset, newText.Length);
                _textEditor.Text = newText;
                _textEditor.CaretOffset = caretOffset;
            }
        }
    }

    private void OnTextEditorTextChanged(object? sender, EventArgs e)
    {
        if (_textEditor == null || _isUpdating) return;

        _isUpdating = true;
        try
        {
            Text = _textEditor.Text;
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
