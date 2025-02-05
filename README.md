# evoleap/AvalonEdit

This is a fork of AvalonEdit.

# 1. Modifications:
- Added "Hiding" feature
    - It enables hiding of lines or group of lines   
    - It shares some logic with "Folding" feature, but with some important differences/notes:
        - Hiding is used as a one-time application (can be toggled on or off), and not periodically checked like folding
        - Hiding sections should not overlap with each other or with folding sections
        - With folding sections, the first line is never collapsed and there is a corresponding visual line - with hiding this is not the case. This has several consequences:
            - Multiple places in code need to be adjusted to overcome that limit
            - It can cause the whole document to be hidden - we need to disable the editor in such case
            - Text removal - multiple actions can lead to text removal and some of them lead to `Editing\EditingCommandHandler.cs -> static ExecutedRoutedEventHandler OnDelete(CaretMovementType caretMovement)` method
                - Current logic for text removal is based on old and new visual positions of the caret, which is fine for folding but not for hiding.
    - Progress:
        - Currently, when we switch to filtered view (where some lines are hidden) we make it read-only, since there are some noticed bugs with text removal (adding lines or modifying parts of lines seems fine - what is tested so far)
        - To enable editing, we need to:
            - Probably differentiate between folding and hiding `CollapsedLineSection` - partially implemented (see also `HeightTree` - it does not directly hold `CollapsedLineSections`).
            - Adjust text removal logic (and potentially some other places) to accommodate this:
                - Removal without selection
                - Removal with selection
                    - Here we also need to split removal into multiple removals in some cases
                - ...
- Added the `UseLongestLineWidthAsScrollableWidth` option
    - This option allows the longest line width to be used as the scrollable width, similar to behavior seen in VS Code and several other editors.
    - Example:
        - ``` 
            textEditor.Options.UseLongestLineWidthAsScrollableWidth = true;
            ...
            textView.ShouldUpdateLongestLineWidth = true;
            textView.InvalidateMeasure();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() =>
            {
                textView.ShouldUpdateLongestLineWidth = false;
            })); 