![example](Images/Logo.png?raw=true)
  
[![NuGet](https://img.shields.io/badge/Nuget-v1.4.0-green.svg)](https://www.nuget.org/packages/WPFHexaEditor/)
[![NetFramework](https://img.shields.io/badge/.Net%20Framework-4.7-green.svg)](https://www.microsoft.com/net/download/windows)
[![NetFramework](https://img.shields.io/badge/Language-C%23%207.0-orange.svg)](https://blogs.msdn.microsoft.com/dotnet/2016/08/24/whats-new-in-csharp-7-0/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://github.com/abbaye/WpfHexEditorControl/blob/master/LICENSE)

A fast, fully customisable Wpf user control for editing file or stream as hexadecimal. 

Can be used in WPF or WinForm application.

Localized in English, French, Russian, Polish, Portuguese and Chinese

### You want to say thank or just like project  ?

Hexeditor control is totaly free and can be used in all project you want like open source and commercial applications. I make it in my free time and a few colaborators help me when they can... Please hit the ⭐️ button or fork and I will be very happy ;) I accept help contribution... 

## Screenshots

Sample with standard ASCII character table
![example](Images/Sample11-NOTBL.png?raw=true)

Sample with custom character table (TBL) on SNES Final Fantasy II US
![example](Images/Sample9-TBL.png?raw=true)

Sample use ByteShiftLeft and BytePerLine properties with custom TBL for edit fixed lenght table...
![example](Images/Sample12-FIXEDTBL-BYTESHIFT.png?raw=true)

Sample use of find dialog... (Replace dialog are under construction)
![example](Images/Sample13-FindDialog.png?raw=true)

## What is TBL (custom character table)
The TBL are small plaintext .tbl files that link every hexadecimal value with a character, which proves most useful when reading and changing text data. Wpf HexEditor support .tbl and you can define your custom character table as you want.

Unicode TBL are supported. For use put value at the right of equal (=) like this (0401=塞西尔) or (42=Д) in you plaintext .tbl file.

![example](Images/TBLExplain.png?raw=true)

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
