using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace ThreeDBuilder.Core.Excel
{
    /// <summary>
    /// Lit le dictionnaire aimants via ClosedXML (remplace l'Excel COM Interop : plus de process
    /// fantômes, pas besoin d'Excel installé — D3). Format : 2 colonnes SANS en-tête, A = réf TC,
    /// B = code physique machine. Feuille « dico » par défaut, sinon la première feuille.
    /// </summary>
    public sealed class MagnetDictionaryReader
    {
        public const string DefaultSheetName = "dico";

        /// <summary>Charge le dictionnaire depuis un fichier .xlsx.</summary>
        public MagnetDictionary Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Chemin du dictionnaire vide.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("Dictionnaire introuvable : " + path, path);

            // Ouverture en lecture avec partage ReadWrite : permet de lire le dictionnaire MÊME s'il est
            // déjà ouvert dans Excel (sinon l'ouverture exclusive bloque/échoue → add-in figé).
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var wb = new XLWorkbook(fs))
            {
                var ws = wb.Worksheets.FirstOrDefault(s => string.Equals(s.Name, DefaultSheetName, StringComparison.OrdinalIgnoreCase))
                         ?? wb.Worksheets.First();
                return ReadWorksheet(ws);
            }
        }

        /// <summary>Lecture testable d'une feuille (séparée de l'ouverture du classeur).</summary>
        public MagnetDictionary ReadWorksheet(IXLWorksheet ws)
        {
            var rows = new List<KeyValuePair<string, string>>();
            // RangeUsed() borne la lecture aux cellules réellement remplies.
            var used = ws.RangeUsed();
            if (used != null)
            {
                foreach (var row in used.RowsUsed())
                {
                    // Colonnes ABSOLUES A (réf TC) et B (code), indépendamment de l'offset de RangeUsed
                    // (row.Cell(1) serait relatif à la plage → lecture de la mauvaise colonne si décalée).
                    int r = row.RowNumber();
                    var tcRef = ws.Cell(r, 1).GetString();
                    var code = ws.Cell(r, 2).GetString();
                    rows.Add(new KeyValuePair<string, string>(tcRef, code));
                }
            }
            return new MagnetDictionary(rows);
        }
    }
}
