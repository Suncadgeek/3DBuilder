using System.Collections.Generic;
using System.Linq;

namespace ThreeDBuilder.Core.Validation
{
    public enum Severity { Warning, Error }

    public sealed class ValidationMessage
    {
        public Severity Severity { get; }
        public string Text { get; }

        public ValidationMessage(Severity severity, string text)
        {
            Severity = severity;
            Text = text;
        }

        public override string ToString() => (Severity == Severity.Error ? "[ERREUR] " : "[AVERT.] ") + Text;
    }

    /// <summary>Accumule erreurs et avertissements de validation amont, avant tout appel NX.</summary>
    public sealed class ValidationResult
    {
        private readonly List<ValidationMessage> _messages = new List<ValidationMessage>();

        public IReadOnlyList<ValidationMessage> Messages => _messages;

        public bool HasErrors => _messages.Any(m => m.Severity == Severity.Error);
        public bool IsValid => !HasErrors;

        public void Error(string text) => _messages.Add(new ValidationMessage(Severity.Error, text));
        public void Warning(string text) => _messages.Add(new ValidationMessage(Severity.Warning, text));

        public void Merge(ValidationResult other)
        {
            if (other != null) _messages.AddRange(other._messages);
        }

        public IEnumerable<string> Errors => _messages.Where(m => m.Severity == Severity.Error).Select(m => m.Text);
        public IEnumerable<string> Warnings => _messages.Where(m => m.Severity == Severity.Warning).Select(m => m.Text);

        public override string ToString() => string.Join("\n", _messages.Select(m => m.ToString()));
    }
}
