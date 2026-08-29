# Throwaway compile check: builds all gameplay scripts with dotnet against
# Unity's reference assemblies. Run:  powershell -File dev-compilecheck.ps1
$ErrorActionPreference = "Stop"

function Make($src, $out, $name) {
    $s = Get-Content -Raw -Encoding UTF8 $src
    $glob = if ($name -eq "zzcheck") {
        "  <ItemGroup>`n    <Compile Include=`"Assets\**\*.cs`" Exclude=`"Assets\Editor\**\*.cs`" />`n  </ItemGroup>"
    } else {
        "  <ItemGroup>`n    <Compile Include=`"Assets\Editor\**\*.cs`" />`n  </ItemGroup>"
    }
    $s = [regex]::Replace($s, '  <ItemGroup>\s*(?:<Compile Include[^>]*/>\s*)+</ItemGroup>', [Text.RegularExpressions.MatchEvaluator]{ param($m) $glob }, 1)
    $s = $s.Replace("<AssemblyName>$($name -replace 'zz','')","<AssemblyName>$name")
    $s = $s.Replace('Temp\bin\Debug\',"Temp\$name\bin\").Replace('Temp\obj\$(MSBuildProjectName)',"Temp\$name\obj\")
    $s = $s.Replace('<ProjectReference Include="Assembly-CSharp.csproj" />','<ProjectReference Include="zzcheck.csproj" />')
    # Unity hasn't regenerated the csproj yet: point the SqlClient reference at the new plugin DLL.
    $s = [regex]::Replace($s, '<Reference Include="System.Data.SqlClient">.*?</Reference>', '', 'Singleline')
    $mds = 'Assets\Plugins\SqlClient\Microsoft.Data.SqlClient.dll'
    if ((Test-Path $mds) -and ($s -notmatch 'Microsoft\.Data\.SqlClient')) {
        $ref = "  <ItemGroup>`n    <Reference Include=`"Microsoft.Data.SqlClient`"><HintPath>$mds</HintPath></Reference>`n  </ItemGroup>"
        $s = $s -replace '(?s)(</Project>)', "$ref`n`$1"
    }
    Set-Content -Encoding UTF8 $out $s
}

Make "Assembly-CSharp.csproj" "zzcheck.csproj" "zzcheck"
Make "Assembly-CSharp-Editor.csproj" "zzcheckeditor.csproj" "zzcheckeditor"

Write-Host "=== runtime ===" -ForegroundColor Cyan
dotnet build zzcheck.csproj -nologo -clp:ErrorsOnly
Write-Host "=== editor ===" -ForegroundColor Cyan
dotnet build zzcheckeditor.csproj -nologo -clp:ErrorsOnly

Remove-Item zzcheck.csproj, zzcheckeditor.csproj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force Temp\zzcheck, Temp\zzcheckeditor -ErrorAction SilentlyContinue
