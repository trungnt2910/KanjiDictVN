using Generator.Converters;
using System.Text;

// Setup
if (OperatingSystem.IsWindows())
{
    Console.OutputEncoding = Encoding.Unicode;
}

// Find input and output directories

var repoDir = Environment.CurrentDirectory;
while (!Directory.Exists(Path.Combine(repoDir, ".git")))
{
    var newRepoDir = Directory.GetParent(repoDir)?.FullName;

    if (newRepoDir == null)
    {
        throw new InvalidOperationException("The current directory is not the project directory (or any of its descendants).");
    }

    repoDir = newRepoDir;
}

var englishInputDir = Path.Combine(repoDir, "inp_en");
var vietnameseOutputDir = Path.Combine(repoDir, "out_vn");
var englishOutputDir = Path.Combine(repoDir, "out_en");

// Debloat

var debloatConverter = new DebloatConverter(englishInputDir);
await debloatConverter.Convert();
debloatConverter.Write(englishOutputDir);

// Han Nom

var hanNomConverter = new HanNomConverter(englishInputDir, Path.Combine(repoDir, "hvcache"));
await hanNomConverter.Convert();
hanNomConverter.Write(vietnameseOutputDir);
