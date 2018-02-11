[![NetFramework](https://img.shields.io/badge/.Net%20Framework-4.7-green.svg)](https://www.microsoft.com/net/download/windows)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://github.com/abbaye/WpfHexEditorControl/blob/master/LICENSE)

A fast, fully customisable Wpf user control for editing file or stream as hexadecimal. 

Can be used in WPF or WinForm application.

Localized in English, French, Russian and Chinese

## Somes features
- Append byte at end of file
- Include HexBox, an Hexadecimal TextBox with spinner
- Fill selection (or another array) with byte.
- Support of common key in window like CTRL+C, CTRL+V, CTRL+Z, CTRL+A, ESC...
- Copy to clipboard as code like C#, VB.Net, C, Java, F# ... 
- Support custom .TBL character table file insted of default ASCII.
- Undo (no redo for now)
- Finds methods (FindFirst, FindNext, FindAll, FindLast, FindSelection) and overload for (string, byte[])
- Highlight byte with somes find methods
- Bookmark
- Group byte in block 
- Show data as hexadecimal or decimal
- ...

## How to use
Add a reference to `WPFHexaEditor.dll` from your project, then add the following namespace to your XAML:

```xaml
xmlns:control="clr-namespace:WpfHexaEditor;assembly=WPFHexaEditor"
```

Insert the control like this:

```xaml
<control:HexEditor/>
<control:HexEditor Width="NaN" Height="NaN"/>
<control:HexEditor Width="Auto" Height="Auto"/>
<control:HexEditor FileName={Binding FileNamePath} Width="Auto" Height="Auto"/>
```
