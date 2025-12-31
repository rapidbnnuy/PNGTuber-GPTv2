using System;
using System.Collections.Generic;

namespace PNGTuber_GPTv2.Domain.Structs
{
    public readonly struct Pronouns
    {
        public string Display { get; }      
        public string Subject { get; }      
        public string Object { get; }       
        public string Possessive { get; }    
        public string PossessivePronoun { get; } 
        public string Reflexive { get; }     
        public string PastTense { get; }     
        public string CurrentTense { get; }  
        public bool Plural { get; }

        public string SubjectLower => Subject.ToLowerInvariant();
        public string ObjectLower => Object.ToLowerInvariant();
        public string PossessiveLower => Possessive.ToLowerInvariant();
        public string PossessivePronounLower => PossessivePronoun.ToLowerInvariant();
        public string ReflexiveLower => Reflexive.ToLowerInvariant();
        public string PastTenseLower => PastTense.ToLowerInvariant();
        public string CurrentTenseLower => CurrentTense.ToLowerInvariant();
        
        public string DisplayWithParens => $"({Display})";

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

        public static readonly Pronouns HeHim = new Pronouns("He/Him", "He", "Him", "His", "His", "Himself", "Was", "Is", false);
        public static readonly Pronouns SheHer = new Pronouns("She/Her", "She", "Her", "Her", "Hers", "Herself", "Was", "Is", false);
        public static readonly Pronouns TheyThem = new Pronouns("They/Them", "They", "Them", "Their", "Theirs", "Themself", "Were", "Are", true);
        public static readonly Pronouns ItIts = new Pronouns("It/Its", "It", "Its", "Its", "Its", "Itself", "Was", "Is", false);

        public static readonly Pronouns HeThey = new Pronouns("He/They", "He", "Him", "His", "His", "Himself", "Was", "Is", false);
        public static readonly Pronouns SheThey = new Pronouns("She/They", "She", "Her", "Her", "Hers", "Herself", "Was", "Is", false);
        public static readonly Pronouns SheHe = new Pronouns("She/He", "She", "Her", "Her", "Hers", "Herself", "Was", "Is", false);
        public static readonly Pronouns HeShe = new Pronouns("He/She", "He", "Him", "His", "His", "Himself", "Was", "Is", false);

        public static readonly Pronouns XeXem = new Pronouns("Xe/Xem", "Xe", "Xem", "Xyr", "Xyrs", "Xemself", "Was", "Is", false);
        public static readonly Pronouns FaeFaer = new Pronouns("Fae/Faer", "Fae", "Faer", "Faer", "Faers", "Faerself", "Was", "Is", false);
        public static readonly Pronouns VeVer = new Pronouns("Ve/Ver", "Ve", "Ver", "Vis", "Vis", "Verself", "Was", "Is", false);
        public static readonly Pronouns AeAer = new Pronouns("Ae/Aer", "Ae", "Aer", "Aer", "Aers", "Aerself", "Was", "Is", false);
        public static readonly Pronouns ZieHir = new Pronouns("Zie/Hir", "Zie", "Hir", "Hir", "Hirs", "Hirself", "Was", "Is", false);
        public static readonly Pronouns PerPer = new Pronouns("Per/Per", "Per", "Per", "Pers", "Pers", "Perself", "Was", "Is", false);
        public static readonly Pronouns EEm = new Pronouns("E/Em", "E", "Em", "Eir", "Eirs", "Emself", "Was", "Is", false);

        public static readonly Pronouns Default = TheyThem;
        
        public static Pronouns MapFromId(string alejoId)
        {
            switch (alejoId.ToLowerInvariant())
            {
                case "hehim": return HeHim;
                case "sheher": return SheHer;
                case "theythem": return TheyThem;
                case "itits": return ItIts;
                
                case "hethem": return HeThey;
                case "shethem": return SheThey;
                case "heshe": return HeShe;
                case "shehe": return SheHe; 
                
                case "xexem": return XeXem;
                case "faefaer": return FaeFaer;
                case "vever": return VeVer;
                case "aeaer": return AeAer;
                case "ziehir": return ZieHir;
                case "perper": return PerPer;
                case "eem": return EEm;
                
                default: 
                    return Default;
            }
        }
    }
}
