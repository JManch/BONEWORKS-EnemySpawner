using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;

[assembly: AssemblyTitle(EnemySpawner.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(EnemySpawner.BuildInfo.Company)]
[assembly: AssemblyProduct(EnemySpawner.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + EnemySpawner.BuildInfo.Author)]
[assembly: AssemblyTrademark(EnemySpawner.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(EnemySpawner.BuildInfo.Version)]
[assembly: AssemblyFileVersion(EnemySpawner.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: MelonModInfo(typeof(EnemySpawner.EnemySpawner), EnemySpawner.BuildInfo.Name, EnemySpawner.BuildInfo.Version, EnemySpawner.BuildInfo.Author, EnemySpawner.BuildInfo.DownloadLink)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonModGame("Stress Level Zero", "BONEWORKS")]