Imports Microsoft.Win32
Imports System.IO
Imports WpfHexaEditor.Core
Imports WpfHexaEditor.Core.CharacterTable
Imports WpfHexaEditor.Dialog
Imports WpfHexEditor.Sample.VB.MySettings

Namespace WPFHexaEditorExample
    Partial Public Class MainWindow

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub OpenMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)

            Dim fileDialog = New OpenFileDialog With {
                .Multiselect = True,
                .CheckFileExists = True
            }

            If fileDialog.ShowDialog() Is Nothing OrElse Not File.Exists(fileDialog.FileName) Then Return

            For Each ti As TabItem In FileTab.Items

                If ti.ToolTip.ToString() = fileDialog.FileName Then
                    ti.IsSelected = True
                    Return
                End If
            Next

            Windows.Application.Current.MainWindow.Cursor = Cursors.Wait

            For Each file As String In fileDialog.FileNames
                FileTab.Items.Add(New TabItem With {
                    .Header = Path.GetFileName(file),
                    .ToolTip = file
                })
            Next

            FileTab.SelectedIndex = FileTab.Items.Count - 1
            Windows.Application.Current.MainWindow.Cursor = Nothing
        End Sub

        Private Sub SaveMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Windows.Application.Current.MainWindow.Cursor = Cursors.Wait
            HexEdit.SubmitChanges()
            Windows.Application.Current.MainWindow.Cursor = Nothing
        End Sub

        Private Sub CloseFileMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            CloseFile()
        End Sub

        Private Sub Window_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs)
            HexEdit.CloseProvider()
            [Default].Save()
        End Sub

        Private Sub ExitMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Close()
        End Sub

        Private Sub CopyHexaMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.CopyToClipboard(CopyPasteMode.HexaString)
        End Sub

        Private Sub CopyStringMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.CopyToClipboard()
        End Sub

        Private Sub DeleteSelectionMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.DeleteSelection()
        End Sub

        Private Sub GOPosition_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Dim position = Nothing

            If Long.TryParse(PositionText.Text, position) Then
                HexEdit.SetPosition(position, 1)
            Else
                MessageBox.Show("Enter long value.")
            End If

            ViewMenu.IsSubmenuOpen = False
        End Sub

        Private Sub PositionText_TextChanged(ByVal sender As Object, ByVal e As TextChangedEventArgs)
            Dim __ = Nothing
            GoPositionButton.IsEnabled = Long.TryParse(PositionText.Text, __)
        End Sub

        Private Sub UndoMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.Undo()
        End Sub

        Private Sub RedoMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.Redo()
        End Sub

        Private Sub SetBookMarkButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.SetBookMark()
        End Sub

        Private Sub DeleteBookmark_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.ClearScrollMarker(ScrollMarker.Bookmark)
        End Sub

        Private Sub FindAllSelection_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.FindAllSelection(True)
        End Sub

        Private Sub SelectAllButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.SelectAll()
        End Sub

        Private Sub CTableASCIIButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.TypeOfCharacterTable = CharacterTableType.Ascii
            CTableAsciiButton.IsChecked = True
            CTableTblButton.IsChecked = False
            CTableTblDefaultAsciiButton.IsChecked = False
        End Sub

        Private Sub CTableTBLButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Dim fileDialog = New OpenFileDialog()

            If fileDialog.ShowDialog() Is Nothing Then Return
            If Not File.Exists(fileDialog.FileName) Then Return

            Windows.Application.Current.MainWindow.Cursor = Cursors.Wait
            HexEdit.LoadTblFile(fileDialog.FileName)
            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile
            CTableAsciiButton.IsChecked = False
            CTableTblButton.IsChecked = True
            CTableTblDefaultAsciiButton.IsChecked = False
            Windows.Application.Current.MainWindow.Cursor = Nothing
        End Sub

        Private Sub CTableTBLDefaultASCIIButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Windows.Application.Current.MainWindow.Cursor = Cursors.Wait
            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile
            HexEdit.LoadDefaultTbl()
            Windows.Application.Current.MainWindow.Cursor = Nothing
        End Sub

        Private Sub SaveAsMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Dim fileDialog = New SaveFileDialog()
            If fileDialog.ShowDialog() IsNot Nothing Then HexEdit.SubmitChanges(fileDialog.FileName, True)
        End Sub

        Private Sub CTableTblDefaultEBCDICButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Windows.Application.Current.MainWindow.Cursor = Cursors.Wait
            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile
            HexEdit.LoadDefaultTbl(DefaultCharacterTableType.EbcdicWithSpecialChar)
            Windows.Application.Current.MainWindow.Cursor = Nothing
        End Sub

        Private Sub CTableTblDefaultEBCDICNoSPButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Windows.Application.Current.MainWindow.Cursor = Cursors.Wait
            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile
            HexEdit.LoadDefaultTbl(DefaultCharacterTableType.EbcdicNoSpecialChar)
            Windows.Application.Current.MainWindow.Cursor = Nothing
        End Sub

        Private Sub FindMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Call New FindWindow(HexEdit, HexEdit.GetSelectionByteArray()) With {
                .Owner = Me
            }.Show()
        End Sub

        Private Sub ReplaceMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Call New FindReplaceWindow(HexEdit, HexEdit.GetSelectionByteArray()) With {
                .Owner = Me
            }.Show()
        End Sub

        Private Sub ReverseSelection_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            HexEdit.ReverseSelection()
        End Sub

        Private Sub FileTab_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)

            'Declare variables
            Dim tc = If(Not (TypeOf sender Is TabControl), Nothing, DirectCast(sender, TabControl))
            If tc Is Nothing Then Return

            Dim ti = If(Not (TypeOf tc.SelectedValue Is TabItem), Nothing, DirectCast(tc.SelectedValue, TabItem))
            If ti Is Nothing Then Return

            'Save currentState
            If e.RemovedItems.Count Then
                Dim lastSelectedTabItem As TabItem = e.RemovedItems(0)

                If e.RemovedItems.Count > 0 AndAlso lastSelectedTabItem IsNot Nothing Then
                    lastSelectedTabItem.Tag = HexEdit.CurrentState
                End If
            End If

            'Load file
            If Not File.Exists(ti.ToolTip.ToString()) Then Return
            HexEdit.FileName = ti.ToolTip.ToString()

            'Update CurrentState
            If Not TypeOf ti.Tag Is XDocument Then Return
            HexEdit.CurrentState = ti.Tag

        End Sub

        Private Sub Image_MouseUp(ByVal sender As Object, ByVal e As MouseButtonEventArgs)
            CloseFile()
        End Sub

        Private Sub CloseFile()
            If FileTab.SelectedIndex = -1 Then Return

            HexEdit.CloseProvider()
            FileTab.Items.RemoveAt(FileTab.SelectedIndex)
        End Sub

        Private Sub CloseAllFileMenu_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            FileTab.Items.Clear()
            HexEdit.CloseProvider()
        End Sub
    End Class
End Namespace
