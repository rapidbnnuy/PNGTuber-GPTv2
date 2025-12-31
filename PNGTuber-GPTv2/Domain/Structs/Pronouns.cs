using System;

namespace PNGTuber_GPTv2.Domain.Structs
{
    public readonly struct Pronouns
    {
        public string Subject { get; }      // He
        public string Object { get; }       // Him
        public string Possessive { get; }    // His (Adjective)
        public string PossessivePronoun { get; } // His (Nominal)
        public string Reflexive { get; }     // Himself
        public string PastTense { get; }     // Was
        public string CurrentTense { get; }  // Is
        public bool Plural { get; }

        public Pronouns(string subject, string obj, string possessive, string possessivePronoun, string reflexive, string pastTense, string currentTense, bool plural)
        {
            Subject = subject;
            Object = obj;
            Possessive = possessive;
            PossessivePronoun = possessivePronoun;
            Reflexive = reflexive;
            PastTense = pastTense;
            CurrentTense = currentTense;
            Plural = plural;
        }

        public static readonly Pronouns Default = new Pronouns("They", "Them", "Their", "Theirs", "Themself", "Were", "Are", true);
    }
}
