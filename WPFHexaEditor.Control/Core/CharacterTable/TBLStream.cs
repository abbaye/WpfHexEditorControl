//////////////////////////////////////////////
// Apache 2.0  - 2003-2017
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexaEditor.Core.CharacterTable
{
    /// <inheritdoc />
    /// <summary>
    /// Cet objet représente un fichier Thingy TBL (entrée + valeur)
    /// </summary>
    public sealed class TblStream : IDisposable
    {
        /// <summary>Chemin vers le fichier (path)</summary>
        private string _fileName;

        /// <summary>Tableau de DTE représentant tous les les entrée du fichier</summary>
        private Dictionary<string, Dte> _dteList = new Dictionary<string, Dte>();

        #region Constructeurs

        /// <summary>
        /// Constructeur permétant de chargé le fichier DTE
        /// </summary>
        /// <param name="fileName"></param>
        public TblStream(string fileName)
        {
            _dteList.Clear();

            //check if exist and load file
            if (File.Exists(fileName))
            {
                _fileName = fileName;
                Load();
            }
            else
                throw new FileNotFoundException();
        }

        /// <summary>
        /// Constructeur permétant de chargé le fichier DTE
        /// </summary>
        public TblStream()
        {
            _dteList.Clear();
            _fileName = string.Empty;
        }

        #endregion Constructeurs

        #region Indexer

        /// <summary>
        /// Indexeur permetant de travailler sur les DTE contenue dans TBL a la facons d'un tableau.
        /// </summary>
        public Dte this[string index]
        {
            get => _dteList[index];
            set => _dteList[index] = value;
        }

        #endregion Indexer

        #region Méthodes

        /// <summary>
        /// Trouver une entré dans la table de jeu qui corestpond a la valeur hexa
        /// </summary>
        /// <param name="hex">Valeur hexa a rechercher dans la TBL</param>
        /// <param name="showSpecialValue">Afficher les valeurs de fin de block et de ligne</param>
        /// <returns></returns>
        public string FindMatch(string hex, bool showSpecialValue)
        {
            if (showSpecialValue)
            {
                if (_dteList.ContainsKey($"/{hex}")) return Properties.Resources.EndTagString; //"<end>";
                if (_dteList.ContainsKey($"*{hex}")) return Properties.Resources.LineTagString; //"<ln>";
            }

            return _dteList.ContainsKey(hex) ? _dteList[hex].Value : "#";
        }

        /// <summary>
        /// Convert data to TBL string. 
        /// </summary>
        /// <returns>
        /// Return string converted to TBL string representation.
        /// Return null on error
        /// </returns>
        public string ToTblString(byte[] data)
        {
            if (data == null) return null;

            var sb = new StringBuilder();

            for (var i = 0; i < data.Length; i++)
            {
                if (i < data.Length - 1)
                {
                    var mte = FindMatch(ByteConverters.ByteToHex(data[i]) + ByteConverters.ByteToHex(data[i + 1]),
                        true);

                    if (mte != "#")
                    {
                        sb.Append(mte);
                        continue;
                    }
                }

                sb.Append(FindMatch(ByteConverters.ByteToHex(data[i]), true));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Chargé la chaine dans l'objet
        /// </summary>
        private void Load(string tblString)
        {
            //Vide la collection
            _dteList.Clear();
            //ouverture du fichier

            //lecture du fichier jusqua la fin et séparation par ligne
            char[] sepEndLine = {'\n'}; //Fin de ligne
            char[] sepEqual = {'='}; //Fin de ligne

            //build strings line
            var textFromString = new StringBuilder(tblString);
            textFromString.Insert(textFromString.Length, new[] {'\r', '\n'});
            var lines = textFromString.ToString().Split(sepEndLine);

            //remplir la collection de DTE : this._DTE
            foreach (var line in lines)
            {
                var info = line.Split(sepEqual);

                //ajout a la collection (ne prend pas encore en charge le Japonais)
                Dte dte;
                try
                {
                    switch (info[0].Length)
                    {
                        case 2:
                            dte = info[1].Length == 2
                                ? new Dte(info[0], info[1].Substring(0, info[1].Length - 1), DteType.Ascii)
                                : new Dte(info[0], info[1].Substring(0, info[1].Length - 1),
                                    DteType.DualTitleEncoding);
                            break;
                        case 4: // >2
                            dte = new Dte(info[0], info[1].Substring(0, info[1].Length - 1),
                                DteType.MultipleTitleEncoding);
                            break;
                        default:
                            continue;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    switch (info[0].Substring(0, 1))
                    {
                        case @"/":
                            dte = new Dte(info[0].Substring(0, info[0].Length - 1), string.Empty, DteType.EndBlock);
                            break;

                        case @"*":
                            dte = new Dte(info[0].Substring(0, info[0].Length - 1), string.Empty, DteType.EndLine);
                            break;
                        //case @"\":
                        default:
                            continue;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    //Du a une entre qui a 2 = de suite... EX:  XX==
                    dte = new Dte(info[0], "=", DteType.DualTitleEncoding);
                }

                _dteList.Add(dte.Entry, dte);
            }

            //Load bookmark
            BookMarks.Clear();
            foreach (var line in lines)
            {
                try
                {
                    if (line.Substring(0, 1) == "(")
                    {
                        var fav = new BookMark();
                        var lineSplited = line.Split(')');
                        fav.Description = lineSplited[1].Substring(0, lineSplited[1].Length - 1);

                        lineSplited = line.Split('h');
                        fav.BytePositionInFile = ByteConverters
                            .HexLiteralToLong(lineSplited[0].Substring(1, lineSplited[0].Length - 1)).position;
                        fav.Marker = ScrollMarker.TblBookmark;
                        BookMarks.Add(fav);
                    }
                }
                catch
                {
                    //Nothing to add if error
                }
            }
        }

        /// <summary>
        /// Chargé le fichier dans l'objet
        /// </summary>
        private void Load()
        {
            //Vide la collection
            _dteList.Clear();
            //ouverture du fichier

            if (!File.Exists(_fileName))
            {
                var fs = File.Create(_fileName);
                fs.Close();
            }

            StreamReader tblFile;
            try
            {
                tblFile = new StreamReader(_fileName, Encoding.ASCII);
            }
            catch
            {
                return;
            }

            if (tblFile.BaseStream.CanRead)
                Load(tblFile.ReadToEnd());

            tblFile.Close();
        }

        /// <summary>
        /// Enregistrer dans le fichier
        /// </summary>
        /// <returns>Retourne vrai si le fichier à été bien enregistré</returns>
        public bool Save()
        {
            //ouverture du fichier
            var myFile = new FileStream(_fileName, FileMode.Create, FileAccess.Write);
            var tblFile = new StreamWriter(myFile, Encoding.ASCII);

            if (tblFile.BaseStream.CanWrite)
            {
                //Save tbl set
                foreach (var dte in _dteList)
                    if (dte.Value.Type != DteType.EndBlock && dte.Value.Type != DteType.EndLine)
                        tblFile.WriteLine(dte.Value.Entry + "=" + dte.Value);
                    else
                        tblFile.WriteLine(dte.Value.Entry);

                //Save bookmark
                tblFile.WriteLine();
                foreach (var mark in BookMarks)
                    tblFile.WriteLine(mark.ToString());

                //Ecriture de 2 saut de ligne a la fin du fichier.
                //(obligatoire pour certain logiciel utilisant les TBL)
                tblFile.WriteLine();
                tblFile.WriteLine();
            }

            //Ferme le fichier TBL
            tblFile.Close();

            return true;
        }

        /// <summary>
        /// Ajouter un element a la collection
        /// </summary>
        /// <param name="dte">objet DTE a ajouter fans la collection</param>
        public void Add(Dte dte) => _dteList.Add(dte.Entry, dte);

        /// <summary>
        /// Effacer un element de la collection a partir d'un objet DTE
        /// </summary>
        /// <param name="dte"></param>
        public void Remove(Dte dte) => _dteList.Remove(dte.Entry);

        #endregion Méthodes

        #region Propriétés

        /// <summary>
        /// Chemin d'acces au fichier (path)
        /// La fonction load doit etre appeler pour rafraichir la fonction
        /// </summary>
        public string FileName
        {
            get => _fileName;
            internal set
            {
                _fileName = value;
                Load();
            }
        }

        /// <summary>
        /// Total d'élement dans l'objet TBL
        /// </summary>
        public int Length => _dteList.Count;

        /// <summary>
        /// Avoir acess au Bookmark
        /// </summary>
        public List<BookMark> BookMarks { get; set; } = new List<BookMark>();

        /// <summary>
        /// Obtenir le total d'entré DTE dans la Table
        /// </summary>
        public int TotalDte => _dteList.Count(l => l.Value.Type == DteType.DualTitleEncoding);

        /// <summary>
        /// Obtenir le total d'entré MTE dans la Table
        /// </summary>
        public int TotalMte => _dteList.Count(l => l.Value.Type == DteType.MultipleTitleEncoding);

        /// <summary>
        /// Obtenir le total d'entré ASCII dans la Table
        /// </summary>
        public int TotalAscii => _dteList.Count(l => l.Value.Type == DteType.Ascii);

        /// <summary>
        /// Obtenir le total d'entré Invalide dans la Table
        /// </summary>
        public int TotalInvalid => _dteList.Count(l => l.Value.Type == DteType.Invalid);

        /// <summary>
        /// Obtenir le total d'entré Japonais dans la Table
        /// </summary>
        public int TotalJaponais => _dteList.Count(l => l.Value.Type == DteType.Japonais);

        /// <summary>
        /// Obtenir le total d'entré Fin de ligne dans la Table
        /// </summary>
        public int TotalEndLine => _dteList.Count(l => l.Value.Type == DteType.EndLine);

        /// <summary>
        /// Obtenir le total d'entré Fin de Block dans la Table
        /// </summary>
        public int TotalEndBlock => _dteList.Count(l => l.Value.Type == DteType.EndBlock);

        /// <summary>
        /// Renvoi le caractere de fin de block
        /// </summary>
        public string EndBlock
        {
            get
            {
                foreach (var dte in _dteList)
                    if (dte.Value.Type == DteType.EndBlock)
                        return dte.Value.Entry;

                return string.Empty;
            }
        }

        /// <summary>
        /// Renvoi le caractere de fin de ligne
        /// </summary>
        public string EndLine
        {
            get
            {
                foreach (var dte in _dteList)
                    if (dte.Value.Type == DteType.EndLine)
                        return dte.Value.Entry;

                return string.Empty;
            }
        }

        /// <summary>
        /// Enable/Disable Readonly on control.
        /// </summary>
        public bool AllowEdit { get; set; }

        #endregion Propriétés

        #region Build default TBL

        public static TblStream CreateDefaultTbl(DefaultCharacterTableType type = DefaultCharacterTableType.Ascii)
        {
            var tbl = new TblStream();

            switch (type)
            {
                case DefaultCharacterTableType.Ascii:
                    for (byte i = 0; i < 255; i++)
                        tbl.Add(new Dte(ByteConverters.ByteToHex(i).ToUpper(), $"{ByteConverters.ByteToChar(i)}"));
                    break;
                case DefaultCharacterTableType.EbcdicWithSpecialChar:
                    tbl.Load(Properties.Resources.EBCDIC);
                    break;
                case DefaultCharacterTableType.EbcdicNoSpecialChar:
                    tbl.Load(Properties.Resources.EBCDICNoSpecialChar);
                    break;
            }

            tbl.AllowEdit = true;
            return tbl;
        }

        #endregion

        #region IDisposable Support

        private bool _disposedValue; // Pour détecter les appels redondants

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dteList = null;
                }

                _disposedValue = true;
            }
        }

        // Ce code est ajouté pour implémenter correctement le modèle supprimable.
        public void Dispose()
        {
            // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
            Dispose(true);
        }

        #endregion
    }
}