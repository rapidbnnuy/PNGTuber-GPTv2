using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace PNGTuber_GPTv2.Tests.Harness
{
    public class QualityControlTests
    {
        private readonly string _sourceDir;

        public QualityControlTests()
        {
            // Traverse up from Tests/bin/Debug/net9.0/ to Solution Root, then down to PNGTuber-GPTv2
            // Currently execution context is usually bin/Debug/...
            // We want /Users/cbruscato/Developer/PNGTuber-GPTv2/PNGTuber-GPTv2
            
            // Hacky path finding:
            var current = Directory.GetCurrentDirectory();
            // Up 5 levels?
            // Test runner path varies. 
            // Better: Find "PNGTuber-GPTv2.sln" then go to "PNGTuber-GPTv2" dir.
            
            var root = FindSolutionRoot(current);
            if (string.IsNullOrEmpty(root))
            {
               // Fallback for flat structure? 
               root = "/Users/cbruscato/Developer/PNGTuber-GPTv2"; 
            }
            
             _sourceDir = Path.Combine(root, "PNGTuber-GPTv2");
        }

        private string FindSolutionRoot(string start)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Any()) return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        private IEnumerable<string> GetSourceFiles()
        {
            // Exclude obj and bin
            return Directory.GetFiles(_sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") 
                         && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
        }

        [Fact]
        public void Codebase_HasNoComments()
        {
            var errors = new List<string>();
            var files = GetSourceFiles();

            foreach (var file in files)
            {
                var code = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                // Check all trivia (whitespace, comments)
                var comments = root.DescendantTrivia()
                    .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                    .ToList();

                if (comments.Any())
                {
                    foreach (var c in comments)
                    {
                        var lineSpan = c.GetLocation().GetLineSpan();
                        errors.Add($"File: {Path.GetFileName(file)} Line: {lineSpan.StartLinePosition.Line + 1} - Found Comment");
                    }
                }
            }

            Assert.Empty(errors);
        }

        [Fact]
        public void Codebase_NoMethodsOver30Lines()
        {
            var errors = new List<string>();
            var files = GetSourceFiles();

            foreach (var file in files)
            {
                var code = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    var loc = method.GetLocation().GetLineSpan();
                    var start = loc.StartLinePosition.Line;
                    var end = loc.EndLinePosition.Line;
                    var length = end - start + 1; // Inclusive

                    // We can be nicer: Exclude braces?
                    // User manifesto: "No method shall exceed 30 lines of code."
                    // Raw count is safer standard. "If it's longer than 30 lines...".
                    
                    // We allow ignoring XML docs or Attributes? 
                    // GetLocation() usually includes attributes.
                    // Let's use Body SourceSpan if available.
                    
                    int lines = length;
                    if (method.Body != null)
                    {
                        var bodyLoc = method.Body.GetLocation().GetLineSpan();
                         // Count lines of the BODY.
                        lines = bodyLoc.EndLinePosition.Line - bodyLoc.StartLinePosition.Line + 1;
                    }
                    else if (method.ExpressionBody != null)
                    {
                         var exprLoc = method.ExpressionBody.GetLocation().GetLineSpan();
                         lines = exprLoc.EndLinePosition.Line - exprLoc.StartLinePosition.Line + 1;
                    }

                    if (lines > 30)
                    {
                        errors.Add($"File: {Path.GetFileName(file)} Method: {method.Identifier.Text} Length: {lines} lines");
                    }
                }
            }
            
            Assert.Empty(errors);
        }
    }
}
