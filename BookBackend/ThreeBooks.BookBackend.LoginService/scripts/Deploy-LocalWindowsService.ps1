[CmdletBinding()]
param(
    [ValidateSet("Install", "Deploy", "Uninstall", "Start", "Stop", "Restart")]
    [string]$Action = "Install",

    [string]$ServiceName = "ThreeBooks.BookBackend.LoginService",

    [string]$DisplayName = "ThreeBooks BookBackend Login Service",

    [string]$PublishDirectory,

    [string]$ExecutableName = "ThreeBooks.BookBackend.LoginService.exe",

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$FirewallRuleName = "$ServiceName.HttpInbound.LocalSubnet"
$FirewallDisplayName = "$DisplayName HTTP (Local Subnet)"

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

function Remove-ServiceFirewallRule {
    param([string]$RuleName)

    $existingRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($null -eq $existingRule) {
        return
    }

    Remove-NetFirewallRule -Name $RuleName
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

function Get-LocalIPv4Addresses {
    return @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object {
            $_.IPAddress -notlike '127.*' -and
            $_.IPAddress -notlike '169.254*' -and
            $_.PrefixOrigin -ne 'WellKnown'
        } |
        Select-Object -ExpandProperty IPAddress -Unique)
}

function Get-ServicePorts {
    param([string]$ServiceConfigPath)

    if (-not (Test-Path -LiteralPath $ServiceConfigPath)) {
        return @()
    }

    $serviceSettings = Get-Content -LiteralPath $ServiceConfigPath -Raw | ConvertFrom-Json
    if ($null -eq $serviceSettings -or [string]::IsNullOrWhiteSpace($serviceSettings.Urls)) {
        return @()
    }

    $ports = foreach ($urlText in $serviceSettings.Urls.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $trimmedUrl = $urlText.Trim()
        $uri = $null
        if ([Uri]::TryCreate($trimmedUrl, [UriKind]::Absolute, [ref]$uri)) {
            if ($uri.IsDefaultPort) {
                if ($uri.Scheme -eq 'https') {
                    443
                }
                else {
                    80
                }
            }
            else {
                $uri.Port
            }

            continue
        }

        if ($trimmedUrl -match ':(\d+)(?:/|$)') {
            [int]$Matches[1]
        }
    }

    return @($ports | Sort-Object -Unique)
}

function Ensure-ServiceFirewallRule {
    param(
        [string]$RuleName,
        [string]$RuleDisplayName,
        [int[]]$Ports
    )

    if ($Ports.Count -eq 0) {
        Write-Host "No HTTP ports were found in appsettings.Service.json. Skipping firewall configuration."
        return
    }

    $existingRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($null -ne $existingRule) {
        Remove-NetFirewallRule -Name $RuleName
    }

    $firewallRuleArguments = @{
        Name = $RuleName
        DisplayName = $RuleDisplayName
        Direction = 'Inbound'
        Action = 'Allow'
        Enabled = 'True'
        Profile = 'Any'
        Protocol = 'TCP'
        LocalPort = ($Ports -join ',')
        RemoteAddress = 'LocalSubnet'
    }

    New-NetFirewallRule @firewallRuleArguments | Out-Null

    Write-Host "Firewall rule '$RuleDisplayName' allows LocalSubnet to TCP ports: $($Ports -join ', ')."
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

    $swaggerUrls = foreach ($urlText in $serviceSettings.Urls.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $trimmedUrl = $urlText.Trim().TrimEnd('/')
        $uri = $null
        if (-not [Uri]::TryCreate($trimmedUrl, [UriKind]::Absolute, [ref]$uri)) {
            "{0}/swagger" -f $trimmedUrl
            continue
        }

        $urlHost = $uri.Host
        if ($urlHost -in @('0.0.0.0', '+', '*', '[::]', '::')) {
            foreach ($address in Get-LocalIPv4Addresses) {
                "{0}://{1}:{2}/swagger" -f $uri.Scheme, $address, $uri.Port
            }

            continue
        }

        "{0}/swagger" -f $trimmedUrl
    }

    return @($swaggerUrls | Sort-Object -Unique)
}

function Write-ServiceReadySummary {
    param(
        [string]$Name,
        [string]$Directory
    )

    $swaggerUrls = @(Get-SwaggerUrls -ServiceConfigPath (Join-Path -Path $Directory -ChildPath "appsettings.Service.json"))

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

function Get-NormalizedServiceBinaryPath {
    param([string]$PathName)

    if ([string]::IsNullOrWhiteSpace($PathName)) {
        return ""
    }

    return $PathName.Trim().Trim('"')
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

    $serviceConfigPath = Join-Path -Path $Directory -ChildPath "appsettings.Service.json"
    $servicePorts = Get-ServicePorts -ServiceConfigPath $serviceConfigPath

    $existingService = Get-ServiceConfiguration -Name $Name
    if ($null -ne $existingService) {
        if (-not $ForceReinstall) {
            throw "Service '$Name' already exists. Re-run with -Force to recreate it."
        }

        Remove-ServiceIfExists -Name $Name
    }

    New-Service -Name $Name -BinaryPathName "`"$executablePath`"" -DisplayName $FriendlyName -StartupType Automatic | Out-Null
    Invoke-Sc -Arguments @("description", $Name, "Local deployment for ThreeBooks BookBackend Login Service.")
    Ensure-ServiceFirewallRule -RuleName $FirewallRuleName -RuleDisplayName $FirewallDisplayName -Ports $servicePorts

    Start-ServiceIfInstalled -Name $Name

    Write-ServiceReadySummary -Name $Name -Directory $Directory
}

function Deploy-ServiceRegistration {
    param(
        [string]$Name,
        [string]$FriendlyName,
        [string]$Directory,
        [string]$ExeName,
        [bool]$ForceReinstall
    )

    $expectedExecutablePath = Join-Path -Path $Directory -ChildPath $ExeName
    $existingService = Get-ServiceConfiguration -Name $Name
    $serviceConfigPath = Join-Path -Path $Directory -ChildPath "appsettings.Service.json"
    $servicePorts = Get-ServicePorts -ServiceConfigPath $serviceConfigPath

    if ($null -eq $existingService) {
        Install-ServiceRegistration -Name $Name -FriendlyName $FriendlyName -Directory $Directory -ExeName $ExeName -ForceReinstall:$false
        return
    }

    $configuredExecutablePath = Get-NormalizedServiceBinaryPath -PathName $existingService.PathName
    if ($ForceReinstall -or $configuredExecutablePath -ne $expectedExecutablePath) {
        Remove-ServiceIfExists -Name $Name
        Install-ServiceRegistration -Name $Name -FriendlyName $FriendlyName -Directory $Directory -ExeName $ExeName -ForceReinstall:$false
        return
    }

    Ensure-ServiceFirewallRule -RuleName $FirewallRuleName -RuleDisplayName $FirewallDisplayName -Ports $servicePorts
    Start-ServiceIfInstalled -Name $Name
    Write-ServiceReadySummary -Name $Name -Directory $Directory
}

switch ($Action) {
    "Install" {
        Assert-Administrator
        $resolvedPublishDirectory = Resolve-PublishDirectory
        Install-ServiceRegistration -Name $ServiceName -FriendlyName $DisplayName -Directory $resolvedPublishDirectory -ExeName $ExecutableName -ForceReinstall $Force.IsPresent
        break
    }
    "Deploy" {
        Assert-Administrator
        $resolvedPublishDirectory = Resolve-PublishDirectory
        Deploy-ServiceRegistration -Name $ServiceName -FriendlyName $DisplayName -Directory $resolvedPublishDirectory -ExeName $ExecutableName -ForceReinstall $Force.IsPresent
        break
    }
    "Uninstall" {
        Assert-Administrator
        Remove-ServiceIfExists -Name $ServiceName
        Remove-ServiceFirewallRule -RuleName $FirewallRuleName
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