using System;
using System.Collections.Generic;
using System.Linq;

namespace ThreeDBuilder.Core.Excel
{
    /// <summary>
    /// Dictionnaire aimants : code physique machine (colonne B) → référence TC (colonne A).
    /// Chargé depuis l'Excel à 2 colonnes (cf. memory 3dbuilder-dictionnaire-aimants). Matching STRICT
    /// (sensible à la casse, D9). L'unicité du code est une contrainte métier (D1) : tout doublon est
    /// remonté dans <see cref="DuplicateCodes"/> → erreur bloquante au preflight.
    /// </summary>
    public sealed class MagnetDictionary
    {
        private readonly Dictionary<string, string> _codeToTcRef;

        public IReadOnlyDictionary<string, string> CodeToTcRef => _codeToTcRef;

        /// <summary>Codes apparaissant plus d'une fois (violation d'unicité D1).</summary>
        public IReadOnlyList<string> DuplicateCodes { get; }

        /// <summary>Nombre de lignes non vides lues (avant déduplication).</summary>
        public int RowCount { get; }

        public MagnetDictionary(IEnumerable<KeyValuePair<string, string>> rows)
        {
            // Matching strict → comparateur ordinal (sensible à la casse).
            _codeToTcRef = new Dictionary<string, string>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var dups = new List<string>();
            int count = 0;

            foreach (var row in rows)
            {
                var tcRef = (row.Key ?? "").Trim();
                var code = (row.Value ?? "").Trim();
                if (tcRef.Length == 0 && code.Length == 0) continue; // ligne vide
                count++;
                if (code.Length == 0) continue;

                if (!seen.Add(code))
                {
                    if (!dups.Contains(code)) dups.Add(code);
                    continue; // on garde la première occurrence
                }
                _codeToTcRef[code] = tcRef;
            }

            RowCount = count;
            DuplicateCodes = dups;
        }

        public bool TryGetTcRef(string code, out string tcRef) => _codeToTcRef.TryGetValue(code ?? "", out tcRef);

        public bool Contains(string code) => _codeToTcRef.ContainsKey(code ?? "");

        public IEnumerable<string> Codes => _codeToTcRef.Keys;
    }
}
