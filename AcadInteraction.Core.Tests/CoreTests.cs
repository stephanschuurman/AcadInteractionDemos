using System.Runtime.InteropServices;

namespace AcadInteraction.Core.Tests
{
    public class AcadCommandsTests
    {
        [Theory]
        [InlineData("Drawing1.dwg")]
        [InlineData("drawing1.DWG")]
        [InlineData("Drawing1")]
        [InlineData(@"C:\temp\Drawing1.dwg")]
        public void MatchesDocument_MatchesNameWithoutExtensionOrFullPath(string document)
        {
            Assert.True(AcadCommands.MatchesDocument(document, "Drawing1.dwg", @"C:\temp\Drawing1.dwg"));
        }

        [Theory]
        [InlineData("Drawing2.dwg")]
        [InlineData("Drawing")]
        [InlineData("")]
        public void MatchesDocument_RejectsOtherDocuments(string document)
        {
            Assert.False(AcadCommands.MatchesDocument(document, "Drawing1.dwg", ""));
        }

        [Fact]
        public void BuildEvalLispCommand_UsesForwardSlashesAndFlattensNewlines()
        {
            string lisp = AcadCommands.BuildEvalLispCommand("(+ 1\r\n2)", @"C:\temp\r.txt");

            Assert.Contains("(open \"C:/temp/r.txt\" \"w\")", lisp);
            Assert.Contains("(lambda () (+ 1  2))", lisp);
            Assert.DoesNotContain("\r", lisp);
            Assert.EndsWith(")\n", lisp);
            Assert.Single(lisp, '\n');
        }

        [Fact]
        public void BuildEvalLispCommand_HasBalancedParentheses()
        {
            string lisp = AcadCommands.BuildEvalLispCommand("(getvar \"DWGNAME\")", @"C:\temp\r.txt");
            Assert.Equal(lisp.Count(c => c == '('), lisp.Count(c => c == ')'));
        }
    }

    public class AccoreConsoleRunnerTests
    {
        [Fact]
        public void BuildArguments_WithoutDrawing_OnlyPassesScript()
        {
            Assert.Equal("/s \"C:\\a.scr\"", AccoreConsoleRunner.BuildArguments(@"C:\a.scr", ""));
        }

        [Fact]
        public void BuildArguments_WithMissingDrawing_OnlyPassesScript()
        {
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dwg");
            Assert.Equal("/s \"C:\\a.scr\"", AccoreConsoleRunner.BuildArguments(@"C:\a.scr", missing));
        }

        [Fact]
        public void BuildArguments_WithExistingDrawing_PassesInputAndScript()
        {
            var drawing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dwg");
            File.WriteAllText(drawing, "");
            try
            {
                Assert.Equal($"/i \"{drawing}\" /s \"C:\\a.scr\"", AccoreConsoleRunner.BuildArguments(@"C:\a.scr", drawing));
            }
            finally
            {
                File.Delete(drawing);
            }
        }

        [Fact]
        public void CreateTempScriptFile_WritesContent_AndDeleteTempFileRemovesIt()
        {
            var runner = new AccoreConsoleRunner(@"C:\does-not-exist\accoreconsole.exe");
            var file = runner.CreateTempScriptFile("_LINE 0,0 1,1\n");

            Assert.Equal("_LINE 0,0 1,1\n", File.ReadAllText(file));
            runner.DeleteTempFile(file);
            Assert.False(File.Exists(file));
        }

        [Fact]
        public void RunScript_WithMissingAccoreConsole_Throws()
        {
            var runner = new AccoreConsoleRunner(@"C:\does-not-exist\accoreconsole.exe");
            var file = runner.CreateTempScriptFile("");
            try
            {
                Assert.Throws<FileNotFoundException>(() => runner.RunScript(file));
            }
            finally
            {
                runner.DeleteTempFile(file);
            }
        }
    }

    public class AcadPathsTests
    {
        [Fact]
        public void FindAccoreConsole_UsesEnvironmentVariable()
        {
            var previous = Environment.GetEnvironmentVariable(AcadPaths.AccoreConsoleEnvironmentVariable);
            Environment.SetEnvironmentVariable(AcadPaths.AccoreConsoleEnvironmentVariable, @"X:\custom\accoreconsole.exe");
            try
            {
                Assert.Equal(@"X:\custom\accoreconsole.exe", AcadPaths.FindAccoreConsole());
            }
            finally
            {
                Environment.SetEnvironmentVariable(AcadPaths.AccoreConsoleEnvironmentVariable, previous);
            }
        }
    }

    public class AcShellExTests
    {
        [Fact]
        public void OpenWithAcLauncher_MissingFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => AcShellEx.OpenWithAcLauncher(@"C:\does-not-exist\x.dwg"));
        }
    }

    public class ComRetryTests
    {
        [Fact]
        public void Invoke_RetriesWhileBusy()
        {
            int calls = 0;
            var result = ComRetry.Invoke(() =>
            {
                if (++calls < 3)
                    throw new COMException("busy", ComRetry.RPC_E_CALL_REJECTED);
                return 42;
            }, delayMs: 1);

            Assert.Equal(42, result);
            Assert.Equal(3, calls);
        }

        [Fact]
        public void Invoke_DoesNotRetryOtherErrors()
        {
            int calls = 0;
            Assert.Throws<InvalidOperationException>(() => ComRetry.Invoke<int>(() =>
            {
                calls++;
                throw new InvalidOperationException();
            }, delayMs: 1));
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Invoke_GivesUpAfterMaxAttempts()
        {
            int calls = 0;
            Assert.Throws<COMException>(() => ComRetry.Invoke<int>(() =>
            {
                calls++;
                throw new COMException("busy", ComRetry.RPC_E_SERVERCALL_RETRYLATER);
            }, attempts: 3, delayMs: 1));
            Assert.Equal(3, calls);
        }
    }

    public class ProcTests
    {
        [Fact]
        public void GetProcess_ReturnsOnlyAcadProcesses()
        {
            Assert.All(Proc.GetProcess(), p => Assert.Equal("acad", p.Value, ignoreCase: true));
        }
    }

	public class ScriptKeywordsTests
	{
		[Fact]
		public void Expand_ReplacesAllKeywords()
		{
			var result = ScriptKeywords.Expand(
				"<acet:cFolderName>|<acet:cBaseName>|<acet:cExtension>|<acet:cFileName>|<ACET:cFullFileName>",
				@"C:\Work\Plan.dwg");
			Assert.Equal(@"C:\Work|Plan|.dwg|Plan.dwg|C:\Work\Plan.dwg", result);
		}

		[Theory]
		[InlineData("_.QSAVE", false)]
		[InlineData("_.SAVEAS 2018 \"<acet:cBaseName>_new\"", true)]
		public void ContainsKeywords(string script, bool expected)
		{
			Assert.Equal(expected, ScriptKeywords.ContainsKeywords(script));
		}
	}

	public class ScpProjectTests
	{
		[Fact]
		public void Parse_ReadsScriptTimeoutAndDrawings()
		{
			var lines = new[]
			{
				"[General_Start]",
				"ScriptFile=C:\\Scripts\\fix.scr",
				"StartUpScript=C:\\Scripts\\startup.scr",
				"TimeOut=120",
				"[General_End]",
				"[DWGList_Start]",
				"C:\\Work\\A.dwg,1",
				"\"C:\\Work\\B.dxf\"",
				"not a drawing",
				"[DWGList_End]"
			};

			var result = ScpProject.Parse(lines);

			Assert.Equal(@"C:\Scripts\fix.scr", result.ScriptFile);
			Assert.Equal(120, result.TimeoutSeconds);
			Assert.Equal(new[] { @"C:\Work\A.dwg", @"C:\Work\B.dxf" }, result.Drawings);
		}
	}

    public class AutodeskProductsTests
    {
        [Theory]
        [InlineData("AutoCAD 2025 - English", "Autodesk", true)]
        [InlineData("Some tool", "Autodesk, Inc.", true)]
        [InlineData("AutoCAD 2025", null, true)]
        [InlineData("Autodesk Desktop App", null, true)]
        [InlineData("Notepad++", "Notepad++ Team", false)]
        [InlineData("AutoCAD plugin", "Other vendor", false)]
        public void IsAutodesk(string name, string? publisher, bool expected)
        {
            Assert.Equal(expected, AutodeskProducts.IsAutodesk(name, publisher));
        }

        [Fact]
        public void Registry_ReadsWithoutThrowing()
        {
            Assert.All(AutodeskProducts.GetInstalledProducts(), p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));
            Assert.All(AutodeskProducts.GetAcadReleases(), r => Assert.NotNull(r.AcadLocation));
        }
    }
}
