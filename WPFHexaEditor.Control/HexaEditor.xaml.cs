//////////////////////////////////////////////
// Apache 2.0  - 2016-2017
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.Core.Interfaces;
using WpfHexaEditor.Dialog;
using WpfHexaEditor.Core.MethodExtention;
using Path = System.IO.Path;

namespace WpfHexaEditor
{
    /// <summary> 
    /// WPF HexEditor control
    /// </summary>
    public partial class HexEditor : IDisposable
    {
        #region Global class variables
        /// <summary>
        /// Byte provider for work with file or stream currently loaded in control.
        /// </summary>
        private ByteProvider _provider;

        /// <summary>
        /// The large change of scroll when clicked on scrollbar
        /// </summary>
        private double _scrollLargeChange = 100;

        /// <summary>
        /// List of byte are highlighted
        /// </summary>
        private Dictionary<long, long> _markedPositionList = new Dictionary<long, long>();

        /// <summary>
        /// Byte position in file when mouse right click occurs.
        /// </summary>
        private long _rightClickBytePosition = -1;

        /// <summary>
        /// Custom character table loaded. Used for show byte as texte.
        /// </summary>
        private TblStream _tblCharacterTable;

        /// <summary>
        /// Hold the count of all byte in file.
        /// </summary>
        private long[] _bytecount;

        /// <summary>
        /// Save the view byte buffer as a field. 
        /// To save the time when Scolling i do not building them every time when scolling.
        /// </summary>
        private byte[] _viewBuffer;

        /// <summary>
        /// Used for control the view on refresh
        /// </summary>
        private long _priLevel;

        /// <summary>
        /// Used with VerticalMoveByTime methods/events to move the scrollbar.
        /// </summary>
        private bool _mouseOnBottom, _mouseOnTop;

        /// <summary>
        /// Used with VerticalMoveByTime methods/events to move the scrollbar.
        /// </summary>
        private long _bottomEnterTimes, _topEnterTimes;

        /// <summary>
        /// Caret used in control to view position
        /// </summary>
        private readonly Caret _caret = new Caret();

        #endregion Global Class variables

        #region Events

        /// <summary>
        /// Occurs when selection start are changed.
        /// </summary>
        public event EventHandler SelectionStartChanged;

        /// <summary>
        /// Occurs when selection stop are changed.
        /// </summary>
        public event EventHandler SelectionStopChanged;

        /// <summary>
        /// Occurs when the lenght of selection are changed.
        /// </summary>
        public event EventHandler SelectionLenghtChanged;

        /// <summary>
        /// Occurs when data are copie to clipboard.
        /// </summary>
        public event EventHandler DataCopied;

        /// <summary>
        /// Occurs when the type of character table are changed.
        /// </summary>
        public event EventHandler TypeOfCharacterTableChanged;

        /// <summary>
        /// Occurs when a long process percent changed.
        /// </summary>
        public event EventHandler LongProcessProgressChanged;

        /// <summary>
        /// Occurs when a long process are started.
        /// </summary>
        public event EventHandler LongProcessProgressStarted;

        /// <summary>
        /// Occurs when a long process are completed.
        /// </summary>
        public event EventHandler LongProcessProgressCompleted;

        /// <summary>
        /// Occurs when readonly property are changed.
        /// </summary>
        public event EventHandler ReadOnlyChanged;

        /// <summary>
        /// Occurs when data are saved to stream/file.
        /// </summary>
        public event EventHandler ChangesSubmited;

        /// <summary>
        /// Occurs when the replace byte by byte are completed
        /// </summary>
        public event EventHandler ReplaceByteCompleted;

        /// <summary>
        /// Occurs when the fill with byte method are completed
        /// </summary>
        public event EventHandler FillWithByteCompleted;

        #endregion Events

        #region Constructor

        public HexEditor()
        {
            InitializeComponent();

            //Refresh view
            UpdateScrollBar();
            InitializeCaret();
            RefreshView(true);

            DataContext = this;
        }

        #endregion Contructor

        #region Build-in CTRL key property

        public bool AllowBuildinCtrlc
        {
            get => (bool) GetValue(AllowBuildinCtrlcProperty);
            set => SetValue(AllowBuildinCtrlcProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLC.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlcProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrlc), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrlv
        {
            get => (bool) GetValue(AllowBuildinCtrlvProperty);
            set => SetValue(AllowBuildinCtrlvProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLV.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlvProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrlv), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrla
        {
            get => (bool) GetValue(AllowBuildinCtrlaProperty);
            set => SetValue(AllowBuildinCtrlaProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLA.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlaProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrla), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrlz
        {
            get => (bool) GetValue(AllowBuildinCtrlzProperty);
            set => SetValue(AllowBuildinCtrlzProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlzProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrlz), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        private static void Control_AllowBuildinCTRLPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue) ctrl.RefreshView();
        }

        #endregion Build-in CTRL key property

        #region Colors/fonts property and methods

        public Brush SelectionFirstColor
        {
            get => (Brush) GetValue(SelectionFirstColorProperty);
            set => SetValue(SelectionFirstColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectionFirstColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionFirstColorProperty =
            DependencyProperty.Register(nameof(SelectionFirstColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.CornflowerBlue, Control_ColorPropertyChanged));

        public Brush SelectionSecondColor
        {
            get => (Brush) GetValue(SelectionSecondColorProperty);
            set => SetValue(SelectionSecondColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectionFirstColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionSecondColorProperty =
            DependencyProperty.Register(nameof(SelectionSecondColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.LightSteelBlue, Control_ColorPropertyChanged));

        public Brush ByteModifiedColor
        {
            get => (Brush) GetValue(ByteModifiedColorProperty);
            set => SetValue(ByteModifiedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteModifiedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteModifiedColorProperty =
            DependencyProperty.Register(nameof(ByteModifiedColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.DarkGray, Control_ColorPropertyChanged));

        public Brush MouseOverColor
        {
            get => (Brush) GetValue(MouseOverColorProperty);
            set => SetValue(MouseOverColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for MouseOverColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MouseOverColorProperty =
            DependencyProperty.Register(nameof(MouseOverColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.LightSkyBlue, Control_ColorPropertyChanged));

        public Brush ByteDeletedColor
        {
            get => (Brush) GetValue(ByteDeletedColorProperty);
            set => SetValue(ByteDeletedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteDeletedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteDeletedColorProperty =
            DependencyProperty.Register(nameof(ByteDeletedColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Red, Control_ColorPropertyChanged));

        public Brush HighLightColor
        {
            get => (Brush) GetValue(HighLightColorProperty);
            set => SetValue(HighLightColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for HighLightColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighLightColorProperty =
            DependencyProperty.Register(nameof(HighLightColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Gold, Control_ColorPropertyChanged));

        public Brush ForegroundOffSetHeaderColor
        {
            get => (Brush) GetValue(ForegroundOffSetHeaderColorProperty);
            set => SetValue(ForegroundOffSetHeaderColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundOffSetHeaderColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundOffSetHeaderColorProperty =
            DependencyProperty.Register(nameof(ForegroundOffSetHeaderColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Gray, Control_ForegroundOffSetHeaderColorPropertyChanged));

        private static void Control_ForegroundOffSetHeaderColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
            {
                ctrl.UpdateHeader();
                ctrl.UpdateLinesOffSet();
            }
        }

        public new Brush Background
        {
            get => (Brush) GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public new Brush Foreground
        {
            get => (Brush) GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Black, Control_ColorPropertyChanged));

        /// <summary>
        /// Second foreground colors used in hexbyte
        /// </summary>
        public Brush ForegroundSecondColor
        {
            get => (Brush)GetValue(ForegroundSecondColorProperty);
            set => SetValue(ForegroundSecondColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundSecond.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundSecondColorProperty =
            DependencyProperty.Register(nameof(ForegroundSecondColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Black, Control_ColorPropertyChanged));


        public Brush ForegroundContrast
        {
            get => (Brush) GetValue(ForegroundContrastProperty);
            set => SetValue(ForegroundContrastProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundContrastColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundContrastProperty =
            DependencyProperty.Register(nameof(ForegroundContrast), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.White, Control_ColorPropertyChanged));

        // Using a DependencyProperty as the backing store for  Background.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.White, Control_BackgroundColorPropertyChanged));

        private static void Control_BackgroundColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.BaseGrid.Background = (Brush) e.NewValue;
        }

        private static void Control_ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.UpdateVisual();
        }

        public new FontFamily FontFamily
        {
            get => (FontFamily) GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontFamily.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(HexEditor),
                new FrameworkPropertyMetadata(new FontFamily("Courier New"),
                    FontFamily_Changed));

        private static void FontFamily_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.RefreshView(true);
        }

        /// <summary>
        /// Call Updatevisual methods for all IByteControl
        /// </summary>
        public void UpdateVisual() => TraverseHexAndStringBytes(ctrl => { ctrl.UpdateVisual(); });

        #endregion Colors/fonts property and methods

        #region Miscellaneous property/methods

        public double ScrollLargeChange
        {
            get => _scrollLargeChange;
            set
            {
                _scrollLargeChange = value;
                UpdateScrollBar();
            }
        }

        /// <summary>
        /// The name of your application to be showing in messagebox title
        /// </summary>
        public string ApplicationName { get; set; } = "Wpf HexEditor";

        /// <summary>
        /// Height of data line. 
        /// </summary>
        public double LineHeight
        {
            get => (double) GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for LineHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(HexEditor),
                new FrameworkPropertyMetadata(18D,
                    LineHeight_PropertyChanged));

        private static void LineHeight_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Control the mouse wheel speed
        /// </summary>
        public MouseWheelSpeed MouseWheelSpeed { get; set; } = MouseWheelSpeed.Normal;

        /// <summary>
        /// Set or get the visual of line offset header
        /// </summary>
        public DataVisualType OffSetStringVisual
        {
            get => (DataVisualType) GetValue(OffSetStringVisualProperty);
            set => SetValue(OffSetStringVisualProperty, value);
        }

        // Using a DependencyProperty as the backing store for OffSetStringVisual.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffSetStringVisualProperty =
            DependencyProperty.Register(nameof(OffSetStringVisual), typeof(DataVisualType), typeof(HexEditor),
                new FrameworkPropertyMetadata(DataVisualType.Hexadecimal,
                    DataVisualTypeProperty_PropertyChanged));


        private static void DataVisualTypeProperty_PropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.UpdateLinesOffSet();
        }

        public DataVisualType DataStringVisual
        {
            get => (DataVisualType) GetValue(DataStringVisualProperty);
            set => SetValue(DataStringVisualProperty, value);
        }

        // Using a DependencyProperty as the backing store for HexByteStringVisual.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataStringVisualProperty =
            DependencyProperty.Register(nameof(DataStringVisual), typeof(DataVisualType), typeof(HexEditor),
                new FrameworkPropertyMetadata(DataVisualType.Hexadecimal,
                    DataStringVisualTypeProperty_PropertyChanged));

        private static void DataStringVisualTypeProperty_PropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
            {
                ctrl.UpdateHeader();

                ctrl.TraverseHexBytes(hctrl =>
                {
                    hctrl.UpdateDataVisualWidth();
                    hctrl.UpdateLabelFromByte();
                });
            }
        }

        #endregion Miscellaneous property/methods

        #region Characters tables property/methods

        /// <summary>
        /// Type of caracter table are used un hexacontrol.
        /// For now, somes character table can be readonly but will change in future
        /// </summary>
        public CharacterTableType TypeOfCharacterTable
        {
            get => (CharacterTableType) GetValue(TypeOfCharacterTableProperty);
            set => SetValue(TypeOfCharacterTableProperty, value);
        }

        // Using a DependencyProperty as the backing store for TypeOfCharacterTable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeOfCharacterTableProperty =
            DependencyProperty.Register(nameof(TypeOfCharacterTable), typeof(CharacterTableType), typeof(HexEditor),
                new FrameworkPropertyMetadata(CharacterTableType.Ascii,
                    TypeOfCharacterTable_PropertyChanged));

        private static void TypeOfCharacterTable_PropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
            {
                ctrl.RefreshView(true);
                ctrl.TypeOfCharacterTableChanged?.Invoke(ctrl, new EventArgs());
            }
        }

        /// <summary>
        /// Show or not Multi Title Enconding (MTE) are loaded in TBL file
        /// </summary>
        public bool TblShowMte
        {
            get => (bool) GetValue(TblShowMteProperty);
            set => SetValue(TblShowMteProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLShowMTE.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblShowMteProperty =
            DependencyProperty.Register(nameof(TblShowMte), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true,
                    TBLShowMTE_PropetyChanged));

        private static void TBLShowMTE_PropetyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Load TBL Character table file in control. (Used for ROM reverse engineering)
        /// Load TBL Bookmark into control.
        /// Change CharacterTable property for use.
        /// </summary>
        public void LoadTblFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                _tblCharacterTable = new TblStream(fileName);

                TblLabel.Visibility = Visibility.Visible;
                TblLabel.ToolTip = $"TBL file : {fileName}";

                UpdateTblBookMark();

                BuildDataLines((int) MaxVisibleLine, true);
                RefreshView(true);
            }
        }

        /// <summary>
        /// Load TBL Character table file in control. (Used for ROM reverse engineering)
        /// Load TBL Bookmark into control.
        /// Change CharacterTable property for use.
        /// </summary>
        public void LoadDefaultTbl(DefaultCharacterTableType type = DefaultCharacterTableType.Ascii)
        {
            _tblCharacterTable = TblStream.CreateDefaultAscii();
            TblShowMte = false;

            TblLabel.Visibility = Visibility.Visible;
            TblLabel.ToolTip = $"{Properties.Resources.DefaultTBLString} : {type}";

            RefreshView();
        }

        /// <summary>
        /// Update TBL bookmark in control
        /// </summary>
        private void UpdateTblBookMark()
        {
            //Load from loaded TBL bookmark
            if (_tblCharacterTable != null)
                foreach (var mark in _tblCharacterTable.BookMarks)
                    SetScrollMarker(mark);
        }

        /// <summary>
        /// Get or set the color of DTE in string panel.
        /// </summary>
        public SolidColorBrush TbldteColor
        {
            get => (SolidColorBrush) GetValue(TbldteColorProperty);
            set => SetValue(TbldteColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TbldteColorProperty =
            DependencyProperty.Register(nameof(TbldteColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Red,
                    TBLColor_Changed));

        private static void TBLColor_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Get or set the color of MTE in string panel.
        /// </summary>
        public SolidColorBrush TblmteColor
        {
            get => (SolidColorBrush) GetValue(TblmteColorProperty);
            set => SetValue(TblmteColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblmteColorProperty =
            DependencyProperty.Register(nameof(TblmteColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.DarkSlateGray,
                    TBLColor_Changed));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TblEndBlockColor
        {
            get => (SolidColorBrush) GetValue(TblEndBlockColorProperty);
            set => SetValue(TblEndBlockColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblEndBlockColorProperty =
            DependencyProperty.Register(nameof(TblEndBlockColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Blue,
                    TBLColor_Changed));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TblEndLineColor
        {
            get => (SolidColorBrush) GetValue(TblEndLineColorProperty);
            set => SetValue(TblEndLineColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblEndLineColorProperty =
            DependencyProperty.Register(nameof(TblEndLineColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Blue,
                    TBLColor_Changed));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TblDefaultColor
        {
            get => (SolidColorBrush) GetValue(TblDefaultColorProperty);
            set => SetValue(TblDefaultColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblDefaultColorProperty =
            DependencyProperty.Register(nameof(TblDefaultColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Black,
                    TBLColor_Changed));

        #endregion Characters tables property/methods

        #region ReadOnly property/event

        /// <summary>
        /// Put the control on readonly mode.
        /// </summary>
        public bool ReadOnlyMode
        {
            get => (bool) GetValue(ReadOnlyModeProperty);
            set => SetValue(ReadOnlyModeProperty, value);
        }

        public static readonly DependencyProperty ReadOnlyModeProperty =
            DependencyProperty.Register("ReadOnlyMode", typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(false,
                    ReadOnlyMode_PropertyChanged));

        private static void ReadOnlyMode_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.RefreshView(true);
        }

        private void Provider_ReadOnlyChanged(object sender, EventArgs e)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                ReadOnlyMode = _provider.ReadOnlyMode;
                ReadOnlyChanged?.Invoke(this, new EventArgs());
            }
        }

        #endregion ReadOnly property/event

        #region ByteModified methods/event/property

        /// <summary>
        /// Stream or file are modified when IsModified are set to true.
        /// </summary>
        public bool IsModified
        {
            get => (bool)GetValue(IsModifiedProperty);
            internal set => SetValue(IsModifiedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsModified.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(HexEditor),
                new PropertyMetadata(false));
        
        private void Control_ByteModified(object sender, EventArgs e)
        {
            if (sender is IByteControl ctrl)
            {
                _provider.AddByteModified(ctrl.Byte, ctrl.BytePositionInFile);
                SetScrollMarker(ctrl.BytePositionInFile, ScrollMarker.ByteModified);
                UpdateByteModified();
            }

            UpdateStatusBar();
        }

        /// <summary>
        /// Delete selection, add scroll marker and update control
        /// </summary>
        public void DeleteSelection()
        {
            if (!CanDelete()) return;
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            var position = SelectionStart > SelectionStop ? SelectionStop : SelectionStart;

            _provider.AddByteDeleted(position, SelectionLength);

            SetScrollMarker(position, ScrollMarker.ByteDeleted);

            UpdateByteModified();
            UpdateSelection();
            UpdateStatusBar();
        }


        private void Control_ByteDeleted(object sender, EventArgs e) => DeleteSelection();

        #endregion ByteModified methods/event

        #region Lines methods

        /// <summary>
        /// Obtain the max line for verticalscrollbar
        /// </summary>
        public long MaxLine => ByteProvider.CheckIsOpen(_provider) ? _provider.Length / BytePerLine : 0;

        /// <summary>
        /// Get the number of row visible in control
        /// </summary>
        public long MaxVisibleLine
        {
            get
            {
                var actualheight = ActualHeight - HexHeaderStackPanel.ActualHeight - StatusBarGrid.ActualHeight;

                if (actualheight < 0) actualheight = 0;

                return (long) (actualheight / LineHeight) + 1;
            }
        }

        #endregion Lines methods

        #region Selection Property/Methods/Event

        /// <summary>
        /// Get the selected line of focus control
        /// </summary>
        public long SelectionLine
        {
            get => (long) GetValue(SelectionLineProperty);
            internal set => SetValue(SelectionLineProperty, value);
        }

        public static readonly DependencyProperty SelectionLineProperty =
            DependencyProperty.Register("SelectionLine", typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(0L));

        private void LineInfoLabel_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is FastTextLine line && e.LeftButton == MouseButtonState.Pressed)
                SelectionStop = ByteConverters.HexLiteralToLong(line.Text).position + BytePerLine - 1;
        }

        private void LineInfoLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FastTextLine line)
            {
                SelectionStart = ByteConverters.HexLiteralToLong(line.Text).position;
                SelectionStop = SelectionStart + BytePerLine - 1;
            }
        }

        private void Control_EscapeKey(object sender, EventArgs e)
        {
            UnSelectAll();
            UnHighLightAll();
            Focus();
        }

        private void Control_CTRLZKey(object sender, EventArgs e) => Undo();

        private void Control_CTRLCKey(object sender, EventArgs e) => CopyToClipboard();

        private void Control_CTRLAKey(object sender, EventArgs e) => SelectAll();

        private void Control_CTRLVKey(object sender, EventArgs e) => Paste(AllowExtend);

        private void Control_MovePageUp(object sender, EventArgs e)
        {
            var visibleLine = MaxVisibleLine;
            var byteToMove = BytePerLine * visibleLine;
            var test = SelectionStart - byteToMove;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test > -1)
                    SelectionStart -= byteToMove;
                else
                    SelectionStart = 0;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test > -1)
                {
                    SelectionStart -= byteToMove;
                    SelectionStop -= byteToMove;
                }
            }

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            if (sender is HexByte || sender is StringByte)
            {
                VerticalScrollBar.Value -= visibleLine - 1;
                SetFocusAtSelectionStart((IByteControl) sender);
            }
        }

        private void Control_MovePageDown(object sender, EventArgs e)
        {
            var visibleLine = MaxVisibleLine;
            var byteToMove = BytePerLine * visibleLine;
            var test = SelectionStart + byteToMove;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test < _provider.Length)
                    SelectionStart += byteToMove;
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test < _provider.Length)
                {
                    SelectionStart += byteToMove;
                    SelectionStop += byteToMove;
                }
            }

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            if (sender is HexByte || sender is StringByte)
            {
                VerticalScrollBar.Value += visibleLine - 1;
                SetFocusAtSelectionStart((IByteControl) sender);
            }
        }

        private void Control_MoveDown(object sender, EventArgs e)
        {
            var test = SelectionStart + BytePerLine;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test < _provider.Length)
                    SelectionStart += BytePerLine;
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test < _provider.Length)
                {
                    SelectionStart += BytePerLine;
                    SelectionStop += BytePerLine;
                }
            }

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void Control_MoveUp(object sender, EventArgs e)
        {
            var test = SelectionStart - BytePerLine;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test > -1)
                    SelectionStart -= BytePerLine;
                else
                    SelectionStart = 0;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test > -1)
                {
                    SelectionStart -= BytePerLine;
                    SelectionStop -= BytePerLine;
                }
            }

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void Control_Click(object sender, EventArgs e)
        {
            if (sender is IByteControl ctrl)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    SelectionStop = ctrl.BytePositionInFile;
                else
                {
                    SelectionStart = ctrl.BytePositionInFile;
                    SelectionStop = ctrl.BytePositionInFile;
                }

                UpdateSelectionColor(ctrl is StringByte ? FirstColor.StringByteData : FirstColor.HexByteData);

                UpdateVisual();
            }
        }

        private void Control_MouseSelection(object sender, EventArgs e)
        {
            //Prevent false mouse selection on file open
            if (SelectionStart == -1)
                return;

            if (sender is IByteControl bCtrl)
            {
                var focusedControl = Keyboard.FocusedElement;

                //update selection
                SelectionStop = bCtrl.BytePositionInFile != -1 ? bCtrl.BytePositionInFile : LastVisibleBytePosition;

                UpdateSelectionColor(focusedControl is HexByte ? FirstColor.HexByteData : FirstColor.StringByteData);

                UpdateSelection();
            }
        }

        /// <summary>
        /// Un highlight all byte as highlighted with find all methods
        /// </summary>
        public void UnHighLightAll()
        {
            _markedPositionList.Clear();
            UpdateHighLight();
            ClearScrollMarker(ScrollMarker.SearchHighLight);
        }

        /// <summary>
        /// Set the start byte position of selection
        /// </summary>
        public long SelectionStart
        {
            get => (long) GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        public static readonly DependencyProperty SelectionStartProperty =
            DependencyProperty.Register("SelectionStart", typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(-1L, SelectionStart_ChangedCallBack,
                    SelectionStart_CoerceValueCallBack));

        private static object SelectionStart_CoerceValueCallBack(DependencyObject d, object baseValue)
        {
            if (d is HexEditor ctrl)
            {
                var value = (long) baseValue;

                if (value < -1)
                    return -1L;

                return !ByteProvider.CheckIsOpen(ctrl._provider) ? -1L : baseValue;
            }

            return -1L;
        }

        private static void SelectionStart_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                if (e.NewValue != e.OldValue)
                {
                    ctrl.SelectionByte = ByteProvider.CheckIsOpen(ctrl._provider)
                        ? ctrl._provider.GetByte(ctrl.SelectionStart).singleByte
                        : null;

                    ctrl.UpdateSelection();
                    ctrl.UpdateSelectionLine();
                    ctrl.UpdateVisual();
                    ctrl.UpdateStatusBar();
                    ctrl.SetScrollMarker(0, ScrollMarker.SelectionStart);

                    ctrl.SelectionStartChanged?.Invoke(ctrl, new EventArgs());
                    ctrl.SelectionLenghtChanged?.Invoke(ctrl, new EventArgs());
                }
        }

        /// <summary>
        /// Set the start byte position of selection
        /// </summary>
        public long SelectionStop
        {
            get => (long) GetValue(SelectionStopProperty);
            set => SetValue(SelectionStopProperty, value);
        }

        public static readonly DependencyProperty SelectionStopProperty =
            DependencyProperty.Register("SelectionStop", typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(-1L, SelectionStop_ChangedCallBack,
                    SelectionStop_CoerceValueCallBack));

        private static object SelectionStop_CoerceValueCallBack(DependencyObject d, object baseValue)
        {
            if (d is HexEditor ctrl)
            {
                var value = (long) baseValue;

                if (value < -1)
                    return -1L;

                if (!ByteProvider.CheckIsOpen(ctrl._provider))
                    return -1L;

                if (value >= ctrl._provider.Length)
                    return ctrl._provider.Length;
            }

            return baseValue;
        }

        private static void SelectionStop_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
            {
                ctrl.UpdateSelection();
                ctrl.UpdateSelectionLine();

                ctrl.SelectionStopChanged?.Invoke(ctrl, new EventArgs());
                ctrl.SelectionLenghtChanged?.Invoke(ctrl, new EventArgs());
            }
        }

        /// <summary>
        /// Reset selection to -1
        /// </summary>
        public void UnSelectAll()
        {
            SelectionStart = -1;
            SelectionStop = -1;
        }

        /// <summary>
        /// Select the entire file
        /// If file are closed the selection will be set to -1
        /// </summary>
        public void SelectAll()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                SelectionStart = 0;
                SelectionStop = _provider.Length;
            }
            else
            {
                SelectionStart = -1;
                SelectionStop = -1;
            }

            UpdateSelection();
        }

        /// <summary>
        /// Get the lenght of byte are selected (base 1)
        /// </summary>
        public long SelectionLength => ByteProvider.GetSelectionLenght(SelectionStart, SelectionStop);

        /// <summary>
        /// Get byte array from current selection
        /// </summary>
        public byte[] SelectionByteArray
        {
            get
            {
                var ms = new MemoryStream();
                CopyToStream(ms, true);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Get string from current selection
        /// </summary>
        public string SelectionString
        {
            get
            {
                var ms = new MemoryStream();
                CopyToStream(ms, true);
                return ByteConverters.BytesToString(ms.ToArray());
            }
        }

        /// <summary>
        /// Get Hexadecimal from current selection
        /// </summary>
        public string SelectionHexa
        {
            get
            {
                var ms = new MemoryStream();
                CopyToStream(ms, true);
                return ByteConverters.ByteToHex(ms.ToArray());
            }
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) //UP
                VerticalScrollBar.Value -= e.Delta / 120 * (int) MouseWheelSpeed;

            if (e.Delta < 0) //Down
                VerticalScrollBar.Value += e.Delta / 120 * -(int) MouseWheelSpeed;
        }

        private void Control_MoveRight(object sender, EventArgs e)
        {
            var test = SelectionStart + 1;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test <= _provider.Length)
                    SelectionStart++;
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test < _provider.Length)
                {
                    SelectionStart++;
                    SelectionStop++;
                }
            }

            if (SelectionStart >= _provider.Length)
                SelectionStart = _provider.Length;

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void Control_MoveLeft(object sender, EventArgs e)
        {
            var test = SelectionStart - 1;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (test > -1)
                    SelectionStart--;
                else
                    SelectionStart = 0;
            }
            else
            {
                if (SelectionStart > SelectionStop)
                    SelectionStart = SelectionStop;
                else
                    SelectionStop = SelectionStart;

                if (test > -1)
                {
                    SelectionStart--;
                    SelectionStop--;
                }
            }

            if (SelectionStart < 0)
                SelectionStart = 0;

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            SetFocusAtSelectionStart(sender as IByteControl);
        }

        private void SetFocusAtSelectionStart(IByteControl sender)
        {
            switch (sender)
            {
                case HexByte _:
                    SetFocusHexDataPanel(SelectionStart);
                    break;
                case StringByte _:
                    SetFocusStringDataPanel(SelectionStart);
                    break;
            }
        }

        private void Control_MovePrevious(object sender, EventArgs e)
        {
            UpdateByteModified();

            //Call move left event
            Control_MoveLeft(sender, new EventArgs());
        }

        private void Control_MoveNext(object sender, EventArgs e)
        {
            UpdateByteModified();

            //Call moveright event
            Control_MoveRight(sender, new EventArgs());
        }

        #endregion Selection Property/Methods/Event

        #region Copy/Paste/Cut Methods

        public CopyPasteMode DefaultCopyToClipboardMode { get; set; } = CopyPasteMode.HexaString;

        /// <summary>
        /// Paste clipboard string without inserting byte at selection start
        /// </summary>
        /// <param name="expendIfneeded">Set AllowExpend to true for working</param>
        private void Paste(bool expendIfneeded)
        {
            if (!ByteProvider.CheckIsOpen(_provider) || SelectionStart <= -1) return;
            
            var clipBoardText = Clipboard.GetText();
            var (success, byteArray) = ByteConverters.IsHexaByteStringValue(clipBoardText);

            #region Expend stream if needed
            var pastelenght = success ? byteArray.Length : clipBoardText.Length;
            var needToBeExtent = _provider.Position + pastelenght > _provider.Length;
            var expend = false;
            if (expendIfneeded && AllowExtend && needToBeExtent)
                if (AppendNeedConfirmation)
                {
                    if (MessageBox.Show(Properties.Resources.PasteExtendByteConfirmationString, ApplicationName,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                        expend = true;
                }
                else
                    expend = true;
            #endregion

            if (success)
                _provider.Paste(SelectionStart, byteArray, expend);
            else
                _provider.Paste(SelectionStart, clipBoardText, expend);

            SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, Properties.Resources.PasteFromClipboardString);
            RefreshView();
        }

        /// <summary>
        /// Fill the selection with a Byte at selection start
        /// </summary>
        public void FillWithByte(byte val) => FillWithByte(SelectionStart, SelectionLength, val);

        /// <summary>
        /// Fill with a Byte at start position
        /// </summary>
        public void FillWithByte(long startPosition, long length, byte val)
        {
            if (!ByteProvider.CheckIsOpen(_provider) || (startPosition <= -1 || length <= 0)) return;

            _provider.FillWithByte(startPosition, length, val);
            SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, Properties.Resources.FillSelectionAloneString);
            RefreshView();
        }

        /// <summary>
        /// Replace byte with another at selection position
        /// </summary>
        public void ReplaceByte(byte original, byte replace) =>
            ReplaceByte(SelectionStart, SelectionLength, original, replace);

        /// <summary>
        /// Replace byte with another at start position
        /// </summary>
        public void ReplaceByte(long startPosition, long length, byte original, byte replace)
        {
            if (!ByteProvider.CheckIsOpen(_provider) || startPosition <= -1 || length <= 0) return;

            _provider.ReplaceByte(startPosition, length, original, replace);
            SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, Properties.Resources.ReplaceWithByteString);
            RefreshView();
        }

        /// <summary>
        /// Return true if Copy method could be invoked.
        /// </summary>
        public bool CanCopy() => SelectionLength >= 1 && ByteProvider.CheckIsOpen(_provider);

        /// <summary>
        /// Return true if delete method could be invoked.
        /// </summary>
        public bool CanDelete() => CanCopy() && !ReadOnlyMode;

        /// <summary>
        /// Copy to clipboard with default CopyPasteMode.ASCIIString
        /// </summary>
        public void CopyToClipboard() => CopyToClipboard(DefaultCopyToClipboardMode);

        /// <summary>
        /// Copy to clipboard the current selection with actual change in control
        /// </summary>
        public void CopyToClipboard(CopyPasteMode copypastemode) => CopyToClipboard(copypastemode, SelectionStart,
            SelectionStop, true, _tblCharacterTable);

        /// <summary>
        /// Copy to clipboard
        /// </summary>
        public void CopyToClipboard(CopyPasteMode copypastemode, long selectionStart, long selectionStop, bool copyChange, TblStream tbl)
        {
            if (!CanCopy()) return;
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            _provider.CopyToClipboard(copypastemode, selectionStart, selectionStop, copyChange, tbl);
        }

        /// <summary>
        /// Copy selection to a stream
        /// </summary>
        /// <param name="output">Output stream is not closed after copy</param>
        /// <param name="copyChange"></param>
        public void CopyToStream(Stream output, bool copyChange) =>
            CopyToStream(output, SelectionStart, SelectionStop, copyChange);

        /// <summary>
        /// Copy selection to a stream
        /// </summary>
        /// <param name="output">Output stream is not closed after copy</param>
        /// <param name="selectionStart"></param>
        /// <param name="selectionStop"></param>
        /// <param name="copyChange"></param>
        public void CopyToStream(Stream output, long selectionStart, long selectionStop, bool copyChange)
        {
            if (!CanCopy()) return;
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            _provider.CopyToStream(output, selectionStart, selectionStop, copyChange);
        }

        /// <summary>
        /// Occurs when data is copied in byteprovider instance
        /// </summary>
        private void Provider_DataCopied(object sender, EventArgs e) => DataCopied?.Invoke(sender, e);

        #endregion Copy/Paste/Cut Methods

        #region Set position methods

        /// <summary>
        /// Set position of cursor
        /// </summary>
        public void SetPosition(long position, long byteLenght)
        {
            SelectionStart = position;
            SelectionStop = position + byteLenght - 1;

            VerticalScrollBar.Value = ByteProvider.CheckIsOpen(_provider) ? GetLineNumber(position) : 0;
        }

        /// <summary>
        /// Get the line number of position in parameter
        /// </summary>
        public double GetLineNumber(long position) => position / BytePerLine;

        /// <summary>
        /// Set position in control at position in parameter
        /// </summary>
        public void SetPosition(long position) => SetPosition(position, 0);

        /// <summary>
        /// Set position in control at position in parameter
        /// </summary>
        public void SetPosition(string hexLiteralPosition) =>
            SetPosition(ByteConverters.HexLiteralToLong(hexLiteralPosition).position);

        /// <summary>
        /// Set position in control at position in parameter with specified selected lenght
        /// </summary>
        public void SetPosition(string hexLiteralPosition, long byteLenght) =>
            SetPosition(ByteConverters.HexLiteralToLong(hexLiteralPosition).position, byteLenght);

        #endregion Set position methods

        #region Visibility property

        /// <summary>
        /// Set or Get value for change visibility of hexadecimal panel
        /// </summary>
        public Visibility HexDataVisibility
        {
            get => (Visibility) GetValue(HexDataVisibilityProperty);
            set => SetValue(HexDataVisibilityProperty, value);
        }

        public static readonly DependencyProperty HexDataVisibilityProperty =
            DependencyProperty.Register("HexDataVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    HexDataVisibility_PropertyChanged,
                    Visibility_CoerceValue));

        private static object Visibility_CoerceValue(DependencyObject d, object baseValue) => 
            (Visibility)baseValue == Visibility.Hidden ? Visibility.Collapsed : (Visibility)baseValue;

        private static void HexDataVisibility_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                switch ((Visibility) e.NewValue)
                {
                    case Visibility.Visible:
                        ctrl.HexDataStackPanel.Visibility = Visibility.Visible;

                        if (ctrl.HeaderVisibility == Visibility.Visible)
                            ctrl.HexHeaderStackPanel.Visibility = Visibility.Visible;
                        break;

                    case Visibility.Collapsed:
                        ctrl.HexDataStackPanel.Visibility = Visibility.Collapsed;
                        ctrl.HexHeaderStackPanel.Visibility = Visibility.Collapsed;
                        break;
                }
        }

        /// <summary>
        /// Set or Get value for change visibility of hexadecimal header
        /// </summary>
        public Visibility HeaderVisibility
        {
            get => (Visibility) GetValue(HeaderVisibilityProperty);
            set => SetValue(HeaderVisibilityProperty, value);
        }

        public static readonly DependencyProperty HeaderVisibilityProperty =
            DependencyProperty.Register("HeaderVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    HeaderVisibility_PropertyChanged,
                    Visibility_CoerceValue));

        private static void HeaderVisibility_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                switch ((Visibility) e.NewValue)
                {
                    case Visibility.Visible:
                        if (ctrl.HexDataVisibility == Visibility.Visible)
                            ctrl.HexHeaderStackPanel.Visibility = Visibility.Visible;
                        break;

                    case Visibility.Collapsed:
                        ctrl.HexHeaderStackPanel.Visibility = Visibility.Collapsed;
                        break;
                }
        }

        /// <summary>
        /// Set or Get value for change visibility of string panel
        /// </summary>
        public Visibility StringDataVisibility
        {
            get => (Visibility) GetValue(StringDataVisibilityProperty);
            set => SetValue(StringDataVisibilityProperty, value);
        }

        public static readonly DependencyProperty StringDataVisibilityProperty =
            DependencyProperty.Register("StringDataVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    StringDataVisibility_ValidateValue,
                    Visibility_CoerceValue));

        private static void StringDataVisibility_ValidateValue(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                switch ((Visibility) e.NewValue)
                {
                    case Visibility.Visible:
                        ctrl.StringDataStackPanel.Visibility = Visibility.Visible;
                        break;
                    case Visibility.Collapsed:
                        ctrl.StringDataStackPanel.Visibility = Visibility.Collapsed;
                        break;
                }
        }

        /// <summary>
        /// Set or Get value for change visibility of status bar
        /// </summary>
        public Visibility StatusBarVisibility
        {
            get => (Visibility) GetValue(StatusBarVisibilityProperty);
            set => SetValue(StatusBarVisibilityProperty, value);
        }

        public static readonly DependencyProperty StatusBarVisibilityProperty =
            DependencyProperty.Register("StatusBarVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    StatusBarVisibility_ValueChange,
                    Visibility_CoerceValue));

        private static void StatusBarVisibility_ValueChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
            {
                switch ((Visibility)e.NewValue)
                {
                    case Visibility.Visible:
                        ctrl.StatusBarGrid.Visibility = Visibility.Visible;
                        break;

                    case Visibility.Collapsed:
                        ctrl.StatusBarGrid.Visibility = Visibility.Collapsed;
                        break;
                }

                ctrl.RefreshView();
            }
        }

        #endregion Visibility property

        #region Undo / Redo

        /// <summary>
        /// Clear undo and change
        /// </summary>
        public void ClearAllChange()
        {
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            _provider.ClearUndoChange();
        }

        /// <summary>
        /// Make undo of last the last bytemodified
        /// </summary>
        public void Undo(int repeat = 1)
        {
            UnSelectAll();

            if (!ByteProvider.CheckIsOpen(_provider)) return;

            for (var i = 0; i < repeat; i++)
                _provider.Undo();

            RefreshView();
        }

        /// <summary>
        /// NOT COMPLETED : Clear the scroll marker when undone 
        /// </summary>
        /// <param name="sender">List of long representing position in file are undone</param>
        /// <param name="e"></param>
        private void Provider_Undone(object sender, EventArgs e)
        {
            switch (sender)
            {
                case List<long> bytePosition:
                    foreach (var position in bytePosition)
                        ClearScrollMarker(position);
                    break;
            }

            IsModified = _provider.UndoCount > 0;
        }

        /// <summary>
        /// Get the undo count
        /// </summary>
        public long UndoCount => ByteProvider.CheckIsOpen(_provider) ? _provider.UndoCount : 0;

        /// <summary>
        /// Get the undo stack
        /// </summary>
        public Stack<ByteModified> UndoStack => ByteProvider.CheckIsOpen(_provider) ? _provider.UndoStack : null;

        #endregion Undo / Redo

        #region Open, Close, Save, byte provider ...

        private void Provider_ChangesSubmited(object sender, EventArgs e)
        {
            if (!(sender is ByteProvider bp)) return;

            //Refresh filename
            var filename = bp.FileName;
            CloseProvider();
            FileName = filename;

            ChangesSubmited?.Invoke(this, new EventArgs());
        }

        private void ProviderStream_ChangesSubmited(object sender, EventArgs e)
        {
            //Refresh stream
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            var stream = new MemoryStream(_provider.Stream.ToArray());
            CloseProvider();
            OpenStream(stream);

            ChangesSubmited?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Set or Get the file with the control will show hex
        /// </summary>
        public string FileName
        {
            get => (string) GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string), typeof(HexEditor),
                new FrameworkPropertyMetadata(string.Empty,
                    FileName_PropertyChanged));

        private static void FileName_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.OpenFile((string) e.NewValue);
        }

        /// <summary>
        /// Set the MemoryStream are used by ByteProvider
        /// </summary>
        public MemoryStream Stream
        {
            get => (MemoryStream) GetValue(StreamProperty);
            set => SetValue(StreamProperty, value);
        }

        // Using a DependencyProperty as the backing store for Stream.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StreamProperty =
            DependencyProperty.Register(nameof(Stream), typeof(MemoryStream), typeof(HexEditor),
                new FrameworkPropertyMetadata(null,
                    Stream_PropertyChanged));

        private static void Stream_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            ctrl.CloseProvider();
            ctrl.OpenStream((MemoryStream) e.NewValue);
        }

        /// <summary>
        /// Get the lenght of file/stream are opened in control
        /// </summary>
        public long Lenght => ByteProvider.CheckIsOpen(_provider) ? _provider.Length : -1;

        /// <summary>
        /// Close file and clear control
        /// ReadOnlyMode is reset to false
        /// </summary>
        public void CloseProvider()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                FileName = string.Empty;
                ReadOnlyMode = false;
                VerticalScrollBar.Value = 0;

                _provider.Close();
            }

            UnHighLightAll();
            ClearAllScrollMarker();
            UnSelectAll();
            RefreshView();
            UpdateHeader();
            UpdateScrollBar();

            //Debug
            Debug.Print("PROVIDER CLOSED");
        }

        /// <summary>
        /// Save to the current stream/file
        /// </summary>
        public void SubmitChanges()
        {
            if (!ByteProvider.CheckIsOpen(_provider) || _provider.ReadOnlyMode) return;

            _provider.SubmitChanges();
        }

        /// <summary>
        /// Save as to another file
        /// </summary>
        public void SubmitChanges(string newfilename, bool overwrite = false)
        {
            if (!ByteProvider.CheckIsOpen(_provider) || _provider.ReadOnlyMode) return;

            _provider.SubmitChanges(newfilename, overwrite);
        }

        /// <summary>
        /// Open file name
        /// </summary>
        /// <param name="filename"></param>
        private void OpenFile(string filename)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                CloseProvider();
                return;
            }

            if (File.Exists(filename))
            {
                CloseProvider();

                _provider = new ByteProvider(filename);

                if (_provider.IsEmpty)
                {
                    CloseProvider();
                    return;
                }

                _provider.ReadOnlyChanged += Provider_ReadOnlyChanged;
                _provider.DataCopiedToClipboard += Provider_DataCopied;
                _provider.ChangesSubmited += Provider_ChangesSubmited;
                _provider.Undone += Provider_Undone;
                _provider.LongProcessChanged += Provider_LongProcessProgressChanged;
                _provider.LongProcessStarted += Provider_LongProcessProgressStarted;
                _provider.LongProcessCompleted += Provider_LongProcessProgressCompleted;
                _provider.LongProcessCanceled += Provider_LongProcessProgressCompleted;
                _provider.FillWithByteCompleted += Provider_FillWithByteCompleted;
                _provider.ReplaceByteCompleted += Provider_ReplaceByteCompleted;

                UpdateScrollBar();
                UpdateHeader();

                //Load file with ASCII character table;
                var previousTable = TypeOfCharacterTable;
                TypeOfCharacterTable = CharacterTableType.Ascii;

                RefreshView(true);

                //Replace previous character table
                TypeOfCharacterTable = previousTable;

                UnSelectAll();

                UpdateTblBookMark();
                UpdateSelectionColor(FirstColor.HexByteData);

                //Update count of byte on file open
                UpdateByteCount();

                //Debug
                Debug.Print("FILE OPENED");
            }
            else
                throw new FileNotFoundException();
        }

        /// <summary>
        /// Open file name
        /// </summary>
        private void OpenStream(MemoryStream stream)
        {
            if (stream.CanRead)
            {
                CloseProvider();

                _provider = new ByteProvider(stream);

                if (_provider.IsEmpty)
                {
                    CloseProvider();
                    return;
                }

                _provider.ReadOnlyChanged += Provider_ReadOnlyChanged;
                _provider.DataCopiedToClipboard += Provider_DataCopied;
                _provider.ChangesSubmited += ProviderStream_ChangesSubmited;
                _provider.Undone += Provider_Undone;
                _provider.LongProcessChanged += Provider_LongProcessProgressChanged;
                _provider.LongProcessStarted += Provider_LongProcessProgressStarted;
                _provider.LongProcessCompleted += Provider_LongProcessProgressCompleted;
                _provider.LongProcessCanceled += Provider_LongProcessProgressCompleted;
                _provider.FillWithByteCompleted += Provider_FillWithByteCompleted;
                _provider.ReplaceByteCompleted += Provider_ReplaceByteCompleted;

                UpdateScrollBar();
                UpdateHeader();

                RefreshView(true);

                UnSelectAll();

                UpdateTblBookMark();
                UpdateSelectionColor(FirstColor.HexByteData);

                //Update count of byte
                UpdateByteCount();

                //Debug
                Debug.Print("STREAM OPENED");
            }
        }

        private void Provider_LongProcessProgressCompleted(object sender, EventArgs e)
        {
            LongProgressProgressBar.Visibility = Visibility.Collapsed;
            CancelLongProcessButton.Visibility = Visibility.Collapsed;

            #region Enable controls

            TraverseHexBytes(ctrl => ctrl.IsEnabled = true);
            TraverseStringBytes(ctrl => ctrl.IsEnabled = true);
            TraverseLineInfos(ctrl => ctrl.IsEnabled = true);
            TraverseHexHeader(ctrl => ctrl.IsEnabled = true);
            TopRectangle.IsEnabled = BottomRectangle.IsEnabled = true;
            VerticalScrollBar.IsEnabled = true;

            #endregion

            LongProcessProgressCompleted?.Invoke(this, new EventArgs());
        }

        private void Provider_LongProcessProgressStarted(object sender, EventArgs e)
        {
            LongProgressProgressBar.Visibility = Visibility.Visible;
            CancelLongProcessButton.Visibility = Visibility.Visible;

            #region Disable controls

            TraverseHexBytes(ctrl => ctrl.IsEnabled = false);
            TraverseStringBytes(ctrl => ctrl.IsEnabled = false);
            TraverseLineInfos(ctrl => ctrl.IsEnabled = false);
            TraverseHexHeader(ctrl => ctrl.IsEnabled = false);
            TopRectangle.IsEnabled = BottomRectangle.IsEnabled = false;
            VerticalScrollBar.IsEnabled = false;

            #endregion

            LongProcessProgressStarted?.Invoke(this, new EventArgs());
        }

        private void Provider_LongProcessProgressChanged(object sender, EventArgs e)
        {
            //Update progress bar
            LongProgressProgressBar.Value = (double) sender;
            Application.Current.DoEvents();

            LongProcessProgressChanged?.Invoke(this, new EventArgs());
        }

        private void Provider_ReplaceByteCompleted(object sender, EventArgs e) =>
            ReplaceByteCompleted?.Invoke(this, new EventArgs());

        private void Provider_FillWithByteCompleted(object sender, EventArgs e) =>
            FillWithByteCompleted?.Invoke(this, new EventArgs());

        private void CancelLongProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            _provider.IsOnLongProcess = false;
        }

        /// <summary>
        /// Check if byteprovider is on long progress and update control
        /// </summary>
        private void CheckProviderIsOnProgress()
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (_provider.IsOnLongProcess) return;
                CancelLongProcessButton.Visibility = Visibility.Collapsed;
                LongProgressProgressBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                CancelLongProcessButton.Visibility = Visibility.Collapsed;
                LongProgressProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        #endregion Open, Close, Save, byte provider ...

        #region Traverses methods

        /// <summary>
        /// Used to make action on all visible hexbyte
        /// </summary>
        private void TraverseHexBytes(Action<HexByte> act, ref bool exit)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //HexByte panel
            foreach (StackPanel hexDataStack in HexDataStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;
                foreach (var ctrl in hexDataStack.Children)
                    if (ctrl is HexByte hexCtrl)
                        act(hexCtrl);

                if (exit) return;
            }
        }

        /// <summary>
        /// Used to make action on all visible hexbyte
        /// </summary>
        private void TraverseHexBytes(Action<HexByte> act)
        {
            var exit = false;
            TraverseHexBytes(act, ref exit);
        }

        /// <summary>
        /// Used to make action on all visible stringbyte
        /// </summary>
        private void TraverseStringBytes(Action<StringByte> act, ref bool exit)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //Stringbyte panel
            foreach (StackPanel stringDataStack in StringDataStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;

                foreach (var ctrl in stringDataStack.Children)
                    if (ctrl is StringByte sbControl)
                        act(sbControl);

                if (exit) return;
            }
        }


        /// <summary>
        /// Used to make action on all visible stringbyte
        /// </summary>
        private void TraverseStringBytes(Action<StringByte> act)
        {
            var exit = false;
            TraverseStringBytes(act, ref exit);
        }

        /// <summary>
        /// Used to make action on all visible hexbyte and stringbyte.
        /// </summary>
        private void TraverseHexAndStringBytes(Action<IByteControl> act, ref bool exit)
        {
            TraverseStringBytes(act, ref exit);
            TraverseHexBytes(act, ref exit);
        }

        /// <summary>
        /// Used to make action on all visible hexbyte and stringbyte.
        /// </summary>
        private void TraverseHexAndStringBytes(Action<IByteControl> act)
        {
            var exit = false;
            TraverseHexAndStringBytes(act, ref exit);
        }

        /// <summary>
        /// Used to make action on all visible lineinfos
        /// </summary>
        private void TraverseLineInfos(Action<FastTextLine> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //lines infos panel
            foreach (var ctrl in LinesInfoStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;

                if (ctrl is FastTextLine lineInfo)
                    act(lineInfo);
            }
        }

        /// <summary>
        /// Used to make action on all visible header
        /// </summary>
        private void TraverseHexHeader(Action<FastTextLine> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //header panel
            foreach (var ctrl in HexHeaderStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;

                if (ctrl is FastTextLine column)
                    act(column);
            }
        }

        /// <summary>
        /// Used to make action on ScrollMarker
        /// </summary>
        private void TraverseScrollMarker(Action<Rectangle> act, ref bool exit)
        {
            for (var i = MarkerGrid.Children.Count - 1; i >= 0; i--)
            {
                if (MarkerGrid.Children[i] is Rectangle rect)
                    act(rect);

                if (exit) return;
            }
        }

        /// <summary>
        /// Used to make action on ScrollMarker
        /// </summary>
        private void TraverseScrollMarker(Action<Rectangle> act)
        {
            var exit = false;
            TraverseScrollMarker(act, ref exit);
        }

        #endregion Traverse methods

        #region BytePerLine property/methods

        /// <summary>
        /// Get or set the number of byte are show in control
        /// </summary>
        public int BytePerLine
        {
            get => (int) GetValue(BytePerLineProperty);
            set => SetValue(BytePerLineProperty, value);
        }

        public static readonly DependencyProperty BytePerLineProperty =
            DependencyProperty.Register("BytePerLine", typeof(int), typeof(HexEditor),
                new FrameworkPropertyMetadata(16, BytePerLine_PropertyChanged,
                    BytePerLine_CoerceValue));

        private static object BytePerLine_CoerceValue(DependencyObject d, object baseValue) => 
            (int) baseValue < 8 ? 8 : ((int) baseValue > 32 ? 32 : baseValue);

        private static void BytePerLine_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
            {
                //refresh
                ctrl.UpdateScrollBar();
                ctrl.BuildDataLines((int) ctrl.MaxVisibleLine, true);
                ctrl.RefreshView(true);
                ctrl.UpdateHeader(true);
            }
        }

        #endregion

        #region Update/Refresh view

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged) RefreshView(true);
        }

        private void VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshView();

        /// <summary>
        /// Update vertical scrollbar with file info
        /// </summary>
        private void UpdateScrollBar()
        {
            VerticalScrollBar.Visibility = Visibility.Collapsed;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                VerticalScrollBar.Visibility = Visibility.Visible;
                VerticalScrollBar.SmallChange = 1;
                VerticalScrollBar.LargeChange = ScrollLargeChange;
                VerticalScrollBar.Maximum = MaxLine - 1;
            }
        }

        /// <summary>
        /// Update de SelectionLine property
        /// </summary>
        private void UpdateSelectionLine() =>
            SelectionLine = ByteProvider.CheckIsOpen(_provider) ? SelectionStart / BytePerLine + 1 : 0;

        /// <summary>
        /// Refresh currentview of hexeditor
        /// </summary>
        /// <param name="controlResize"></param>
        /// <param name="refreshData"></param>
        public void RefreshView(bool controlResize = false, bool refreshData = true)
        {
#if DEBUG
            var start = DateTime.Now;
#endif

            UpdateLinesOffSet();

            if (refreshData)
                UpdateViewers(controlResize);

            //Update visual of byte control
            UpdateByteModified();
            UpdateSelection();
            UpdateHighLight();
            UpdateStatusBar();
            UpdateVisual();

            CheckProviderIsOnProgress();

            if (controlResize)
            {
                UpdateScrollMarkerPosition();
                UpdateHeader();
            }
#if DEBUG
            Debug.Print($"REFRESH TIME: {(DateTime.Now - start).Milliseconds} ms");
#endif
        }

        /// <summary>
        /// Update the selection of byte in hexadecimal panel
        /// </summary>
        private void UpdateSelectionColor(FirstColor coloring)
        {
            switch (coloring)
            {
                case FirstColor.HexByteData:
                    TraverseHexBytes(ctrl => { ctrl.FirstSelected = true; });
                    TraverseStringBytes(ctrl => { ctrl.FirstSelected = false; });
                    break;
                case FirstColor.StringByteData:
                    TraverseHexBytes(ctrl => { ctrl.FirstSelected = false; });
                    TraverseStringBytes(ctrl => { ctrl.FirstSelected = true; });
                    break;
            }
        }

        /// <summary>
        /// Build the stringbyte and hexabyte control used byte hexeditor
        /// </summary>
        /// <param name="maxline">Number of line to build</param>
        /// <param name="rebuild"></param>
        private void BuildDataLines(int maxline, bool rebuild = false)
        {
            var reAttachEvents = false;

            if (rebuild)
            {
                reAttachEvents = true;

                StringDataStackPanel.Children.Clear();
                HexDataStackPanel.Children.Clear();
            }

            for (var lineIndex = StringDataStackPanel.Children.Count; lineIndex < maxline; lineIndex++)
            {
                #region Build StringByte

                var dataLineStack = new StackPanel
                {
                    Height = LineHeight,
                    Orientation = Orientation.Horizontal
                };

                for (var i = 0; i < BytePerLine; i++)
                {
                    if (_tblCharacterTable == null && (ByteSpacerPositioning == ByteSpacerPosition.Both ||
                                                       ByteSpacerPositioning == ByteSpacerPosition.StringBytePanel))
                        AddByteSpacer(dataLineStack, i);

                    var sbCtrl = new StringByte(this)
                    {
                        InternalChange = true,
                        ReadOnlyMode = ReadOnlyMode,
                        TblCharacterTable = _tblCharacterTable,
                        TypeOfCharacterTable = TypeOfCharacterTable,
                        Byte = null,
                        ByteNext = null,
                        BytePositionInFile = -1
                    };

                    sbCtrl.InternalChange = false;

                    dataLineStack.Children.Add(sbCtrl);
                }
                StringDataStackPanel.Children.Add(dataLineStack);

                #endregion

                #region Build HexByte

                var hexaDataLineStack = new StackPanel
                {
                    Height = LineHeight,
                    Orientation = Orientation.Horizontal
                };

                for (var i = 0; i < BytePerLine; i++)
                {
                    if (ByteSpacerPositioning == ByteSpacerPosition.Both ||
                        ByteSpacerPositioning == ByteSpacerPosition.HexBytePanel)
                        AddByteSpacer(hexaDataLineStack, i);

                    var byteControl = new HexByte(this)
                    {
                        InternalChange = true,
                        ReadOnlyMode = ReadOnlyMode,
                        Byte = null,
                        BytePositionInFile = -1
                    };

                    byteControl.InternalChange = false;

                    hexaDataLineStack.Children.Add(byteControl);
                }

                HexDataStackPanel.Children.Add(hexaDataLineStack);

                #endregion

                reAttachEvents = true;
            }

            #region Attach/detach events to each IByteControl

            if (reAttachEvents)
                TraverseHexAndStringBytes(ctrl =>
                {
                    #region Detach events

                    ctrl.ByteModified -= Control_ByteModified;
                    ctrl.MoveNext -= Control_MoveNext;
                    ctrl.MovePrevious -= Control_MovePrevious;
                    ctrl.MouseSelection -= Control_MouseSelection;
                    ctrl.Click -= Control_Click;
                    ctrl.RightClick -= Control_RightClick;
                    ctrl.MoveUp -= Control_MoveUp;
                    ctrl.MoveDown -= Control_MoveDown;
                    ctrl.MoveLeft -= Control_MoveLeft;
                    ctrl.MoveRight -= Control_MoveRight;
                    ctrl.MovePageDown -= Control_MovePageDown;
                    ctrl.MovePageUp -= Control_MovePageUp;
                    ctrl.ByteDeleted -= Control_ByteDeleted;
                    ctrl.EscapeKey -= Control_EscapeKey;
                    ctrl.CtrlaKey -= Control_CTRLAKey;
                    ctrl.CtrlzKey -= Control_CTRLZKey;
                    ctrl.CtrlcKey -= Control_CTRLCKey;
                    ctrl.CtrlvKey -= Control_CTRLVKey;

                    #endregion

                    #region Attach events

                    ctrl.ByteModified += Control_ByteModified;
                    ctrl.MoveNext += Control_MoveNext;
                    ctrl.MovePrevious += Control_MovePrevious;
                    ctrl.MouseSelection += Control_MouseSelection;
                    ctrl.Click += Control_Click;
                    ctrl.RightClick += Control_RightClick;
                    ctrl.MoveUp += Control_MoveUp;
                    ctrl.MoveDown += Control_MoveDown;
                    ctrl.MoveLeft += Control_MoveLeft;
                    ctrl.MoveRight += Control_MoveRight;
                    ctrl.MovePageDown += Control_MovePageDown;
                    ctrl.MovePageUp += Control_MovePageUp;
                    ctrl.ByteDeleted += Control_ByteDeleted;
                    ctrl.EscapeKey += Control_EscapeKey;
                    ctrl.CtrlaKey += Control_CTRLAKey;
                    ctrl.CtrlzKey += Control_CTRLZKey;
                    ctrl.CtrlcKey += Control_CTRLCKey;
                    ctrl.CtrlvKey += Control_CTRLVKey;

                    #endregion
                });

            #endregion
        }

        /// <summary>
        /// Update the data and string stackpanels yo current view;
        /// </summary>
        private void UpdateViewers(bool controlResize)
        {
            var curLevel = ++_priLevel;
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (controlResize)
                {
                    #region Control need to resize

                    if (_viewBuffer == null)
                    {
                        _viewBuffer = new byte[MaxVisibleLine * BytePerLine + 1];
                        BuildDataLines((int) MaxVisibleLine);
                    }
                    else
                    {
                        if (_viewBuffer.Length < MaxVisibleLine * BytePerLine + 1)
                        {
                            BuildDataLines((int) MaxVisibleLine);
                            _viewBuffer = new byte[MaxVisibleLine * BytePerLine + 1];
                        }
                    }

                    #endregion
                }

                if (LinesInfoStackPanel.Children.Count == 0)
                    return;

                var firstInfoLabel = LinesInfoStackPanel.Children[0] as FastTextLine;
                var startPosition = ByteConverters.HexLiteralToLong(firstInfoLabel.Tag.ToString()).position;
                var sizeReadyToRead = LinesInfoStackPanel.Children.Count * BytePerLine + 1;
                _provider.Position = startPosition;
                var readSize = _provider.Read(_viewBuffer, 0, sizeReadyToRead);
                var index = 0;

                #region Hex byte refresh

                TraverseHexBytes(byteControl =>
                {
                    byteControl.Action = ByteAction.Nothing;
                    byteControl.ReadOnlyMode = ReadOnlyMode;

                    byteControl.InternalChange = true;

                    if (index < readSize && _priLevel == curLevel)
                    {
                        byteControl.Byte = _viewBuffer[index];
                        byteControl.BytePositionInFile = startPosition + index;
                    }
                    else
                    {
                        byteControl.Byte = null;
                        byteControl.BytePositionInFile = -1;
                    }
                    byteControl.InternalChange = false;
                    index++;
                });

                #endregion

                index = 0;

                #region string byte refresh

                TraverseStringBytes(sbCtrl =>
                {
                    sbCtrl.Action = ByteAction.Nothing;
                    sbCtrl.ReadOnlyMode = ReadOnlyMode;

                    sbCtrl.InternalChange = true;
                    sbCtrl.TblCharacterTable = _tblCharacterTable;
                    sbCtrl.TypeOfCharacterTable = TypeOfCharacterTable;

                    if (index < readSize)
                    {
                        sbCtrl.Byte = _viewBuffer[index];
                        sbCtrl.BytePositionInFile = startPosition + index;

                        sbCtrl.ByteNext = index < readSize - 1 ? (byte?) _viewBuffer[index + 1] : null;
                    }
                    else
                    {
                        sbCtrl.Byte = null;
                        sbCtrl.ByteNext = null;
                        sbCtrl.BytePositionInFile = -1;
                    }
                    sbCtrl.InternalChange = false;
                    index++;
                });

                #endregion
            }
            else
            {
                #region Clear IByteControl

                _viewBuffer = null;
                TraverseHexAndStringBytes(ctrl => { ctrl.Clear(); });

                #endregion
            }
        }

        /// <summary>
        /// Update byte are modified
        /// </summary>
        private void UpdateByteModified()
        {
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            var modifiedBytesDictionary = _provider.GetByteModifieds(ByteAction.All);

            TraverseHexAndStringBytes(ctrl =>
            {
                if (!modifiedBytesDictionary.TryGetValue(ctrl.BytePositionInFile, out var byteModified)) return;

                ctrl.InternalChange = true;
                ctrl.Byte = byteModified.Byte;

                if (byteModified.Action == ByteAction.Modified || byteModified.Action == ByteAction.Deleted)
                    ctrl.Action = byteModified.Action;

                ctrl.InternalChange = false;
            });

            IsModified = _provider.UndoCount > 0;
        }

        /// <summary>
        /// Update the selection of byte
        /// </summary>
        private void UpdateSelection()
        {
            var minSelect = SelectionStart <= SelectionStop ? SelectionStart : SelectionStop;
            var maxSelect = SelectionStart <= SelectionStop ? SelectionStop : SelectionStart;

            TraverseHexAndStringBytes(ctrl =>
            {
                if (ctrl.BytePositionInFile >= minSelect &&
                    ctrl.BytePositionInFile <= maxSelect &&
                    ctrl.BytePositionInFile != -1)
                    ctrl.IsSelected = ctrl.Action != ByteAction.Deleted;
                else
                    ctrl.IsSelected = false;
            });
        }

        /// <summary>
        /// Update bytes as marked on findall()
        /// </summary>
        private void UpdateHighLight()
        {
            if (_markedPositionList.Count > 0)
                TraverseHexAndStringBytes(ctrl => ctrl.IsHighLight = _markedPositionList.ContainsKey(ctrl.BytePositionInFile));
            else //Un highlight all            
                TraverseHexAndStringBytes(ctrl => ctrl.IsHighLight = false);
        }

        /// <summary>
        /// Update the position info panel at left of the control
        /// </summary>
        private void UpdateHeader(bool clear = false)
        {
            //Clear before refresh
            if (clear) HexHeaderStackPanel.Children.Clear();

            if (ByteProvider.CheckIsOpen(_provider))
                for (var i = HexHeaderStackPanel.Children.Count; i < BytePerLine; i++)
                {
                    if (ByteSpacerPositioning == ByteSpacerPosition.Both ||
                        ByteSpacerPositioning == ByteSpacerPosition.HexBytePanel)
                        AddByteSpacer(HexHeaderStackPanel, i, true);

                    //Create control
                    var lineInfoLabel = new FastTextLine(this)
                    {
                        Height = LineHeight,
                        AutoWidth = false,
                        Foreground = ForegroundOffSetHeaderColor,
                        RenderPoint = new Point(2, 0),
                        ToolTip = $"Column : {i}"
                    };

                    #region Set text visual of header

                    switch (DataStringVisual)
                    {
                        case DataVisualType.Hexadecimal:
                            lineInfoLabel.Text = ByteConverters.ByteToHex((byte) i);
                            lineInfoLabel.Width = 20;
                            break;
                        case DataVisualType.Decimal:
                            lineInfoLabel.Text = i.ToString("d3");
                            lineInfoLabel.Width = 25;
                            break;
                    }

                    #endregion

                    //Add to stackpanel
                    HexHeaderStackPanel.Children.Add(lineInfoLabel);
                }
        }

        /// <summary>
        /// Update the position info panel at left of the control
        /// </summary>
        private void UpdateLinesOffSet()
        {
            var fds = MaxVisibleLine;

            //If the lines are less than "visible lines" create them;
            var linesCount = LinesInfoStackPanel.Children.Count;

            if (linesCount < fds)
            {
                for (var i = 0; i < fds - linesCount; i++)
                {
                    var lineInfoLabel = new FastTextLine(this)
                    {
                        Height = LineHeight,
                        Foreground = ForegroundOffSetHeaderColor,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    //Events
                    lineInfoLabel.MouseDown += LineInfoLabel_MouseDown;
                    lineInfoLabel.MouseMove += LineInfoLabel_MouseMove;

                    LinesInfoStackPanel.Children.Add(lineInfoLabel);
                }
            }

            TraverseLineInfos(ctrl => { ctrl.Text = string.Empty; });

            if (ByteProvider.CheckIsOpen(_provider))
            {
                for (var i = 0; i < fds; i++)
                {
                    var firstLineByte = ((long) VerticalScrollBar.Value + i) * BytePerLine;
                    var lineInfoLabel = (FastTextLine) LinesInfoStackPanel.Children[i];

                    if (firstLineByte < _provider.Length)
                    {
                        #region Set text visual

                        var tag = $"0x{ByteConverters.LongToHex(firstLineByte).ToUpper()}";

                        lineInfoLabel.Tag = tag;
                        switch (OffSetStringVisual)
                        {
                            case DataVisualType.Hexadecimal:
                                lineInfoLabel.Text = tag;
                                break;
                            case DataVisualType.Decimal:
                                lineInfoLabel.Text = $"d{firstLineByte:d8}";
                                break;
                        }

                        #endregion

                        lineInfoLabel.ToolTip = $"{Properties.Resources.FirstByteString} : {firstLineByte}";
                    }
                }
            }
        }


        /// <summary>
        /// Get first visible byte position in control
        /// </summary>
        /// <returns>Return -1 of no file open</returns>
        private long FirstVisibleBytePosition
        {
            get
            {
                long rtn = -1;
                var count = 0;
                var exit = false;

                TraverseHexBytes(ctrl =>
                {
                    if (++count == 1)
                    {
                        rtn = ctrl.BytePositionInFile;
                        exit = true;
                    }
                }, ref exit);

                return rtn;
            }
        }

        /// <summary>
        /// Return True if SelectionStart are visible in control
        /// </summary>
        public bool SelectionStartIsVisible
        {
            get
            {
                var rtn = false;
                TraverseHexBytes(ctrl =>
                {
                    if (ctrl.BytePositionInFile == SelectionStart)
                        rtn = true;
                }, ref rtn);

                return rtn;
            }
        }

        /// <summary>
        /// Get last visible byte position in control
        /// </summary>
        /// <returns>Return -1 of no file open.</returns>
        private long LastVisibleBytePosition => FirstVisibleBytePosition + (MaxVisibleLine - 1) * BytePerLine - 1;

        #endregion First/Last visible byte methods

        #region Focus Methods

        /// <summary>
        /// Set focus on byte
        /// </summary>
        private void SetFocusHexDataPanel(long bytePositionInFile)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (bytePositionInFile >= _provider.Length)
                    return;

                var rtn = false;
                TraverseHexBytes(ctrl =>
                {
                    if (ctrl.BytePositionInFile == bytePositionInFile)
                    {
                        ctrl.Focus();
                        rtn = true;
                    }
                }, ref rtn);

                if (rtn) return;

                if (VerticalScrollBar.Value < VerticalScrollBar.Maximum)
                    VerticalScrollBar.Value++;

                if (!SelectionStartIsVisible && SelectionLength == 1)
                    SetPosition(SelectionStart, 1);
            }
        }

        /// <summary>
        /// Set focus on byte
        /// </summary>
        private void SetFocusStringDataPanel(long bytePositionInFile)
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                if (bytePositionInFile >= _provider.Length)
                    return;

                var rtn = false;
                TraverseStringBytes(ctrl =>
                {
                    if (ctrl.BytePositionInFile == bytePositionInFile)
                    {
                        ctrl.Focus();
                        rtn = true;
                    }
                }, ref rtn);

                if (rtn) return;

                if (VerticalScrollBar.Value < VerticalScrollBar.Maximum)
                    VerticalScrollBar.Value++;

                if (!SelectionStartIsVisible && SelectionLength == 1)
                    SetPosition(SelectionStart, 1);
            }
        }

        #endregion Focus Methods

        #region Find methods

        /// <summary>
        /// Find first occurence of string in stream. Search start as startPosition.
        /// </summary>
        public long FindFirst(string text, long startPosition = 0) =>
            FindFirst(ByteConverters.StringToByte(text), startPosition);

        /// <summary>
        /// Find first occurence of byte[] in stream. Search start as startPosition.
        /// </summary>
        public long FindFirst(byte[] data, long startPosition = 0)
        {
            if (data == null) return -1;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                try
                {
                    var position = _provider.FindIndexOf(data, startPosition).First();
                    SetPosition(position, data.Length);
                    return position;
                }
                catch
                {
                    UnSelectAll();
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find next occurence of string in stream search start at SelectionStart.
        /// </summary>
        public long FindNext(string text) => FindNext(ByteConverters.StringToByte(text));

        /// <summary>
        /// Find next occurence of byte[] in stream search start at SelectionStart.
        /// </summary>
        public long FindNext(byte[] data)
        {
            if (data == null) return -1;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                try
                {
                    var position = _provider.FindIndexOf(data, SelectionStart + 1).First();
                    SetPosition(position, data.Length);
                    return position;
                }
                catch
                {
                    UnSelectAll();
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find last occurence of string in stream search start at SelectionStart.
        /// </summary>
        public long FindLast(string text) => FindLast(ByteConverters.StringToByte(text));

        /// <summary>
        /// Find first occurence of byte[] in stream.
        /// </summary>
        public long FindLast(byte[] data)
        {
            if (data == null) return -1;

            if (ByteProvider.CheckIsOpen(_provider))
            {
                try
                {
                    var position = _provider.FindIndexOf(data, SelectionStart + 1).Last();
                    SetPosition(position, data.Length);
                    return position;
                }
                catch
                {
                    UnSelectAll();
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find all occurence of string in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(string text) => FindAll(ByteConverters.StringToByte(text));

        /// <summary>
        /// Find all occurence of byte[] in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(byte[] data)
        {
            if (data == null) return null;

            UnHighLightAll();

            return ByteProvider.CheckIsOpen(_provider) ? _provider.FindIndexOf(data) : null;
        }

        /// <summary>
        /// Find all occurence of string in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(string text, bool highLight) =>
            FindAll(ByteConverters.StringToByte(text), highLight);

        /// <summary>
        /// Find all occurence of string in stream. Highlight occurance in stream is MarcAll as true
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(byte[] data, bool highLight)
        {
            if (data == null) return null;

            ClearScrollMarker(ScrollMarker.SearchHighLight);

            if (highLight)
            {
                var positions = FindAll(data);

                var findAll = positions as IList<long> ?? positions.ToList();
                foreach (var position in findAll)
                {
                    if (!_markedPositionList.ContainsValue(position))
                        for (var i = position; i < position + data.Length; i++)
                            _markedPositionList.Add(i, i);


                    SetScrollMarker(position, ScrollMarker.SearchHighLight);
                }

                UnSelectAll();
                UpdateHighLight();

                return findAll;
            }
            return FindAll(data);
        }

        /// <summary>
        /// Find all occurence of SelectionByteArray in stream. Highlight byte finded
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAllSelection(bool highLight) => 
            SelectionLength > 0 ? FindAll(SelectionByteArray, highLight) : null;

        #endregion Find methods

        #region Statusbar

        /// <summary>
        /// Update statusbar for somes property dont support dependency property
        /// </summary>
        private void UpdateStatusBar()
        {
            if (StatusBarVisibility == Visibility.Visible)
                if (ByteProvider.CheckIsOpen(_provider))
                {
                    #region Show lenght  TODO:REFRESH ONLY WHEN NEEDED

                    var mb = false;
                    long deletedBytesCount = _provider.GetByteModifieds(ByteAction.Deleted).Count;
                    long addedBytesCount = _provider.GetByteModifieds(ByteAction.Added).Count;

                    //is mega bytes ?
                    double lenght = (_provider.Length - deletedBytesCount + addedBytesCount) / 1024;

                    if (lenght > 1024)
                    {
                        lenght = lenght / 1024;
                        mb = true;
                    }

                    FileLengthKbLabel.Content = Math.Round(lenght, 2) +
                                                (mb
                                                    ? $" {Properties.Resources.MBTagString}"
                                                    : $" {Properties.Resources.KBTagString}");
                    //FileLengthKbLabel.ToolTip = $" {_provider.Length - deletedBytesCount} {Properties.Resources.ByteString}";

                    #endregion

                    #region Byte count of selectionStart

                    if (AllowByteCount && _bytecount != null && SelectionStart > -1)
                    {
                        ByteCountPanel.Visibility = Visibility.Visible;

                        var val = _provider.GetByte(SelectionStart).singleByte.Value;
                        CountOfByteSumLabel.Content = _bytecount[val];
                        CountOfByteLabel.Content = $"0x{ByteConverters.LongToHex(val)}";
                    }
                    else
                        ByteCountPanel.Visibility = Visibility.Collapsed;

                    #endregion
                }
                else
                {
                    FileLengthKbLabel.Content = 0;
                    CountOfByteLabel.Content = 0;
                }
        }

        #endregion Statusbar

        #region Bookmark and other scrollmarker

        /// <summary>
        /// Get all bookmark are currently set
        /// </summary>
        public IEnumerable<BookMark> BookMarks
        {
            get
            {
                var bmList = new List<BookMark>();

                TraverseScrollMarker(sm =>
                {
                    if (sm.Tag is BookMark bm && bm.Marker == ScrollMarker.Bookmark)
                        bmList.Add(bm);
                });

                foreach (var bm in bmList)
                    yield return bm;
            }
        }

        /// <summary>
        /// Set bookmark at specified position
        /// </summary>
        /// <param name="position"></param>
        public void SetBookMark(long position) => SetScrollMarker(position, ScrollMarker.Bookmark);

        /// <summary>
        /// Set bookmark at selection start
        /// </summary>
        public void SetBookMark() => SetScrollMarker(SelectionStart, ScrollMarker.Bookmark);

        /// <summary>
        /// Set marker at position using bookmark object
        /// </summary>
        /// <param name="mark"></param>
        private void SetScrollMarker(BookMark mark) =>
            SetScrollMarker(mark.BytePositionInFile, mark.Marker, mark.Description);

        /// <summary>
        /// Set marker at position
        /// </summary>
        private void SetScrollMarker(long position, ScrollMarker marker, string description = "")
        {
            if (ByteProvider.CheckIsOpen(_provider))
            {
                double rightPosition = 0;
                var exit = false;

                //create bookmark
                var bookMark = new BookMark
                {
                    Marker = marker,
                    BytePositionInFile = position,
                    Description = description
                };

                #region Remove selection start marker and set position

                if (marker == ScrollMarker.SelectionStart)
                {
                    TraverseScrollMarker(sm =>
                    {
                        if (sm.Tag is BookMark mark && mark.Marker == ScrollMarker.SelectionStart)
                        {
                            MarkerGrid.Children.Remove(sm);
                            exit = true;
                        }
                    }, ref exit);

                    bookMark.BytePositionInFile = SelectionStart;
                }

                #endregion

                #region Set position in scrollbar

                var topPosition =
                    (GetLineNumber(bookMark.BytePositionInFile) * VerticalScrollBar.Track.TickHeight(MaxLine) - 1)
                    .Round(1);

                if (double.IsNaN(topPosition))
                    topPosition = 0;

                #endregion

                #region Check if position already exist and exit if exist                

                if (marker != ScrollMarker.SelectionStart)
                {
                    exit = false;

                    TraverseScrollMarker(sm =>
                    {
                        if (sm.Tag is BookMark mark && mark.Marker == marker &&
                            (int) sm.Margin.Top == (int) topPosition)
                            exit = true;
                    }, ref exit);

                    if (exit) return;
                }

                #endregion

                #region Build rectangle

                var rect = new Rectangle
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Tag = bookMark,
                    Width = 5,
                    Height = 3,
                    DataContext = bookMark
                };

                #endregion

                #region Set somes properties for different marker

                switch (marker)
                {
                    case ScrollMarker.TblBookmark:
                    case ScrollMarker.Bookmark:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush) TryFindResource("BookMarkColor");
                        break;
                    case ScrollMarker.SearchHighLight:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush) TryFindResource("SearchBookMarkColor");
                        rect.HorizontalAlignment = HorizontalAlignment.Center;
                        break;
                    case ScrollMarker.SelectionStart:
                        rect.Fill = (SolidColorBrush) TryFindResource("SelectionStartBookMarkColor");
                        rect.Width = VerticalScrollBar.ActualWidth;
                        rect.Height = 3;
                        break;
                    case ScrollMarker.ByteModified:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush) TryFindResource("ByteModifiedMarkColor");
                        rect.HorizontalAlignment = HorizontalAlignment.Right;
                        break;
                    case ScrollMarker.ByteDeleted:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush) TryFindResource("ByteDeletedMarkColor");
                        rect.HorizontalAlignment = HorizontalAlignment.Right;
                        rightPosition = 4;
                        break;
                }

                rect.MouseDown += Rect_MouseDown;
                //rect.DataContext = new ByteModified {BytePositionInFile = position};
                rect.Margin = new Thickness(0, topPosition, rightPosition, 0);

                #endregion

                //Add to grid
                MarkerGrid.Children.Add(rect);
            }
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rect && rect.Tag is BookMark bm)
                SetPosition(bm.Marker != ScrollMarker.SelectionStart ? bm.BytePositionInFile : SelectionStart, 1);
        }

        /// <summary>
        /// Update all scroll marker position
        /// </summary>
        private void UpdateScrollMarkerPosition()
        {
            TraverseScrollMarker(ctrl =>
            {
                if (ctrl.Tag is BookMark bm)
                {
                    ctrl.Margin = new Thickness
                    (
                        0,
                        GetLineNumber(bm.BytePositionInFile) * VerticalScrollBar.Track.TickHeight(MaxLine) -
                        ctrl.ActualHeight,
                        0,
                        0
                    );
                }
            });
        }

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        public void ClearAllScrollMarker() => MarkerGrid.Children.Clear();

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        public void ClearScrollMarker(ScrollMarker marker)
        {
            TraverseScrollMarker(sm =>
            {
                if (sm.Tag is BookMark mark && mark.Marker == marker)
                    MarkerGrid.Children.Remove(sm);
            });
        }

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        /// <param name="position"></param>
        public void ClearScrollMarker(ScrollMarker marker, long position)
        {
            TraverseScrollMarker(sm =>
            {
                if (sm.Tag is BookMark mark && mark.Marker == marker && mark.BytePositionInFile == position)
                    MarkerGrid.Children.Remove(sm);
            });
        }

        /// <summary>
        /// Clear ScrollMarker at position
        /// </summary>
        public void ClearScrollMarker(long position)
        {
            TraverseScrollMarker(sm =>
            {
                if (sm.Tag is BookMark mark && mark.BytePositionInFile == position)
                    MarkerGrid.Children.Remove(sm);
            });
        }

        #endregion Bookmark and other scrollmarker

        #region Context menu

        /// <summary>
        /// Allow or not the context menu to appear on right-click
        /// </summary>
        public bool AllowContextMenu { get; set; } = true;

        private void Control_RightClick(object sender, EventArgs e)
        {
            if (AllowContextMenu)
            {
                //position                
                if (sender is IByteControl ctrl)
                    _rightClickBytePosition = ctrl.BytePositionInFile;

                if (SelectionLength <= 1)
                {
                    SelectionStart = _rightClickBytePosition;
                    SelectionStop = _rightClickBytePosition;
                }

                //update ctrl
                CopyAsCMenu.IsEnabled = false;
                CopyAsciicMenu.IsEnabled = false;
                FindAllCMenu.IsEnabled = false;
                CopyHexaCMenu.IsEnabled = false;
                UndoCMenu.IsEnabled = false;
                DeleteCMenu.IsEnabled = false;
                FillByteCMenu.IsEnabled = false;
                CopyTblcMenu.IsEnabled = false;

                if (SelectionLength > 0)
                {
                    CopyAsciicMenu.IsEnabled = true;
                    CopyAsCMenu.IsEnabled = true;
                    FindAllCMenu.IsEnabled = true;
                    CopyHexaCMenu.IsEnabled = true;
                    DeleteCMenu.IsEnabled = true;
                    FillByteCMenu.IsEnabled = true;

                    if (_tblCharacterTable != null)
                        CopyTblcMenu.IsEnabled = true;
                }

                if (UndoCount > 0)
                    UndoCMenu.IsEnabled = true;

                //Show context menu
                Focus();
                CMenu.Visibility = Visibility.Visible;
            }
        }

        private void FindAllCMenu_Click(object sender, RoutedEventArgs e) => FindAll(SelectionByteArray, true);

        private void CopyHexaCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.HexaString);

        private void CopyASCIICMenu_Click(object sender, RoutedEventArgs e) =>
            CopyToClipboard(CopyPasteMode.AsciiString);

        private void CopyCSharpCMenu_Click(object sender, RoutedEventArgs e) =>
            CopyToClipboard(CopyPasteMode.CSharpCode);

        private void CopyFSharpCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.FSharp);

        private void CopyVBNetCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.VbNetCode);

        private void CopyCCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.CCode);

        private void CopyJavaCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.JavaCode);

        private void CopyTBLCMenu_Click(object sender, RoutedEventArgs e) => CopyToClipboard(CopyPasteMode.TblString);

        private void DeleteCMenu_Click(object sender, RoutedEventArgs e) => DeleteSelection();

        private void UndoCMenu_Click(object sender, RoutedEventArgs e) => Undo();

        private void BookMarkCMenu_Click(object sender, RoutedEventArgs e) => SetBookMark(_rightClickBytePosition);

        private void ClearBookMarkCMenu_Click(object sender, RoutedEventArgs e) =>
            ClearScrollMarker(ScrollMarker.Bookmark);

        private void PasteMenu_Click(object sender, RoutedEventArgs e) => Paste(false); //Paste Without Insert

        private void SelectAllCMenu_Click(object sender, RoutedEventArgs e) => SelectAll();

        private void FillByteCMenu_Click(object sender, RoutedEventArgs e)
        {
            var window = new GiveByteWindow();

            //For present crash When used in Winform
            try
            {
                window.Owner = Application.Current.MainWindow;
            }
            catch
            {
                // TODO : add Winform code
            }
            
            if (window.ShowDialog() == true && window.HexTextBox.LongValue <= 255)
                FillWithByte((byte) window.HexTextBox.LongValue);
        }

        private void ReplaceByteCMenu_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReplaceByteWindow();

            //For present crash When used in Winform
            try
            {
                window.Owner = Application.Current.MainWindow;
            }
            catch
            {
                // TODO : add Winform code
            }

            if (window.ShowDialog() == true && window.HexTextBox.LongValue <= 255 && window.ReplaceHexTextBox.LongValue <= 255)
                ReplaceByte((byte) window.HexTextBox.LongValue, (byte) window.ReplaceHexTextBox.LongValue);
        }

        #endregion Context menu

        #region Bottom and top rectangle

        /// <summary>
        /// Vertical Move Method By Time,
        /// </summary>
        /// <param name="readToMove">whether the veticalbar value should be changed</param>
        /// <param name="distance">the value that vertical value move down(negative for up)</param>
        private void VerticalMoveByTime(Func<bool> readToMove, Func<double> distance)
        {
            ThreadPool.QueueUserWorkItem(cb =>
            {
                while (readToMove())
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Mouse.LeftButton == MouseButtonState.Pressed)
                        {
                            VerticalScrollBar.Value += distance();

                            //Selection stop
                            if (_mouseOnBottom)
                                SelectionStop = LastVisibleBytePosition;
                            else if (_mouseOnTop)
                                SelectionStop = FirstVisibleBytePosition;
                        }

                        //Give the control to dispatcher for do events
                        Application.Current.DoEvents();
                    });
                }
            });
        }

        private void BottomRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            _mouseOnBottom = true;
            var curTime = ++_bottomEnterTimes;

            VerticalMoveByTime
            (
                () => _mouseOnBottom && curTime == _bottomEnterTimes,
                () => (int) MouseWheelSpeed
            );
        }

        private void BottomRectangle_MouseLeave(object sender, MouseEventArgs e) => _mouseOnBottom = false;

        private void TopRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            var curTime = ++_topEnterTimes;
            _mouseOnTop = true;

            VerticalMoveByTime
            (
                () => _mouseOnTop && curTime == _topEnterTimes,
                () => -(int) MouseWheelSpeed
            );
        }

        private void TopRectangle_MouseLeave(object sender, MouseEventArgs e) => _mouseOnTop = false;

        #endregion Bottom and Top rectangle

        #region Highlight selected byte        

        /// <summary>
        /// Byte at selection start
        /// </summary>
        internal byte? SelectionByte { get; set; }

        /// <summary>
        /// Set to true for highlight the same byte are selected in view.
        /// </summary>
        public bool AllowAutoHightLighSelectionByte { get; set; } = true;

        /// <summary>
        /// Brush used to color the selectionbyte
        /// </summary>
        public Brush AutoHighLiteSelectionByteBrush
        {
            get => (Brush) GetValue(AutoHighLiteSelectionByteBrushProperty);
            set => SetValue(AutoHighLiteSelectionByteBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for AutoHighLiteSelectionByteBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoHighLiteSelectionByteBrushProperty =
            DependencyProperty.Register(nameof(AutoHighLiteSelectionByteBrush), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.LightBlue,
                    AutoHighLiteSelectionByteBrush_Changed));

        private static void AutoHighLiteSelectionByteBrush_Changed(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.UpdateVisual();
        }

        #endregion Highlight selected byte

        #region ByteCount property/methods

        public bool AllowByteCount
        {
            get => (bool) GetValue(AllowByteCountProperty);
            set => SetValue(AllowByteCountProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowByteCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowByteCountProperty =
            DependencyProperty.Register(nameof(AllowByteCount), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(false, AllowByteCount_PropertyChanged));

        private static void AllowByteCount_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
            {
                if (e.NewValue != e.OldValue)
                    ctrl.UpdateByteCount();

                ctrl.UpdateStatusBar();
            }
        }

        /// <summary>
        /// Update the bytecount var.
        /// </summary>
        private void UpdateByteCount()
        {
            _bytecount = null;

            if (ByteProvider.CheckIsOpen(_provider))
                if (AllowByteCount) _bytecount = _provider.GetByteCount();
        }

        #endregion ByteCount Property

        #region IDisposable Support

        private bool _disposedValue; // for detect redondants call

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                //Dispose managed object
                if (disposing)
                {
                    _provider?.Dispose();
                    _tblCharacterTable?.Dispose();
                    _viewBuffer = null;
                    _markedPositionList = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion

        #region IByteControl grouping

        public ByteSpacerPosition ByteSpacerPositioning
        {
            get => (ByteSpacerPosition) GetValue(ByteSpacerPositioningProperty);
            set => SetValue(ByteSpacerPositioningProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacerPositioning.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerPositioningProperty =
            DependencyProperty.Register(nameof(ByteSpacerPositioning), typeof(ByteSpacerPosition), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerPosition.Both, ByteSpacer_Changed));

        public ByteSpacerWidth ByteSpacerWidthTickness
        {
            get => (ByteSpacerWidth) GetValue(ByteSpacerWidthTicknessProperty);
            set => SetValue(ByteSpacerWidthTicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerWidthTicknessProperty =
            DependencyProperty.Register(nameof(ByteSpacerWidthTickness), typeof(ByteSpacerWidth), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerWidth.Normal, ByteSpacer_Changed));

        private static void ByteSpacer_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView(true);
        }

        public ByteSpacerGroup ByteGrouping
        {
            get => (ByteSpacerGroup) GetValue(ByteGroupingProperty);
            set => SetValue(ByteGroupingProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteGrouping.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteGroupingProperty =
            DependencyProperty.Register(nameof(ByteGrouping), typeof(ByteSpacerGroup), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerGroup.EightByte, ByteSpacer_Changed));

        public ByteSpacerVisual ByteSpacerVisualStyle
        {
            get => (ByteSpacerVisual) GetValue(ByteSpacerVisualStyleProperty);
            set => SetValue(ByteSpacerVisualStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacerVisualStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerVisualStyleProperty =
            DependencyProperty.Register(nameof(ByteSpacerVisualStyle), typeof(ByteSpacerVisual), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerVisual.Empty, ByteSpacer_Changed));

        /// <summary>
        /// Add byte spacer
        /// </summary>
        private void AddByteSpacer(StackPanel stack, int colomn, bool forceEmpty = false)
        {
            if (colomn % (int) ByteGrouping == 0 && colomn > 0)
            {
                if (!forceEmpty)
                    switch (ByteSpacerVisualStyle)
                    {
                        case ByteSpacerVisual.Empty:
                            stack.Children.Add(new TextBlock {Width = (int) ByteSpacerWidthTickness});
                            break;
                        case ByteSpacerVisual.Line:

                            #region Line

                            stack.Children.Add(new Line
                            {
                                Y2 = LineHeight,
                                X1 = (int) ByteSpacerWidthTickness / 2D,
                                X2 = (int) ByteSpacerWidthTickness / 2D,
                                Stroke = BorderBrush,
                                StrokeThickness = 1,
                                Width = (int) ByteSpacerWidthTickness
                            });

                            #endregion

                            break;
                        case ByteSpacerVisual.Dash:

                            #region LineDash

                            stack.Children.Add(new Line
                            {
                                Y2 = LineHeight - 1,
                                X1 = (int) ByteSpacerWidthTickness / 2D,
                                X2 = (int) ByteSpacerWidthTickness / 2D,
                                Stroke = BorderBrush,
                                StrokeDashArray = new DoubleCollection(new double[] {2}),
                                StrokeThickness = 1,
                                Width = (int) ByteSpacerWidthTickness
                            });

                            #endregion

                            break;
                    }
                else
                    stack.Children.Add(new TextBlock {Width = (int) ByteSpacerWidthTickness});
            }
        }

        #endregion IByteControl grouping

        #region Caret support

        /// <summary>
        /// Initialize the caret
        /// </summary>
        private void InitializeCaret()
        {
            BaseGrid.Children.Add(_caret);
            _caret.CaretHeight = FontSize;
            _caret.BlinkPeriod = 600;
            _caret.Hide();
        }

        internal void MoveCaret(Point point) => _caret.MoveCaret(point);

        internal bool IsVisibleCaret => _caret.IsVisibleCaret;

        internal void HideCaret() => _caret.Hide();

        public bool IsCaretVisible => _caret.IsVisibleCaret;

        #endregion

        #region Append/expend bytes to end of file
        /// <summary>
        /// Allow control to append/expend byte at end of file
        /// </summary>
        public bool AllowExtend { get; set; }
        
        /// <summary>
        /// Show a message box is true before append byte at end of file
        /// </summary>
        public bool AppendNeedConfirmation { get; set; } = true;

        /// <summary>
        /// Append one byte at end of file
        /// </summary>
        internal void AppendByte(byte[] bytesToAppend)
        {
            if (!AllowExtend) return;
            if (!ByteProvider.CheckIsOpen(_provider)) return;

            if (AppendNeedConfirmation)
                if (MessageBox.Show(Properties.Resources.AppendByteConfirmationString, ApplicationName,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

            _provider?.AppendByte(bytesToAppend);
            RefreshView();
        }

        #endregion

        #region Text drop support

        /// <summary>
        /// Allow the control to catch the file dropping 
        /// Note : AllowDrop need to be true
        /// </summary>
        public bool AllowFileDrop { get; set; } = true;

        /// <summary>
        /// Allow the control to catch the text dropping 
        /// Note : AllowDrop need to be true
        /// </summary>
        //public bool AllowTextDrop { get; set; } = true;

        /// <summary>
        /// Show a messagebox for confirm open when a file are already open
        /// </summary>
        public bool FileDroppingConfirmation { get; set; } = true;

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            #region Text Dropping (Will be supported soon)
            //var textDrop = e.Data.GetData(DataFormats.Text);
            //if (textDrop != null && AllowTextDrop)
            //{
            //    var textDropped = textDrop as string[];

            //    return;
            //}
            #endregion

            #region File dropping (Only open first selected file catched in GetData)
            var fileDrop = e.Data.GetData(DataFormats.FileDrop);
            if (fileDrop != null && AllowFileDrop)
            {
                var filename = fileDrop as string[];

                if (!ByteProvider.CheckIsOpen(_provider))
                    FileName = filename[0];
                else
                {
                    if (FileDroppingConfirmation && MessageBox.Show(
                            $"{Properties.Resources.FileDroppingConfirmationString} {Path.GetFileName(filename[0])} ?", ApplicationName,
                            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        FileName = filename[0];
                    else
                        FileName = filename[0];
                }
            }
            #endregion
        }

        #endregion

        #region Save/Load control state

        /// <summary>
        /// Save the current state of ByteProvider in a xml text file.
        /// </summary>
        public void SaveCurrentState(string filename)
        {
            if (!ByteProvider.CheckIsOpen(_provider)) return;
            _provider.SaveState(filename);
        }

        /// <summary>
        /// Load state of control from a xml text file.
        /// </summary>
        public void LoadCurrentState(string filename)
        {
            if (!ByteProvider.CheckIsOpen(_provider)) return;
            _provider.LoadState(filename);
            RefreshView();
        }

        #endregion
    }
}