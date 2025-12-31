using System;

namespace PNGTuber_GPTv2.Domain.Structs
{
    public readonly struct Pronouns
    {
        public string Display { get; }      // He/Him
        public string Subject { get; }      // He
        public string Object { get; }       // Him
        public string Possessive { get; }    // His (Adjective)
        public string PossessivePronoun { get; } // His (Nominal)
        public string Reflexive { get; }     // Himself
        public string PastTense { get; }     // Was
        public string CurrentTense { get; }  // Is
        public bool Plural { get; }

        public Pronouns(string display, string subject, string obj, string possessive, string possessivePronoun, string reflexive, string pastTense, string currentTense, bool plural)
        {
            Display = display;
            Subject = subject;
            Object = obj;
            Possessive = possessive;
            PossessivePronoun = possessivePronoun;
            Reflexive = reflexive;
            PastTense = pastTense;
            CurrentTense = currentTense;
            Plural = plural;
        }

        public static readonly Pronouns TheyThem = new Pronouns("They/Them", "They", "Them", "Their", "Theirs", "Themself", "Were", "Are", true);
        public static readonly Pronouns HeHim = new Pronouns("He/Him", "He", "Him", "His", "His", "Himself", "Was", "Is", false);
        public static readonly Pronouns SheHer = new Pronouns("She/Her", "She", "Her", "Her", "Hers", "Herself", "Was", "Is", false);
        
        // For backwards compatibility/default
        public static readonly Pronouns Default = TheyThem;
    }
}
