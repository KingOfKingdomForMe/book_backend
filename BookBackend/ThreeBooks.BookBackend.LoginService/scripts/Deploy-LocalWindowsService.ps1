[CmdletBinding()]
param(
    [ValidateSet("Install", "Uninstall", "Start", "Stop", "Restart")]
    [string]$Action = "Install",

    [string]$ServiceName = "ThreeBooks.BookBackend.LoginService",

    [string]$DisplayName = "ThreeBooks BookBackend Login Service",

    [string]$PublishDirectory,

    [string]$ExecutableName = "ThreeBooks.BookBackend.LoginService.exe",

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($currentIdentity)

    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Administrator privileges are required to manage the Windows service. Start PowerShell as Administrator and try again."
    }
}

function Get-ServiceConfiguration {
    param([string]$Name)

    return Get-CimInstance -ClassName Win32_Service -Filter "Name='$Name'" -ErrorAction SilentlyContinue
}

function Invoke-Sc {
    param([string[]]$Arguments)

    & sc.exe @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe failed with exit code $LASTEXITCODE. Arguments: $($Arguments -join ' ')"
    }
}

function Stop-ServiceIfRunning {
    param([string]$Name)

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        return
    }

    if ($service.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Stopped) {
        return
    }

    Stop-Service -Name $Name -Force
    $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Stopped, [TimeSpan]::FromSeconds(30))
}

function Start-ServiceIfInstalled {
    param([string]$Name)

    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        throw "Service '$Name' was not found. Install it first."
    }

    if ($service.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Running) {
        Start-Service -Name $Name
        $service.WaitForStatus([System.ServiceProcess.ServiceControllerStatus]::Running, [TimeSpan]::FromSeconds(30))
    }
}

function Remove-ServiceIfExists {
    param([string]$Name)

    $serviceConfiguration = Get-ServiceConfiguration -Name $Name
    if ($null -eq $serviceConfiguration) {
        return
    }

    Stop-ServiceIfRunning -Name $Name
    Invoke-Sc -Arguments @("delete", $Name)
}

function Resolve-PublishDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PublishDirectory)) {
        return (Resolve-Path -LiteralPath $PublishDirectory).Path
    }

    $defaultDirectory = Join-Path -Path $PSScriptRoot -ChildPath ".."
    $resolvedDefaultDirectory = (Resolve-Path -LiteralPath $defaultDirectory).Path
    $defaultExecutablePath = Join-Path -Path $resolvedDefaultDirectory -ChildPath $ExecutableName

    if (Test-Path -LiteralPath $defaultExecutablePath) {
        return $resolvedDefaultDirectory
    }

    throw "PublishDirectory is required when the script is executed from the source scripts folder."
}

function Get-SwaggerUrls {
    param([string]$ServiceConfigPath)

    if (-not (Test-Path -LiteralPath $ServiceConfigPath)) {
        return @()
    }

    $serviceSettings = Get-Content -LiteralPath $ServiceConfigPath -Raw | ConvertFrom-Json
    if ($null -eq $serviceSettings -or [string]::IsNullOrWhiteSpace($serviceSettings.Urls)) {
        return @()
    }

    return $serviceSettings.Urls.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries) |
        ForEach-Object { "{0}/swagger" -f $_.Trim().TrimEnd('/') }
}

function Install-ServiceRegistration {
    param(
        [string]$Name,
        [string]$FriendlyName,
        [string]$Directory,
        [string]$ExeName,
        [bool]$ForceReinstall
    )

    $executablePath = Join-Path -Path $Directory -ChildPath $ExeName
    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "Published executable was not found: $executablePath"
    }

    $runtimeConfigPath = Join-Path -Path $Directory -ChildPath "ThreeBooks.BookBackend.LoginService.runtimeconfig.json"
    if (-not (Test-Path -LiteralPath $runtimeConfigPath)) {
        throw "Published runtimeconfig.json was not found: $runtimeConfigPath"
    }

    $existingService = Get-ServiceConfiguration -Name $Name
    if ($null -ne $existingService) {
        if (-not $ForceReinstall) {
            throw "Service '$Name' already exists. Re-run with -Force to recreate it."
        }

        Remove-ServiceIfExists -Name $Name
    }

    New-Service -Name $Name -BinaryPathName "`"$executablePath`"" -DisplayName $FriendlyName -StartupType Automatic | Out-Null
    Invoke-Sc -Arguments @("description", $Name, "Local deployment for ThreeBooks BookBackend Login Service.")

    Start-ServiceIfInstalled -Name $Name

    $swaggerUrls = Get-SwaggerUrls -ServiceConfigPath (Join-Path -Path $Directory -ChildPath "appsettings.Service.json")

    Write-Host "Service '$Name' is running."
    Write-Host "Published path: $Directory"
    if ($swaggerUrls.Count -gt 0) {
        Write-Host "Swagger URLs:"
        foreach ($swaggerUrl in $swaggerUrls) {
            Write-Host "  $swaggerUrl"
        }
    }
    else {
        Write-Host "Swagger URL depends on the Urls setting in appsettings.Service.json."
    }
}

switch ($Action) {
    "Install" {
        Assert-Administrator
        $resolvedPublishDirectory = Resolve-PublishDirectory
        Install-ServiceRegistration -Name $ServiceName -FriendlyName $DisplayName -Directory $resolvedPublishDirectory -ExeName $ExecutableName -ForceReinstall $Force.IsPresent
        break
    }
    "Uninstall" {
        Assert-Administrator
        Remove-ServiceIfExists -Name $ServiceName
        break
    }
    "Start" {
        Assert-Administrator
        Start-ServiceIfInstalled -Name $ServiceName
        break
    }
    "Stop" {
        Assert-Administrator
        Stop-ServiceIfRunning -Name $ServiceName
        break
    }
    "Restart" {
        Assert-Administrator
        Stop-ServiceIfRunning -Name $ServiceName
        Start-ServiceIfInstalled -Name $ServiceName
        break
    }
}