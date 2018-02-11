//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using WpfHexaEditor.Core.Interfaces;

namespace WpfHexaEditor.Core.Bytes
{
    public class ByteModified : IByteModified
    {
        #region Constructor

        /// <summary>
        /// Default contructor
        /// </summary>
        public ByteModified()
        {
        }

        /// <summary>
        /// complete contructor
        /// </summary>
        public ByteModified(byte? val, ByteAction action, long bytePositionInFile, long undoLenght)
        {
            Byte = val;
            Action = action;
            BytePositionInFile = bytePositionInFile;
            UndoLenght = undoLenght;
        }

        #endregion constructor

        #region properties

        /// <summary>
        /// Byte mofidied
        /// </summary>
        public byte? Byte { get; set; }

        /// <summary>
        /// Action have made in this byte
        /// </summary>
        public ByteAction Action { get; set; } = ByteAction.Nothing;

        /// <summary>
        /// Get of Set te position in file
        /// </summary>
        public long BytePositionInFile { get; set; } = -1;

        /// <summary>
        /// Number of byte to undo when this byte is reach
        /// </summary>
        public long UndoLenght { get; set; } = 1;

        #endregion properties

        #region Methods

        /// <summary>
        /// Check if the object is valid and data can be used for action
        /// </summary>
        public bool IsValid => BytePositionInFile > -1 && Action != ByteAction.Nothing && Byte != null;

        /// <summary>
        /// String representation of byte
        /// </summary>
        public override string ToString() =>
            $"ByteModified - Action:{Action} Position:{BytePositionInFile} Byte:{Byte}";

        /// <summary>
        /// Clear object
        /// </summary>
        public void Clear()
        {
            Byte = null;
            Action = ByteAction.Nothing;
            BytePositionInFile = -1;
            UndoLenght = 1;
        }

        /// <summary>
        /// Copy Current instance to another
        /// </summary>
        /// <returns></returns>
        public ByteModified GetCopy() => new ByteModified
        {
            Action = Action,
            Byte = Byte,
            UndoLenght = UndoLenght,
            BytePositionInFile = BytePositionInFile
        };

        /// <summary>
        /// Get if bytemodified is valid
        /// </summary>
        public static bool CheckIsValid(ByteModified byteModified) => byteModified != null && byteModified.IsValid;

        #endregion Methods
        
    }
}