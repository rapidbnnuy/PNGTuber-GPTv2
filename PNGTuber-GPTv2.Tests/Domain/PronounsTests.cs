using Xunit;
using PNGTuber_GPTv2.Domain.Structs;

namespace PNGTuber_GPTv2.Tests.Domain
{
    public class PronounsTests
    {
        [Fact]
        public void MapFromId_HeHim_ReturnsCorrectGrammar()
        {
            var p = Pronouns.MapFromId("hehim");
            Assert.Equal("He/Him", p.Display);
            Assert.Equal("He", p.Subject);
            Assert.Equal("Him", p.Object);
            Assert.Equal("His", p.Possessive);
            Assert.False(p.Plural);
        }

        [Fact]
        public void MapFromId_XeXem_ReturnsCorrectGrammar()
        {
            var p = Pronouns.MapFromId("xexem");
            Assert.Equal("Xe/Xem", p.Display);
            Assert.Equal("Xe", p.Subject);
            Assert.Equal("Xem", p.Object);
            Assert.Equal("Xyr", p.Possessive);
            Assert.False(p.Plural);
        }

        [Fact]
        public void MapFromId_TheyThem_ReturnsPlural()
        {
            var p = Pronouns.MapFromId("theythem");
            Assert.True(p.Plural);
        }

        [Fact]
        public void MapFromId_Unknown_ReturnsTheyThem()
        {
            var p = Pronouns.MapFromId("garbage_id");
            Assert.Equal("They/Them", p.Display);
            Assert.True(p.Plural);
        }
        
        [Fact]
        public void LowercaseProperties_AreCorrect()
        {
            var p = Pronouns.MapFromId("hehim");
            Assert.Equal("he", p.SubjectLower);
            Assert.Equal("him", p.ObjectLower);
        }
    }
}
