using System.IO;
using System.Windows;

namespace WpfHexaEditor.Dialog
{
    /// <summary>
    /// Logique d'interaction pour FindReplaceWindow.xaml
    /// </summary>
    public partial class FindReplaceWindow
    {
        private MemoryStream _findMs;
        private MemoryStream _replaceMs;
        private readonly HexEditor _parent;

        public FindReplaceWindow(HexEditor parent)
        {
            InitializeComponent();

            //Parent hexeditor for "binding" search
            _parent = parent;
            
            InitializeMStreamFind();
            InitializeMStreamReplace();
        }

        private void FindAllButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindAll(FindHexEdit.GetAllBytes(), true);

        private void FindFirstButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindFirst(FindHexEdit.GetAllBytes());

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void FindNextButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindNext(FindHexEdit.GetAllBytes());

        private void FindLastButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindLast(FindHexEdit.GetAllBytes());

        private void ClearButton_Click(object sender, RoutedEventArgs e) => InitializeMStreamFind();

        private void ClearReplaceButton_Click(object sender, RoutedEventArgs e) => InitializeMStreamReplace();

        /// <summary>
        /// Initialize stream and hexeditor
        /// </summary>
        private void InitializeMStreamFind()
        {
            FindHexEdit.CloseProvider();
            _findMs = new MemoryStream(1);
            _findMs.WriteByte(0);
            FindHexEdit.Stream = _findMs;
        }

        /// <summary>
        /// Initialize stream and hexeditor
        /// </summary>
        private void InitializeMStreamReplace()
        {
            ReplaceHexEdit.CloseProvider();
            _replaceMs = new MemoryStream(1);
            _replaceMs.WriteByte(0);
            ReplaceHexEdit.Stream = _replaceMs;
        }

    }
}
