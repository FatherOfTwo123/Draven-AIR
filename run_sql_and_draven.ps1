param(
    [string]$GameClientRoot = ''
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionPath = Join-Path $RepoRoot 'Draven.sln'
$DravenRoot = Join-Path $RepoRoot 'Draven'
$DatabaseRoot = Join-Path $RepoRoot 'Database'
$ToolRoot = Join-Path $env:USERPROFILE 'tools'
$MySqlRoot = Join-Path $ToolRoot 'mysql-8.0.43-winx64'
$MySqlData = Join-Path $ToolRoot 'mysql-data'
$MySqlZip = Join-Path $ToolRoot 'mysql-8.0.43-winx64.zip'
$MySqlZipUrl = 'https://cdn.mysql.com//archives/mysql-8.0/mysql-8.0.43-winx64.zip'
$MySqlBin = Join-Path $MySqlRoot 'bin'
$MySqlExe = Join-Path $MySqlBin 'mysql.exe'
$MySqlAdminExe = Join-Path $MySqlBin 'mysqladmin.exe'
$MySqlServerExe = Join-Path $MySqlBin 'mysqld.exe'
$NuGetExe = Join-Path $ToolRoot 'nuget.exe'
$FrameworkPath = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319'
$DravenOutDir = 'bin\Release2\'
$DravenExe = Join-Path $DravenRoot 'bin\Release2\Draven.exe'
$TranscriptPath = Join-Path $RepoRoot 'run_sql_and_draven.log'

function Write-Step([string]$Message)
{
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Add-UserPath([string]$PathToAdd)
{
    $currentUserPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $parts = @()

    if (-not [string]::IsNullOrWhiteSpace($currentUserPath))
    {
        $parts = $currentUserPath.Split(';') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    }

    if ($parts -notcontains $PathToAdd)
    {
        $newUserPath = ($parts + $PathToAdd) -join ';'
        [Environment]::SetEnvironmentVariable('Path', $newUserPath, 'User')
    }

    $sessionParts = $env:Path.Split(';') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    if ($sessionParts -notcontains $PathToAdd)
    {
        $env:Path = ($sessionParts + $PathToAdd) -join ';'
    }
}

function Set-GameClientRoot([string]$Root)
{
    if ([string]::IsNullOrWhiteSpace($Root))
    {
        return
    }

    if (-not (Test-Path $Root))
    {
        throw "Game client root not found: $Root"
    }

    $resolvedRoot = (Resolve-Path $Root).Path
    $env:DRAVEN_GAME_CLIENT_ROOT = $resolvedRoot
    Write-Step "Using game client root: $resolvedRoot"
}

function Ensure-NuGet
{
    if (Test-Path $NuGetExe)
    {
        return
    }

    Write-Step 'Downloading nuget.exe'
    Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $NuGetExe
}

function Ensure-MySqlFiles
{
    if (Test-Path $MySqlExe)
    {
        return
    }

    Write-Step 'Downloading portable MySQL'
    New-Item -ItemType Directory -Path $ToolRoot -Force | Out-Null

    if (-not (Test-Path $MySqlZip))
    {
        Invoke-WebRequest -Uri $MySqlZipUrl -OutFile $MySqlZip
    }

    Write-Step 'Extracting portable MySQL'
    Expand-Archive -Path $MySqlZip -DestinationPath $ToolRoot -Force
}

function Ensure-MySqlInitialized
{
    if (Test-Path (Join-Path $MySqlData 'mysql'))
    {
        return
    }

    Write-Step 'Initializing MySQL data directory'
    New-Item -ItemType Directory -Path $MySqlData -Force | Out-Null
    & $MySqlServerExe --initialize-insecure --basedir="$MySqlRoot" --datadir="$MySqlData"

    if ($LASTEXITCODE -ne 0)
    {
        throw 'MySQL initialization failed.'
    }
}

function Test-MySqlAlive
{
    if (-not (Test-Path $MySqlAdminExe))
    {
        return $false
    }

    $previousPreference = $ErrorActionPreference

    try
    {
        $ErrorActionPreference = 'Continue'
        & $MySqlAdminExe '--protocol=tcp' '--host=127.0.0.1' '--port=3306' '--user=root' ping *> $null
        return ($LASTEXITCODE -eq 0)
    }
    catch
    {
        return $false
    }
    finally
    {
        $ErrorActionPreference = $previousPreference
    }
}

function Start-MySql
{
    if (Test-MySqlAlive)
    {
        Write-Step 'MySQL already running'
        return
    }

    Write-Step 'Starting MySQL'
    $arguments = @(
        "--basedir=$MySqlRoot",
        "--datadir=$MySqlData",
        '--port=3306',
        '--bind-address=127.0.0.1',
        '--default-authentication-plugin=mysql_native_password'
    )

    Start-Process -FilePath $MySqlServerExe -ArgumentList $arguments -WindowStyle Hidden | Out-Null

    for ($i = 0; $i -lt 30; $i++)
    {
        Start-Sleep -Seconds 1
        if (Test-MySqlAlive)
        {
            Write-Step 'MySQL is alive'
            return
        }
    }

    throw 'MySQL did not start within 30 seconds.'
}

function Invoke-MySqlText([string]$SqlText, [string]$Database = '')
{
    $args = @('--host=127.0.0.1', '--port=3306', '--user=root', '--protocol=tcp')

    if (-not [string]::IsNullOrWhiteSpace($Database))
    {
        $args += $Database
    }

    $SqlText | & $MySqlExe @args

    if ($LASTEXITCODE -ne 0)
    {
        throw "MySQL command failed for database '$Database'."
    }
}

function Configure-MySqlAuth
{
    Write-Step 'Configuring MySQL auth for old connector'
    $sql = @"
ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY '';
CREATE USER IF NOT EXISTS 'root'@'127.0.0.1' IDENTIFIED WITH mysql_native_password BY '';
ALTER USER 'root'@'127.0.0.1' IDENTIFIED WITH mysql_native_password BY '';
GRANT ALL PRIVILEGES ON *.* TO 'root'@'127.0.0.1' WITH GRANT OPTION;
FLUSH PRIVILEGES;
"@

    Invoke-MySqlText -SqlText $sql
}

function Import-Database
{
    Write-Step 'Creating lol database'
    Invoke-MySqlText -SqlText "CREATE DATABASE IF NOT EXISTS lol CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

    Write-Step 'Importing draven.sql'
    Invoke-MySqlText -SqlText ([System.IO.File]::ReadAllText((Join-Path $DatabaseRoot 'draven.sql'))) -Database 'lol'

    Write-Step 'Importing champs.sql'
    Invoke-MySqlText -SqlText ([System.IO.File]::ReadAllText((Join-Path $DatabaseRoot 'champs.sql'))) -Database 'lol'
}

function Restore-Packages
{
    Write-Step 'Restoring NuGet packages'
    & $NuGetExe restore $SolutionPath -PackagesDirectory (Join-Path $RepoRoot 'packages') -NonInteractive

    if ($LASTEXITCODE -ne 0)
    {
        throw 'NuGet restore failed.'
    }
}

function Stop-DravenIfPossible
{
    $running = Get-Process Draven -ErrorAction SilentlyContinue

    if (-not $running)
    {
        return $true
    }

    Write-Step 'Stopping old Draven process'
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1

    return (-not (Get-Process Draven -ErrorAction SilentlyContinue))
}

function Build-Draven
{
    if (-not (Test-Path $FrameworkPath))
    {
        throw ".NET reference assemblies missing: $FrameworkPath"
    }

    $canBuild = Stop-DravenIfPossible
    if (-not $canBuild -and (Test-Path $DravenExe))
    {
        Write-Warning 'Could not stop old Draven. Using existing Release2 build.'
        return
    }

    Write-Step 'Building Draven Release2'
    Push-Location $RepoRoot
    try
    {
        & dotnet build $SolutionPath -c Release "/p:FrameworkPathOverride=$FrameworkPath" "/p:OutDir=$DravenOutDir"

        if ($LASTEXITCODE -ne 0)
        {
            throw 'dotnet build failed.'
        }
    }
    finally
    {
        Pop-Location
    }
}

function Launch-Draven
{
    if (-not (Test-Path $DravenExe))
    {
        throw "Draven executable not found: $DravenExe"
    }

    Write-Step 'Launching Draven in this console'
    Write-Host ''
    Push-Location (Split-Path $DravenExe -Parent)
    try
    {
        & $DravenExe
        exit $LASTEXITCODE
    }
    finally
    {
        Pop-Location
    }
}

Start-Transcript -Path $TranscriptPath -Append | Out-Null

try
{
    Write-Step 'Ensuring tools exist'
    Set-GameClientRoot $GameClientRoot
    Ensure-MySqlFiles
    Ensure-NuGet
    Add-UserPath $MySqlBin

    Ensure-MySqlInitialized
    Start-MySql
    Configure-MySqlAuth
    Import-Database
    Restore-Packages
    Build-Draven
    Launch-Draven
}
finally
{
    Stop-Transcript | Out-Null
}
